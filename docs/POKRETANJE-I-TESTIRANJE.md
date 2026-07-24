# Comeback — Lokalno pokretanje i testiranje

> **Napomena o hostovanju.** Želeli smo da rešenje postavimo na javni server sa
> sopstvenim domenom, ali se to u ovom trenutku pokazalo kao finansijski
> preskupo (najjeftiniji dostupan server bio je ~35 €/mesečno). Zbog toga se
> aplikacija pokreće i demonstrira **lokalno**, po uputstvu ispod. Sav backend
> radi u Docker kontejnerima, pa je pokretanje jednostavno i ponovljivo.

---

## 1. Preduslovi

Instaliraj:

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (mora biti **pokrenut**)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) — za pokretanje testova
- [Node.js 20+](https://nodejs.org/) i npm — za frontend
- (opciono) [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet):
  `dotnet tool install --global dotnet-ef`

Provera da je sve na mestu:

```bash
docker --version
dotnet --version
node --version
```

---

## 2. Backend (Docker)

Ceo backend (7 baza, RabbitMQ, Redis, Seq, MailDev, 8 .NET servisa) diže se
jednom komandom:

```bash
cd infra/docker
docker compose up -d --build
```

- Prvo pokretanje traje par minuta (build image-a).
- **Migracije baza se izvršavaju automatski** kad servisi startuju — ništa se ne
  radi ručno.
- **Admin nalog se automatski kreira** (vidi sekciju 5).

### (Opciono) Cloudinary za upload slika

Upload profilnih/meč slika koristi Cloudinary. Bez njega aplikacija radi, ali
upload slike neće raditi. Da ga uključiš:

```bash
cd infra/docker
cp .env.example .env
# otvori .env i popuni CLOUDINARY_CLOUD_NAME / API_KEY / API_SECRET
docker compose up -d
```

Ostale tajne (lozinke baza, JWT) imaju dev-podrazumevane vrednosti u
`docker-compose.yml`, pa ih ne moraš dirati.

### Provera da su servisi podignuti

```bash
docker compose ps          # svi treba da su "running"/"healthy"
docker compose logs -f gateway auth-api
```

---

## 3. Frontend (Angular)

U novom terminalu:

```bash
cd web/comeback-web
npm install
npm start
```

Aplikacija se otvara na **http://localhost:4200** i priča sa backendom preko
API Gateway-a na `http://localhost:5000` (već podešeno u
`src/environments/environment.ts`).

---

## 4. Pristupne tačke (portovi)

| Servis | URL | Namena |
|---|---|---|
| **Frontend** | http://localhost:4200 | Aplikacija |
| **API Gateway** | http://localhost:5000 | Ulaz za sve API pozive |
| Auth API | http://localhost:5104 | Swagger po servisu |
| Profile API | http://localhost:5210 | |
| Rating API | http://localhost:5310 | |
| Notification API | http://localhost:5410 | |
| Match API | http://localhost:5510 | |
| Chat API | http://localhost:5610 | |
| Social API | http://localhost:5420 | |
| **MailDev (inbox)** | http://localhost:1080 | Pregled poslatih mejlova |
| **RabbitMQ** | http://localhost:15672 | Message broker UI |
| **Seq (logovi)** | http://localhost:5342 | Centralizovani logovi |

Kredencijali za RabbitMQ i baze: `comeback` / `comeback_dev`.

---

## 5. Test nalozi

**Admin** se automatski kreira pri prvom pokretanju Auth servisa:

| Polje | Vrednost |
|---|---|
| Email | `d7petrovic@gmail.com` |
| Lozinka | `Test!234` |

Obične korisnike napravi kroz **registraciju** u aplikaciji (verifikacioni mejl
stiže u **MailDev**, http://localhost:1080 — klikni link iz mejla).

---

## 6. Ručno testiranje funkcionalnosti

Predlog redosleda za demo/proveru:

1. **Registracija i verifikacija** — registruj korisnika → otvori MailDev
   (`:1080`) → klikni verifikacioni link.
2. **Login** — prijava sa verifikovanim nalogom.
3. **Profil** — popuni profil, dodaj **nacionalnost** (prikaz zastavice),
   upload slike (traži Cloudinary), pogledaj **liste pratilaca**.
4. **Rating** — proveri XP/ELO prikaz.
5. **Mečevi** — kreiraj meč, objavi **javni poziv "tražim igrača"** koji se
   pojavljuje na feed-u.
6. **Feed / objave (Social)** — kreiraj objavu, javni pozivi vidljivi u feed-u.
7. **Chat** — pokreni **grupni chat**, šalji/**briši poruke** (poruke uživo
   preko SignalR-a).
8. **Notifikacije** — akcije drugih korisnika stižu kao **žive notifikacije**
   (SignalR, tip + payload).

> Za testiranje "uživo" funkcija (chat/notifikacije) otvori aplikaciju u dva
> browsera / dva naloga paralelno.

---

## 7. Automatski testovi

Unit i integracioni testovi po servisima (auth, match, profile, rating, social):

```bash
# svi testovi
dotnet test

# pojedinačan projekat, npr:
dotnet test tests/services/auth/Comeback.Auth.Application.Tests
```

> Integracioni testovi koriste Testcontainers, pa Docker mora biti pokrenut.

---

## 8. Posle izmena koda

Backend se **ne** osvežava sam — rebuild servisa koji si menjao:

```bash
cd infra/docker
docker compose build profile-api && docker compose up -d profile-api
```

Frontend (`npm start`) se osvežava automatski (hot reload).

---

## 9. Zaustavljanje i čišćenje

```bash
cd infra/docker

docker compose down          # zaustavi sve (podaci ostaju)
docker compose down -v       # zaustavi + OBRIŠI sve baze/podatke (čist start)
```

---

## Kratak pregled arhitekture

```
Angular (4200)
     │  HTTP + WebSocket (SignalR)
     ▼
API Gateway (5000, YARP)
     ├─ Auth          ─ Postgres
     ├─ Profile       ─ Postgres    ┐
     ├─ Rating        ─ Postgres    │  komunikacija i preko
     ├─ Match         ─ Postgres    ├─ RabbitMQ (event bus)
     ├─ Chat          ─ Postgres    │
     ├─ Notification  ─ Postgres    ┘
     └─ Social        ─ Postgres + Redis (cache)

Prateći alati: Seq (logovi), MailDev (mejlovi), Cloudinary (slike, eksterno)
```
