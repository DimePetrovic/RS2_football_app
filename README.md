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

Ovo diže sve zavisnosti i servise. **Migracije baza se izvršavaju automatski**
pri startu svakog servisa — ne pokreću se ručno. Takođe se **automatski kreira
admin nalog** (`d7petrovic@gmail.com` / `Test!234`).

| Servis / alat | URL |
|---|---|
| **Frontend** | http://localhost:4200 |
| **API Gateway** | http://localhost:5000 |
| Auth API | http://localhost:5104 |
| Profile API | http://localhost:5210 |
| Rating API | http://localhost:5310 |
| Notification API | http://localhost:5410 |
| Match API | http://localhost:5510 |
| Chat API | http://localhost:5610 |
| Social API | http://localhost:5420 |
| RabbitMQ Management | http://localhost:15672 |
| MailDev (inbox) | http://localhost:1080 |
| Seq (logovi) | http://localhost:5342 |

> Kredencijali RabbitMQ / baze: `comeback` / `comeback_dev`

### 2. Cloudinary (skladištenje medija) — opciono

**Svi mediji (slike/video) se čuvaju na Cloudinary-ju** — frontend šalje fajl
direktno, uz potpis sa backenda; u bazi ostaje samo URL.

**Aplikacija radi i bez Cloudinary-ja** — pokreće se normalno i sve funkcioniše
osim otpremanja medija. Funkcije koje traže Cloudinary nalog za testiranje:

- **Slika profila (avatar)** — Profil → *Izmeni profil*
- **Slika grupe** — Grupe → *Napravi / Izmeni grupu*
- **Galerija meča** — Meč → *Mediji* (slike/video)

Da uključiš upload, napravi besplatan Cloudinary nalog i popuni kredencijale:

```bash
cd infra/docker
cp .env.example .env
# u .env popuni CLOUDINARY_CLOUD_NAME / API_KEY / API_SECRET
docker compose up -d profile-api match-api
```

> **Detaljno uputstvo** (pravljenje naloga, gde se nalaze ta tri podatka, gde se
> upisuju) je u [`docs/POKRETANJE-I-TESTIRANJE.md`](docs/POKRETANJE-I-TESTIRANJE.md),
> sekcija **2b**.
>
> Za demo/evaluaciju najlakše je koristiti jedan namenski (throwaway) besplatan
> nalog i podeliti njegove kredencijale uz projekat. Ako je repo javan, ne
> commituj tajnu u kod — prosledi je uz `.env`.

### 3. Frontend

```bash
cd web/comeback-web
npm install
npm start
```

Aplikacija se otvara na **http://localhost:4200**.

---

## Testovi

```bash
dotnet test
```

Integracioni testovi koriste Testcontainers, pa Docker mora biti pokrenut.

---

## Dokumentacija koda

Backend je dokumentovan Doxygen komentarima. Generisanje (traži `doxygen` i
`python` na PATH-u):

```bash
doxygen Doxyfile
```

Otvori zatim `docs/api/html/index.html`. Detalji su u
[`docs/POKRETANJE-I-TESTIRANJE.md`](docs/POKRETANJE-I-TESTIRANJE.md), sekcija **8**.

---

## Struktura projekta

```
src/
  building-blocks/    # Deljeni kod (eventi, izuzeci, pipeline, Cloudinary)
  gateway/            # API Gateway + BFF (YARP, port 5000)
  services/
    auth/             # Autentifikacija i nalozi
    profile/          # Profili igrača, nacionalnosti, pratioci
    rating/           # XP i ELO rejting
    match/            # Mečevi i javni pozivi ("tražim igrača")
    chat/             # Grupni chat (SignalR)
    notification/     # Email + žive notifikacije (SignalR)
    social/           # Feed i objave (+ Redis cache)

web/
  comeback-web/       # Angular 19 aplikacija

infra/
  docker/             # docker-compose.yml (+ .env.example)

tests/
  services/           # Unit i integracioni testovi po servisu
```

## Nakon izmena koda

Backend servisi se ne ažuriraju automatski — potrebno je rebuild:

```bash
cd infra/docker
docker compose build <ime-servisa> && docker compose up -d <ime-servisa>
```

Na primer: `docker compose build profile-api && docker compose up -d profile-api`
