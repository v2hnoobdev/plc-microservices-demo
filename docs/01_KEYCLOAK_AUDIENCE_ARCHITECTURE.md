# Multi-Audience Strategy

## Tổng quan

Kiến trúc này sử dụng **multiple audiences** để phân tách rõ ràng giữa:

- **Public API access** - Frontend/External clients
- **Service-to-service access** - Internal microservices communication

## Kiến trúc

```
┌──────────────────────────────────────────────────────────────────┐
│                         KEYCLOAK                                 │
│                                                                  │
│  Client Scopes:                                                  │
│  ┌─────────────┐  ┌──────────────────┐  ┌──────────────────┐   │
│  │ public-api  │  │ plc-user-service │  │ plc-order-service│   │
│  │ (audience)  │  │   (audience)     │  │   (audience)     │   │
│  └─────────────┘  └──────────────────┘  └──────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
         │                     │                      │
         │                     │                      │
    ┌────┴────┐           ┌────┴─────┐          ┌────┴─────┐
    │         │           │          │          │          │
    ▼         ▼           ▼          ▼          ▼          ▼

┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│   Frontend      │   │   Gateway       │   │  User Service   │
│  plc-frontend   │   │  plc-gateway    │   │   (Internal)    │
│                 │   │                 │   │                 │
│ Token audience: │   │ Token audience: │   │ Token audience: │
│  - public-api   │   │  - public-api   │   │  - plc-user-    │
│                 │   │  - plc-user-*   │   │    service      │
│                 │   │  - plc-order-*  │   │                 │
└─────────────────┘   └─────────────────┘   └─────────────────┘
         │                     │
         │    HTTP Request     │
         └────────────────────►│
              + Bearer token   │
              aud: public-api  │
                               │ Forward request
                               │ + Same token
                               ▼
                        ┌─────────────────┐
                        │  User Service   │
                        │                 │
                        │ Validates:      │
                        │ ✓ public-api OR │
                        │ ✓ plc-user-*    │
                        └─────────────────┘
```

## Use Cases

### 1. Frontend → Backend Services (qua Gateway)

- **Token audience**: `public-api`
- **Flow**: Frontend → Gateway → User Service
- **Validation**: User Service accepts `aud: "public-api"`

### 2. Service-to-Service (Internal)

- **Token audience**: `plc-user-service`, `plc-order-service`, etc.
- **Flow**: Order Service → User Service (direct)
- **Validation**: User Service accepts `aud: "plc-user-service"`

### 3. Gateway → Backend Services (Internal)

- **Token audience**: `public-api` + specific services
- **Flow**: Gateway uses Client Credentials with specific audience
- **Validation**: Backend accepts service-specific audience

## Keycloak Configuration

### Bước 1: Tạo Client Scopes

#### 1.1. Public API Scope

1. Vào **Client Scopes** → **Create client scope**
2. **Settings**:
   - Name: `public-api`
   - Type: `Optional`
   - Protocol: `OpenID Connect`
   - Display on consent screen: `OFF`
   - Include in token scope: `ON`
3. Click **Save**

4. Tab **Mappers** → **Configure a new mapper**
5. Chọn **Audience**
6. **Settings**:
   - Name: `public-api-audience`
   - Included Custom Audience: `public-api` (đặt tên giống tên scope cho dễ phân biệt)
   - Add to ID token: `OFF`
   - Add to access token: `ON`
7. Click **Save**

#### 1.2. Service-Specific Scopes

Tạo tương tự cho mỗi service:

**User Service Scope:**

- Scope Name: `plc-user-service`
- Audience Name: `plc-user-service-audience`
- Audience mapper: `plc-user-service`

**Order Service Scope:**

- Scope Name: `plc-order-service`
- Audience Name: `plc-order-service-audience`
- Audience mapper: `plc-order-service`

**Identity Service Scope:**

- Scope Name: `plc-identity-service`
- Audience Name: `plc-identity-service-audience`
- Audience mapper: `plc-identity-service`

### Bước 2: Cấu hình Clients

#### 2.1. Frontend Client (`plc-frontend`)

