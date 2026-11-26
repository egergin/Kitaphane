# Database Design

# 1. Database Overview

Kitaphane PostgreSQL kullanacaktır.

ORM olarak Entity Framework Core, PostgreSQL provider olarak Npgsql kullanılacaktır.

Database schema EF Core migrations üzerinden yönetilecektir.

MVP kapsamında aşağıdaki temel tablolar bulunacaktır:

- AspNetUsers (ASP.NET Core Identity)
- Books
- Tags
- BookTags
- ReadingGoals
- RefreshTokens
- PendingRegistrations
- PasswordResetCodes

Statistics veya ReadingProgress tablosu oluşturulmayacaktır.

## 1.1. Genel Konvansiyonlar

- Tüm primary key'ler `Guid` (PostgreSQL `uuid`) tipindedir.
- Okuma ve hedef tarihleri (StartDate, FinishDate, EndDate) saat bilgisi içermez: `DateOnly` → PostgreSQL `date`.
- Zaman damgaları (CreatedAt, UpdatedAt, ExpiresAt vb.) UTC olarak saklanır: `DateTime` (UTC) → PostgreSQL `timestamptz`.
- CreatedAt ve UpdatedAt değerleri DbContext üzerinden otomatik atanır.
- Enum'lar database'de string olarak saklanır (ör. `COMPLETED`).

# 2. User Entity

ASP.NET Core Identity kullanılacaktır.

User modeli IdentityUser üzerinden genişletilebilir.

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

- Id: Primary key (Guid).
- Name: Kullanıcının adı.
- Email: Unique, doğrulanmış email.
- PasswordHash: Identity tarafından yönetilen şifre hash'i.
- ProfileImage: Profil görseli URL'si.
- CreatedAt: Kayıt zamanı.
- UpdatedAt: Son güncellenme zamanı.

CreatedAt ve UpdatedAt kullanıcıya gösterilmeyecektir.

Şifreler kesinlikle plaintext olarak saklanmamalıdır.

# 3. Book Entity

text

```
Book
-----------------------
Id
UserId
Title
Author
PageCount
CurrentPage
Status
CoverImage
ISBN
Rating
Notes
StartDate
FinishDate
CreatedAt
UpdatedAt
```

## 3.1. Id

Primary key.

## 3.2. UserId

Kitabın sahibi olan kullanıcının foreign key alanıdır.

text

```
Book.UserId -> User.Id
```

Her Book kaydı bir kullanıcıya ait olmalıdır.

Domain'deki Book entity'si Identity user nesnesine navigation property ile bağlanmaz; ilişki yalnızca `UserId` foreign key'i ile Infrastructure katmanındaki entity configuration'da kurulur.

## 3.3. Title

Kitap adı.

Required.

Google Books üzerinden otomatik gelebilir ancak kullanıcı değiştirebilir.

## 3.4. Author

Kitap yazarı.

Required.

Google Books üzerinden otomatik gelebilir ancak kullanıcı değiştirebilir.

## 3.5. PageCount

Toplam sayfa sayısı.

Required. Pozitif integer olmalıdır.

Google Books sayfa sayısı döndürmezse kullanıcı tarafından girilmelidir.

## 3.6. CurrentPage

Kullanıcının mevcut okuma sayfası.

Default:

text

```
0
```

Validation:

text

```
0 <= CurrentPage <= PageCount
```

## 3.7. Status

Enum:

text

```
TO_READ
READING
COMPLETED
```

UI karşılıkları:

text

```
TO_READ  -> Okunacak
READING  -> Okunuyor
COMPLETED -> Okundu
```

Status kuralları (Application katmanında uygulanır):

- COMPLETED'a geçişte CurrentPage = PageCount yapılır; FinishDate boşsa bugünün tarihi atanır.
- READING'e geçişte StartDate boşsa bugünün tarihi atanır.
- COMPLETED'dan çıkışta FinishDate temizlenir.
- CurrentPage'in PageCount'a ulaşması status'u otomatik değiştirmez.

