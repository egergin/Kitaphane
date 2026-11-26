# Authentication & Authorization

# 1. Overview

Kitaphane kullanıcı hesabı sistemi zorunlu olan bir web uygulamasıdır.

Authentication için ASP.NET Core Identity kullanılacaktır.

Her kullanıcı yalnızca kendi hesabına ait verilere erişebilmelidir.

Authentication ve authorization kontrolleri backend tarafında uygulanmalıdır.

# 2. Authentication Technology

- ASP.NET Core Identity (kullanıcı yönetimi, password hashing)
- ASP.NET Core Web API
- JWT Bearer authentication (access token)
- Refresh token (HttpOnly cookie)
- E-posta gönderimi (production: Resend HTTPS API, local: Mailpit SMTP); gönderilemezse 503 `EMAIL_DELIVERY_FAILED`

Authentication mekanizması güvenli şekilde uygulanmalıdır.

Şifreler kesinlikle plaintext olarak tutulmamalıdır.

ASP.NET Core Identity'nin password hashing mekanizması kullanılmalıdır.

## 2.1. Token Stratejisi

text

```
Access token
- JWT, kısa ömürlü (varsayılan 60 dakika)
- Response body ile döner
- Frontend yalnızca bellekte (memory) tutar; localStorage/sessionStorage kullanılmaz
- Her API isteğinde Authorization: Bearer <token> header'ı ile gönderilir

Refresh token
- Rastgele üretilmiş opak değer, uzun ömürlü (varsayılan 7 gün)
- HttpOnly, Secure, SameSite=Strict cookie olarak gönderilir
- Cookie path: /api/auth
- Database'de yalnızca hash'i saklanır (RefreshTokens tablosu)
- Her refresh işleminde rotate edilir: eski token iptal edilir, yenisi verilir
- İptal edilmiş bir token tekrar kullanılırsa kullanıcının tüm refresh token'ları iptal edilir
- "Oturumumu açık tut" işaretliyse cookie kalıcıdır (7 gün); değilse tarayıcı oturumu cookie'sidir
  (tarayıcı kapanınca silinir). Bu tercih token rotate edilirken korunur (RefreshTokens.IsPersistent).
```

Sayfa yenilendiğinde access token bellekten silinir; frontend açılışta `POST /api/auth/refresh` çağırarak oturumu sessizce yeniler.

Refresh token her kullanımda değiştiği için aynı anda birden fazla sekmenin refresh yapması oturumu sonlandırabilir. Frontend refresh isteklerini sekme içinde tek istekte birleştirir ve sekmeler arasında Web Locks API ile sıraya koyar.

Access token süresi dolduğunda API 401 döner; frontend bir kez refresh dener, başarısız olursa kullanıcıyı login ekranına yönlendirir.

JWT ayarları (secret, issuer, audience, süreler) environment variable üzerinden yönetilir.

# 3. User Model

User modeli ASP.NET Core Identity üzerinden genişletilebilir.

text

```
User
-----------------------
Id
Name
Email
PasswordHash
ProfileImage
CreatedAt
UpdatedAt
```

Açıklamalar:

- Id: Kullanıcı primary key (Guid).
- Name: Kullanıcının adı.
- Email: Kullanıcının email adresi (unique, doğrulanmış).
- PasswordHash: Identity tarafından yönetilen şifre hash'i.
- ProfileImage: Profil görseli URL'si (nullable).
- CreatedAt: Hesabın oluşturulma zamanı.
- UpdatedAt: Hesabın son güncellenme zamanı.

CreatedAt ve UpdatedAt kullanıcıya gösterilmeyecektir.

Kullanıcı yalnızca e-posta doğrulaması tamamlandıktan sonra oluşturulduğu için tüm kullanıcıların `EmailConfirmed` değeri true'dur.

# 4. Authentication Screens

Uygulamada aşağıdaki ekranlar bulunmalıdır:

- Login ("Oturumumu açık tut" seçeneği ile)
- Register
- Kodu Doğrula (6 haneli kod girişi; kayıt ve şifre sıfırlama akışlarında ortak)
- Forgot Password (e-posta ile kod isteme)
- Reset Password (yeni şifre oluşturma)

Logout işlemi de desteklenmelidir.

## 4.1. Şifre Kuralı

Şifre en az 8, en fazla 128 karakter olmalı; en az bir harf ve en az bir rakam içermelidir. Kural FluentValidation ve Identity password options ile backend'de, zod ile frontend'de uygulanır.