1. Vào **Clients** → **plc-frontend**
2. Tab **Client scopes**
3. Click **Add client scope**
4. Chọn `public-api`
5. Chọn **Default** (không phải Optional)
6. Click **Add**

**Kết quả**: Frontend token sẽ có `aud: ["public-api", "account"]`

#### 2.2. Gateway Client (`plc-gateway`)

**Tạo mới nếu chưa có:**

1. **Clients** → **Create client**
2. **General Settings**:
   - Client type: `OpenID Connect`
   - Client ID: `plc-gateway`
3. Click **Next**
4. **Capability config**:
   - Client authentication: **ON** (Confidential)
   - Authorization: **OFF**
   - Authentication flow:
     - Service accounts roles: **ON**
     - Direct access grants: **OFF**
     - Standard flow: **OFF**
5. Click **Save**

**Thêm Client Scopes:**

1. Tab **Client scopes**
2. Add các scopes sau (tất cả Default):
   - `public-api`
   - `plc-user-service`
   - `plc-order-service`
   - `plc-identity-service`

**Kết quả**: Gateway token sẽ có `aud: ["public-api", "plc-user-service", "plc-order-service", ...]`

#### 2.3. Service-specific Clients (Optional - cho service-to-service)

**User Service Client (`plc-user-service`):**

1. **Clients** → **Create client**
2. Client ID: `plc-user-service`
3. Client authentication: **ON**
4. Service accounts roles: **ON**
5. **Client scopes** → Add `plc-user-service` (Default)

**Kết quả**: Token chỉ có `aud: ["plc-user-service"]`

### Bước 3: Verify Configuration

Test token từ Frontend:

```bash
# Get token from plc-frontend
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=plc-frontend" \
  -d "grant_type=password" \
  -d "username=testuser" \
  -d "password=Test@123"
```

Decode JWT, kiểm tra:

```json
{
  "aud": ["public-api", "account"],
  "azp": "plc-frontend",
  "preferred_username": "testuser"
}
```

Test token từ Gateway:

```bash
# Get token from plc-gateway (Client Credentials)
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_GATEWAY_SECRET" \
  -d "grant_type=client_credentials"
```

Decode JWT, kiểm tra:

```json
{
  "aud": ["public-api", "plc-user-service", "plc-order-service", "account"],
  "azp": "plc-gateway",
  "clientId": "plc-gateway"
}
```

## Backend Service Configuration

### User Service

**appsettings.json:**

```json
{
  "JwtBearer": {
    "Authority": "http://localhost:8080/realms/plc-microservices-demo",
    "ValidAudiences": ["public-api", "plc-user-service"],
    "RequireHttpsMetadata": false,
    "ValidateAudience": true,
    "ValidateIssuer": true,
    "ValidateLifetime": true
  }
}
```

**Program.cs:**

```csharp
// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtBearer");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtSettings["Authority"];
        options.RequireHttpsMetadata = bool.Parse(jwtSettings["RequireHttpsMetadata"] ?? "false");  // cần set thành true khi dùng HTTPS ở production

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = bool.Parse(jwtSettings["ValidateAudience"] ?? "true"),
            ValidateIssuer = bool.Parse(jwtSettings["ValidateIssuer"] ?? "true"),
            ValidateLifetime = bool.Parse(jwtSettings["ValidateLifetime"] ?? "true"),
            ValidateIssuerSigningKey = true,
            ValidAudiences = jwtSettings.GetSection("ValidAudiences").Get<string[]>(),

            // Map Keycloak claims to ASP.NET Core identity claims
            NameClaimType = "preferred_username",
            RoleClaimType = "realm_access.roles"
        };
    });
```

### Order Service

**appsettings.json:**

```json
{
  "JwtBearer": {
    "Authority": "http://localhost:8080/realms/plc-microservices-demo",
    "ValidAudiences": ["public-api", "plc-order-service"],
    "RequireHttpsMetadata": false
  }
}
```

### Identity Service

**appsettings.json:**

```json
{
  "JwtBearer": {
    "Authority": "http://localhost:8080/realms/plc-microservices-demo",
    "ValidAudiences": ["public-api", "plc-identity-service"],
    "RequireHttpsMetadata": false
  }
}
```