## 3.8. CoverImage

Kitap kapak görselinin URL'si.

Nullable olabilir.

Google Books tarafından sağlanabilir.

## 3.9. ISBN

Kitabın ISBN değeridir.

Nullable olabilir.

Desteklenecek formatlar:

- ISBN-10
- ISBN-13

Normalize edilmiş halde (tire ve boşluklar temizlenmiş, ISBN-10'daki `X` büyük harf) saklanır.

Google Books her iki formatı da döndürüyorsa ISBN-13 tercih edilir.

Aynı kullanıcı için unique'dir: `(UserId, ISBN)` unique index, yalnızca ISBN null değilse uygulanır.

## 3.10. Rating

Kullanıcının kitaba verdiği puan.

Nullable integer.

Validation:

text

```
1 <= Rating <= 10
```

Puan verilmemişse null olmalıdır.

## 3.11. Notes

Kullanıcının kitaba ilişkin kişisel notları.

Nullable.

## 3.12. StartDate

Kitabın okunmaya başlandığı tarih (`date`).

Nullable.

## 3.13. FinishDate

Kitabın tamamlandığı tarih (`date`).

Nullable.

Kitap COMPLETED değilse null'dır.

StartDate ve FinishDate birlikte doluysa StartDate <= FinishDate olmalıdır.

## 3.14. CreatedAt

Book kaydının oluşturulma zamanı.

## 3.15. UpdatedAt

Book kaydının son güncellenme zamanı.

# 4. Tag Entity

text

```
Tag
----------------
Id
UserId
Name
CreatedAt
```

Tag kullanıcı tarafından oluşturulur.

Google Books kategorileri kullanılmayacaktır.

Aynı kullanıcı aynı tag adını case-insensitive şekilde tekrar oluşturamamalıdır.

Tag'ler kullanıcıya ait olmalıdır.

Kullanıcı kendi tag'lerini birden fazla kitabında kullanabilmelidir.

Tag silindiğinde ilgili BookTag kayıtları da silinir (cascade).

Tag yeniden adlandırma MVP kapsamında değildir.

# 5. BookTag Entity

text

```
BookTag
----------------
BookId
TagId
```

Book ve Tag arasında many-to-many ilişki sağlar.

Composite primary key kullanılır:

text

```
(BookId, TagId)
```

İlişki:

text

```
Book N ─── N Tag
```

# 6. ReadingGoal Entity

text

```
ReadingGoal
----------------
Id
UserId
Type
Target
StartDate
EndDate
CreatedAt
UpdatedAt
```

## 6.1. Type

text

```
BOOK_COUNT
PAGE_COUNT
```

## 6.2. Target

Kullanıcının ulaşmak istediği hedef değeri.

Pozitif olmalıdır.

## 6.3. StartDate

Hedef başlangıç tarihi (`date`). Required.

## 6.4. EndDate

Hedef bitiş tarihi (`date`). Required.

EndDate > StartDate olmalıdır.

Aynı kullanıcının çakışan tarih aralıklarına sahip birden fazla hedefi olabilir.

Hedef başlık alanı yoktur; UI başlığı Type, Target ve tarih aralığından üretir.

Hedef ilerlemesi mevcut Book verilerinden hesaplanmalıdır (bkz. §10).

# 6A. RefreshToken Entity

text

```
RefreshToken
----------------
Id
UserId
TokenHash
ExpiresAt
CreatedAt
RevokedAt
ReplacedByTokenId
IsPersistent
```

- TokenHash: Refresh token'ın SHA-256 hash'i; ham değer saklanmaz. Unique index.
- ExpiresAt: Token geçerlilik bitişi.
- RevokedAt: Logout, rotate veya şifre değişikliği ile iptal zamanı (nullable).
- ReplacedByTokenId: Rotate sonrası yerine geçen token (nullable). Reuse detection için kullanılır.
- IsPersistent: "Oturumumu açık tut" tercihi; rotate edilen token'a aktarılır.

Hash'ler (refresh token, doğrulama kodu, reset token) sunucu secret'ından türetilen anahtarla HMAC-SHA256 olarak saklanır.

Süresi dolmuş/iptal edilmiş token'lar periyodik olarak temizlenebilir.

# 6B. PendingRegistration Entity

Kayıt isteği e-posta doğrulanana kadar burada bekler; doğrulama sonrası kullanıcı oluşturulur ve kayıt silinir.

text

```
PendingRegistration
----------------
Id
Email
NormalizedEmail
Name
PasswordHash
CodeHash
CodeExpiresAt
FailedAttempts
LastCodeSentAt
CreatedAt
```

- Email / NormalizedEmail: Girilen email ve aramalarda kullanılan normalize hali. NormalizedEmail unique (aynı email için tek bekleyen kayıt).
- PasswordHash: Identity `IPasswordHasher` ile üretilmiş hash.
- CodeHash: 6 haneli doğrulama kodunun hash'i.
- CodeExpiresAt: Kod geçerlilik bitişi (oluşturulduktan 15 dakika sonra).
- FailedAttempts: Yanlış deneme sayısı (en fazla 5).
- LastCodeSentAt: Tekrar gönderim sınırlaması için (60 saniye).

Süresi dolmuş bekleyen kayıtlar periyodik olarak veya yeni kayıt isteğinde temizlenir.

# 6C. PasswordResetCode Entity

Kod tabanlı şifre sıfırlama durumunu tutar. Kullanıcı başına en fazla bir kayıt bulunur.

text

```
PasswordResetCode
----------------
Id
UserId
CodeHash
CodeExpiresAt
FailedAttempts
LastCodeSentAt
ResetTokenHash
ResetTokenExpiresAt
CreatedAt
```

- UserId: Unique; kullanıcı silinince kayıt da silinir (cascade).
- CodeHash: 6 haneli sıfırlama kodunun hash'i. Kod doğrulandığında null yapılır (tek kullanımlık).
- CodeExpiresAt, FailedAttempts, LastCodeSentAt: Kayıt doğrulama koduyla aynı kurallar (15 dk, 5 deneme, 60 sn).
- ResetTokenHash / ResetTokenExpiresAt: Kod doğrulandıktan sonra verilen 15 dakikalık tek kullanımlık reset token. Şifre değiştirildiğinde kayıt silinir.

# 7. Relationships

Ana ilişkiler:

text

```
User 1 ─── N Book

User 1 ─── N ReadingGoal

User 1 ─── N Tag

User 1 ─── N RefreshToken

Book N ─── N Tag
```

Book-Tag many-to-many ilişkisi BookTag üzerinden kurulacaktır.

Delete davranışları:

- Book silindiğinde BookTag kayıtları silinir (cascade).
- Tag silindiğinde BookTag kayıtları silinir (cascade).
- User silindiğinde Book, Tag, ReadingGoal, RefreshToken ve PasswordResetCode kayıtları silinir (cascade). Hesap silme MVP UI'ında yoktur.

# 8. Indexes

Performans için gerekli index'ler oluşturulmalıdır.

Index'ler:

text

```
Book (UserId, UpdatedAt DESC)                    -- ana sayfa listeleme ve sıralama
Book (UserId, ISBN) UNIQUE WHERE ISBN IS NOT NULL -- kullanıcı başına ISBN tekilliği

Tag (UserId, lower(Name)) UNIQUE                 -- kullanıcı başına case-insensitive tag tekilliği

ReadingGoal (UserId)

RefreshToken (TokenHash) UNIQUE
RefreshToken (UserId)

PendingRegistration (NormalizedEmail) UNIQUE
PasswordResetCode (UserId) UNIQUE
```

User email unique olmalıdır (Identity NormalizedEmail index'i).

Books tablosunda ayrıca check constraint'ler bulunur: `PageCount > 0`, `0 <= CurrentPage <= PageCount`, `Rating NULL veya 1-10`. Status `TO_READ / READING / COMPLETED` string olarak saklanır.

Status ve Title için ayrı index oluşturulmaz; MVP'de bu alanlara göre filtreleme yoktur. İstatistik sorguları `(UserId, ...)` index'i ile kullanıcı bazında daraltılır.

# 9. Reading Percentage

Database'de ReadingPercentage alanı oluşturulmayacaktır.

Hesaplama:

text

```
(CurrentPage / PageCount) * 100
```

Örnek:

text

```
CurrentPage = 250
PageCount = 500
```

Sonuç:

text

```
50%
```

Division by zero engellenmelidir.

Reading percentage frontend tarafından hesaplanır; API CurrentPage ve PageCount değerlerini döner.

# 10. Statistics Strategy

Statistics isimli bir tablo oluşturulmayacaktır.

Aşağıdaki bilgiler Book verilerinden hesaplanmalıdır:

- Total books
- Completed books
- Reading books
- To-read books
- Completed books in selected period
- Pages read in selected period
- Average book length
- Books completed by author

Gereksiz aggregate tablolar oluşturulmamalıdır.

Dönem `[PeriodStart, Today]` olarak bugünden geriye kayan pencere ile tanımlanır (1m, 6m, 1y).

Formüller:

text

```
CompletedBooks  = Books WHERE Status = COMPLETED AND FinishDate IN period

CompletedCount  = COUNT(CompletedBooks)

PagesRead       = SUM(CompletedBooks.PageCount)
                + SUM(Books.CurrentPage WHERE Status = READING AND StartDate IN period)

AverageLength   = AVG(CompletedBooks.PageCount)   -- kitap yoksa 0

ByAuthor        = CompletedBooks GROUP BY Author → COUNT
```

Reading Goal ilerlemesi aynı formüllerle, period yerine hedefin `[StartDate, EndDate]` aralığı kullanılarak hesaplanır:

text

```
BOOK_COUNT → CompletedCount
PAGE_COUNT → PagesRead
```

# 11. Reading History Limitation

MVP'de ReadingProgress tablosu oluşturulmayacaktır.

Aşağıdaki gibi geçmiş ilerleme kayıtları tutulmayacaktır:

text

```
10 September -> 100 pages
11 September -> 130 pages
12 September -> 170 pages
```

Yalnızca Book.CurrentPage üzerinden mevcut ilerleme takip edilecektir.

# 12. Global Book Catalog Limitation

Global veya shared BookDefinition tablosu oluşturulmayacaktır.

Aynı kitap 100 farklı kullanıcı tarafından eklenirse database'de 100 ayrı Book kaydı bulunabilir.

Bu bilinçli bir MVP tasarım kararıdır.

# 13. Database Migrations

Entity Framework Core migrations kullanılmalıdır.

İlk migration oluşturulmalıdır.

Database schema source control altında migration dosyalarıyla yönetilmelidir.

Development ortamında migration'lar uygulama açılışında otomatik uygulanır.

Production ortamında migration'lar uygulama açılışında otomatik çalışmaz; ayrı bir adım (CLI / CI) ile uygulanır.

Production ortamında database manuel olarak değiştirilmemelidir.

# 14. Data Validation

Backend seviyesinde aşağıdaki kurallar uygulanmalıdır:

- Title required.
- Author required.
- PageCount required ve > 0.
- CurrentPage >= 0.
- CurrentPage <= PageCount.
- Rating null veya 1-10.
- Status geçerli enum değerlerinden biri.
- StartDate <= FinishDate (ikisi de doluysa).
- ISBN-10 ve ISBN-13 formatları desteklenmeli (checksum dahil); kullanıcı başına unique.
- Tag Name required, kullanıcı başına case-insensitive unique.
- ReadingGoal Target pozitif olmalı.
- ReadingGoal EndDate > StartDate.

Detaylı API validation kuralları `api-design.md` dosyasında tanımlanacaktır.