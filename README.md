# Comeback

Digitalna platforma za rekreativni fudbal.

> Detaljno uputstvo za pokretanje i testiranje (portovi, ručni scenariji,
> automatski testovi) nalazi se u **[`docs/POKRETANJE-I-TESTIRANJE.md`](docs/POKRETANJE-I-TESTIRANJE.md)**.

## Preduslovi

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (pokrenut)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) — za testove
- [Node.js 20+](https://nodejs.org/) i npm

---

## Pokretanje

### 1. Backend (Docker)

```bash
cd infra/docker
docker compose up -d --build
```

Ovim se pokreću sve zavisnosti i servisi. **Migracije baza se izvršavaju automatski**
pri startu svakog servisa — ne pokreću se ručno. Takođe se **automatski kreira
admin nalog** (`admin@comeback.com` / `Test!234`).

| Servis / alat | URL |
|---|---|
| **Frontend** (pokreće se zasebno — korak 3) | http://localhost:4200 |
| **API Gateway** | http://localhost:5000 |
| Auth API | http://localhost:5104 |
| Profile API | http://localhost:5210 |
| Rating API | http://localhost:5310 |
| Notification API | http://localhost:5410 |
| Social API | http://localhost:5420 |
| Match API | http://localhost:5510 |
| Chat API | http://localhost:5610 |
| RabbitMQ Management | http://localhost:15672 |
| MailDev (inbox) | http://localhost:1080 |
| Seq (logovi) | http://localhost:5342 |

> Kredencijali RabbitMQ / baze: `comeback` / `comeback_dev`

### 2. Cloudinary (skladištenje medija) — opciono

**Svi mediji (slike/video) se čuvaju na Cloudinary-ju** — frontend šalje fajl
direktno, uz potpis sa backenda; u bazi ostaje samo URL.

**Aplikacija radi i bez Cloudinary-ja** — pokreće se normalno i sve funkcioniše
osim otpremanja medija. Funkcije koje zahtevaju Cloudinary nalog za testiranje:

- **Slika profila (avatar)** — Profil → *Izmeni profil*
- **Slika grupe** — Grupe → *Napravi / Izmeni grupu*
- **Galerija meča** — Meč → *Mediji* (slike/video)

Za uključivanje otpremanja medija potrebno je napraviti besplatan Cloudinary
nalog i popuniti kredencijale:

```bash
cd infra/docker
cp .env.example .env
# u .env se popunjavaju CLOUDINARY_CLOUD_NAME / API_KEY / API_SECRET
docker compose up -d profile-api match-api
```

> **Detaljno uputstvo** (pravljenje naloga, gde se navedeni podaci nalaze i gde
> se upisuju) je u [`docs/POKRETANJE-I-TESTIRANJE.md`](docs/POKRETANJE-I-TESTIRANJE.md),
> sekcija **2b**.
>
> Za demo/evaluaciju najlakše je koristiti jedan namenski privremeni besplatan
> nalog i podeliti njegove kredencijale uz projekat. Ako je repozitorijum
> javan, tajna se ne unosi u kod, već se prosleđuje uz `.env`.

### 3. Frontend

```bash
cd web/comeback-web
npm install
npm start
```

Aplikacija se otvara na **http://localhost:4200**.

### 4. Demo podaci (opciono)

Kada je sistem pokrenut, pomoćni alat popunjava aplikaciju demo podacima
(16 korisnika, grupe, odigrani mečevi sa rezultatima, feed sa lajkovima i
komentarima):

```bash
dotnet run --project tools/Comeback.DemoSeeder
```

Prvo pokretanje traje ~5 minuta (registracije prolaze kroz stvarnu verifikaciju
mejla preko MailDev-a); ponovno pokretanje je bezbedno i traje ispod minuta.
Demo prijava:
`marko.petrovic@demo.comeback.com` / `Test!234`. Detalji u
[`docs/POKRETANJE-I-TESTIRANJE.md`](docs/POKRETANJE-I-TESTIRANJE.md), sekcija 5.

---

## Testovi

```bash
dotnet test
```

Integracioni testovi koriste Testcontainers, pa Docker mora biti pokrenut.

---

## Dokumentacija koda

Backend je dokumentovan Doxygen komentarima. Generisanje (zahteva `doxygen` i
`python` na PATH-u):

```bash
doxygen Doxyfile
```

Generisana dokumentacija se zatim otvara iz `docs/api/html/index.html`. Detalji su u
[`docs/POKRETANJE-I-TESTIRANJE.md`](docs/POKRETANJE-I-TESTIRANJE.md), sekcija **8**.

---

## Arhitektura sistema

Sistem je organizovan kao skup mikroservisa (.NET 8) iza API Gateway-a, sa
Angular aplikacijom kao jedinim klijentom. Dijagrami u nastavku su u Mermaid
formatu i GitHub ih prikazuje automatski.

### Komponentni dijagram

Svaki mikroservis ima sopstvenu PostgreSQL bazu na koju se direktno vezuje
(*database-per-service*; baze se ne dele). Servisi komuniciraju sinhrono (HTTP,
pune strelice) i asinhrono preko RabbitMQ integracionih događaja (isprekidane
strelice, MassTransit + transakcioni outbox). Sve serverske komponente se
pokreću kroz Docker Compose (`infra/docker`).

Mediji se otpremaju iz browsera direktno na Cloudinary, ali tek nakon što
nadležni servis (Profile ili Match) izda kriptografski potpis za otpremanje —
u bazi se čuva samo URL medija.

```mermaid
flowchart TB
    subgraph Klijent
        SPA["Angular 19 SPA<br/>localhost:4200"]
    end

    GW["API Gateway — YARP + BFF<br/>localhost:5000"]

    subgraph Backend["Backend — mikroservisi (.NET 8)"]
        subgraph P1[ ]
            AUTH["Auth API<br/>:5104"] --- AUTHDB[("Auth DB")]
        end
        subgraph P2[ ]
            PROFILE["Profile API<br/>:5210"] --- PROFILEDB[("Profile DB")]
        end
        subgraph P3[ ]
            RATING["Rating API<br/>:5310"] --- RATINGDB[("Rating DB")]
        end
        subgraph P4[ ]
            MATCH["Match API<br/>:5510"] --- MATCHDB[("Match DB")]
        end
        subgraph P5[ ]
            CHAT["Chat API<br/>:5610"] --- CHATDB[("Chat DB")]
        end
        subgraph P6[ ]
            NOTIF["Notification API<br/>:5410"] --- NOTIFDB[("Notification DB")]
        end
        subgraph P7[ ]
            SOCIAL["Social API<br/>:5420"] --- SOCIALDB[("Social DB")]
        end

        MQ["RabbitMQ"]
        REDIS[("Redis keš")]
    end

    style P1 fill:none,stroke:none
    style P2 fill:none,stroke:none
    style P3 fill:none,stroke:none
    style P4 fill:none,stroke:none
    style P5 fill:none,stroke:none
    style P6 fill:none,stroke:none
    style P7 fill:none,stroke:none

    MAIL["SMTP server<br/>(MailDev u razvoju)"]
    SEQ["Seq<br/>centralizovani logovi"]
    CLD["Cloudinary<br/>skladište medija"]

    SPA -->|"REST + SignalR (WebSocket)"| GW
    GW --> AUTH & PROFILE & RATING & MATCH & CHAT & NOTIF & SOCIAL

    MATCH -->|HTTP| PROFILE
    MATCH -->|HTTP| RATING
    SOCIAL -->|HTTP| PROFILE
    SOCIAL -->|HTTP| MATCH
    CHAT -->|HTTP| PROFILE
    NOTIF -->|HTTP| PROFILE

    AUTH -.->|objavljuje događaje| MQ
    PROFILE -.->|objavljuje događaje| MQ
    MATCH -.->|objavljuje događaje| MQ
    MQ -.->|isporučuje događaje| PROFILE & RATING & SOCIAL & NOTIF

    SOCIAL --- REDIS
    NOTIF -->|SMTP| MAIL
    Backend -.->|Serilog| SEQ
    SPA -.->|"otpremanje medija (uz potpis backenda)"| CLD
    PROFILE & MATCH -.->|"izdavanje potpisa i administracija medija"| CLD
```

### Dijagram paketa servisa

Svaki mikroservis se sastoji od četiri projekta (paketa) organizovana po
principima Clean Architecture: `Comeback.<Servis>.Api`, `.Application`,
`.Domain` i `.Infrastructure`, uz zajednički paket `Comeback.BuildingBlocks`.
Strelica na dijagramu označava zavisnost („zavisi od"). Jedini izuzetak je
Notification servis, koji nema poseban Domain projekat.

```mermaid
flowchart TB
    API["Api<br/>Minimal API endpointi, SignalR hubovi, Swagger"]
    APP["Application<br/>CQRS — MediatR komande i upiti, DTO-ovi, FluentValidation"]
    DOM["Domain<br/>entiteti, enumi, domenski događaji"]
    INF["Infrastructure<br/>EF Core + PostgreSQL, MassTransit, HTTP klijenti, migracije"]
    BB["Comeback.BuildingBlocks<br/>primitivi (Entity, AggregateRoot), integracioni događaji, middleware"]

    API --> APP --> DOM
    API --> INF
    INF --> APP
    INF --> DOM
    APP & INF --> BB
```

### Model podataka (klasni dijagrami po servisu)

Model podataka je prikazan klasnim dijagramima, po jedan za svaki servis.
Kompozicija (linija sa rombom) označava agregat čiji delovi nastaju i nestaju
sa celinom, obična asocijacija vezu ostvarenu fizičkim stranim ključem, a
isprekidana linija logičku vezu bez fizičkog stranog ključa. Svaki servis je
zaseban bounded context sa sopstvenom bazom, pa između servisa **ne postoje
fizički strani ključevi** — među-servisne veze su logičke, preko
identifikatora (`UserId`, `MatchId`, `GroupId`). Sufiks `?` u tipu polja
označava opciono polje. Detalji o ključevima i ograničenjima jedinstvenosti
navedeni su u tekstu uz svaki dijagram; tipovi poput `MatchStatus` su
enumeracije definisane u Domain sloju. Radi čitljivosti prikazana su ključna
polja, ne sva.

#### Auth

```mermaid
classDiagram
    class ApplicationUser {
        +Guid Id
        +string UserName
        +string Email
        +UserRole Role
        +AccountStatus AccountStatus
        +DateTime CreatedAt
    }
    class RefreshToken {
        +Guid Id
        +Guid UserId
        +string Token
        +DateTime ExpiresAt
        +string CreatedByIp
        +DateTime? RevokedAt
    }
    ApplicationUser "1" -- "0..*" RefreshToken : poseduje
```

Polje `RefreshToken.UserId` je strani ključ ka `ApplicationUser`, a vrednost
`Token` je jedinstvena. Enumeracije: `UserRole` (Player, Organizer, Admin);
`AccountStatus` (PendingEmailVerification, Active, Suspended, Deactivated).

#### Profile

```mermaid
classDiagram
    class UserProfile {
        +Guid Id
        +Guid UserId
        +string Username
        +string Email
        +string FirstName
        +string LastName
        +DateOnly DateOfBirth
        +Position PreferredPosition
        +bool CanPlayGoalkeeper
        +int YouthSeasons
        +int SeniorSeasons
        +string? Nationality
        +string? Bio
        +string? AvatarUrl
        +SkillLevel? SkillLevel
    }
    class PlayerGroup {
        +Guid Id
        +string Name
        +string? AvatarUrl
        +DateTime CreatedAt
    }
    class PlayerGroupMember {
        +Guid Id
        +Guid GroupId
        +Guid ProfileId
        +GroupMemberRole Role
        +DateTime JoinedAt
    }
    class PlayerFollow {
        +Guid Id
        +Guid FollowerUserId
        +Guid FollowedUserId
        +DateTime CreatedAt
    }
    PlayerGroup "1" *-- "0..*" PlayerGroupMember : članovi
    UserProfile "1" .. "0..*" PlayerGroupMember : učlanjen
    UserProfile "1" .. "0..*" PlayerFollow : prati / praćen
```

Polje `UserProfile.UserId` je jedinstveno i predstavlja logičku vezu ka nalogu
u Auth servisu. Kombinacija (`GroupId`, `ProfileId`) člana grupe je
jedinstvena, kao i kombinacija (`FollowerUserId`, `FollowedUserId`) praćenja.
Enumeracije: `Position` (Goalkeeper, Defender, Midfielder, Forward);
`SkillLevel` (Beginner, Intermediate, Advanced, Professional);
`GroupMemberRole` (Member, Captain).

#### Match

```mermaid
classDiagram
    class Match {
        +Guid Id
        +string Title
        +MatchType Type
        +MatchStatus Status
        +Guid OrganizerUserId
        +string? Location
        +DateTime StartsAt
        +int? DurationMinutes
        +int PlayersPerTeam
        +int MaxSubstitutes
        +int? HomeScore
        +int? AwayScore
        +Guid? GroupId
        +Guid? OpponentGroupId
        +Guid? SecondOrganizerUserId
    }
    class MatchParticipant {
        +Guid Id
        +Guid MatchId
        +Guid UserId
        +string DisplayName
        +bool IsOrganizer
        +MatchParticipantStatus Status
        +MatchTeam Team
        +bool IsCaptain
        +bool IsGuest
    }
    class MatchGoal {
        +Guid Id
        +Guid MatchId
        +Guid ScorerUserId
        +MatchTeam ScoringTeam
        +bool IsOwnGoal
        +Guid? AssistUserId
    }
    class MatchPlayerReview {
        +Guid Id
        +Guid MatchId
        +Guid ReviewerParticipantId
        +Guid ReviewedParticipantId
        +decimal OverallRating
        +decimal? GoalkeepingRating
        +decimal? DefenseRating
        +decimal? AttackRating
        +decimal? EffortRating
        +string? Comment
    }
    class MatchMedia {
        +Guid Id
        +Guid MatchId
        +Guid UploadedByUserId
        +MatchMediaType MediaType
        +string Url
        +string? ThumbnailUrl
        +MatchMediaStatus Status
    }
    Match "1" *-- "0..*" MatchParticipant : učesnici
    Match "1" *-- "0..*" MatchGoal : golovi
    Match "1" .. "0..*" MatchPlayerReview : recenzije
    Match "1" .. "0..*" MatchMedia : galerija
    MatchParticipant "1" .. "0..*" MatchPlayerReview : recenzent / recenziran
```

Učesnici i golovi su deo agregata meča i vezani su fizičkim stranim ključem;
recenzije i mediji referenciraju meč logički. Kombinacija (`MatchId`, `UserId`)
učesnika je jedinstvena, kao i trojka (`MatchId`, `ReviewerParticipantId`,
`ReviewedParticipantId`) recenzije. Enumeracije: `MatchType` (Independent,
GroupMatch, GroupVsGroup); `MatchStatus` (Scheduled, ResultOverdue,
ResultSubmitted, ResultConfirmed, Missed, Cancelled);
`MatchParticipantStatus` (Invited, Accepted, Declined, Withdrawn, Removed);
`MatchTeam` (None, Home, Away); `MatchMediaType` (Image, Video);
`MatchMediaStatus` (Uploaded, Processing, Active, Hidden, Removed, Rejected).

#### Chat

```mermaid
classDiagram
    class Conversation {
        +Guid Id
        +ConversationType Type
        +Guid? GroupId
        +string? Title
        +string? GroupAvatarUrl
        +DateTime CreatedAt
    }
    class ConversationMember {
        +Guid Id
        +Guid ConversationId
        +Guid UserId
        +string DisplayName
        +DateTime JoinedAt
        +DateTime? LastReadAt
        +DateTime? ClearedAt
    }
    class Message {
        +Guid Id
        +Guid ConversationId
        +Guid SenderUserId
        +string SenderDisplayName
        +string EncryptedContent
        +DateTime SentAt
    }
    class HiddenMessage {
        +Guid Id
        +Guid UserId
        +Guid MessageId
        +DateTime HiddenAt
    }
    Conversation "1" *-- "0..*" ConversationMember : članovi
    Conversation "1" *-- "0..*" Message : poruke
    Message "1" .. "0..*" HiddenMessage : sakrivanja
```

Polje `Conversation.GroupId` je logička veza ka grupi u Profile servisu i
jedinstveno je kada postoji. Kombinacija (`ConversationId`, `UserId`) člana je
jedinstvena, kao i kombinacija (`UserId`, `MessageId`) sakrivene poruke.
Sadržaj poruka se čuva šifrovan (AES). Enumeracije: `ConversationType`
(Direct, Group).

#### Rating

```mermaid
classDiagram
    class PlayerXp {
        +Guid Id
        +Guid UserId
        +int CareerXp
        +int MatchXp
        +int YouthSeasons
        +int SeniorSeasons
    }
    class AwardedMatchXp {
        +Guid MatchId
        +Guid UserId
        +DateTime AwardedAt
    }
    PlayerXp "1" .. "0..*" AwardedMatchXp : evidencija dodela
```

Polje `PlayerXp.UserId` je jedinstveno. Entitet `AwardedMatchXp` ima kompozitni
primarni ključ (`MatchId`, `UserId`), čime se obezbeđuje da se XP za isti meč
i istog igrača dodeli najviše jednom. Polja `TotalXp` i `Level` su izvedena
(računaju se iz `CareerXp` i `MatchXp`), pa se ne čuvaju u bazi.

#### Social

```mermaid
classDiagram
    class Post {
        +Guid Id
        +PostType Type
        +Guid MatchId
        +string MatchTitle
        +int HomeScore
        +int AwayScore
        +Guid OrganizerUserId
        +string? Position
        +string? Location
        +DateTime? StartsAt
        +DateTime CreatedAt
    }
    class PostParticipant {
        +Guid Id
        +Guid PostId
        +Guid UserId
        +string DisplayName
    }
    class PostComment {
        +Guid Id
        +Guid PostId
        +Guid AuthorUserId
        +string Content
        +DateTime CreatedAt
    }
    class PostLike {
        +Guid Id
        +Guid PostId
        +Guid UserId
        +DateTime CreatedAt
    }
    class UserFeedItem {
        +Guid Id
        +Guid UserId
        +Guid PostId
        +DateTime CreatedAt
    }
    Post "1" *-- "0..*" PostParticipant : učesnici
    Post "1" *-- "0..*" PostComment : komentari
    Post "1" *-- "0..*" PostLike : lajkovi
    Post "1" .. "0..*" UserFeedItem : stavke feed-a
```

Polje `Post.MatchId` je logička veza ka meču u Match servisu; kombinacija
(`MatchId`, `Type`) je jedinstvena. Jedinstvene su i kombinacije (`PostId`,
`UserId`) lajka i (`UserId`, `PostId`) stavke feed-a. Entitet `UserFeedItem`
je denormalizovani model čitanja (*fan-out* feed-a po korisniku). Enumeracije:
`PostType` (MatchResult, PlayerWanted).

#### Notification

```mermaid
classDiagram
    class InAppNotification {
        +Guid Id
        +Guid RecipientUserId
        +string Type
        +string Title
        +string Body
        +string? Payload
        +bool IsRead
        +DateTime CreatedAt
        +DateTime? ReadAt
    }
```

Polje `RecipientUserId` je logička veza ka korisniku, a `Payload` sadrži
dodatne podatke notifikacije u JSON formatu.

### Ključni tokovi (sekvencijalni dijagrami)

#### Registracija i verifikacija emaila

Pune strelice su sinhroni pozivi, a isprekidane povratne poruke — odgovor se
uvek vraća istim lancem kojim je zahtev stigao. Pošto se događaji objavljuju
asinhrono (transakcioni outbox), od trenutka kada Auth servis završi obradu
tok se deli na dve paralelne grane (`par` fragment): vraćanje odgovora
korisniku i obradu objavljenog događaja.

```mermaid
sequenceDiagram
    autonumber
    actor U as Korisnik
    participant SPA as Angular SPA
    participant GW as Gateway (YARP)
    participant A as Auth API
    participant MQ as RabbitMQ
    participant N as Notification API
    participant P as Profile API

    activate U
    U->>+SPA: popunjava formu za registraciju
    SPA->>+GW: POST /api/auth/register
    GW->>+A: prosleđuje zahtev
    A->>A: kreira nalog (status PendingEmailVerification)
    par odgovor korisniku
        A-->>GW: 200 OK
        GW-->>SPA: 200 OK
        SPA-->>U: obaveštenje o proveri mejla
    and obrada događaja
        A--)MQ: objavljuje EmailVerificationRequested
        activate MQ
        MQ--)N: isporučuje EmailVerificationRequested
        deactivate MQ
        activate N
        N--)U: mejl sa verifikacionim linkom (SMTP)
        deactivate N
    end
    deactivate A
    deactivate GW
    deactivate SPA

    U->>+SPA: otvara verifikacioni link
    SPA->>+GW: POST /api/auth/complete-registration
    GW->>+A: prosleđuje zahtev
    A->>A: potvrđuje mejl, nalog postaje Active
    par odgovor korisniku
        A-->>GW: 200 OK
        GW-->>SPA: 200 OK
        SPA-->>U: registracija završena, prijava moguća
    and obrada događaja
        A--)MQ: objavljuje UserEmailConfirmed
        activate MQ
        MQ--)P: isporučuje UserEmailConfirmed
        deactivate MQ
        activate P
        P->>P: kreira UserProfile
        deactivate P
    end
    deactivate A
    deactivate GW
    deactivate SPA
    deactivate U
```

#### Unos rezultata meča

Nakon upisa rezultata tok se deli na dve paralelne grane: vraćanje odgovora
korisniku i obradu objavljenog događaja `MatchResultSubmitted`, koji RabbitMQ
isporučuje trima pretplaćenim servisima — svaki od njih ga obrađuje nezavisno
(ugnežđeni `par` fragment).

```mermaid
sequenceDiagram
    autonumber
    actor O as Organizator
    participant SPA as Angular SPA
    participant GW as Gateway (YARP)
    participant MA as Match API
    participant MQ as RabbitMQ
    participant R as Rating API
    participant S as Social API
    participant N as Notification API

    activate O
    O->>+SPA: unosi rezultat i strelce
    SPA->>+GW: POST /api/matches/{id}/result
    GW->>+MA: prosleđuje zahtev
    MA->>MA: validira i upisuje rezultat (status ResultSubmitted)
    par odgovor korisniku
        MA-->>GW: 204 No Content
        GW-->>SPA: 204 No Content
        SPA-->>O: prikaz potvrde
    and obrada događaja
        MA--)MQ: objavljuje MatchResultSubmitted
        activate MQ
        par Rating
            MQ--)R: isporučuje MatchResultSubmitted
            activate R
            R->>R: obračunava i dodeljuje XP učesnicima
            deactivate R
        and Social
            MQ--)S: isporučuje MatchResultSubmitted
            activate S
            S->>+MA: GET detalji meča (HTTP)
            MA-->>-S: učesnici i rezultat
            S->>S: kreira Post i puni feed pratilaca
            deactivate S
        and Notification
            MQ--)N: isporučuje MatchResultSubmitted
            activate N
            N->>N: upisuje in-app notifikacije
            N--)O: SignalR push učesnicima
            deactivate N
        end
        deactivate MQ
    end
    deactivate MA
    deactivate GW
    deactivate SPA
    deactivate O
```

---

## Struktura projekta

```
src/
  building-blocks/    # Deljeni kod (integracioni događaji, izuzeci, middleware, Cloudinary)
  gateway/            # API Gateway + BFF (YARP, port 5000)
  services/
    auth/             # Autentifikacija i nalozi
    profile/          # Profili igrača, nacionalnosti, pratioci
    rating/           # XP i ELO rejting
    match/            # Mečevi i javni pozivi ("tražim igrača")
    chat/             # Grupni chat (SignalR)
    notification/     # Email + žive notifikacije (SignalR)
    social/           # Feed i objave (+ Redis keš)

web/
  comeback-web/       # Angular 19 aplikacija

infra/
  docker/             # docker-compose.yml (+ .env.example)

tools/
  Comeback.DemoSeeder/  # Alat za punjenje aplikacije demo podacima

docs/                 # Uputstvo za pokretanje i generisana dokumentacija koda

tests/
  services/           # Unit i integracioni testovi po servisu
  e2e/                # End-to-end testovi
```

## Nakon izmena koda

Backend servisi se ne ažuriraju automatski — potrebno je ponovo ih izgraditi:

```bash
cd infra/docker
docker compose build <ime-servisa> && docker compose up -d <ime-servisa>
```

Na primer: `docker compose build profile-api && docker compose up -d profile-api`
