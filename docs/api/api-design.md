# API Design

# 1. Overview

Kitaphane backend RESTful API yaklaşımıyla geliştirilecektir.

Backend teknolojileri:

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- Npgsql
- ASP.NET Core Identity
- JWT Bearer authentication
- FluentValidation
- OpenAPI / Swagger

Entity modelleri doğrudan API response olarak expose edilmemelidir.

Request ve response işlemlerinde DTO kullanılmalıdır.

# 2. API Conventions

- RESTful endpoint naming kullanılmalıdır.
- HTTP method'ları doğru kullanılmalıdır.
- Request ve response DTO'ları tanımlanmalıdır.
- JSON property isimleri camelCase'dir.
- Enum'lar JSON'da string olarak taşınır (ör. `"COMPLETED"`).
- Tarihler (`DateOnly`) `YYYY-MM-DD`, zaman damgaları ISO 8601 UTC formatındadır.
- ID'ler Guid'dir.
- Backend validation uygulanmalıdır.
- Tutarlı error response formatı kullanılmalıdır.
- Ownership kontrolleri server-side uygulanmalıdır.
- Async/await kullanılmalıdır.
- `/api/auth/*` (register, verify-email, resend-verification, login, refresh, logout, forgot-password, verify-reset-code, reset-password) ve `/api/health` dışındaki tüm endpoint'ler authentication gerektirir. Bu kural varsayılan (fallback) authorization policy ile uygulanır; bu yüzden anonim isteklerde bilinmeyen adresler de 401 döner.
- İstekler `Accept-Language: tr | en` başlığı taşır; API e-posta dilini buna göre seçer.

# 3. Authentication Endpoints

http

```
POST /api/auth/register              → 202 Accepted
POST /api/auth/verify-email          → 200 OK (access token + refresh cookie)
POST /api/auth/resend-verification   → 202 Accepted
POST /api/auth/login                 → 200 OK (access token + refresh cookie)
POST /api/auth/refresh               → 200 OK (yeni access token + rotate edilmiş refresh cookie)
POST /api/auth/logout                → 204 No Content
POST /api/auth/forgot-password       → 202 Accepted (her zaman)
POST /api/auth/verify-reset-code     → 200 OK ({ resetToken })
POST /api/auth/reset-password        → 204 No Content
POST /api/auth/change-password       → 204 No Content
GET  /api/auth/me                    → 200 OK (UserProfileDto)
```

Token alan response'ların body'si:

json

```
{
  "accessToken": "eyJ...",
  "expiresAt": "2026-11-25T18:30:00Z",
  "user": { "id": "...", "name": "...", "email": "...", "profileImage": null }
}
```

Akış detayları: `authentication.md`.

# 4. Profile Endpoints

http

```
GET /api/profile
PUT /api/profile
```

`PUT /api/profile` request:

json

```
{
  "name": "İrem",
  "profileImage": "https://..."
}
```

- `name` required.
- `profileImage` nullable, geçerli bir http/https URL olmalıdır.
- Email değiştirme MVP kapsamında değildir; istekteki fazladan alanlar yok sayılır.
- Her iki uç da `UserProfileDto` döner (`GET /api/auth/me` ile aynı yapı). `name` kırpılarak kaydedilir; boş/boşluk `profileImage` görseli kaldırır (null).

Şifre değiştirme: `POST /api/auth/change-password`.

# 5. Books Endpoints

http

```
GET    /api/books
GET    /api/books/{id}
POST   /api/books
PUT    /api/books/{id}
DELETE /api/books/{id}
```

## GET /api/books

Authenticated kullanıcının kitaplarını döndürür.

Başka kullanıcıların kitapları kesinlikle döndürülmemelidir.

Pagination desteklenir. Kitaplar `UpdatedAt` azalan sırayla döner.

Örnek:

http

```
GET /api/books?page=1&pageSize=18
```

- `page` varsayılan 1, minimum 1.
- `pageSize` yalnızca `18`, `36`, `72`, `108` değerlerinden biri olabilir; varsayılan 18. Diğer değerler 400 (`VALIDATION_ERROR`) döner.

Response:

json

```
{
  "items": [ BookListItemDto ],
  "page": 1,
  "pageSize": 18,
  "totalCount": 57,
  "hasNext": true
}
```

