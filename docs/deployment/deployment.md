# Deployment

Kitaphane production ortamı dört servisten oluşur:

```text
Tarayıcı ──> Cloudflare Pages (kitaphane.pages.dev)
               ├── /*        React uygulaması (statik dosyalar, SPA)
               └── /api/*    Pages Function (frontend/functions/api/[[path]].ts)
                               │  + X-Kitaphane-Proxy-Secret, X-Kitaphane-Client-IP
                               ▼
                 Google Cloud Run (europe-west3, Frankfurt) ── ASP.NET Core API
                               │                     └── SMTP ──> Resend
                               ▼
                 Neon PostgreSQL (aws-eu-central-1, Frankfurt)
```

Kararlar (2026-11-26):

- **Tek origin:** Tarayıcı yalnızca Pages adresiyle konuşur; `/api/*` istekleri Pages Function ile Cloud Run'a iletilir. Bu sayede CORS gerekmez ve `SameSite=Strict` refresh token çerezi değişmeden çalışır (`*.pages.dev` ile `*.run.app` farklı siteler olduğu için doğrudan çağrıda çerez gönderilmezdi).
- **Backend yalnızca proxy'den erişilebilir:** Production'da `PROXY_SHARED_SECRET` zorunludur. Bu başlığı taşımayan istekler 403 `FORBIDDEN` alır (yalnızca `/api/health` açıktır). Proxy ziyaretçinin IP'sini (`CF-Connecting-IP`) `X-Kitaphane-Client-IP` olarak iletir; auth rate limit bu IP'ye göre uygulanır. İki başlık da proxy'de her zaman üzerine yazılır, ziyaretçi tarafından taklit edilemez.
- **HTTPS:** Cloud Run TLS'i sonlandırır ve `X-Forwarded-Proto` gönderir; API bu başlığa güvenir (`UseForwardedHeaders`).
- **Migration'lar ayrı adımdır:** API production'da migration çalıştırmaz; idempotent SQL betiği her sürümde Neon'a uygulanır.
- **Alan adı yok:** Frontend `<proje>.pages.dev` adresindedir. Resend doğrulanmış alan adı olmadan yalnızca Resend hesabının e-posta adresine gönderim yapabilir; gerçek kullanıcılar kayıt/şifre kodu alabilsin diye bir alan adı bağlanıp Resend'de doğrulanmalıdır (bkz. §7).

Komutlar repo kök dizininden çalıştırılır. `<...>` ile gösterilen değerleri kendi değerlerinle değiştir. Gizli değerleri (şifre, API anahtarı, JWT secret) komut geçmişinde bırakmamak için aşağıdaki gibi dosyadan/stdin'den ver ve işin bitince geçici dosyaları sil.

## 0. Ön koşullar

