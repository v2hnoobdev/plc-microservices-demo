# PLC Demo App

Vue 3 + TypeScript frontend demonstrating **Authorization Code Flow + PKCE** with Keycloak.

## Architecture

Frontend sử dụng **Multi-Audience Strategy**:
- Token có `aud: "public-api"` (cho public API access)
- Tất cả API calls đi qua **Gateway** (port 5000)
- Backend services validate multiple audiences (`public-api` cho frontend, service-specific cho internal calls)

📖 **Chi tiết kiến trúc**: Xem [`docs/AUDIENCE_ARCHITECTURE.md`](../docs/AUDIENCE_ARCHITECTURE.md)

## Prerequisites

1. **Node.js** (v18+)
2. **Keycloak** running on http://localhost:8080 (với `public-api` client scope đã cấu hình)
3. **Gateway** running on http://localhost:5000
4. **User Service** running on http://localhost:5002 (accessed via Gateway)

## Setup

### 1. Install Dependencies

```bash
npm install
```

### 2. Configure Keycloak Client

Truy cập Keycloak Admin Console: http://localhost:8080

#### 2.1. Tạo Frontend Client

1. Vào **Clients** → **Create client**
2. **General Settings**:
   - Client type: `OpenID Connect`
   - Client ID: `plc-frontend`
3. Click **Next**
4. **Capability config**:
   - Client authentication: **OFF** (Public client)
   - Authorization: **OFF**
   - Authentication flow:
     - **Standard flow**: ON
     - Direct access grants: OFF
     - Implicit flow: OFF
5. Click **Next**
6. **Login settings**:
   - Root URL: `http://localhost:3000`
   - Valid redirect URIs:
     - `http://localhost:3000/*`
   - Valid post logout redirect URIs:
     - `http://localhost:3000/*`
   - Web origins:
     - `http://localhost:3000`
7. Click **Save**

#### 2.2. Add Public API Scope

**Lưu ý**: Client scope `public-api` phải được tạo trước. Xem hướng dẫn tại `docs/AUDIENCE_ARCHITECTURE.md`

1. Vẫn trong client `plc-frontend`
2. Tab **Client scopes**
3. Click **Add client scope**
4. Chọn `public-api`
5. Chọn **Default** (không phải Optional)
6. Click **Add**

#### 2.3. Verify Configuration

Kiểm tra lại settings:

- **Access Type**: Public
- **Standard Flow Enabled**: ON
- **Direct Access Grants Enabled**: OFF ← QUAN TRỌNG!
- **Valid Redirect URIs**: `http://localhost:3000/*`
- **Web Origins**: `http://localhost:3000`
- **Client Scopes**: `public-api` (Default)

### 3. Environment Variables

File `.env` đã được tạo với config mặc định:

```env
# Keycloak Configuration
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=plc-microservices-demo
VITE_KEYCLOAK_CLIENT_ID=plc-frontend

# API Configuration - All requests go through Gateway
VITE_API_BASE_URL=http://localhost:5000
```

**Lưu ý**: Frontend gọi API qua Gateway (port 5000), không trực tiếp gọi User Service (port 5002).

Nếu cần thay đổi, chỉnh sửa file `.env`.

## Running the App

### Development Server

```bash
npm run dev
```

App sẽ chạy tại: http://localhost:3000

### Build for Production

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Testing Authorization Code Flow

### Bước 1: Khởi động các services

```bash
# Terminal 1: Keycloak (nếu chưa chạy)
cd E:\PLC\DesignMicroservicesDraft\DemoProject\infrastructure\docker
docker-compose up -d

# Terminal 2: Gateway
cd E:\PLC\DesignMicroservicesDraft\DemoProject\src\Gateway\PLC.Gateway
dotnet run

# Terminal 3: User Service
cd E:\PLC\DesignMicroservicesDraft\DemoProject\src\Services\User\PLC.User.API
dotnet run

# Terminal 4: Frontend
cd E:\PLC\DesignMicroservicesDraft\DemoProject\frontend-app
npm run dev
```

**Lưu ý**: Gateway phải chạy trước vì frontend gọi API qua Gateway.

### Bước 2: Test Login Flow

1. Mở browser: http://localhost:3000
2. Click **"Login with Keycloak"**
3. Browser redirect đến Keycloak: http://localhost:8080/realms/plc-microservices-demo/protocol/openid-connect/auth?...
4. Đăng nhập với:

   - Username: `testuser`
   - Password: `Test@123`

   Hoặc:

   - Username: `admin`
   - Password: `Admin@123`

5. Keycloak redirect về: http://localhost:3000?code=...&session_state=...
6. Frontend tự động exchange code → tokens
7. Hiển thị trang authenticated với username

### Bước 3: Test API Call

1. Click **"Get All Users"**
2. Frontend gọi Gateway → User Service với `Authorization: Bearer <token>`
3. Hiển thị danh sách users

### Bước 4: Kiểm tra Token (Optional)

1. Mở Console (F12)
2. Paste đoạn code sau để xem token:
   ```javascript
   console.log('Access Token:', window.keycloak.token);
   console.log('Parsed Token:', window.keycloak.tokenParsed);
   ```
3. Verify `aud` claim có `"public-api"`

## How Authorization Code Flow Works

```
1. User clicks "Login"
   ├─ Frontend redirects to Keycloak
   └─ URL: /auth?response_type=code&client_id=plc-frontend&code_challenge=...

2. User logs in on Keycloak
   ├─ Username/password on Keycloak page (NOT our app!)
   └─ Keycloak validates credentials

3. Keycloak redirects back with code
   ├─ URL: http://localhost:3000?code=abc123&session_state=xyz
   └─ Code is single-use, short-lived

4. Frontend exchanges code for tokens
   ├─ POST /token
   ├─ Body: code + code_verifier (PKCE)
   └─ Response: {access_token, refresh_token, id_token}

5. Frontend calls API with token
   ├─ Authorization: Bearer <access_token>
   └─ Backend validates signature with Keycloak public key

6. Auto refresh before expiry
   ├─ keycloak.onTokenExpired event
   └─ keycloak.updateToken(30)
```

## Troubleshooting

### ❌ Lỗi: "Invalid redirect_uri"

**Fix**: Vào Keycloak → Clients → plc-frontend → Valid Redirect URIs: `http://localhost:3000/*`

### ❌ Lỗi 401 khi call API

**Nguyên nhân**: Token không có `aud: "public-api"`

**Fix**:
1. Đảm bảo đã tạo client scope `public-api` với audience mapper (xem `docs/AUDIENCE_ARCHITECTURE.md`)
2. Add client scope `public-api` vào `plc-frontend` client (Default, không phải Optional)
3. Đảm bảo User Service có `"ValidAudiences": ["public-api", "plc-user-service"]` trong appsettings.json

### ❌ Lỗi 404 khi call API

**Nguyên nhân**: Gateway chưa chạy hoặc YARP routing chưa đúng

**Fix**:
1. Đảm bảo Gateway đang chạy tại `http://localhost:5000`
2. Kiểm tra `.env` có `VITE_API_BASE_URL=http://localhost:5000`
3. Kiểm tra Gateway logs để debug routing issues

## Tech Stack

- **Vue 3** - Progressive JavaScript framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **Tailwind CSS** - Utility-first CSS
- **Lucide Icons** - Beautiful icons
- **Keycloak-JS** - Keycloak adapter for JavaScript