Opsiyonel `search` parametresi (en fazla 100 karakter) başlık veya yazarda büyük/küçük harf duyarsız arama yapar; Türkçe ı/I/İ harfleri "i" ile eşleşir. Ana sayfanın üst barındaki "Kitap adı veya yazar ara..." kutusu bu parametreyi kullanır.

http

```
GET /api/books?page=1&pageSize=18&search=dostoyevski
```

İleride desteklenebilecek filtreler:

- Status filtering
- Tag filtering

## GET /api/books/{id}

Belirtilen kitabın detaylarını döndürür (`BookDetailDto`).

Book ownership kontrol edilmelidir.

## POST /api/books

Yeni kitap oluşturur. 201 Created + `BookDetailDto` döner.

UserId request üzerinden alınmamalıdır.

Authenticated user ID backend tarafından belirlenmelidir.

Aynı kullanıcıda aynı ISBN'e sahip kitap varsa 409 Conflict (`BOOK_ISBN_EXISTS`) döner.

Yeni kitap tag'siz oluşturulur.

## PUT /api/books/{id}

Kitap bilgilerini günceller (tam güncelleme). 200 OK + `BookDetailDto` döner.

Ownership kontrolü zorunludur.

Status ve tarih kuralları (§13) uygulanır.

## DELETE /api/books/{id}

Kitabı siler. 204 No Content döner.

Ownership kontrolü zorunludur.

## GET /api/books/completed

"Okuduğum Kitaplar" listesi: yalnızca `COMPLETED` kitaplar, son bitirilen en üstte (FinishDate azalan, sonra UpdatedAt azalan). `page`, `pageSize` ve `search` parametreleri `GET /api/books` ile aynıdır.

Response: `PagedResult<CompletedBookDto>`.

json

```
{
  "id": "...",
  "title": "1984",
  "author": "George Orwell",
  "coverImage": null,
  "isbn": "9780451524935",
  "pageCount": 352,
  "rating": 9,
  "finishDate": "2026-11-20"
}
```

## PUT /api/books/{id}/status

Yalnızca okuma durumunu değiştirir; status/tarih kuralları uygulanır (§13). "Okuduğum Kitaplar" ekranındaki X butonu kitabı listeden çıkarmak için `{ "status": "READING" }` gönderir: kitap silinmez, bitiş tarihi temizlenir.

Request: `{ "status": "TO_READ" | "READING" | "COMPLETED" }`. Response: `BookDetailDto`. Ownership kontrolü zorunludur.

# 6. Google Books Endpoints

http

```
GET /api/google-books/search?query=...
GET /api/google-books/isbn/{isbn}
```

Google Books API çağrıları backend üzerinden yapılmalıdır.

Google Books API key frontend'e gönderilmemelidir.

Arama sonuçlarından mümkün olduğunca aşağıdaki bilgiler alınmalıdır:

- Title
- Author
- PageCount
- ISBN
- CoverImage

Mapping kuralları:

- Birden fazla yazar varsa `", "` ile birleştirilerek tek Author değeri üretilir.
- ISBN: `ISBN_13` varsa o, yoksa `ISBN_10` alınır; normalize edilir.
- CoverImage: `imageLinks.thumbnail` (yoksa `smallThumbnail`), `http://` → `https://` dönüştürülür.
- PageCount gelmezse `null` döner; frontend kullanıcıdan girmesini ister.
- Arama sonuçları en fazla 20 kayıtla sınırlanır.

Google Books categories / subjects bilgileri kullanılmayacaktır.

Google Books sonuçları doğrudan database'e kaydedilmemelidir.

Kullanıcı kitap bilgilerini değiştirebilmeli ve son kaydı onaylamalıdır.

Google Books erişilemezse, hata döndürürse (ör. 429 kota aşımı) veya geçersiz yanıt verirse 502 Bad Gateway (`GOOGLE_BOOKS_UNAVAILABLE`) döner. API key içeren URL loglanmaz; yalnızca durum kodu loglanır.

Diğer kurallar:

