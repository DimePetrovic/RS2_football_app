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

Ostale tajne (lozinke baza, JWT) imaju dev-podrazumevane vrednosti u
`docker-compose.yml`, pa ih ne moraš dirati. Jedini eksterni servis je
**Cloudinary** (skladištenje medija) — vidi sekciju 2b.

## 2b. Cloudinary — skladištenje medija (za testiranje uploada)

**Svi mediji (slike i video) u aplikaciji čuvaju se na Cloudinary-ju.** Backend
generiše potpisani zahtev, a fajl se sa frontenda šalje direktno na Cloudinary;
u bazi se čuva samo URL. Zbog toga za testiranje uploada treba **Cloudinary
nalog** (besplatan je dovoljan).

> Aplikacija se pokreće i radi i **bez** Cloudinary-ja — ne pada. Samo funkcije
> ispod neće moći da otpreme sliku dok ne uneseš kredencijale.

### Funkcije koje zahtevaju Cloudinary nalog

| Funkcija | Gde u aplikaciji | Šta se otprema |
|---|---|---|
| **Slika profila (avatar)** | Profil → *Izmeni profil* → upload avatara | Slika profila korisnika |
| **Slika grupe** | Grupe → *Napravi grupu* / *Izmeni grupu* → upload slike | Avatar grupe (isti mehanizam kao profilni) |
| **Galerija meča** | Meč → *Mediji* → dodaj slike/video | Foto/video galerija meča (batch upload) |

Sve ostalo (registracija, login, profili, nacionalnosti, mečevi bez medija,
feed, chat, notifikacije, rating) radi i bez Cloudinary-ja.

### Korak 1 — Napravi besplatan nalog

1. Idi na **https://cloudinary.com/users/register_free**.
2. Registruj se (email + lozinka, ili Google/GitHub) i **potvrdi email**.
3. Uloguj se — otvoriće se **Console / Dashboard**.

### Korak 2 — Nađi 3 podatka (kredencijale)

Na **Dashboard**-u (početna strana konzole), u kartici
**"Product Environment Credentials"** (u novijoj konzoli: **Settings ⚙️ → API Keys**)
nalaze se tačno tri vrednosti:

| Podatak | Kako izgleda | Napomena |
|---|---|---|
| **Cloud name** | npr. `dxy12abcd` | jedinstveno ime tvog "clouda" |
| **API Key** | niz cifara, npr. `123456789012345` | javni ključ |
| **API Secret** | dugačak string | **TAJNA** — klikni *Reveal* / 👁 da ga vidiš; ne deli javno |

### Korak 3 — Upiši ih lokalno

```bash
cd infra/docker
cp .env.example .env
```

Otvori fajl **`infra/docker/.env`** i popuni (bez navodnika, tačno sa Dashboard-a):

```dotenv
CLOUDINARY_CLOUD_NAME=dxy12abcd
CLOUDINARY_API_KEY=123456789012345
CLOUDINARY_API_SECRET=tvoj_api_secret_odavde
```

`docker compose` automatski učitava `.env` iz `infra/docker/` foldera.

### Korak 4 — Restartuj servise koji koriste medije

Kredencijale čitaju **profile-api** (avatar/grupe) i **match-api** (galerija meča):

```bash
docker compose up -d profile-api match-api
```

Nakon toga upload radi. Otpremljene slike možeš videti i u Cloudinary konzoli
pod **Media Library**.

> **Za predaju/evaluaciju:** najlakše je napraviti **jedan namenski (throwaway)
> besplatan nalog** samo za ovaj projekat i proslediti ta tri podatka uz projekat
> (npr. u već popunjenom `.env`), da evaluatori ne moraju da prave svoj nalog.
> Ako je repozitorijum javan, ne commituj `API Secret` u kod — prosledi `.env`
> odvojeno.

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
>
> Funkcije sa **otpremanjem slika** — avatar profila (korak 3), slika grupe i
> galerija meča — zahtevaju **Cloudinary nalog** (vidi sekciju 2b). Bez njega
> ostatak scenarija radi normalno.

---

## 7. Automatski testovi

```bash
# svi testovi
dotnet test

# pojedinačan projekat, npr:
dotnet test tests/services/auth/Comeback.Auth.Application.Tests
```

Pokriveni servisi:

| Vrsta | Servisi |
|---|---|
| Unit testovi | auth, chat, match, notification, profile, rating, social |
| Integracioni testovi | auth, match, profile, social |

> Integracioni testovi koriste Testcontainers, pa Docker mora biti pokrenut.

### Frontend testovi

```bash
cd web/comeback-web
npm test          # ng test
```

Podrazumevano otvara Chrome i ostaje u watch režimu. Za jednokratno pokretanje
(npr. u CI-ju):

```bash
npx ng test --watch=false --browsers=ChromeHeadless
```

### End-to-end testovi

Integracioni testovi podižu **jedan** servis u procesu i fejkuju messaging i pozive ka
drugim servisima. E2e testovi rade suprotno — gađaju **podignut sistem** preko gateway-a
i prolaze kroz prave RabbitMQ skokove između servisa.

Zato **nisu deo `Comeback.sln`** i ne pokreću ih `dotnet test`; pokreću se namenski, nad
stackom koji već radi:

```bash
# 1. podigni sistem (ako vec nije)
cd infra/docker && docker compose up -d

# 2. pokreni e2e
cd ../..
dotnet test tests/e2e/Comeback.E2ETests
```

Ako stack nije dostupan, testovi padaju sa porukom koja kaže šta da pokreneš.
Adrese se po potrebi menjaju promenljivama `E2E_GATEWAY_URL` (podrazumevano
`http://localhost:5000`) i `E2E_MAILDEV_URL` (`http://localhost:1080`).

Pokriveni tokovi:

| Test | Šta prolazi kroz sistem |
|---|---|
| Registracija → verifikacioni mejl → potvrda → prijava | gateway → auth → RabbitMQ → notification → SMTP → MailDev |
| Potvrđena registracija pravi profil i on je pretraživ | auth → RabbitMQ → profile, pa `ILIKE` pretraga |
| Token izdat od auth servisa važi i u drugim servisima | auth → gateway → profile / notification |
| Gateway odbija neautentifikovane zahteve | gateway → profile / notification / match |

---

## 8. Dokumentacija koda (Doxygen)

Backend kod je dokumentovan XML doc komentarima iz kojih se generiše HTML
dokumentacija. **Generisani HTML se ne commituje** (`docs/api/` je u
`.gitignore`) — pravi se po potrebi.

### Preduslovi

- [Doxygen](https://www.doxygen.nl/download.html)
- **Python** — `Doxyfile` propušta `.cs` fajlove kroz
  `scripts/doxygen-csharp-filter.py`, pa `python` mora biti na PATH-u.
  Skripta je u repozitorijumu, ne instalira se ništa dodatno.

```bash
doxygen --version
python --version
```

### Generisanje

Iz **korena repozitorijuma**:

```bash
doxygen Doxyfile
```

Zatim otvori **`docs/api/html/index.html`** u browseru.

### Šta je pokriveno

Ceo `src/` rekurzivno (`*.cs`), bez `obj/`, `bin/` i `Migrations/` foldera.
Eventualna upozorenja se upisuju u `docs/api/doxygen-warnings.log`.

---

## 9. Posle izmena koda

Backend se **ne** osvežava sam — rebuild servisa koji si menjao:

```bash
cd infra/docker
docker compose build profile-api && docker compose up -d profile-api
```

Frontend (`npm start`) se osvežava automatski (hot reload).

---

## 10. Zaustavljanje i čišćenje

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