Şifre değiştirme Profile ekranındaki Account settings bölümünde yer alır.

# 5. Authentication API

http

```
POST /api/auth/register
POST /api/auth/verify-email
POST /api/auth/resend-verification
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
POST /api/auth/forgot-password
POST /api/auth/verify-reset-code
POST /api/auth/reset-password
POST /api/auth/change-password
GET  /api/auth/me
```

Doğrulama ve şifre sıfırlama e-postaları isteğin `Accept-Language` başlığına göre Türkçe veya İngilizce gönderilir (varsayılan Türkçe).

## Register

Kayıt isteğini başlatır; kullanıcı hesabı bu adımda oluşturulmaz.

Request: `name`, `email`, `password`.

1. Email'in mevcut bir kullanıcıya ait olup olmadığı kontrol edilir (varsa 409 Conflict).
2. Şifre, şifre kuralına göre doğrulanır (bkz. §4.1).
3. Şifre Identity `IPasswordHasher` ile hash'lenir.
4. 6 haneli doğrulama kodu üretilir ve hash'lenir.
5. Bilgiler `PendingRegistrations` tablosuna kaydedilir. Aynı email için bekleyen kayıt varsa üzerine yazılır.
6. Doğrulama kodu e-posta ile gönderilir. Aynı email için 60 saniye içinde tekrar kayıt olunursa bilgiler güncellenir ama yeni kod gönderilmez (önceki kod geçerli kalır).

Response: 202 Accepted.

## Verify Email

Request: `email`, `code`.

- Kod 15 dakika geçerlidir.
- En fazla 5 yanlış deneme yapılabilir; aşılırsa bekleyen kayıt geçersiz olur ve kullanıcı yeniden kayıt olmalı veya yeni kod istemelidir.
- Kod doğruysa kullanıcı `PendingRegistrations` içindeki bilgilerle oluşturulur (`EmailConfirmed = true`), bekleyen kayıt silinir.
- Kullanıcı otomatik olarak giriş yapmış olur: access token response body'de, refresh token kalıcı cookie'de döner.

## Resend Verification

Request: `email`.

Bekleyen kayıt için yeni kod üretir, deneme sayacını sıfırlar ve e-posta gönderir.

Kötüye kullanımı önlemek için aynı email için 60 saniyede en fazla bir kez kod gönderilir.

Bekleyen kayıt bulunmasa bile 202 döner (email enumeration önlenir).

## Login

Request: `email`, `password`, `rememberMe`.

Kullanıcı kimlik bilgilerini doğrular. Hatalıysa 401 `INVALID_CREDENTIALS` döner.

Başarılı olursa access token response body'de, refresh token HttpOnly cookie'de döner. `rememberMe` false ise cookie tarayıcı oturumu cookie'sidir.

Hatalı giriş denemelerinde hesap kilitlenmez (Identity lockout kullanılmaz). Brute-force denemelerine karşı auth endpoint'lerindeki rate limiting yeterlidir.

## Refresh

Refresh token cookie'sini doğrular, rotate eder ve yeni access token döner.

## Logout

Mevcut refresh token'ı iptal eder ve cookie'yi siler.

## Forgot Password

Şifre sıfırlama, Penpot tasarımındaki gibi e-posta ile gönderilen 6 haneli kod üzerinden yapılır:

text

```
Şifremi Unuttum (e-posta) → Kodu Doğrula (6 haneli kod) → Yeni Şifre Oluştur
```

Request: `email`.

Kullanıcı varsa 6 haneli bir sıfırlama kodu üretilir, hash'lenerek `PasswordResetCodes` tablosuna kaydedilir ve e-posta ile gönderilir. Kod kuralları kayıt doğrulama koduyla aynıdır: 15 dakika geçerli, en fazla 5 deneme, 60 saniyede en fazla bir gönderim. "Kodu Yeniden Gönder" aynı endpoint'i tekrar çağırır.

Kullanıcı bulunmasa bile her zaman 202 döner (email enumeration önlenir).

## Verify Reset Code

Request: `email`, `code`.

Kod doğruysa kod tüketilir (tekrar kullanılamaz) ve 15 dakika geçerli, tek kullanımlık bir `resetToken` döner. Token database'de hash'li saklanır.

Response: `{ "resetToken": "..." }`.

## Reset Password

Request: `email`, `resetToken`, `newPassword`.

