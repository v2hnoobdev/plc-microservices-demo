# Keycloak Setup Guide - Complete Reference

Hướng dẫn đầy đủ về setup và sử dụng Keycloak Identity Server cho microservices architecture.

## Mục lục

1. [Kiến trúc tổng quan](#kiến-trúc-tổng-quan)
2. [Setup Keycloak với Docker](#setup-keycloak-với-docker)
3. [Cấu hình Realm và Clients](#cấu-hình-realm-và-clients)
4. [Setup cho Testing Client Credentials Flow](#setup-cho-testing-client-credentials-flow)
5. [Testing và Troubleshooting](#testing-và-troubleshooting)

---

## Kiến trúc tổng quan

### Luồng Authentication & Authorization

```
┌─────────────────────────────────────────────────────────────┐
│                        Keycloak                             │
│                    (Identity Server)                        │
│                                                             │
│  Realm: plc-microservices-demo                              │
│  Client Scopes: backend-api (shared audience)               │
│                                                             │
│  Clients:                                                   │
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
cd .\infrastructure\docker
docker compose -f keycloak-compose.yml up -d
```

### Bước 2: Kiểm tra logs

```bash
# Xem logs Keycloak
docker compose logs -f keycloak

# Xem logs PostgreSQL
docker compose logs -f postgres-keycloak

# Kiểm tra trạng thái
docker compose ps
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
   - Password: (xem trong cấu hình compose)

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

| Role Name | Description        |
| --------- | ------------------ |
| `user`    | Standard user role |
| `admin`   | Administrator role |

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
   - Password: `admin@123`
   - Temporary: OFF
4. Tab "Role mapping" → Assign roles: `admin`, `user`

#### User 2: Test User

Lặp lại với:

- Username: `testuser`
- Email: `testuser@plc.com`
- Password: `test@123`
- Roles: `user`

---

## Setup cho Testing Client Credentials Flow

> **⚠️ LƯU Ý**: Phần này chỉ dùng để test **Client Credentials Flow** (service-to-service authentication) trong môi trường development.

### Mục đích

Setup này giúp bạn test nhanh việc lấy token từ Keycloak bằng **Client Credentials Grant**:

- Không cần user login
- Dùng `client_id` + `client_secret` để lấy token
- Phù hợp cho service-to-service communication

### Khi nào dùng gì?

[OAuth Flows](https://frontegg.com/blog/oauth-flows)

| Grant Type                    | Use Case            | Cần Secret?           | Có User Context? |
| ----------------------------- | ------------------- | --------------------- | ---------------- |
| **Client Credentials**        | Service-to-service  | ✅ Yes                | ❌ No            |
| **Password Grant**            | Testing/Development | ✅ Yes                | ✅ Yes           |
| **Authorization Code + PKCE** | Production Frontend | ❌ No (Public client) | ✅ Yes           |

### Bước 1: Tạo Client Scope `backend-api` (Testing Only)

1. Vào **Client scopes** (menu bên trái)
2. Click "Create client scope"
3. Điền:
   - **Name**: `backend-api`
   - **Type**: Default
   - **Protocol**: OpenID Connect
   - **Display on consent screen**: OFF
4. Click "Save"

5. Tab **Mappers** → Click **Add mapper** → **By configuration**
6. Chọn **Audience**
7. Điền:
   - **Name**: `backend-api-audience`
   - **Mapper Type**: Audience
   - **Included Custom Audience**: `backend-api`
   - **Add to ID token**: OFF
   - **Add to access token**: ON ✓
8. Click "Save"

### Bước 2: Tạo Test Client (Client Credentials)

1. Vào **Clients** → "Create client"
2. **General Settings**:
   - Client type: `OpenID Connect`
   - Client ID: `plc-gateway`
3. Click "Next"

4. **Capability config**:
   - Client authentication: **ON** (Confidential)
   - Authorization: **OFF**
   - **Authentication flow**:
     - Service accounts roles: **ON** ← Cho Client Credentials
     - Direct access grants: **ON** ← Cho Password Grant (testing)
     - Standard flow: **OFF** (không cần cho testing)
5. Click "Save"

6. Tab **Client scopes**:

   - Click **Add client scope**
   - Chọn `backend-api`
   - Chọn **Default**
   - Click "Add"

7. Tab **Credentials**:
   - Copy **Client secret** → Lưu lại

### Bước 3: Test Client Credentials Flow

```bash
# Lấy token bằng Client Credentials (không cần username/password)
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_CLIENT_SECRET"
```

Token sẽ có:

```json
{
  "aud": ["backend-api", "account"],
  "azp": "plc-gateway",
  "clientId": "plc-gateway"
  // Không có preferred_username vì không phải user login
}
```

### Bước 4: (Optional) Test Password Grant Flow

Nếu muốn test với user credentials:

```bash
curl -X POST "http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=plc-gateway" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "username=testuser" \
  -d "password=Test@123"
```

### Cấu hình Backend Service (Testing)

**appsettings.json**:

```json
{
  "JwtBearer": {
    "Authority": "http://localhost:8080/realms/plc-microservices-demo",
    "ValidAudiences": ["public-api", "backend-api"], //
    "RequireHttpsMetadata": false,
    "ValidateAudience": true,
    "ValidateIssuer": true,
    "ValidateLifetime": true
  }
}
```

**Program.cs**:

```csharp
var jwtSettings = builder.Configuration.GetSection("JwtBearer");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = jwtSettings["Authority"];
        options.RequireHttpsMetadata = bool.Parse(jwtSettings["RequireHttpsMetadata"] ?? "false");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = bool.Parse(jwtSettings["ValidateAudience"] ?? "true"),
            ValidateIssuer = bool.Parse(jwtSettings["ValidateIssuer"] ?? "true"),
            ValidateLifetime = bool.Parse(jwtSettings["ValidateLifetime"] ?? "true"),
            ValidateIssuerSigningKey = true,

            // Support multiple audiences
            ValidAudiences = jwtSettings.GetSection("ValidAudiences").Get<string[]>(),

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

**Client Credentials token:**

```json
{
  "exp": 1766650200,
  "iat": 1766649900,
  "iss": "http://localhost:8080/realms/plc-microservices-demo",
  "aud": ["backend-api", "account"],  ← Audience cho testing
  "azp": "plc-gateway",
  "clientId": "plc-gateway"
  // Không có preferred_username
}
```

**Password Grant token:**

```json
{
  "exp": 1766650200,
  "iat": 1766649900,
  "iss": "http://localhost:8080/realms/plc-microservices-demo",
  "aud": ["backend-api", "account"],  ← Audience cho testing
  "preferred_username": "testuser",   ← Username (chỉ có khi dùng password grant)
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

**Kiểm tra audience trong token:**

Expect audience: `backend-api`

```json
// ❌ SAI - token không có audience mà service expect
{"aud": "plc-gateway"}

// ✅ ĐÚNG
{"aud": ["backend-api", "account"]}
```

**Fix**:

1. **Cho testing**: Add client scope `backend-api` vào client
2. **Cho production**: Add client scope `public-api` vào frontend client
3. **Backend service**: Đảm bảo `ValidAudiences` include audience của token:
   ```json
   "ValidAudiences": ["public-api", "backend-api"]  // Support cả 2
   ```

#### Nguyên nhân 3: appsettings.Development.json override

File `appsettings.Development.json` override `appsettings.json` khi chạy Development mode!

```json
// appsettings.json
{
  "JwtBearer": {
    "ValidAudiences": ["backend-api"]  // ✓
  }
}

// appsettings.Development.json - PHẢI KHỚP!
{
  "JwtBearer": {
    "ValidAudiences": ["plc-user-service"]  // ✗ SAI - bị override!
  }
}
```

**Fix**: Đảm bảo cả 2 files có cùng `ValidAudiences`!

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

### ❌ Lỗi: Không lấy được tên user từ token

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

---

## Các Endpoints quan trọng

| Endpoint          | URL                                                                                    | Mô tả              |
| ----------------- | -------------------------------------------------------------------------------------- | ------------------ |
| Well-known Config | `http://localhost:8080/realms/plc-microservices-demo/.well-known/openid-configuration` | OIDC configuration |
| Token Endpoint    | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token`    | Lấy token          |
| JWKS Endpoint     | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/certs`    | Public keys        |
| UserInfo Endpoint | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/userinfo` | User info          |
| Logout Endpoint   | `http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/logout`   | Logout             |
| Admin Console     | `http://localhost:8080/admin`                                                          | Keycloak Admin     |

---

### Production

- RequireHttpsMetadata: **true**
- "Direct access grants" chỉ dùng cho service-to-service communication
- Client secrets trong environment variables
- Token lifetime ngắn hơn
- Enable refresh tokens
- Rate limiting cho token endpoint
- Monitoring và logging

---

## Tóm tắt nhanh

### Setup Testing (Client Credentials Flow)

1. **Start Keycloak**: `docker-compose up -d`
2. **Create Realm**: `plc-microservices-demo`
3. **Create Users**: admin, testuser với roles
4. **Create Client Scope**: `backend-api` với audience mapper (testing only)
5. **Create Test Client**: `plc-gateway` với Client Credentials + Password Grant
6. **Add Scope**: Add `backend-api` scope vào client (Default)
7. **Config Services**:
   ```json
   "ValidAudiences": ["public-api", "backend-api"]  // Cả 2 files
   ```
8. **Get Token**:
   - Client Credentials: `grant_type=client_credentials`
   - Password Grant: `grant_type=password` + username/password
9. **Call API**: `Authorization: Bearer <token>`

### Setup Production (Authorization Code Flow + PKCE)

[`01. Multi-Audience Strategy Setup`](./01_KEYCLOAK_AUDIENCE_ARCHITECTURE.md)

---

## Tham khảo

- Keycloak Documentation: https://www.keycloak.org/documentation
- OpenID Connect: https://openid.net/connect/
- JWT.io: https://jwt.io
- ASP.NET Core Authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
