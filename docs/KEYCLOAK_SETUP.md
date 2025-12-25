# Keycloak Setup Guide

Huong dan setup Keycloak Identity Server voi Docker Compose

## Yeu cau

- Docker Desktop (Windows/Mac) hoac Docker Engine (Linux)
- Docker Compose

## Kien truc

```
┌─────────────────────────────────────────┐
│         Keycloak Container              │
│    Port: 8080                           │
│    - Admin UI: /admin                   │
│    - Realms: /realms/{realm}            │
│         │                                │
│         ▼                                │
│  ┌──────────────────────┐               │
│  │  PostgreSQL DB       │               │
│  │  Port: 5432          │               │
│  └──────────────────────┘               │
└─────────────────────────────────────────┘
```

## Buoc 1: Chay Keycloak

### 1.1. Start services

```bash
cd E:\PLC\DesignMicroservicesDraft\DemoProject\infrastructure\docker
docker-compose up -d
```

### 1.2. Kiem tra logs

```bash
# Xem logs cua Keycloak
docker-compose logs -f keycloak

# Xem logs cua PostgreSQL
docker-compose logs -f postgres-keycloak
```

### 1.3. Cho Keycloak khoi dong (~ 30-60 giay)

Kiem tra health:

```bash
docker-compose ps
```

Ket qua mong doi:

```
NAME                IMAGE                              STATUS
keycloak            quay.io/keycloak/keycloak:23.0     Up (healthy)
postgres-keycloak   postgres:16-alpine                 Up (healthy)
```

## Buoc 2: Truy cap Keycloak Admin Console

### 2.1. Mo trinh duyet

URL: http://localhost:8080

### 2.2. Dang nhap Admin Console

Click "Administration Console"

**Credentials:**

- Username: `admin`
- Password: `admin`

## Buoc 3: Tao Realm cho PLC Application

Realm la mot isolated space cho application cua ban.

### 3.1. Tao Realm moi

1. Click dropdown "master" o goc tren ben trai
2. Click "Create Realm"
3. Dien thong tin:
   - Realm name: `plc-microservices-demo`
   - Enabled: ON
4. Click "Create"

### 3.2. Config Realm Settings

1. Vao "Realm settings" (menu ben trai)
2. Tab "General":
   - User profile enabled: ON
   - User-managed access: ON
3. Tab "Login":
   - User registration: ON (cho phep users tu dang ky)
   - Forgot password: ON
   - Remember me: ON
4. Click "Save"

## Buoc 4: Tao Client cho Gateway/Services

Client dai dien cho application se su dung Keycloak.

### 4.1. Tao Client cho Gateway

1. Vao "Clients" (menu ben trai)
2. Click "Create client"
3. Tab "General Settings":
   - Client type: `OpenID Connect`
   - Client ID: `plc-gateway`
4. Click "Next"
5. Tab "Capability config":
   - Client authentication: ON
   - Authorization: OFF
   - Authentication flow:
     - [x] Standard flow
     - [x] Direct access grants
6. Click "Next"
7. Tab "Login settings":
   - Root URL: `http://localhost:5000`
   - Home URL: `http://localhost:5000`
   - Valid redirect URIs: `http://localhost:5000/*`
   - Valid post logout redirect URIs: `http://localhost:5000/*`
   - Web origins: `http://localhost:5000`
8. Click "Save"

### 4.2. Lay Client Secret

1. Vao "Clients" > "plc-gateway"
2. Tab "Credentials"
3. Copy "Client secret" (se can dung trong config)

**Luu y:** Ghi lai Client Secret nay!

Example: `rPzXKG7vQJ8xN5mK9wYL3sT4uV2aH6bE`

## Buoc 5: Tao Client cho Services (User Service, Order Service)

### 5.1. Tao Client cho User Service

Lap lai Buoc 4.1 voi:

- Client ID: `plc-user-service`
- Root URL: `http://localhost:5002`
- Valid redirect URIs: `http://localhost:5002/*`

### 5.2. Tao Client cho Order Service

Lap lai Buoc 4.1 voi:

- Client ID: `plc-order-service`
- Root URL: `http://localhost:5003`
- Valid redirect URIs: `http://localhost:5003/*`

## Buoc 6: Tao Roles

Roles de phan quyen users.

### 6.1. Tao Realm Roles

1. Vao "Realm roles" (menu ben trai)
2. Click "Create role"
3. Tao cac roles sau:

