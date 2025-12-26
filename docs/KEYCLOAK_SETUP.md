# Keycloak Setup Guide - Complete Reference

Hướng dẫn đầy đủ về setup và sử dụng Keycloak Identity Server cho microservices architecture.

## Mục lục

1. [Kiến trúc tổng quan](#kiến-trúc-tổng-quan)
2. [Setup Keycloak với Docker](#setup-keycloak-với-docker)
3. [Cấu hình Realm và Clients](#cấu-hình-realm-và-clients)
4. [Shared Audience cho Backend Services](#shared-audience-cho-backend-services)
5. [Hiểu về Client ID & Secret](#hiểu-về-client-id--secret)
6. [Testing và Troubleshooting](#testing-và-troubleshooting)

---

## Kiến trúc tổng quan

### Luồng Authentication & Authorization

```
┌─────────────────────────────────────────────────────────────┐
│                        Keycloak                              │
│                    (Identity Server)                         │
│                                                              │
│  Realm: plc-microservices-demo                              │
│  Client Scopes: backend-api (shared audience)               │
│                                                              │
│  Clients:                                                    │
│  ├─ plc-gateway          (confidential)                     │
│  ├─ plc-user-service     (confidential)                     │
│  └─ plc-order-service    (confidential)                     │
└─────────────────────────────────────────────────────────────┘
                          │
                          │ Issues JWT tokens with
                          │ aud: "backend-api"
                          ▼
      ┌───────────────────────────────────────┐
      │           Frontend/Client             │
      │   - Sends username/password           │
      │   - Gets JWT token                    │
      │   - Stores token                      │
      └───────────────────────────────────────┘
                          │
                          │ Authorization: Bearer <token>
                          ▼
      ┌───────────────────────────────────────┐
      │        API Gateway (YARP)             │
      │   - Validates token (optional)        │
      │   - Routes to backend services        │
      └───────────────────────────────────────┘
                          │
              ┌───────────┴───────────┐
              ▼                       ▼
    ┌─────────────────┐     ┌─────────────────┐
    │  User Service   │     │  Order Service  │
    │  Port: 5002     │     │  Port: 5003     │
    │                 │     │                 │
    │  Validates:     │     │  Validates:     │
    │  - Token sig    │     │  - Token sig    │
    │  - aud=backend  │     │  - aud=backend  │
    │  - iss, exp     │     │  - iss, exp     │
    └─────────────────┘     └─────────────────┘
```

### Nguyên tắc quan trọng

1. **Client ID + Secret**: Dùng để **LẤY token** (authentication)
2. **Authority + Audience**: Dùng để **VALIDATE token** (authorization)
3. **Shared Audience**: Tất cả backend services dùng chung `backend-api`
4. **Public Key**: Services validate token bằng public key từ Keycloak

---

## Setup Keycloak với Docker

### Yêu cầu

- Docker Desktop (Windows/Mac) hoặc Docker Engine (Linux)
- Docker Compose

### Bước 1: Khởi động Keycloak

```bash
cd E:\PLC\DesignMicroservicesDraft\DemoProject\infrastructure\docker
docker-compose up -d
```

### Bước 2: Kiểm tra logs

```bash
# Xem logs Keycloak
docker-compose logs -f keycloak

# Xem logs PostgreSQL
docker-compose logs -f postgres-keycloak

# Kiểm tra trạng thái
docker-compose ps
```

Kết quả mong đợi:
```
NAME                IMAGE                              STATUS
keycloak            quay.io/keycloak/keycloak:26.4     Up (healthy)
postgres-keycloak   postgres:16-alpine                 Up (healthy)
```

### Bước 3: Truy cập Admin Console

1. Mở trình duyệt: http://localhost:8080
2. Click "Administration Console"
3. Đăng nhập:
   - Username: `admin`
   - Password: `admin`

---

## Cấu hình Realm và Clients

### Bước 1: Tạo Realm

1. Click dropdown "master" ở góc trên bên trái
2. Click "Create Realm"
3. Điền thông tin:
   - **Realm name**: `plc-microservices-demo`
   - **Enabled**: ON
4. Click "Create"

### Bước 2: Cấu hình Realm Settings

1. Vào "Realm settings" (menu bên trái)
2. Tab "General":
   - User profile enabled: ON
   - User-managed access: ON
3. Tab "Login":
   - User registration: ON
   - Forgot password: ON
   - Remember me: ON
4. Click "Save"

### Bước 3: Tạo Roles

1. Vào "Realm roles"
2. Tạo các roles sau:

| Role Name | Description |
|-----------|-------------|
| `user` | Standard user role |
| `admin` | Administrator role |
| `manager` | Manager role |

### Bước 4: Tạo Test Users

#### User 1: Admin
1. Vào "Users" → "Add user"
2. Điền:
   - Username: `admin`
   - Email: `admin@plc.com`
   - First name: `Admin`
   - Last name: `User`
   - Email verified: ON
   - Enabled: ON
3. Tab "Credentials" → Set password:
   - Password: `Admin@123`
   - Temporary: OFF
4. Tab "Role mapping" → Assign roles: `admin`, `user`

#### User 2: Test User
Lặp lại với:
- Username: `testuser`
- Email: `testuser@plc.com`
- Password: `Test@123`
- Roles: `user`

---

## Shared Audience cho Backend Services

### Tại sao cần Shared Audience?

**Vấn đề**: Mỗi client mặc định tạo token với `aud` = client_id của chính nó:
- Token từ `plc-gateway` có `aud=plc-gateway`
- Nhưng User Service expect `aud=plc-user-service` → **401 Unauthorized**

**Giải pháp**: Tạo một **shared audience** (`backend-api`) mà tất cả backend services đều chấp nhận.

### Bước 1: Tạo Client Scope cho Shared Audience

1. Vào **Client scopes** (menu bên trái)
2. Click "Create client scope"
3. Điền:
   - **Name**: `backend-api`
   - **Type**: Default
   - **Protocol**: OpenID Connect
   - **Display on consent screen**: OFF
4. Click "Save"

### Bước 2: Thêm Audience Mapper

1. Vẫn trong client scope `backend-api`
2. Tab **Mappers**
3. Click **Add mapper** → **By configuration**
4. Chọn **Audience**
5. Điền:
   - **Name**: `backend-api-audience`
   - **Mapper Type**: Audience
   - **Included Custom Audience**: `backend-api`
   - **Add to ID token**: OFF
   - **Add to access token**: ON ✓ (QUAN TRỌNG!)
6. Click "Save"

### Bước 3: Tạo Clients với Shared Audience

#### 3.1. Tạo Gateway Client

1. Vào **Clients** → "Create client"
2. Tab "General Settings":
   - **Client type**: OpenID Connect
   - **Client ID**: `plc-gateway`
3. Click "Next"
4. Tab "Capability config":
   - **Client authentication**: ON (confidential)
   - **Authorization**: OFF
   - **Authentication flow**:
     - ☑ Standard flow
     - ☑ Direct access grants ✓ (QUAN TRỌNG!)
5. Click "Next"
6. Tab "Login settings":
   - **Root URL**: `http://localhost:5000`
   - **Valid redirect URIs**: `http://localhost:5000/*`
   - **Valid post logout redirect URIs**: `http://localhost:5000/*`
   - **Web origins**: `http://localhost:5000`
7. Click "Save"

#### 3.2. Add Shared Audience vào Gateway Client

1. Vẫn trong client `plc-gateway`
2. Tab **Client scopes**
3. Click **Add client scope**
4. Chọn `backend-api`
5. Chọn **Default** (không phải Optional)
6. Click "Add"

#### 3.3. Lấy Client Secret

1. Tab **Credentials**
2. Copy **Client secret** → Lưu lại để dùng sau

#### 3.4. Tạo User Service Client

Lặp lại Bước 3.1-3.3 với:
- **Client ID**: `plc-user-service`
- **Root URL**: `http://localhost:5002`
- **Valid redirect URIs**: `http://localhost:5002/*`
- **Web origins**: `http://localhost:5002`
- **Add client scope**: `backend-api` (Default)

#### 3.5. Tạo Order Service Client

Lặp lại với:
- **Client ID**: `plc-order-service`
- **Root URL**: `http://localhost:5003`
- **Valid redirect URIs**: `http://localhost:5003/*`
- **Add client scope**: `backend-api` (Default)

### Bước 4: Verify Token có Shared Audience

Lấy token để kiểm tra:

```bash
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_GATEWAY_SECRET" \
  -d "username=testuser" \
  -d "password=Test@123"
```

Copy `access_token` và decode tại https://jwt.io - phải thấy:

```json
{
  "aud": ["backend-api", "account"],  ← Có backend-api!
  "iss": "http://localhost:8080/realms/plc-microservices-demo",
  "preferred_username": "testuser",
  ...
}
```

---

## Hiểu về Client ID & Secret

### Khi nào cần Client Secret?

#### ✅ CẦN Client Secret: Khi LẤY TOKEN

```bash
# Frontend/Client lấy token
curl -X POST "http://localhost:8080/.../token" \
  -d "grant_type=password" \
  -d "client_id=plc-gateway" \
  -d "client_secret=abc123" \    ← CẦN SECRET
  -d "username=admin" \
  -d "password=Admin@123"
```

#### ❌ KHÔNG CẦN Secret: Khi VALIDATE TOKEN

```csharp
// Backend Service chỉ validate token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // CHỈ CẦN Authority và Audience
        options.Authority = "http://localhost:8080/realms/plc-microservices-demo";
        options.Audience = "backend-api";
        // KHÔNG CẦN client_secret!
    });
```

### Luồng hoạt động

```
1. Frontend/Testing Tool
   ├─ Gửi: client_id + client_secret + username + password
   ├─ Nhận: JWT token
   └─ Lưu token

2. Frontend/Testing Tool → Backend Service
   ├─ Gửi: Authorization: Bearer <token>
   └─ KHÔNG GỬI client_secret

3. Backend Service
   ├─ Nhận: token từ header
   ├─ Tải: public key từ Keycloak (JWKS)
   ├─ Verify: signature, iss, aud, exp
   └─ Accept/Reject request
```

### Cấu hình Services

**appsettings.json** (User Service, Order Service):
```json
{
  "JwtBearer": {
    "Authority": "http://localhost:8080/realms/plc-microservices-demo",
    "Audience": "backend-api",  // Shared audience
    "RequireHttpsMetadata": false
  }
}
```

**appsettings.Development.json** (QUAN TRỌNG - phải khớp!):
```json
{
  "JwtBearer": {
    "Authority": "http://localhost:8080/realms/plc-microservices-demo",
    "Audience": "backend-api",  // PHẢI GIỐNG appsettings.json!
    "RequireHttpsMetadata": false
  }
}
```

**Program.cs**:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtSettings["Authority"];
        options.Audience = jwtSettings["Audience"];
        options.RequireHttpsMetadata = bool.Parse(jwtSettings["RequireHttpsMetadata"] ?? "false");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Map Keycloak claims
            NameClaimType = "preferred_username",
            RoleClaimType = "realm_access.roles"
        };
    });
```

---

## Testing và Troubleshooting

### Test 1: Lấy Token

#### Với Gateway Client
```bash
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_GATEWAY_SECRET" \
  -d "username=admin" \
  -d "password=Admin@123"
```

#### Với User Service Client
```bash
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=plc-user-service" \
  -d "client_secret=YOUR_USER_SERVICE_SECRET" \
  -d "username=testuser" \
  -d "password=Test@123"
```

Response:
```json
{
  "access_token": "eyJhbGc...",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "refresh_token": "eyJhbGc...",
  "token_type": "Bearer"
}
```

### Test 2: Verify Token

1. Copy `access_token`
2. Vào https://jwt.io
3. Paste token
4. Kiểm tra payload:

```json
{
  "exp": 1766650200,
  "iat": 1766649900,
  "iss": "http://localhost:8080/realms/plc-microservices-demo",
  "aud": ["backend-api", "account"],  ← PHẢI CÓ backend-api
  "preferred_username": "testuser",   ← Username
  "email": "testuser@plc.com",
  "realm_access": {
    "roles": ["user", "offline_access", ...]
  }
}
```

### Test 3: Call API với Token

```bash
# Test /api/users/me endpoint
curl -X GET "http://localhost:5002/api/users/me" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

Response thành công:
```json
{
  "username": "testuser",
  "isAuthenticated": true,
  "authenticationType": "Bearer",
  "claims": [...]
}
```

### Test 4: Test tất cả endpoints

```bash
# Get all users
curl -X GET "http://localhost:5002/api/users" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get user by ID
curl -X GET "http://localhost:5002/api/users/1" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Create user
curl -X POST "http://localhost:5002/api/users" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "newuser",
    "email": "new@plc.com",
    "fullName": "New User",
    "department": "IT"
  }'
```

---

## Troubleshooting

### ❌ Lỗi 401 Unauthorized

#### Nguyên nhân 1: Thiếu client_secret
```bash
# SAI - thiếu client_secret
curl ... -d "client_id=plc-gateway" -d "username=admin" ...

# ĐÚNG - có client_secret
curl ... -d "client_id=plc-gateway" -d "client_secret=abc123" -d "username=admin" ...
```

#### Nguyên nhân 2: Token không có audience đúng
```json
// SAI - token có aud=plc-gateway nhưng service expect backend-api
{"aud": "plc-gateway"}

// ĐÚNG - token có backend-api
{"aud": ["backend-api", "account"]}
```

**Fix**: Đảm bảo đã add client scope `backend-api` vào client (Bước 3.2)

#### Nguyên nhân 3: appsettings.Development.json override

File `appsettings.Development.json` override `appsettings.json` khi chạy Development mode!

```json
// appsettings.json
{
  "JwtBearer": {
    "Audience": "backend-api"  // ✓
  }
}

// appsettings.Development.json - PHẢI KHỚP!
{
  "JwtBearer": {
    "Audience": "plc-user-service"  // ✗ SAI - bị override!
  }
}
```

**Fix**: Sửa cả 2 files cho giống nhau!

#### Nguyên nhân 4: Direct Access Grants chưa enable

1. Vào **Clients** → client của bạn
2. Tab **Settings**
3. Capability config:
   - **Direct access grants**: Phải ON
4. Click "Save"

#### Nguyên nhân 5: Token hết hạn

Token mặc định hết hạn sau 5 phút. Lấy token mới:

```bash
curl -X POST "http://localhost:8080/.../token" \
  -d "grant_type=password" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_SECRET" \
  -d "username=admin" \
  -d "password=Admin@123"
```

### ❌ Lỗi: Username null trong User.Identity.Name

**Nguyên nhân**: ASP.NET Core không map `preferred_username` claim

**Fix**: Thêm vào Program.cs
```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    NameClaimType = "preferred_username",  // Map claim này
    RoleClaimType = "realm_access.roles"
};
```

### ❌ Lỗi: invalid_client

**Nguyên nhân**:
- Client secret sai
- Client authentication chưa enable

**Fix**:
1. Kiểm tra client secret trong Keycloak
2. Copy lại secret mới nếu cần

### ❌ Keycloak không start

```bash
# Kiểm tra logs
docker-compose logs keycloak

# Kiểm tra port 8080 có bị chiếm không
netstat -ano | findstr :8080

# Restart services
docker-compose restart
```

---

## PowerShell Test Script

Tạo file `test-keycloak-auth.ps1`:

```powershell
# Configuration
$realm = "plc-microservices-demo"
$keycloakUrl = "http://localhost:8080"
$clientId = "plc-gateway"
$clientSecret = "YOUR_CLIENT_SECRET_HERE"
$username = "testuser"
$password = "Test@123"
$userServiceUrl = "http://localhost:5002"

# Get token
Write-Host "Getting token..." -ForegroundColor Cyan
$tokenResponse = Invoke-RestMethod -Method Post `
  -Uri "$keycloakUrl/realms/$realm/protocol/openid-connect/token" `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
    grant_type = "password"
    client_id = $clientId
    client_secret = $clientSecret
    username = $username
    password = $password
  }

$token = $tokenResponse.access_token
Write-Host "✓ Token received" -ForegroundColor Green
Write-Host "Token expires in: $($tokenResponse.expires_in) seconds" -ForegroundColor Yellow

# Decode token (base64)
$tokenParts = $token.Split('.')
$payload = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[1] + "=="))
$payloadJson = $payload | ConvertFrom-Json

Write-Host "`nToken Claims:" -ForegroundColor Cyan
Write-Host "  iss: $($payloadJson.iss)"
Write-Host "  aud: $($payloadJson.aud -join ', ')"
Write-Host "  preferred_username: $($payloadJson.preferred_username)"
Write-Host "  email: $($payloadJson.email)"

# Test API
Write-Host "`nTesting API..." -ForegroundColor Cyan
$headers = @{
  Authorization = "Bearer $token"
}

try {
  $userInfo = Invoke-RestMethod -Method Get `
    -Uri "$userServiceUrl/api/users/me" `
    -Headers $headers

  Write-Host "✓ API call successful" -ForegroundColor Green
  Write-Host "  Username: $($userInfo.username)"
  Write-Host "  Is Authenticated: $($userInfo.isAuthenticated)"

  $users = Invoke-RestMethod -Method Get `
    -Uri "$userServiceUrl/api/users" `
    -Headers $headers

  Write-Host "`n✓ Got $($users.Count) users" -ForegroundColor Green
  $users | Format-Table -Property id, username, email, department

} catch {
  Write-Host "✗ API call failed: $($_.Exception.Message)" -ForegroundColor Red
}
```

Chạy:
```powershell
.\test-keycloak-auth.ps1
```

---

## Các Endpoints quan trọng

| Endpoint | URL | Mô tả |
|----------|-----|-------|
| Well-known Config | `http://localhost:8080/realms/plc-microservices-demo/.well-known/openid-configuration` | OIDC configuration |
| Token Endpoint | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token` | Lấy token |
| JWKS Endpoint | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/certs` | Public keys |
| UserInfo Endpoint | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/userinfo` | User info |
| Logout Endpoint | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/logout` | Logout |
| Admin Console | `http://localhost:8080/admin` | Keycloak Admin |

---

## Quản lý Docker

```bash
# Stop services
docker-compose down

# Stop và xóa data
docker-compose down -v

# Xem logs
docker-compose logs -f

# Restart Keycloak
docker-compose restart keycloak

# Xem resource usage
docker stats
```

---

## Best Practices

### Development
- ✅ Dùng shared audience (`backend-api`)
- ✅ Client authentication: ON (confidential)
- ✅ Direct access grants: ON (cho testing)
- ✅ Token lifetime ngắn (5-15 phút)
- ✅ Kiểm tra cả 2 appsettings files

### Production
- ✅ RequireHttpsMetadata: **true**
- ✅ Client secrets trong environment variables
- ✅ Token lifetime ngắn hơn
- ✅ Enable refresh tokens
- ✅ Rate limiting cho token endpoint
- ✅ Monitoring và logging

---

## Tóm tắt nhanh

1. **Start Keycloak**: `docker-compose up -d`
2. **Create Realm**: `plc-microservices-demo`
3. **Create Client Scope**: `backend-api` với audience mapper
4. **Create Clients**: Gateway, User Service, Order Service
5. **Add Scope**: Add `backend-api` scope vào tất cả clients (Default)
6. **Create Users**: admin, testuser với roles
7. **Config Services**: Audience = `backend-api` trong cả 2 appsettings files
8. **Get Token**: Dùng client_id + client_secret + username + password
9. **Call API**: Authorization: Bearer <token>
10. **Verify**: Token có `aud=backend-api`, services validate OK

**Lưu ý quan trọng**: appsettings.Development.json override appsettings.json - phải sửa CẢ HAI!

---

## Tham khảo

- Keycloak Documentation: https://www.keycloak.org/documentation
- OpenID Connect: https://openid.net/connect/
- JWT.io: https://jwt.io
- ASP.NET Core Authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
