# Deployment

Kitaphane production ortamı dört servisten oluşur:

```text
Tarayıcı ──> Cloudflare Pages (kitaphane.pages.dev)
               ├── /*        React uygulaması (statik dosyalar, SPA)
               └── /api/*    Pages Function (frontend/functions/api/[[path]].ts)
                               │  + X-Kitaphane-Proxy-Secret, X-Kitaphane-Client-IP
                               ▼
                 Render Web Service (Frankfurt, Docker) ── ASP.NET Core API
                               │                     └── SMTP ──> Resend
                               ▼
                 Neon PostgreSQL (aws-eu-central-1, Frankfurt)
```

Kararlar (2026-11-26):

- **Backend Render'ın ücretsiz planında:** Kart bilgisi gerektirmez. Ücretsiz servis 15 dakika istek almazsa uyur; uyuyan servise gelen ilk istek, servis yeniden başlayana kadar 30–60 saniye bekler. Neon da boşta kalan veritabanını uyutur (ilk bağlantı birkaç saniye sürer; API bağlantı hatalarında 3 kez yeniden dener).
- **Tek origin:** Tarayıcı yalnızca Pages adresiyle konuşur; `/api/*` istekleri Pages Function ile Render'a iletilir. Bu sayede CORS gerekmez ve `SameSite=Strict` refresh token çerezi değişmeden çalışır (`*.pages.dev` ile `*.onrender.com` farklı siteler olduğu için doğrudan çağrıda çerez gönderilmezdi).
- **Backend yalnızca proxy'den erişilebilir:** Production'da `PROXY_SHARED_SECRET` zorunludur. Bu başlığı taşımayan istekler 403 `FORBIDDEN` alır (yalnızca `/api/health` açıktır). Proxy ziyaretçinin IP'sini (`CF-Connecting-IP`) `X-Kitaphane-Client-IP` olarak iletir; auth rate limit bu IP'ye göre uygulanır. İki başlık da proxy'de her zaman üzerine yazılır, ziyaretçi tarafından taklit edilemez.
- **HTTPS:** Render TLS'i sonlandırır ve `X-Forwarded-Proto` gönderir; API bu başlığa güvenir (`UseForwardedHeaders`). İmaj, Render'ın verdiği `PORT`'u dinler (yoksa 8080).
- **Migration'lar ayrı adımdır:** API production'da migration çalıştırmaz; idempotent SQL betiği her sürümde Neon'a uygulanır.
- **Otomatik deploy:** Kod özel bir GitHub reposundadır; Render ve Cloudflare Pages bu repoya bağlıdır ve `main` dalına her push'ta otomatik deploy eder.
- **Alan adı yok:** Frontend `<proje>.pages.dev` adresindedir. Resend doğrulanmış alan adı olmadan yalnızca Resend hesabının e-posta adresine gönderim yapabilir; gerçek kullanıcılar kayıt/şifre kodu alabilsin diye bir alan adı bağlanıp Resend'de doğrulanmalıdır (bkz. §7).

Komutlar repo kök dizininden çalıştırılır. `<...>` ile gösterilen değerleri kendi değerlerinle değiştir.

## 0. Ön koşullar

