# Lokalizacioni problemi u backendu

## Problem 2 — Exception poruke na srpskom jeziku

### Opis

Domain i Application slojevi bacaju izuzetke sa srpskim porukama koje direktno putuju do HTTP odgovora i prikazuju se korisniku. Primeri:

- `"Meč nije pronađen."` (NotFoundException, 404)
- `"Samo organizator može uneti rezultat."` (ForbiddenException, 403)
- `"Poziv je već prihvaćen, odbijen ili nije aktivan."` (BusinessRuleException, 400)
- `"Poruka ne može biti prazna."` (BusinessRuleException, 400)

Pogođeni fajlovi (sve u `src/services/`):

- `match/Comeback.Match.Domain/Entities/Match.cs` — domenski invarijanti
- `match/Comeback.Match.Application/Features/Matches/Commands/**` — command handleri
- `chat/Comeback.Chat.Application/Features/**` — command i query handleri
- `notification/Comeback.Notification.Application/Features/**`

### Zašto je ovo loše

Backend ne treba da zna na kom jeziku korisnik konzumira UI. Ako se aplikacija ikada proširi na drugi jezik, svaki exception message morao bi biti pronađen i preveden ručno u backendu. Pored toga, greška se ne može lako "matchovati" na frontendu (npr. za custom prikaz) jer je slobodan tekst.

### Ispravno rešenje

Koristiti **error kodove** umesto tekstualnih poruka. Exception nosi kod (string ili enum), a frontend ga mapira u lokalizovani string iz `sr-latn.json`.

Primer:

```csharp
// Backend
throw new BusinessRuleException("match.result.alreadySubmitted");

// Frontend — errors sekcija u sr-latn.json
"match.result.alreadySubmitted": "Rezultat je već unet za ovaj meč."
```

Ovaj pristup zahteva:
1. Promenu exception klasa da nose `string ErrorCode` umesto slobodnog teksta
2. Global exception handler koji vraća `{ "code": "match.result.alreadySubmitted" }` (ili `ProblemDetails` sa `extensions.code`)
3. Angular interceptor ili service koji čita `code` i prevodi ga

---

## Problem 3 — Notifikacije i email sadržaj na srpskom

### Opis

Push notifikacije (in-app) i verifikacioni email kreiraju se na bekendu sa srpskim tekstom hardkodovanim direktno u consumer klasama i command handlerima.

Pogođeni fajlovi:

- `notification/Comeback.Notification.Infrastructure/Messaging/MatchInvitationSentConsumer.cs`
- `notification/Comeback.Notification.Infrastructure/Messaging/MatchCancelledConsumer.cs`
- `notification/Comeback.Notification.Infrastructure/Messaging/MatchParticipantWithdrawnConsumer.cs`
- `notification/Comeback.Notification.Infrastructure/Messaging/MatchInvitationRespondedConsumer.cs`
- `notification/Comeback.Notification.Infrastructure/Messaging/MatchResultSubmittedConsumer.cs`
- `notification/Comeback.Notification.Application/Features/Emails/SendVerificationEmail/SendVerificationEmailCommandHandler.cs`

### Zašto je ovo drugačiji problem

Za razliku od exception poruka, notifikacije i emailovi se **isporučuju van Angular konteksta** — šalju se korisnikovom uređaju ili email klijentu. Nema načina da Angular intercept-uje sadržaj i prevede ga nakon prijema. Lokalizacija mora da se desi **pre slanja**.

### Moguće opcije

**Opcija A — Notification keys (preporučeno za budućnost)**

Notification servis čuva `{ titleKey: "notification.matchInvitation.title", bodyKey: "notification.matchInvitation.body", params: { organizer: "...", ... } }`. Mobilna aplikacija (ili web push handler) prima keys i lokalizuje ih na uređaju.

Ovo je arhitekturalno ispravno, ali zahteva da i native mobilna aplikacija (ako se ikad napravi) implementira isti i18n mehanizam.

**Opcija B — Prihvatiti kompromis (trenutni status)**

Budući da je Comeback jednojezična platforma (srpski), a notifikacije i emailovi uvek idu srpskim korisnicima, hardkodovani srpski tekst u notification servisu je svesni kompromis — prihvatljiv dok god ne postoji potreba za višejezičnošću.

### Preporuka

Ostaviti opciju B za sada. Ako se pojavi potreba za višejezičnošću ili native mobilnom aplikacijom, tada preći na opciju A. Ovo **nije prioritet** dok god je aplikacija jednojezična.
