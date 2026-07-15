# Comeback

Digitalna platforma za rekreativni fudbal.

## Preduslovi

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Node.js 20+](https://nodejs.org/) i npm
- [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) alat: `dotnet tool install --global dotnet-ef`

---

## Pokretanje

### 1. Backend (Docker)

```bash
cd infra/docker
docker compose up -d
```

Ovo pokreće sve zavisnosti i servise:

| Servis | URL |
|---|---|
| **API Gateway** | http://localhost:5000 |
| Auth API | http://localhost:5104 |
| Profile API | http://localhost:5210 |
| Rating API | http://localhost:5310 |
| RabbitMQ Management | http://localhost:15672 |
| MailDev (inbox) | http://localhost:1080 |
| Seq (logovi) | http://localhost:5342 |

> **Kredencijali RabbitMQ / baze:** `comeback` / `comeback_dev`

### 2. Migracije baza podataka

Migracije se primenjuju ručno nakon prvog podizanja kontejnera:

```bash
# Auth baza (port 5432)
dotnet ef database update \
  --project src/services/auth/Comeback.Auth.Infrastructure \
  --startup-project src/services/auth/Comeback.Auth.Api \
  --connection "Host=localhost;Port=5432;Database=comeback_auth;Username=comeback;Password=comeback_dev"

# Profile baza (port 5433)
dotnet ef database update \
  --project src/services/profile/Comeback.Profile.Infrastructure \
  --startup-project src/services/profile/Comeback.Profile.Api \
  --connection "Host=localhost;Port=5433;Database=comeback_profile;Username=comeback;Password=comeback_dev"

# Rating baza (port 5434)
dotnet ef database update \
  --project src/services/rating/Comeback.Rating.Infrastructure \
  --startup-project src/services/rating/Comeback.Rating.Api \
  --connection "Host=localhost;Port=5434;Database=comeback_rating;Username=comeback;Password=comeback_dev"
```

### 3. Frontend

```bash
cd web/comeback-web
npm install
npm start
```

Aplikacija se otvara na **http://localhost:4200**.

---

## Struktura projekta

```
src/
  building-blocks/    # Deljeni kod (eventi, izuzeci, pipeline)
  gateway/            # API Gateway + BFF (YARP, port 5000)
  services/
    auth/             # Autentifikacija i nalozi (port 5104)
    profile/          # Profili igrača (port 5210)
    rating/           # XP i ELO rejting (port 5310)
    notification/     # Email obaveštenja

web/
  comeback-web/       # Angular 19 aplikacija

infra/
  docker/             # docker-compose.yml
```

## Nakon izmena koda

Backend servisi se ne ažuriraju automatski — potrebno je rebuild:

```bash
cd infra/docker
docker compose build <ime-servisa>
docker compose up -d <ime-servisa>
```

Na primer: `docker compose build profile-api && docker compose up -d profile-api`