- Hesaplar: GitHub, Render, Cloudflare, Neon, Resend.
- Yerelde gereken araçlar: Docker (migration için `psql`), .NET 10 SDK + `dotnet-ef` (`dotnet tool install --global dotnet-ef`).
- Gizli değerler repo kökündeki geçici dosyalarda tutulur ve panellere dosyadan aktarılır (hepsi `.gitignore`'da): `neon-url.txt`, `db-connection.txt`, `resend-api-key.txt`, `google-books-key.txt`, `jwt-secret.txt`, `proxy-secret.txt`, `render.env`. Dosyalarda sonda satır sonu olmamalıdır (değerin parçası sayılır).

## 1. Neon (veritabanı)

1. Neon konsolunda yeni proje: ad `kitaphane`, Postgres 17, bölge **AWS Europe Central 1 (Frankfurt)**.
2. "Connect" penceresinde **Connection pooling kapalı** (doğrudan bağlantı) adresi kopyala; örnek:
   `postgresql://neondb_owner:<şifre>@ep-xxx-123456.eu-central-1.aws.neon.tech/neondb?sslmode=require`
3. Bu adresi repo kökünde `neon-url.txt` dosyasına kaydet (tek satır).
4. API'nin kullandığı Npgsql biçimine çevir ve `db-connection.txt` dosyasına yaz:

```text
Host=ep-xxx-123456.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=<şifre>;SSL Mode=VerifyFull;Channel Binding=Require;Gss Encryption Mode=Disable;Maximum Pool Size=10;Timeout=30
```

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

## 4. Render (backend)

JWT ve proxy anahtarlarını rastgele üret (proxy anahtarı 5. adımda Cloudflare'e de verilir):

```bash
openssl rand -base64 48 | tr -d '\n' > jwt-secret.txt
openssl rand -hex 32 | tr -d '\n' > proxy-secret.txt
```

Render'a aktarılacak ortam değişkenlerini `render.env` dosyasında topla (gizli değerler dosyalardan okunur):

```bash
cat > render.env <<EOF
JWT_ISSUER=kitaphane-api
JWT_AUDIENCE=kitaphane-web
JWT_ACCESS_TOKEN_MINUTES=60
JWT_REFRESH_TOKEN_DAYS=7
AUTH_RATE_LIMIT_PER_MINUTE=20
SMTP_HOST=smtp.resend.com
SMTP_PORT=465
SMTP_USERNAME=resend
SMTP_FROM_EMAIL=onboarding@resend.dev
SMTP_FROM_NAME=Kitaphane
DATABASE_CONNECTION_STRING=$(cat db-connection.txt)
JWT_SECRET=$(cat jwt-secret.txt)
PROXY_SHARED_SECRET=$(cat proxy-secret.txt)
SMTP_PASSWORD=$(cat resend-api-key.txt)
GOOGLE_BOOKS_API_KEY=$(cat google-books-key.txt)
EOF
```

[Render Dashboard](https://dashboard.render.com)'da *New → Web Service*:

1. *Git Provider → GitHub*: Render GitHub uygulamasına yalnızca bu repo için izin ver ve repoyu seç.
2. Name `kitaphane-api` (adres `https://kitaphane-api.onrender.com` olur; alınmışsa Render sonuna ek koyar), Language **Docker**, Branch `main`, Region **Frankfurt (EU Central)**.
3. Root Directory **`backend`**, Dockerfile Path **`./Dockerfile`** (Docker build context `backend/` klasörüdür).
4. Instance Type **Free**.
5. *Environment Variables → Add from .env*: `render.env` dosyasının içeriğini yapıştır. Eklenen 15 değişkenin değerlerinin tam ve tırnaksız olduğunu kontrol et (özellikle `DATABASE_CONNECTION_STRING`).
6. *Advanced*: Health Check Path `/api/health`, Auto-Deploy **On Commit**.
7. *Deploy Web Service*. İlk derleme birkaç dakika sürer; ilerlemesi servisin *Logs* / *Events* sayfasında görünür.

Bitince servis adresini not al ve kontrol et:

```bash
curl -s https://<render-adresi>/api/health          # 200 Healthy (servis uyuyorsa ilk yanıt 30–60 sn sürebilir)
curl -s -o /dev/null -w "%{http_code}\n" https://<render-adresi>/api/auth/me   # 403 (proxy'siz erişim kapalı)
```

## 5. Cloudflare Pages (frontend)

Cloudflare panelinde *Workers & Pages → Create → Pages → Connect to Git*:

1. GitHub'ı bağla (yalnızca bu repoya izin ver) ve repoyu seç.
2. Project name `kitaphane` (adres `kitaphane.pages.dev` olur; alınmışsa başka bir ad), Production branch `main`.
3. Build ayarları: Framework preset **None**, Build command `npm run build`, Build output directory `dist`, Root directory **`frontend`**. Node sürümü `frontend/.node-version` dosyasından okunur (24).
4. *Environment variables* (Production) → her ikisini **Encrypt** ile ekle:
   - `BACKEND_URL` = Render servis adresi (`https://kitaphane-api.onrender.com`, sonda `/` olmadan)
   - `PROXY_SHARED_SECRET` = `proxy-secret.txt` içeriği
5. *Save and Deploy*. `frontend/functions/` klasörü otomatik olarak Pages Function'a çevrilir; `/api/*` dışındaki adresler React uygulamasını açar (SPA).

Değişkenleri ilk derlemeden sonra eklediysen veya değiştirdiysen *Deployments → Retry deployment* ile yeniden deploy et; Functions yeni değerleri ancak yeni deploy'da görür.

Kurulum bitince geçici dosyaları sil:

```bash
rm -f neon-url.txt resend-api-key.txt google-books-key.txt db-connection.txt jwt-secret.txt proxy-secret.txt render.env migrate.sql
```

## 6. Kontrol listesi

- `https://<ad>.pages.dev` açılıyor, `https://<ad>.pages.dev/api/health` 200 dönüyor (backend uyuyorsa ilk istek gecikir).
- Resend hesabının e-posta adresiyle kayıt ol: doğrulama kodu gelir, hesap açılır.
- Sayfayı yenile: oturum korunur (refresh token çerezi çalışıyor).
- Kitap ekle, Google Books araması yap, profil istatistiklerini aç.

## 7. Güncelleme ve sonraki adımlar

Yeni sürüm:

1. Yeni migration varsa önce §2'yi tekrarla (migration, backend'den önce uygulanır).
2. `main` dalına push: Render backend'i, Cloudflare Pages frontend'i otomatik deploy eder.

Ortam değişkeni veya secret değiştirmek: Render'da servisin *Environment* sayfasında değeri güncelle ve kaydet; Render yeni değerle yeniden deploy eder. Proxy anahtarı değişirse Cloudflare Pages'teki `PROXY_SHARED_SECRET` de aynı değerle güncellenip yeniden deploy edilmelidir.

Uyku gecikmesi: Ücretsiz servis 15 dakika boşta kalınca uyur. İstenirse bir uptime servisi `/api/health` adresini 10–14 dakikada bir çağırarak servisi uyanık tutabilir; tek servisin sürekli çalışması Render'ın aylık ücretsiz kullanım saatine sığar, ancak bu Render'ın ücretsiz plan koşullarına bağlıdır ve değişebilir.

Alan adı bağlanınca:

- Cloudflare Pages → Custom domains ile alan adını ekle.
- Resend → Domains'te alan adını ekleyip DNS kayıtlarını (SPF, DKIM) gir; doğrulanınca Render'da `SMTP_FROM_EMAIL`'i `no-reply@<alan-adı>` yap. Bundan sonra herkese e-posta gönderilebilir.