**Role 1:**

- Role name: `user`
- Description: `Standard user role`

**Role 2:**

- Role name: `admin`
- Description: `Administrator role`

**Role 3:**

- Role name: `manager`
- Description: `Manager role`

## Buoc 7: Tao Test User

### 7.1. Tao user

1. Vao "Users" (menu ben trai)
2. Click "Add user"
3. Dien thong tin:
   - Username: `testuser`
   - Email: `testuser@plc.com`
   - First name: `Test`
   - Last name: `User`
   - Email verified: ON
   - Enabled: ON
4. Click "Create"

### 7.2. Set password

1. Tab "Credentials"
2. Click "Set password"
3. Dien:
   - Password: `testuser123`
   - Password confirmation: `testuser123`
   - Temporary: OFF (khong bat doi password lan dau)
4. Click "Save"

### 7.3. Gan role

1. Tab "Role mapping"
2. Click "Assign role"
3. Chon role `user`
4. Click "Assign"

### 7.4. Tao Admin User

Lap lai 7.1-7.3 voi:

- Username: `admin`
- Email: `admin@plc.com`
- Password: `admin123`
- Role: `admin`, `user`

## Buoc 8: Test Authentication

### 8.1. Lay Access Token bang Postman/curl

**Endpoint:**

```
POST http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token
```

**Headers:**

```
Content-Type: application/x-www-form-urlencoded
```

**Body (x-www-form-urlencoded):**

```
grant_type=password
client_id=plc-gateway
client_secret={CLIENT_SECRET_TU_BUOC_4.2}
username=testuser
password=testuser123
```

**Response:**

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI...",
  "expires_in": 300,
  "refresh_expires_in": 1800,
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI...",
  "token_type": "Bearer"
}
```

### 8.2. Verify token

Copy `access_token` va paste vao https://jwt.io

Kiem tra:

- Issuer (iss): `http://localhost:8080/realms/plc-microservices-demo`
- Subject (sub): user ID
- Roles: trong `realm_access.roles`

## Buoc 9: Lay Public Key de validate JWT

ASP.NET Core services can public key de validate JWT token.

### 9.1. Realm Public Key

Vao: http://localhost:8080/realms/plc-microservices-demo

Trong JSON response, tim:

```json
{
  "realm": "plc-microservices-demo",
  "public_key": "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...",
  ...
}
```

### 9.2. JWKS Endpoint (Recommended)

URL: http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/certs

ASP.NET Core se tu dong lay public keys tu endpoint nay.

## Buoc 10: Cac Endpoints quan trong

**Well-known config:**

```
http://localhost:8080/realms/plc-microservices-demo/.well-known/openid-configuration
```

**Authorization endpoint:**

```
http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/auth
```

**Token endpoint:**

```
http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/token
```

**Userinfo endpoint:**

```
http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/userinfo
```

**Logout endpoint:**

```
http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/logout
```

## Quan ly Docker Compose

### Stop services

```bash
docker-compose down
```

### Stop va xoa data

```bash
docker-compose down -v
```

### Xem logs

```bash
docker-compose logs -f
```

### Restart Keycloak

```bash
docker-compose restart keycloak
```

## Troubleshooting

### Keycloak khong start

1. Kiem tra Docker Desktop dang chay
2. Kiem tra port 8080 chua bi su dung:
   ```bash
   netstat -ano | findstr :8080
   ```
3. Xem logs:
   ```bash
   docker-compose logs keycloak
   ```

### Quen admin password

1. Stop Keycloak:
   ```bash
   docker-compose down
   ```
2. Sua `docker-compose.yml`, doi `KEYCLOAK_ADMIN_PASSWORD`
3. Start lai:
   ```bash
   docker-compose up -d
   ```

### Database connection error

1. Kiem tra PostgreSQL container:
   ```bash
   docker-compose ps postgres-keycloak
   ```
2. Restart:
   ```bash
   docker-compose restart postgres-keycloak
   docker-compose restart keycloak
   ```

## Tiep theo

Sau khi setup xong Keycloak:

1. Config Gateway de forward /auth requests toi Keycloak
2. Tao User Service voi JWT authentication
3. Test authentication flow

## Tham khao

- Keycloak Documentation: https://www.keycloak.org/documentation
- Keycloak Admin Console: http://localhost:8080/admin
- OpenID Connect: https://openid.net/connect/