- `resetToken` geçersiz veya süresi dolmuşsa 400 `INVALID_RESET_TOKEN` döner.
- Yeni şifre mevcut şifreyle aynıysa 400 `PASSWORD_SAME_AS_CURRENT` döner. Şifre geçmişi tutulmaz.
- Başarılı olursa kullanıcının tüm refresh token'ları iptal edilir; kullanıcı yeni şifresiyle giriş yapar.

## Change Password

Authenticated kullanıcı için `currentPassword` ve `newPassword` alır.

- Mevcut şifre hatalıysa 400 `INVALID_CURRENT_PASSWORD` döner.
- Yeni şifre mevcut şifreyle aynıysa 400 `PASSWORD_SAME_AS_CURRENT` döner.
- Başarılı olursa mevcut oturum dışındaki tüm refresh token'lar iptal edilir.

## Current User

Authenticated kullanıcının profil bilgilerini döndürür.

Profil güncelleme `PUT /api/profile` üzerinden yapılır (bkz. `api-design.md`).

# 6. User Isolation

Bu uygulamanın en kritik güvenlik gereksinimlerinden biridir.

Her Book kaydı bir User'a ait olmalıdır.

text

```
Book.UserId
```

alanı üzerinden kullanıcı ile ilişkilendirilir.

API'deki bütün Book endpoint'lerinde authenticated user's ID kullanılmalıdır.

Kullanıcı request içerisinde başka bir userId göndererek başka kullanıcının kitaplarına erişememelidir.

Örneğin:

http

```
GET /api/books
```

yalnızca giriş yapan kullanıcının kitaplarını döndürmelidir.

Aşağıdaki API tasarımından mümkün olduğunca kaçınılmalıdır:

http

```
GET /api/users/{userId}/books
```

User ID request üzerinden güvenilir kabul edilmemelidir.

Authenticated user ID server-side authentication context üzerinden (JWT `sub` claim) alınmalıdır.

# 7. Authorization Rules

Her Book request'inde ownership kontrolü yapılmalıdır.

Örnek:

text

```
Book.Id == requestedBookId
AND
Book.UserId == CurrentUser.Id
```

kontrol edilmelidir.

Aynı yaklaşım aşağıdaki kaynaklara uygulanmalıdır:

- Books
- Tags
- Reading Goals
- Profile
- Statistics

Kullanıcı başka bir kullanıcıya ait kaydı okuyamamalı, değiştirememeli veya silememelidir.

Başka bir kullanıcıya ait kaynağa erişim denemelerinde, kaynağın varlığını sızdırmamak için 404 Not Found dönülür.

# 8. Authentication Security

Aşağıdaki güvenlik kuralları uygulanmalıdır:

- Passwords plaintext tutulmamalıdır.
- ASP.NET Core Identity password hashing kullanılmalıdır.
- Access token yalnızca frontend belleğinde tutulmalıdır.
- Refresh token HttpOnly, Secure, SameSite=Strict cookie ile taşınmalı ve database'de hash'li saklanmalıdır.
- E-posta doğrulama kodları hash'li saklanmalı, süreli ve deneme sınırlı olmalıdır.
- Forgot password ve resend verification endpoint'leri email enumeration'a izin vermemelidir.
- Auth endpoint'lerinde IP başına rate limiting uygulanmalıdır (varsayılan dakikada 20 istek, `AUTH_RATE_LIMIT_PER_MINUTE`).
- Production ortamında HTTPS kullanılmalıdır.
- API secrets frontend'e gönderilmemelidir.
- JWT secret ve SMTP bilgileri gibi secret değerler environment variable üzerinden yönetilmelidir.
- Sensitive information (şifre, token, doğrulama kodu) loglanmamalıdır.
- CORS yalnızca frontend origin'ine izin verecek şekilde, credentials destekli yapılandırılmalıdır.
- Backend authorization zorunlu olmalıdır.

# 9. User Registration and Profile

Kullanıcı yalnızca e-posta doğrulaması tamamlandığında oluşturulur; CreatedAt ve UpdatedAt bu anda atanır.

Doğrulanmamış bekleyen kayıtlar süresi dolduktan sonra temizlenir.

Profil bilgileri authenticated kullanıcıya ait olmalıdır.

Kullanıcı başka bir kullanıcıya ait profil bilgilerini görüntüleyememelidir.

Email değiştirme MVP kapsamında değildir.

# 10. Related Documentation

- Database schema: `database-design.md`
- Authentication endpoints: `api-design.md`
- Protected routes and authentication state: `frontend-design.md`
