using System.Text;
using System.Text.Json;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using PLC.User.API.Data;
using PLC.User.API.Models;
using PLC.User.API.Models.KeycloakEvents;
using Microsoft.EntityFrameworkCore;

namespace PLC.User.API.Services;

/// <summary>
/// Background service để đồng bộ thông rin user từ Keycloak events -> NATS JetStream
/// </summary>
public class KeycloakEventConsumer : BackgroundService
{
    private readonly ILogger<KeycloakEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private INatsConnection? _natsConnection;

    public KeycloakEventConsumer(
        ILogger<KeycloakEventConsumer> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Keycloak Event Consumer starting...");

        try
        {
            // Kết nối tới NATS
            var natsUrl = _configuration.GetValue<string>("NATS:Url") ?? "nats://localhost:4222";
            var opts = NatsOpts.Default with { Url = natsUrl };
            _natsConnection = new NatsConnection(opts);

            await _natsConnection.ConnectAsync();
            _logger.LogInformation("Connected to NATS at {NatsUrl}", natsUrl);

            // Get JetStream context
            var jetStream = new NatsJSContext(_natsConnection);

            // Subscribe to keycloak admin events stream
            var streamName = "keycloak-admin-event-stream";
            var consumerName = "user-service-consumer";
            var filterSubject = "keycloak.event.admin.*.success.user.*";

            _logger.LogInformation("Creating/Getting JetStream consumer: {ConsumerName} for stream: {StreamName}",
                consumerName, streamName);

            var consumer = await jetStream.CreateOrUpdateConsumerAsync(
                streamName,
                new ConsumerConfig
                {
                    Name = consumerName,
                    DurableName = consumerName,
                    FilterSubject = filterSubject,
                    AckPolicy = ConsumerConfigAckPolicy.Explicit,
                    DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                },
                stoppingToken);

            _logger.LogInformation("JetStream consumer created/updated successfully");

            await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: stoppingToken))
            {
                try
                {
                    _logger.LogDebug("Received message on subject: {Subject}", msg.Subject);

                    var adminEvent = JsonSerializer.Deserialize<KeycloakAdminEvent>(msg.Data ?? "");
                    if (adminEvent == null)
                    {
                        _logger.LogWarning("Failed to deserialize admin event");
                        await msg.AckAsync(cancellationToken: stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Processing Keycloak event: {ResourceType} - {OperationType}",
                        adminEvent.ResourceType, adminEvent.OperationType);

                    // Xử lý event
                    await ProcessAdminEvent(adminEvent, stoppingToken);

                    // Ack message
                    await msg.AckAsync(cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    // Vẫn ack để không block queue
                    await msg.AckAsync(cancellationToken: stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Keycloak Event Consumer");
            throw;
        }
    }

    private async Task ProcessAdminEvent(KeycloakAdminEvent adminEvent, CancellationToken cancellationToken)
    {
        // Chỉ xử lý USER events
        if (!adminEvent.ResourceType.Equals("USER", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Parse user representation
        if (string.IsNullOrEmpty(adminEvent.Representation))
        {
            _logger.LogWarning("Admin event has no representation data");
            return;
        }

        var userRepresentation = JsonSerializer.Deserialize<KeycloakUserRepresentation>(adminEvent.Representation);
        if (userRepresentation == null)
        {
            _logger.LogWarning("Failed to deserialize user representation");
            return;
        }

        // Tạo scope để sử dụng DbContext
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        var operation = adminEvent.OperationType.ToUpper();

        switch (operation)
        {
            case "CREATE":
                await HandleUserCreate(dbContext, userRepresentation, cancellationToken);
                break;

            case "UPDATE":
                await HandleUserUpdate(dbContext, userRepresentation, cancellationToken);
                break;

            case "DELETE":
                await HandleUserDelete(dbContext, userRepresentation, cancellationToken);
                break;

            default:
                _logger.LogDebug("Ignoring operation type: {OperationType}", operation);
                break;
        }
    }

    private async Task HandleUserCreate(UserDbContext dbContext, KeycloakUserRepresentation userRep, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user from Keycloak: {Username} (KeycloakId: {KeycloakId})",
            userRep.Username, userRep.Id);

        // Parse KeycloakUserId
        if (!Guid.TryParse(userRep.Id, out var keycloakUserId))
        {
            _logger.LogError("Invalid Keycloak User ID format: {Id}", userRep.Id);
            return;
        }

        var existingUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

        if (existingUser != null)
        {
            _logger.LogWarning("User with KeycloakId {KeycloakId} already exists, skipping create", keycloakUserId);
            return;
        }

        // Extract role từ attributes (nếu có)
        string? role = null;
        if (userRep.Attributes != null && userRep.Attributes.ContainsKey("role"))
        {
            role = userRep.Attributes["role"].FirstOrDefault();
        }

        // Tạo user mới
        var newUser = new Models.User
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Username = userRep.Username,
            Email = userRep.Email ?? "",
            FirstName = userRep.FirstName,
            LastName = userRep.LastName,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created successfully: {UserId} - {Username}", newUser.Id, newUser.Username);
    }

    private async Task HandleUserUpdate(UserDbContext dbContext, KeycloakUserRepresentation userRep, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user from Keycloak: {Username} (KeycloakId: {KeycloakId})", userRep.Username, userRep.Id);

        if (!Guid.TryParse(userRep.Id, out var keycloakUserId))
        {
            _logger.LogError("Invalid Keycloak User ID format: {Id}", userRep.Id);
            return;
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User with KeycloakId {KeycloakId} not found, creating new user", keycloakUserId);
            await HandleUserCreate(dbContext, userRep, cancellationToken);
            return;
        }

        // Extract role từ attributes
        string? role = null;
        if (userRep.Attributes != null && userRep.Attributes.ContainsKey("role"))
        {
            role = userRep.Attributes["role"].FirstOrDefault();
        }

        // Update user fields
        user.Username = userRep.Username;
        user.Email = userRep.Email ?? user.Email;
        user.FirstName = userRep.FirstName;
        user.LastName = userRep.LastName;
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User updated successfully: {UserId} - {Username}", user.Id, user.Username);
    }

    private async Task HandleUserDelete(UserDbContext dbContext, KeycloakUserRepresentation userRep, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user from Keycloak: {Username} (KeycloakId: {KeycloakId})", userRep.Username, userRep.Id);

        if (!Guid.TryParse(userRep.Id, out var keycloakUserId))
        {
            _logger.LogError("Invalid Keycloak User ID format: {Id}", userRep.Id);
            return;
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User with KeycloakId {KeycloakId} not found, nothing to delete", keycloakUserId);
            return;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User deleted successfully: {UserId} - {Username}", user.Id, user.Username);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Keycloak Event Consumer stopping...");

        if (_natsConnection != null)
        {
            await _natsConnection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
