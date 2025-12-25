# Infrastructure - Docker

Docker Compose setup cho PLC Microservices Demo

## Services

### Keycloak (Identity Server)

- **Port:** 8080
- **Admin Console:** http://localhost:8080/admin
- **Admin User:** admin / admin (default)
- **Database:** PostgreSQL

### PostgreSQL (Keycloak DB)

- **Port:** 5432
- **Database:** keycloak
- **User:** keycloak
- **Password:** \*\*\*\*\*\*\*\*\*\*

## Quick Start

### 1. Start tat ca services

```bash
docker-compose up -d
```

### 2. Kiem tra status

```bash
docker-compose ps
```

### 3. Xem logs

```bash
# All services
docker-compose logs -f

# Chi Keycloak
docker-compose logs -f keycloak

# Chi PostgreSQL
docker-compose logs -f postgres-keycloak
```

### 4. Stop services

```bash
docker-compose down
```

### 5. Stop va xoa data

```bash
docker-compose down -v
```

## Setup Keycloak

Xem huong dan chi tiet: [KEYCLOAK_SETUP.md](./KEYCLOAK_SETUP.md)

## Ports

| Service    | Port | URL                   |
| ---------- | ---- | --------------------- |
| Keycloak   | 8080 | http://localhost:8080 |
| PostgreSQL | 5432 | localhost:5432        |

## Networks

Tat ca services chay tren network: `plc-network`

## Volumes

- `postgres_keycloak_data` - PostgreSQL data persistence