- İki endpoint de `GoogleBookSearchResultDto[]` döner (aynı ISBN'in birden fazla baskısı olabilir).
- `query` boş olamaz, en fazla 200 karakterdir. `isbn` normalize edilir ve checksum'ı geçersizse Google'a gidilmeden 400 döner.
- Başlığı olmayan sonuçlar atlanır. Bulunamayan alanlar `null` döner; kullanıcı formda tamamlar.
- Aynı arama 10 dakika boyunca sunucu belleğinde cache'lenir (kota tasarrufu).
- `GOOGLE_BOOKS_API_KEY` pratikte zorunludur: anahtarsız istekler Google tarafından 429 ile reddedilebilir.

# 7. Tags Endpoints

http

```
GET    /api/tags
POST   /api/tags
DELETE /api/tags/{id}
```

`POST /api/tags` request: `{ "name": "Klasik" }`. 201 Created + `TagDto` döner.

`DELETE /api/tags/{id}` tag'i ve tüm kitap ilişkilerini siler. 204 No Content döner.

Tag yeniden adlandırma (PUT) MVP kapsamında değildir.

Kitap-tag ilişkisi için:

http

```
POST   /api/books/{bookId}/tags
DELETE /api/books/{bookId}/tags/{tagId}
```

`POST /api/books/{bookId}/tags` request: `{ "tagId": "..." }`. 204 No Content döner; ilişki zaten varsa da 204 döner (idempotent).

Hem kitabın hem tag'in authenticated kullanıcıya ait olduğu kontrol edilmelidir.

Kullanıcı yalnızca kendi tag'lerini görüntüleyebilmeli ve yönetebilmelidir.

Tag oluşturulurken aynı kullanıcı için case-insensitive duplicate kontrolü yapılmalıdır; duplicate varsa 409 Conflict (`TAG_EXISTS`) döner.

Google Books categories / subjects bilgileri kullanılmamalıdır.

# 8. Reading Goals Endpoints

http

```
GET    /api/reading-goals
POST   /api/reading-goals
PUT    /api/reading-goals/{id}
DELETE /api/reading-goals/{id}
```

Reading Goal türleri:

text

```
BOOK_COUNT
PAGE_COUNT
```

Create/Update request:

json

```
{
  "type": "BOOK_COUNT",
  "target": 30,
  "startDate": "2026-01-01",
  "endDate": "2026-12-31"
}
```

Goal progress mevcut Book verilerinden hesaplanmalıdır (formüller: `database-design.md` §10).

Çakışan tarih aralıklı hedeflere izin verilir.

- `POST` 201 Created, `PUT` 200 OK (ikisi de `ReadingGoalDto` döner), `DELETE` 204 No Content.
- `GET` sayfalama yapmaz; hedefler `startDate` azalan (sonra `endDate` azalan) sırada döner.
- `target` 1–1.000.000 arası tam sayıdır; `startDate` ve `endDate` zorunludur ve `endDate > startDate` olmalıdır; aralık iki ucu dahil kabul edilir.
- `current` gerçek ilerlemedir ve hedefi aşabilir; `percentage` = ⌊current / target × 100⌋, 100 ile sınırlanmaz (bar'ı frontend sınırlar).
- Başka kullanıcının veya olmayan bir hedef 404 `READING_GOAL_NOT_FOUND` döner.

Kullanıcı yalnızca kendi hedeflerine erişebilmelidir.

# 9. Statistics Endpoints

http

```
GET /api/statistics/overview
GET /api/statistics/reading?period=1m
GET /api/statistics/reading?period=6m
GET /api/statistics/reading?period=1y
```

`period` yalnızca `1m`, `6m`, `1y` değerlerini alabilir. Dönem bugünden geriye kayan penceredir. Eksik veya farklı bir değer (ör. `2m`, `1M`) 400 `VALIDATION_ERROR` döner. `periodStart` = bugün − 1/6/12 ay, `periodEnd` = bugün (UTC); iki uç dahildir. `averageBookLength` tam sayıya yuvarlanır (0,5 yukarı). `booksByAuthor` sayıya göre azalan, eşitlikte yazar adına göre sıralanır.

## Overview

Şu istatistikleri döndürmelidir:

- Total books
- Completed books
- Reading books
- To-read books

json

```
{
  "totalBooks": 42,
  "completedBooks": 20,
  "readingBooks": 3,
  "toReadBooks": 19
}
```

## Reading Statistics

Seçilen döneme göre:

- Bu dönemde tamamlanan kitap sayısı
- Bu dönemde okunan sayfa sayısı
- Ortalama kitap uzunluğu
- Yazar bazında tamamlanan kitap sayıları

hesaplanmalıdır.

json

```
{
  "period": "6m",
  "periodStart": "2026-05-25",
  "periodEnd": "2026-11-25",
  "completedBooks": 8,
  "pagesRead": 3120,
  "averageBookLength": 352,
  "booksByAuthor": [
    { "author": "Fyodor Dostoyevski", "count": 2 }
  ]
}
```

Formüller: `database-design.md` §10.

Statistics tablosu oluşturulmamalıdır.

İstatistikler mevcut Book kayıtlarından hesaplanmalıdır.

MVP'de detaylı reading history tutulmadığından geçmiş sayfa hareketleri üzerinden analiz yapılmamalıdır.

# 10. DTOs

Entity modelleri doğrudan API response olarak expose edilmemelidir.

Aşağıdaki DTO'lar kullanılacaktır:

text

```
AuthResponseDto
BookListItemDto
CompletedBookDto
BookDetailDto
CreateBookDto
UpdateBookDto
PagedResult<T>
GoogleBookSearchResultDto
TagDto
UserProfileDto
UpdateProfileDto
ReadingGoalDto
CreateReadingGoalDto / UpdateReadingGoalDto
StatisticsOverviewDto
ReadingStatisticsDto
```

## BookListItemDto

Home Page için gereksiz Book bilgileri frontend'e gönderilmemelidir.

Örnek:

json

```
{
  "id": "...",
  "title": "Suç ve Ceza",
  "author": "Fyodor Dostoyevski",
  "coverImage": "...",
  "currentPage": 360,
  "pageCount": 500
}
```

Reading percentage frontend tarafından `currentPage` ve `pageCount` üzerinden hesaplanır.

Database'de saklanmamalıdır.

## BookDetailDto

Kitap detay ekranının ihtiyaç duyduğu bütün kitap bilgilerini içermelidir:

`id, title, author, isbn, pageCount, currentPage, status, coverImage, rating, notes, startDate, finishDate, tags: TagDto[], createdAt, updatedAt`

## CreateBookDto

`title, author, pageCount, currentPage?, status?, coverImage?, isbn?, rating?, notes?, startDate?, finishDate?`

`status` verilmezse `TO_READ`, `currentPage` verilmezse 0 kabul edilir.

UserId authenticated user context üzerinden alınmalıdır.

## UpdateBookDto

`title, author, pageCount, currentPage, status, coverImage?, isbn?, rating?, notes?, startDate?, finishDate?`

Tag'ler bu DTO ile değil, kitap-tag endpoint'leri ile yönetilir.

## GoogleBookSearchResultDto

`googleBooksId, title, author, pageCount?, isbn?, coverImage?`

Google Books categories / subjects bilgileri DTO'ya dahil edilmemelidir.

## TagDto

`id, name`

## UserProfileDto

`id, name, email, profileImage`

## ReadingGoalDto

json

```
{
  "id": "...",
  "type": "PAGE_COUNT",
  "target": 10000,
  "startDate": "2026-01-01",
  "endDate": "2026-12-31",
  "current": 4200,
  "percentage": 42
}
```

`percentage` 100 ile sınırlanmaz (hedef aşılabilir); UI gösterimde sınırlayabilir.

## StatisticsOverviewDto / ReadingStatisticsDto

Bkz. §9 örnekleri.

# 11. Validation

Backend validation her zaman uygulanmalıdır.

FluentValidation kullanılacaktır.

Book validation:

text

```
Title required
Author required
PageCount required, > 0
CurrentPage >= 0
CurrentPage <= PageCount
Rating null veya 1-10
Status geçerli enum değerlerinden biri
StartDate <= FinishDate (ikisi de doluysa)
ISBN null veya geçerli ISBN-10 / ISBN-13 (checksum dahil)
CoverImage null veya geçerli http/https URL
```

Diğer validation kuralları:

text

```
Tag: Name required, en fazla 50 karakter
ReadingGoal: Type geçerli enum, Target > 0, EndDate > StartDate
Profile: Name required, ProfileImage null veya geçerli URL
Register: Name required (en fazla 100), Email geçerli, Password en az 8 karakter + en az bir harf + en az bir rakam
Login: Email geçerli, Password required, RememberMe (bool)
VerifyEmail / VerifyResetCode: Code 6 haneli rakam
ResetPassword / ChangePassword: NewPassword şifre kuralına uygun
```

Frontend validation kullanıcı deneyimi için ayrıca uygulanmalıdır.

Ancak güvenlik ve veri bütünlüğü açısından frontend validation'a güvenilmemelidir.

# 12. Reading Percentage Calculation

Database'de ReadingPercentage alanı bulunmamalıdır.

Hesaplama:

text

```
(CurrentPage / PageCount) * 100
```

Division by zero engellenmelidir.

Hesaplama frontend tarafından yapılır; sonuç tam sayıya yuvarlanır.

# 13. Book Status Rules

Desteklenen durumlar:

text

```
TO_READ
READING
COMPLETED
```

Backend tarafından create ve update işlemlerinde uygulanır:

- COMPLETED'a geçişte CurrentPage = PageCount; FinishDate boşsa bugünün tarihi atanır.
- READING'e geçişte StartDate boşsa bugünün tarihi atanır.
- COMPLETED'dan çıkışta FinishDate temizlenir.
- CurrentPage'in PageCount'a ulaşması status'u değiştirmez.

# 14. API Error Format

Tüm hata response'ları aynı formatı kullanır:

json

```
{
  "message": "Book not found.",
  "code": "BOOK_NOT_FOUND"
}
```

Validation hatalarında alan bazlı hatalar `errors` alanında döner:

json

```
{
  "message": "One or more validation errors occurred.",
  "code": "VALIDATION_ERROR",
  "errors": {
    "pageCount": ["PageCount must be greater than 0."]
  }
}
```

Hatalar global exception handling middleware ile bu formata dönüştürülür.

`message` geliştirici/fallback amaçlıdır; frontend kullanıcıya gösterilecek metni (Türkçe veya İngilizce) `code` üzerinden i18n dosyalarından seçer.

Örnek kodlar:

text

```
VALIDATION_ERROR
UNAUTHORIZED
FORBIDDEN
NOT_FOUND            (bilinmeyen route)
METHOD_NOT_ALLOWED
INVALID_CREDENTIALS
EMAIL_ALREADY_EXISTS
VERIFICATION_CODE_INVALID
VERIFICATION_CODE_EXPIRED
VERIFICATION_ATTEMPTS_EXCEEDED
INVALID_REFRESH_TOKEN
INVALID_RESET_TOKEN
INVALID_CURRENT_PASSWORD
PASSWORD_SAME_AS_CURRENT
BOOK_NOT_FOUND
BOOK_ISBN_EXISTS
TAG_NOT_FOUND
TAG_EXISTS
READING_GOAL_NOT_FOUND
GOOGLE_BOOKS_UNAVAILABLE
RATE_LIMITED
INTERNAL_ERROR
```

# 15. HTTP Responses

Kullanılan HTTP status code'ları:

- 200 OK
- 201 Created
- 202 Accepted (register, resend-verification, forgot-password)
- 204 No Content
- 400 Bad Request (validation hataları dahil)
- 401 Unauthorized
- 404 Not Found (başka kullanıcıya ait kaynaklar dahil)
- 409 Conflict (duplicate email, ISBN, tag)
- 429 Too Many Requests
- 500 Internal Server Error
- 502 Bad Gateway (Google Books erişilemezse)

422 kullanılmaz.

# 16. Authorization Requirements

Bütün kullanıcıya özel kaynaklarda ownership kontrolü yapılmalıdır.

Özellikle:

- Books
- Tags
- Reading Goals
- Profile
- Statistics

User ID request body'sinden veya query parametresinden güvenilir kabul edilmemelidir.

Authenticated user ID server-side (JWT `sub` claim) alınmalıdır.

Başka kullanıcıya ait kaynaklarda 404 dönülür.

# 17. Pagination

Book list endpoint'i pagination destekler.

Örnek:

http

```
GET /api/books?page=1&pageSize=18
```

Response şekli `PagedResult<T>`: `items, page, pageSize, totalCount, hasNext`.

Frontend sonsuz kaydırma ile sonraki sayfaları listeye ekler ve her zaman `pageSize=18` kullanır; API 36, 72 ve 108 değerlerini de kabul eder.

# 18. OpenAPI / Swagger

API OpenAPI / Swagger üzerinden dokümante edilmelidir.

Request DTO'ları, response DTO'ları, endpoint'ler ve hata cevapları dokümantasyonda tanımlanmalıdır.

Swagger UI üzerinden Bearer token ile test yapılabilmelidir.

Swagger UI yalnızca Development ortamında açıktır.