(Các service khác cấu hình Program.cs theo tương tự)

## Frontend Configuration

**.env:**

```env
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=plc-microservices-demo
VITE_KEYCLOAK_CLIENT_ID=plc-frontend

VITE_API_BASE_URL=http://localhost:5000   // gateway
```

Token đã có `aud: "public-api"` nhờ Default client scope.

## Gateway Configuration

Gateway chỉ forward request.

**Nếu Gateway cần gọi backend services (advanced):**

1. Lưu Gateway client secret vào **appsettings.json**
2. Implement token acquisition với Client Credentials
3. Sử dụng token có service-specific audience

## Authorization Policies (Advanced)

Có thể tạo policies khác nhau cho từng audience:

**Program.cs:**

```csharp
builder.Services.AddAuthorization(options =>
{
    // Policy cho Public API
    options.AddPolicy("PublicApiPolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("aud", "public-api");
    });

    // Policy cho Service-to-Service
    options.AddPolicy("ServiceToServicePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("aud", "plc-user-service");
    });

    // Policy cho cả hai
    options.AddPolicy("FlexiblePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var audienceClaim = context.User.FindFirst("aud");
            if (audienceClaim == null) return false;

            var validAudiences = new[] { "public-api", "plc-user-service" };
            return validAudiences.Contains(audienceClaim.Value);
        });
    });
});
```

**Controller usage:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // Chỉ accept public-api
    [HttpGet]
    [Authorize(Policy = "PublicApiPolicy")]
    public async Task<IActionResult> GetAllUsers()
    {
        // ...
    }

    // Chỉ accept service-to-service
    [HttpPost("internal/sync")]
    [Authorize(Policy = "ServiceToServicePolicy")]
    public async Task<IActionResult> SyncUsers()
    {
        // ...
    }

    // Accept cả hai
    [HttpGet("{id}")]
    [Authorize] // Default - accept any valid audience
    public async Task<IActionResult> GetUser(Guid id)
    {
        // ...
    }
}
```

## Testing Scenarios

### Test 1: Frontend → Gateway → User Service

```bash
# 1. Login từ frontend (browser)
# 2. Lấy token từ browser console
# 3. Test API call

curl -X GET "http://localhost:5000/api/users" \
  -H "Authorization: Bearer <FRONTEND_TOKEN>"
```

**Expected**: ✅ 200 OK (token có `aud: "public-api"`)

### Test 2: Gateway Client Credentials → User Service

```bash
# 1. Get gateway token
TOKEN=$(curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_SECRET" \
  -d "grant_type=client_credentials" | jq -r '.access_token')

# 2. Call User Service directly
curl -X GET "http://localhost:5002/api/users" \
  -H "Authorization: Bearer $TOKEN"
```

**Expected**: ✅ 200 OK (token có `aud: ["public-api", "plc-user-service"]`)

### Test 3: Service-specific Token

```bash
# 1. Get user-service-specific token
TOKEN=$(curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=plc-user-service-client" \
  -d "client_secret=YOUR_SECRET" \
  -d "grant_type=client_credentials" | jq -r '.access_token')

# 2. Call User Service
curl -X GET "http://localhost:5002/api/users" \
  -H "Authorization: Bearer $TOKEN"
```

**Expected**: ✅ 200 OK (token có `aud: "plc-user-service"`)

### Test 4: Invalid Audience

```bash
# Get token từ frontend client nhưng KHÔNG có public-api scope
# (Remove public-api từ Default scopes)

curl -X GET "http://localhost:5002/api/users" \
  -H "Authorization: Bearer <TOKEN_WITHOUT_PUBLIC_API>"
```

**Expected**: ❌ 401 Unauthorized

## Tài liệu tham khảo

- [RFC 7519 - JWT Audience Claim](https://tools.ietf.org/html/rfc7519#section-4.1.3)
- [Keycloak - Client Scopes](https://www.keycloak.org/docs/latest/server_admin/#_client_scopes)
- [Microsoft - Multiple JwtBearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/limitingidentitybyscheme)