- Hesaplar: GitHub, Google Cloud (faturalandırma açık), Cloudflare, Neon, Resend.
- Kod **özel (private) bir GitHub reposunda** durur; Cloud Run (Cloud Build) ve Cloudflare Pages bu repoya bağlanır ve `main` dalına her push'ta otomatik deploy eder. Makinede gcloud veya wrangler kurmak gerekmez.
- Yerelde gereken araçlar: Docker (migration için `psql`), .NET 10 SDK + `dotnet-ef` (`dotnet tool install --global dotnet-ef`).
- Gizli değerler repo kökündeki geçici dosyalarda tutulur ve konsollara dosya olarak yüklenir (hepsi `.gitignore`'da): `neon-url.txt`, `db-connection.txt`, `resend-api-key.txt`, `google-books-key.txt`, `jwt-secret.txt`, `proxy-secret.txt`.

## 1. Neon (veritabanı)

1. Neon konsolunda yeni proje: ad `kitaphane`, Postgres 17, bölge **AWS Europe Central 1 (Frankfurt)**.
2. "Connect" penceresinde **Connection pooling kapalı** (doğrudan bağlantı) adresi kopyala; örnek:
   `postgresql://neondb_owner:<şifre>@ep-xxx-123456.eu-central-1.aws.neon.tech/neondb?sslmode=require`
3. Bu adresi repo kökünde `neon-url.txt` dosyasına kaydet (tek satır). Bu ve aşağıdaki geçici dosyalar `.gitignore`'dadır.
4. API'nin kullandığı Npgsql biçimine çevir (bunu `db-connection.txt` adlı geçici dosyaya yaz):

```text
Host=ep-xxx-123456.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=<şifre>;SSL Mode=VerifyFull;Channel Binding=Require;Gss Encryption Mode=Disable;Maximum Pool Size=10;Timeout=30
```

Neon boşta kalan veritabanını uyutur; ilk bağlantı birkaç saniye sürebilir. API bağlantı hatalarında 3 kez yeniden dener.

## 2. Migration'lar

Şemayı Neon'a uygula. Betik idempotenttir; her sürümde yeniden çalıştırılabilir, yalnızca eksik migration'ları uygular.

```bash
DATABASE_CONNECTION_STRING="Host=unused" dotnet ef migrations script --idempotent \
  -p backend/src/Kitaphane.Infrastructure -s backend/src/Kitaphane.API -o migrate.sql
```

```bash
docker run --rm -i -e DATABASE_URL="$(cat neon-url.txt)" postgres:17-alpine \
  sh -c 'psql "$DATABASE_URL" -v ON_ERROR_STOP=1' < migrate.sql
```

## 3. Resend (e-posta)

1. Resend'de API anahtarı oluştur (izin: *Sending access*). Anahtar SMTP şifresidir; repo kökünde `resend-api-key.txt` dosyasına kaydet.
2. Alan adı yokken gönderen adres `onboarding@resend.dev` olmalıdır ve e-postalar yalnızca Resend hesabının e-posta adresine gider.
3. SMTP ayarları: `smtp.resend.com`, port `465` (TLS), kullanıcı adı `resend`.

## 4. Google Cloud Run (backend)

JWT ve proxy anahtarlarını rastgele üret (proxy anahtarı 5. adımda Cloudflare'e de verilir):

```bash
openssl rand -base64 48 | tr -d '\n' > jwt-secret.txt
openssl rand -hex 32 | tr -d '\n' > proxy-secret.txt
```

**4.1 Proje:** [Google Cloud Console](https://console.cloud.google.com)'da yeni proje aç ve bir faturalandırma hesabı bağla.

**4.2 Secret Manager:** *Security → Secret Manager → Create secret*. Her biri için "Upload file" ile dosyayı seç:

| Secret adı | Dosya |
|---|---|
| `kitaphane-db-connection` | `db-connection.txt` |
| `kitaphane-jwt-secret` | `jwt-secret.txt` |
| `kitaphane-proxy-secret` | `proxy-secret.txt` |
| `kitaphane-smtp-password` | `resend-api-key.txt` |
| `kitaphane-google-books-key` | `google-books-key.txt` |

**4.3 Servis:** *Cloud Run → Deploy container → GitHub (Continuously deploy from a repository)*:

1. *Set up with Cloud Build*: sağlayıcı GitHub, Cloud Build GitHub uygulamasına yalnızca bu repo için izin ver, repoyu seç.
2. Branch `^main$`, Build type **Dockerfile**, Source location **`/backend/Dockerfile`**.
3. Service name `kitaphane-api`, Region **europe-west3 (Frankfurt)**, Authentication **Allow public access** (erişimi API'deki proxy anahtarı sınırlar).
4. Billing *request-based*, Minimum instances `0`, Maximum instances `2`.
5. *Containers → Settings*: Container port `8080`, Memory `512 MiB`.
6. *Variables & Secrets → Environment variables*:

| Ad | Değer |
|---|---|
| `JWT_ISSUER` | `kitaphane-api` |
| `JWT_AUDIENCE` | `kitaphane-web` |
| `JWT_ACCESS_TOKEN_MINUTES` | `60` |
| `JWT_REFRESH_TOKEN_DAYS` | `7` |
| `AUTH_RATE_LIMIT_PER_MINUTE` | `20` |
| `SMTP_HOST` | `smtp.resend.com` |
| `SMTP_PORT` | `465` |
| `SMTP_USERNAME` | `resend` |
| `SMTP_FROM_EMAIL` | `onboarding@resend.dev` |
| `SMTP_FROM_NAME` | `Kitaphane` |

7. *Variables & Secrets → Secrets exposed as environment variables* (sürüm: `latest`):

| Ortam değişkeni | Secret |
|---|---|
| `DATABASE_CONNECTION_STRING` | `kitaphane-db-connection` |
| `JWT_SECRET` | `kitaphane-jwt-secret` |
| `PROXY_SHARED_SECRET` | `kitaphane-proxy-secret` |
| `SMTP_PASSWORD` | `kitaphane-smtp-password` |
| `GOOGLE_BOOKS_API_KEY` | `kitaphane-google-books-key` |

   Konsol, servis hesabının bu secret'ları okuma izni (*Secret Manager Secret Accessor*) olmadığını söylerse "Grant" ile izni ver.

8. *Create*. İlk derleme birkaç dakika sürer; ilerlemesi *Cloud Build → History*'de görünür. Bitince servis adresini (`https://kitaphane-api-....run.app`) not al ve kontrol et:

```bash
curl -s https://<cloud-run-adresi>/api/health          # 200 Healthy
curl -s -o /dev/null -w "%{http_code}\n" https://<cloud-run-adresi>/api/auth/me   # 403 (proxy'siz erişim kapalı)
```

İstersen *Cloud Build → Triggers*'da tetikleyiciye "Included files filter" olarak `backend/**` ekle; böylece yalnızca frontend değişen push'lar backend'i yeniden derlemez.

## 5. Cloudflare Pages (frontend)

Cloudflare panelinde *Workers & Pages → Create → Pages → Connect to Git*:

1. GitHub'ı bağla (yalnızca bu repoya izin ver) ve repoyu seç.
2. Project name `kitaphane` (adres `kitaphane.pages.dev` olur; alınmışsa başka bir ad), Production branch `main`.
3. Build ayarları: Framework preset **None**, Build command `npm run build`, Build output directory `dist`, Root directory **`frontend`**. Node sürümü `frontend/.node-version` dosyasından okunur (24).
4. *Environment variables* (Production) → her ikisini **Encrypt** ile ekle:
   - `BACKEND_URL` = Cloud Run servis adresi (`https://kitaphane-api-....run.app`, sonda `/` olmadan)
   - `PROXY_SHARED_SECRET` = `proxy-secret.txt` içeriği
5. *Save and Deploy*. `frontend/functions/` klasörü otomatik olarak Pages Function'a çevrilir; `/api/*` dışındaki adresler React uygulamasını açar (SPA).

Değişkenleri ilk derlemeden sonra eklediysen veya değiştirdiysen *Deployments → Retry deployment* ile yeniden deploy et; Functions yeni değerleri ancak yeni deploy'da görür.

Kurulum bitince geçici dosyaları sil:

```bash
rm -f neon-url.txt resend-api-key.txt google-books-key.txt db-connection.txt jwt-secret.txt proxy-secret.txt migrate.sql
```

## 6. Kontrol listesi

- `https://<ad>.pages.dev` açılıyor, `https://<ad>.pages.dev/api/health` 200 dönüyor.
- Resend hesabının e-posta adresiyle kayıt ol: doğrulama kodu gelir, hesap açılır.
- Sayfayı yenile: oturum korunur (refresh token çerezi çalışıyor).
- Kitap ekle, Google Books araması yap, profil istatistiklerini aç.

## 7. Güncelleme ve sonraki adımlar

Yeni sürüm:

1. Yeni migration varsa önce §2'yi tekrarla (migration, backend'den önce uygulanır).
2. `git push` (`main`): Cloud Build backend'i, Cloudflare Pages frontend'i otomatik deploy eder.

Secret değiştirmek: Secret Manager'da secret'a yeni sürüm ekle (*New version*), ardından Cloud Run'da *Edit & deploy new revision* ile yeni revizyon aç (secret'lar `latest` olarak okunur ama yalnızca yeni revizyonda yenilenir).

Alan adı bağlanınca:

- Cloudflare Pages → Custom domains ile alan adını ekle.
- Resend → Domains'te alan adını ekleyip DNS kayıtlarını (SPF, DKIM) gir; doğrulanınca Cloud Run'da `SMTP_FROM_EMAIL`'i `no-reply@<alan-adı>` yapıp yeni revizyon aç. Bundan sonra herkese e-posta gönderilebilir.
