# Deployment (produkcija)

Cilj: ceo stack na jednom serveru, jedan domen, automatski HTTPS.
Arhitektura na serveru:

```
Internet ──▶ Caddy (web, :443)  ──┬─ /            ▶ Angular SPA (statika)
                                  ├─ /api/*       ▶ gateway ▶ mikroservisi
                                  └─ /hubs/*  (WS) ▶ gateway ▶ chat/notif
```

Interni servisi (7× Postgres, RabbitMQ, Redis, Seq, 8× .NET) **nemaju javne
portove** — dostupni su samo unutar Docker mreže.

---

## 0. Preduslovi

- Server: **Hetzner CAX21** (4 vCPU ARM / 8 GB / 80 GB), Ubuntu 24.04
- Domen sa pristupom DNS-u kod registrara
- Cloudinary nalog (upload slika) i Brevo nalog (SMTP)

---

## 1. Server: osnovno podešavanje

SSH kao root, pa:

```bash
# non-root korisnik
adduser comeback && usermod -aG sudo comeback

# Firewall — Docker zaobilazi ufw za PUBLISHED portove, ali mi i ne
# objavljujemo interne portove, pa je ufw i dalje korisna zaštita SSH-a.
ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw --force enable

# SWAP (4 GB) — build 8 .NET image-a ume da probije 8 GB RAM-a
fallocate -l 4G /swapfile && chmod 600 /swapfile
mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
```

Docker + Compose plugin:

```bash
curl -fsSL https://get.docker.com | sh
usermod -aG docker comeback
```

Odjavi se i uloguj kao `comeback`.

---

## 2. DNS

Kod registrara napravi **A** zapis:

| Tip | Ime | Vrednost |
|---|---|---|
| A | `@` (ili `comeback`) | `<IP servera>` |

Sačekaj da se propagira: `dig +short comeback.example.com` treba da vrati IP.
(Caddy neće moći da izda sertifikat dok DNS ne pokazuje na server.)

---

## 3. Kod na server

```bash
git clone <repo-url> comeback && cd comeback
git checkout develop
cp infra/docker/.env.prod.example infra/docker/.env.prod
nano infra/docker/.env.prod   # popuni SVE vrednosti (vidi dole)
```

### Popunjavanje `.env.prod`

- `DOMAIN` / `ACME_EMAIL` — tvoj domen (bez `https://`) i email
- `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `SEQ_ADMIN_PASSWORD`, `JWT_SECRET`
  → generiši sa `openssl rand -base64 32`
- `CHAT_ENCRYPTION_KEY` → `openssl rand -base64 32` (mora biti tačno 32 bajta base64)
- `CLOUDINARY_*` → Cloudinary dashboard
- `SMTP_*` → Brevo → *SMTP & API → SMTP* (login + master password / SMTP key).
  Domen u `SMTP_FROM_EMAIL` verifikuj u Brevo-u da mejlovi ne padnu u spam.

---

## 4. Deploy

```bash
cd infra/docker
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

Prvi build (na ARM-u, 9 image-a) traje ~10–20 min. Migracije baza se izvrše
**automatski** kad servisi startuju (`db.Database.Migrate()`), ništa ručno.

Prati podizanje:

```bash
docker compose -f docker-compose.prod.yml logs -f web gateway auth-api
```

Caddy će sam izvući Let's Encrypt sertifikat čim DNS pokazuje na server.

---

## 5. Provera

Otvori `https://comeback.example.com` i testiraj:

- [ ] SPA se učita preko HTTPS (validan sertifikat)
- [ ] Registracija → stigne verifikacioni email (Brevo)
- [ ] Login radi
- [ ] Upload slike profila (Cloudinary)
- [ ] Chat — poruke uživo (WebSocket `/hubs/chat`)
- [ ] Notifikacije uživo (`/hubs/notifications`)
- [ ] Feed / mečevi / rating

Ako nešto ne radi: `docker compose -f docker-compose.prod.yml ps` i `logs <servis>`.

---

## 6. Ažuriranje posle izmena koda

```bash
cd ~/comeback && git pull
cd infra/docker
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

Rebuilduje samo servise čiji se kod/slojevi promenio.

---

## 7. Backup baza (preporuka pre odbrane)

```bash
for db in auth profile rating match chat notification social; do
  docker compose -f docker-compose.prod.yml exec -T postgres-$db \
    pg_dump -U comeback comeback_$db > ~/backup_${db}.sql
done
```

---

## Napomene / troškovi

- **Seq** (logovi) i **RabbitMQ management** UI nisu javno izloženi. Za pristup
  koristi SSH tunel, npr: `ssh -L 5342:localhost:5342 comeback@<IP>` uz privremeno
  objavljivanje porta, ili `docker compose exec`.
- Mesečni trošak: server ~€6.5 (Hetzner CAX21) + domen. Cloudinary i Brevo imaju
  besplatne tarife dovoljne za demo.
- MailDev iz dev okruženja **nije** u prod compose-u — zamenjen pravim SMTP-om.
