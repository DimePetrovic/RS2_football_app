# Comeback — Arhitektonske odluke

Ovaj dokument beleži sve dogovorene arhitektonske i tehničke odluke projekta.
Svaka odluka ima status, kratko obrazloženje i alternativu koja je razmatrana.

---

## Tehnički stack

| Oblast | Odluka |
|---|---|
| Backend runtime | .NET 8 (LTS) |
| Frontend framework | Angular 19 |
| Baza podataka (primarna) | PostgreSQL (po jedan database/schema per mikroservis) |
| Keširanje | Redis |
| Pretraga | Elasticsearch / OpenSearch |
| Message broker | RabbitMQ + MassTransit |
| Real-time | SignalR |
| Kontejnerizacija | Docker + Docker Compose (lokalno) |
| Source control strategija | Trunk-based development, kratki feature branch-evi |

---

## Backend odluke

### B1 — Interna arhitektura mikroservisa: Clean/Onion Architecture

Svaki mikroservis prati četvoroslojnu Clean Architecture:

```
Domain        ← entiteti, value objekti, domenski događaji, domenski izuzeci
Application   ← komande, upiti, handler-i (MediatR), interfejsi, DTO-ovi
Infrastructure ← EF Core, repozitorijumi, integracije, outbox
API           ← Minimal API endpoint-i, middleware, DI konfiguracija
```

Pravila zavisnosti su stroga: svaki sloj može zavisiti samo od slojeva "unutra" (prema Domain-u). Domain ne zna ništa o infrastrukturi.

**Alternativa razmatrana:** Vertical Slice Architecture — odbačena jer za veći tim i dugoročni projekat eksplicitna podela odgovornosti donosi više vrednosti od brzine inicijalnog razvoja.

---

### B2 — CQRS sa MediatR i pipeline behavior-ima

Sva poslovna logika prolazi kroz MediatR pipeline:

- `ICommand<TResponse>` / `IQuery<TResponse>` markeri
- Pipeline behavior-i (redosled izvršavanja):
  1. `LoggingBehavior` — loguje ulazne parametre i vreme izvršavanja
  2. `ValidationBehavior` — automatski poziva FluentValidation validatore
  3. `TransactionBehavior` — za komande koje menjaju stanje (EF Core transakcija)
  4. `IdempotencyBehavior` — sprečava duplo procesiranje (za ključne operacije)

**Alternativa razmatrana:** Direktni pozivi servisnih klasa — odbačeno jer pipeline behaviors pružaju centralizovano mesto za cross-cutting concerns bez duplikacije.

---

### B3 — API layer: Minimal API

Svaki mikroservis koristi Minimal API endpointe u `API` sloju.

Endpointi se grupišu po feature-ima unutar `Endpoints/` foldera:

```
API/
  Endpoints/
    Auth/
      RegisterEndpoint.cs
      LoginEndpoint.cs
    ...
  Program.cs
```

**Alternativa razmatrana:** Controllers — odbačeno jer Minimal API u .NET 8 pruža bolji developer experience i manji overhead, uz bolju kompoziciju sa feature folder pristupom.

---

### B4 — Error handling: Custom izuzeci + globalni middleware

Domenski sloj definiše hijerarhiju izuzetaka:

```
DomainException (base)
  ├── ValidationException
  ├── NotFoundException
  ├── ConflictException
  ├── ForbiddenException
  └── BusinessRuleException
```

Globalni middleware u API sloju hvata sve izuzetke i mapira ih u RFC 7807 `ProblemDetails` odgovore sa standardnom strukturom.

HTTP status kodovi se mapiraju centralizovano — domenski sloj ne zna ništa o HTTP-u.

**Alternativa razmatrana:** `Result<T>` / `ErrorOr<T>` pattern (eksplicitni return tipovi) — odbačeno zbog preferencije jednostavnosti u ovom projektu.

---

### B5 — Validacija: FluentValidation kao MediatR pipeline behavior

Svaki command/query može imati odgovarajući `AbstractValidator<T>`. `ValidationBehavior` automatski pronalazi i poziva validator pre handler-a. Nema `ModelState`, nema ručnih `if` provjera u handler-ima.

---

### B6 — Logovanje: Serilog

Structured logging sa Serilog. Svaki zahtev nosi `CorrelationId` koji se propagira kroz sve servise (header `X-Correlation-Id`). Log sinkovi: konzola (lokalno), po potrebi OpenTelemetry/Seq za dalje.

---

### B7 — Async komunikacija: Outbox pattern

Svaki mikroservis koji publishuje domenski događaj koristi Outbox pattern:
- Događaj se čuva u outbox tabeli u istoj SQL transakciji sa domenskom promenom
- Background worker čita outbox i publishuje na RabbitMQ
- Garantuje "at-least-once" isporuku

Consumer strana koristi idempotency key za zaštitu od duplog procesiranja.

---

### B8 — Autentifikacija: ASP.NET Core Identity + JWT

- **Access token:** kratkog trajanja (15 min), prenosi se u `Authorization: Bearer` header-u
- **Refresh token:** dužeg trajanja, čuva se u HTTP-only cookie (zaštita od XSS)
- Auth Service je jedini koji izdaje i validira tokene
- Ostali servisi validiraju JWT lokalno (shared signing key ili public key)

---

### B9 — Medijski fajlovi: Cloudinary sa signed direct upload-om

Slike i snimci (profilne slike, mediji meča) čuvaju se na Cloudinary-ju; relacione baze čuvaju samo metadata i URL (spec §5.8, entitet `MatchMedia`).

Tok upload-a:
1. Frontend traži potpis od nadležnog servisa (`POST .../upload-signature`) — servis proverava poslovna pravila (npr. da je korisnik učesnik meča) i vraća `cloudName/apiKey/timestamp/folder/signature`
2. Browser šalje fajl **direktno** Cloudinary API-ju sa tim potpisom (fajl ne prolazi kroz Gateway — bitno za video od 100 MB)
3. Frontend potvrđuje upload backend-u koji čuva metadata (`MatchMedia` u match_db, `AvatarUrl` u profile_db)

Zajednički kod (`ICloudinaryMediaService`: potpisivanje + brisanje) živi u BuildingBlocks (`Infrastructure/Media`). Api secret nikad ne napušta backend; auth interceptor na frontendu šalje JWT samo ka `apiUrl`. Konfiguracija preko `Cloudinary` sekcije / `CLOUDINARY_*` env varijabli.

**Alternativa razmatrana:** upload kroz backend (multipart preko Gateway-a) — odbačena jer bi veliki video fajlovi opterećivali Gateway i servise, a Cloudinary potpisani upload daje istu kontrolu pristupa bez tog troška.

---

### B10 — Pozadinski poslovi: Hangfire (Match servis)

Zakazivanje poslova vezanih za životni ciklus meča koristi **Hangfire** sa PostgreSQL storage-om, unutar Match servisa (Hangfire šema u `match_db`).

Poslovi:
- **Podsetnik za rezultat** — pri kreiranju meča zakazuje se odloženi posao za `EndsAt + 15min` koji šalje obaveštenje organizatoru ako rezultat nije unet. Id posla se čuva na `Match.ResultReminderJobId`. Izmena vremena → otkaži stari + zakaži novi; otkazivanje meča → otkaži posao.
- **Dnevni prolazak (12h UTC)** — recurring job eskalira status: `Scheduled` (kome je prošao `EndsAt`) → `ResultOverdue` (+ obaveštenje organizatoru); `ResultOverdue` → `Missed` (+ obaveštenje učesnicima). Prozor pretrage 7 dana.

Novi statusi: `ResultOverdue`, `Missed`. Podrazumevano trajanje meča kada nije uneto je **2h** (`Match.DefaultDurationMinutes`, `Match.EndsAt`). Unos rezultata dozvoljen u `Scheduled` i `ResultOverdue`.

Hangfire logika ostaje tanka: job klase (`MatchReminderJob`) samo delegiraju na MediatR komande (`SendResultReminderCommand`, `ProcessOverdueMatchesCommand`), pa je poslovna logika testabilna bez Hangfire-a. Scheduler je iza `IMatchJobScheduler` interfejsa (fejkuje se u testovima). Recurring job se registruje preko DI `IRecurringJobManager`-a (ne statičkog `JobStorage.Current`, koji pri startu još nije inicijalizovan).

**Alternativa razmatrana:** MassTransit message scheduling — odbačeno jer RabbitMQ delayed-exchange ne podržava otkazivanje/reschedule pojedinačnih poruka (potrebno za izmenu/otkazivanje meča), a za cron/recurring bi zahtevao dodatni Quartz sloj. Hangfire pokriva odloženo + otkazivo + recurring + perzistenciju iz jednog paketa.

---

## Frontend odluke

### F1 — Angular 19: Standalone komponente, OnPush, Signali

Sve komponente su `standalone: true`. Nema `NgModule`-a osim u `AppModule` bootstrap tački.

Obavezno na svakom komponentu:
```typescript
changeDetection: ChangeDetectionStrategy.OnPush
```

State se izražava kroz Angular Signale (`signal()`, `computed()`, `effect()`). `async` pipe se ne koristi tamo gde signal može da zameni.

---

### F2 — Angular state management: Signal services + NgRx SignalStore selektivno

- **Signal services** — za lokalni i feature-level state (forma, filter, paginacija)
- **NgRx SignalStore** — za složen, deljeni state koji se koristi u više komponenti (feed, aktivne utakmice, profil korisnika)

Ne koristimo NgRx Store (action/reducer/effect) — isključivo SignalStore API.

---

### F3 — UI komponente: Angular Material + Tailwind CSS

- **Angular Material 19** (M3) — za interaktivne komponente: forme, dugmad, dijalozi, snackbar-ovi, tabele, navigation
- **Tailwind CSS v3** — za layout, spacing, tipografiju, utility stilove (v4 nije kompatibilan sa Angular SCSS pipeline-om)
- **Tailwind `tw-` prefiks** — sprečava koliziju sa Angular Material CSS klasama
- **Custom M3 tema** — implementira se uz prvi UI feature (skeleton koristi prebuilt `azure-blue.css`)

Docker Compose fajlovi su u `infra/docker/`, dokumentacija u `docs/`.

---

### F4 — Forme: Reactive Forms

Sve forme koriste `ReactiveFormsModule`. Template-driven forme se ne koriste. Forme se tipiziraju sa `FormGroup<T>` (striktno tipizovanje u Angular 19).

---

### F5 — Folder struktura Angular aplikacije

```
src/
  app/
    core/
      auth/           ← auth guard, interceptori, token servis
      http/           ← base HTTP servis, error interceptor
      layout/         ← shell, navigation, header
    shared/
      components/     ← reusable UI komponente
      pipes/
      directives/
      models/         ← shared DTO interfejsi
    features/
      auth/           ← login, register
      profile/        ← moj profil, tuđi profil
      matches/        ← utakmice, kreiranje, detalji
      competitions/   ← takmičenja
      facilities/     ← tereni
      feed/           ← feed, objave
      ...
  environments/
```

Svaki feature folder:
```
feature-name/
  components/
  services/
  store/          ← (ako se koristi NgRx SignalStore)
  models/
  feature.routes.ts
```

---

### F6 — HTTP i API komunikacija

- Sav HTTP saobraćaj ide ka Gateway/BFF (jedan base URL)
- `HttpClient` sa interceptorima: auth (dodaje token), error (mapira u domenski error), correlation-id
- Svaki feature ima svoj servis za HTTP pozive — nema direktnih HttpClient poziva u komponentama

---

## Monorepo odluke

### M1 — Struktura repozitorijuma

```
/
  src/
    services/
      auth/
        Comeback.Auth.Domain/
        Comeback.Auth.Application/
        Comeback.Auth.Infrastructure/
        Comeback.Auth.Api/
      profile/
        Comeback.Profile.Domain/
        Comeback.Profile.Application/
        Comeback.Profile.Infrastructure/
        Comeback.Profile.Api/
      match/
      competition/
      facility/
      social/
      chat/
      notification/
      rating/
      admin/
    gateway/
      Comeback.Gateway/
    building-blocks/
      Comeback.BuildingBlocks/
  web/
    angular/            ← Angular web aplikacija
  infra/
    docker/
    scripts/
  docs/                 ← dokumentacija, dijagrami
  Comeback.sln          ← jedan root solution koji referencira sve projekte
  DECISIONS.md          ← ovaj dokument
  docker-compose.yml
  docker-compose.override.yml
```

### M2 — Imenovanje .NET projekata

Konvencija: `Comeback.{ServiceName}.{Layer}`

Primeri:
- `Comeback.Auth.Domain`
- `Comeback.Auth.Application`
- `Comeback.Auth.Infrastructure`
- `Comeback.Auth.Api`
- `Comeback.Profile.Domain`
- `Comeback.Gateway`
- `Comeback.BuildingBlocks`

### M3 — Building blocks library

Deljeni .NET class library koji mikroservisi referenciraju:
- Bazne klase: `BaseEntity`, `AggregateRoot`, `DomainEvent`
- Interfejsi: `ICommand`, `IQuery`, `IRepository`
- MediatR pipeline behavior-i (deljeni)
- Middleware (CorrelationId, exception handler)
- Extensions metode

Pravilo: `building-blocks` ne sme sadržati domensku logiku ni zavisnosti od konkretnih servisa.

---

## Konvencije koda

### .NET

| Stvar | Konvencija |
|---|---|
| Klase, interfejsi | `PascalCase` |
| Privatna polja | `_camelCase` |
| Lokalne varijable | `camelCase` |
| Async metode | Sufiks `Async` |
| Komande | `CreateMatchCommand`, `InvitePlayerCommand` |
| Upiti | `GetMatchByIdQuery`, `GetPlayerProfileQuery` |
| Handler-i | `CreateMatchCommandHandler` |
| Events (domenski) | `MatchCreatedEvent` |
| Events (integracioni) | `MatchCreatedIntegrationEvent` |

### Angular

| Stvar | Konvencija |
|---|---|
| Fajlovi | `kebab-case.component.ts` |
| Klase | `PascalCase` |
| Signal-i | `camelCase` (suffix po potrebi, npr. `matches$` za observable kompatibilnost) |
| Interfejsi/DTO | Prefiks `I` se **ne koristi** — `PlayerProfile`, `MatchSummary` |
| Store-ovi | `MatchStore`, `ProfileStore` |

---

## Otvorene odluke (za kasniju fazu)

- [ ] **Mobilna aplikacija**: Ionic + Angular ili React Native (odlučuje se kada krećemo sa mobilnim)
- [ ] **Deployment**: cloud provajder (Azure, AWS, VPS) — odlučuje se pred deploy
- [ ] **CI/CD**: GitHub Actions (verovatno) — detalji kad se postavi pipeline
- [ ] **Elasticsearch vs OpenSearch** — odlučuje se pri implementaciji pretraga

---

*Poslednja izmena: 13. jun 2026.*
