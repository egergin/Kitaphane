# Software Requirements Specification (SRS)

# 1. Project Overview

## 1.1. Project Name

Kitaphane

## 1.2. Project Description

Kitaphane, kullanıcıların okudukları, okumakta oldukları ve gelecekte okumak istedikleri kitapları kişisel olarak takip edebileceği bir web uygulamasıdır.

Her kullanıcı yalnızca kendi kitaplarını görüntüleyebilmeli ve yönetebilmelidir.

Uygulama bir MVP olarak geliştirilecektir. Gereksiz karmaşıklıktan, overengineering'den ve ihtiyaç duyulmayan özelliklerden kaçınılmalıdır.

## 1.3. Main User Flow

1. Kullanıcı kayıt olur (e-postasına gelen doğrulama kodunu girerek hesabını oluşturur) veya giriş yapar.
2. Ana sayfada kendisine ait kitapları kartlar halinde görüntüler.
3. Kitap kartına tıklayarak kitap detaylarına ulaşır.
4. Kitabın bilgilerini ve okuma durumunu görüntüler veya düzenler.
5. Kitap Ekle kartından yeni kitap ekler.
6. Kitap adı veya ISBN ile Google Books API üzerinden arama yapar.
7. Google Books sonuçlarından kitap seçer.
8. Kitap bilgileri forma otomatik doldurulur.
9. Kullanıcı bilgileri değiştirebilir ve onaylayarak kaydedebilir.
10. Kullanıcı okuma durumunu ve mevcut sayfa ilerlemesini takip eder.
11. Profil ekranında kitap ve okuma istatistiklerini görüntüler.
12. Profil ekranından okuma hedefleri oluşturur ve yönetir.

# 2. Functional Requirements

## FR-01: User Authentication

Uygulama kullanıcı hesabı sistemine sahip olmalıdır.

Desteklenen işlemler:

- Register (e-posta doğrulama kodu ile)
- Email Verification
- Login
- Logout
- Token Refresh
- Forgot Password (e-posta ile 6 haneli kod)
- Reset Password (kod doğrulandıktan sonra yeni şifre)
- Change Password
- Current User Information

Kayıt sırasında e-posta doğrulaması zorunludur. Kullanıcı hesabı, e-postaya gönderilen doğrulama kodu girilene kadar oluşturulmaz.

Şifre sıfırlama da e-postaya gönderilen 6 haneli kod ile yapılır (Şifremi Unuttum → Kodu Doğrula → Yeni Şifre Oluştur).

Giriş ekranında "Oturumumu açık tut" seçeneği bulunur: işaretliyse oturum 7 gün sürer, değilse tarayıcı kapanınca sona erer.

Şifre en az 8 karakter olmalı, en az bir harf ve bir rakam içermelidir. Yeni şifre mevcut şifreden farklı olmalıdır (şifre geçmişi tutulmaz).

Authentication JWT (kısa ömürlü access token + HttpOnly cookie'de refresh token) ile yapılır.

Kullanıcı yalnızca kendi hesabına ait verilere erişebilmelidir.

Detaylar: `authentication.md`

## FR-02: Book Management

Kullanıcı kendi kitaplarını oluşturabilmeli, görüntüleyebilmeli, güncelleyebilmeli ve silebilmelidir.

Her kitap bir kullanıcıya ait olmalıdır.

Desteklenen kitap alanları:

- Title
- Author
- PageCount
- CurrentPage
- Status
- CoverImage
- ISBN
- Rating
- Notes
- StartDate
- FinishDate
- CreatedAt
- UpdatedAt

Kitap durumları:

- TO_READ
- READING
- COMPLETED

Zorunlu alanlar: Title, Author, PageCount (> 0).

Aynı kullanıcı aynı ISBN'e sahip bir kitabı ikinci kez ekleyemez. ISBN girilmemiş kitaplarda bu kural uygulanmaz.

Kullanıcı kitap detay ekranından kitabı onay vererek silebilir.

Status ve tarih kuralları (backend tarafından zorunlu uygulanır):

- Kitap COMPLETED durumuna getirildiğinde CurrentPage = PageCount yapılır; FinishDate boşsa bugünün tarihi atanır.
- Kitap READING durumuna getirildiğinde StartDate boşsa bugünün tarihi atanır.
- Kitap COMPLETED durumundan başka bir duruma alındığında FinishDate temizlenir.
- StartDate ve FinishDate birlikte doluysa StartDate <= FinishDate olmalıdır.
- CurrentPage değerinin PageCount'a ulaşması status'u otomatik olarak değiştirmez.

## FR-03: Reading Progress

Kullanıcı mevcut okuma sayfasını güncelleyebilmelidir.

Reading percentage şu değerlerden hesaplanmalıdır:

(CurrentPage / PageCount) * 100

ReadingPercentage database'de ayrı bir alan olarak tutulmamalıdır.

Reading percentage frontend tarafından hesaplanır; API CurrentPage ve PageCount değerlerini döner.

CurrentPage değeri 0 ile PageCount arasında olmalıdır.

Kitap COMPLETED durumuna getirildiğinde CurrentPage otomatik olarak PageCount yapılır (bkz. FR-02).

## FR-04: Google Books Integration

Kullanıcı kitap adı veya ISBN üzerinden Google Books API ile kitap arayabilmelidir.

Google Books üzerinden mümkün olduğunca şu bilgiler alınmalıdır:

- Title
- Author
- PageCount
- ISBN
- CoverImage

Google Books categories / subjects bilgileri kullanılmayacaktır.

Google Books sonuçları doğrudan kaydedilmemelidir.

Kullanıcı kitap bilgilerini düzenleyebilmeli ve son kaydı onaylamalıdır.

Google Books sonucu sayfa sayısı içermiyorsa PageCount alanı boş gelir ve kullanıcı doldurmadan kitap kaydedilemez.

ISBN normalize edilir (tire/boşluk temizlenir); Google Books her iki formatı da döndürüyorsa ISBN-13 tercih edilir.

## FR-05: Tags

Kullanıcı kendi tag'lerini oluşturabilmelidir.

Tag'ler kitaplara atanabilmeli ve kaldırılabilmelidir.

Google Books kategorileri otomatik olarak tag oluşturmak için kullanılmayacaktır.

Yeni kitap oluşturulduğunda başlangıçta hiçbir tag bulunmamalıdır.

Kullanıcı kendi tag'lerini silebilir; silinen tag tüm kitaplardan kaldırılır.

Tag yeniden adlandırma MVP kapsamında değildir.

## FR-06: Book Details

Kitap detay ekranında aşağıdaki alanlar görüntülenebilmeli ve düzenlenebilmelidir:

- Cover
- Title
- Author
- ISBN
- Page count
- Current page
- Status
- Rating
- Tags
- Notes
- Start date
- Finish date

Rating 1–10 arası sayı seçici ile girilir.

## FR-07: User Profile

Kullanıcı profil ekranında şu bilgileri görüntüleyebilmelidir:

- Name
- Email
- Profile image
- Account settings

Account settings kapsamı:

- Name güncelleme
- Profile image URL güncelleme (dosya yükleme MVP kapsamında değildir)
- Şifre değiştirme (mevcut şifre doğrulanarak)

Email değiştirme MVP kapsamında değildir.

## FR-08: Book Statistics

Profil ekranında aşağıdaki istatistikler gösterilmelidir:

- Total books
- Completed books
- Reading books
- To-read books

## FR-09: Reading Statistics

Kullanıcı aşağıdaki dönemlerden birini seçebilmelidir:

- 1 Month
- 6 Months
- 1 Year

Seçilen döneme göre aşağıdaki istatistikler hesaplanmalıdır:

- Bu dönemde tamamlanan kitap sayısı
- Bu dönemde okunan sayfa sayısı
- Ortalama kitap uzunluğu
- Hangi yazarlardan kaç kitap okunduğu

İstatistikler mevcut Book verilerinden hesaplanmalıdır.

Dönem, bugünden geriye kayan pencere olarak tanımlanır (ör. 1 Month = son 1 ay: [bugün − 1 ay, bugün]).

Hesaplama kuralları:

- **Tamamlanan kitap sayısı:** Status = COMPLETED ve FinishDate dönem içinde olan kitaplar.
- **Okunan sayfa sayısı:** Dönem içinde tamamlanan kitapların PageCount toplamı + Status = READING ve StartDate dönem içinde olan kitapların CurrentPage toplamı.
- **Ortalama kitap uzunluğu:** Dönem içinde tamamlanan kitapların ortalama PageCount değeri (kitap yoksa 0).
- **Yazar dağılımı:** Dönem içinde tamamlanan kitapların Author alanına göre gruplanmış sayısı.

Aylık grafik MVP kapsamında bulunmamaktadır.

## FR-10: Reading Goals

Kullanıcı profil ekranından okuma hedefleri oluşturabilmeli, görüntüleyebilmeli, güncelleyebilmeli ve silebilmelidir.

Hedef türleri:

- BOOK_COUNT
- PAGE_COUNT

Örnek hedefler:

- 2026 yılında 30 kitap okumak.
- 2026 yılında 10000 sayfa okumak.

Hedef ilerlemesi mevcut Book verilerinden hesaplanmalıdır.

Hedef kuralları:

- Kullanıcı StartDate ve EndDate değerlerini serbestçe seçer; EndDate > StartDate olmalıdır.
- Target pozitif olmalıdır.
- Çakışan tarih aralıklarına sahip birden fazla hedef oluşturulabilir.
- BOOK_COUNT ilerlemesi: FinishDate hedef aralığında olan COMPLETED kitap sayısı.
- PAGE_COUNT ilerlemesi: FR-09'daki "okunan sayfa sayısı" kuralı, hedefin tarih aralığı için uygulanır.

## FR-11: Book Listing

Ana sayfada yalnızca authenticated user's books gösterilmelidir.

Kitaplar card layout ile listelenmelidir.

Kitaplar son güncellenen en üstte olacak şekilde (UpdatedAt azalan) sıralanır.

Book list API'si pagination destekler. UI sonsuz kaydırma ile sonraki sayfaları listeye ekler; "Kitap Ekle" kartı her zaman listenin en sonunda kalır.

Kitaplar 18'lik gruplar halinde yüklenir; sonraki kitaplar sayfanın sonuna gelindikçe otomatik yüklenir (sonsuz kaydırma). Kullanıcıya sayfa boyutu seçeneği sunulmaz.

Üst bardaki arama kutusu ile kitaplar başlık veya yazara göre aranabilir.

# 3. Non-Functional Requirements

## NFR-01: Security

- Passwords plaintext olarak saklanmamalıdır.
- User ownership server-side kontrol edilmelidir.
- API secrets frontend'e gönderilmemelidir.
- Google Books API key backend tarafında tutulmalıdır.
- SQL injection riskine karşı EF Core parameterized queries kullanılmalıdır.
- Backend validation zorunludur.
- CORS kontrollü yapılandırılmalıdır.
- Production ortamında HTTPS kullanılmalıdır.
- Access token yalnızca frontend belleğinde tutulmalı; refresh token HttpOnly, Secure, SameSite cookie içinde taşınmalı ve database'de hash'li saklanmalıdır.
- E-posta doğrulama kodları ve şifre sıfırlama token'ları süreli olmalı ve hash'li saklanmalıdır.
- Sensitive information (şifre, token, doğrulama kodu) loglanmamalıdır.

## NFR-02: Maintainability

- Business logic ve UI birbirinden ayrılmalıdır.
- Kod okunabilir ve maintainable olmalıdır.
- SOLID prensipleri dikkate alınmalıdır.
- Strongly typed yapılar tercih edilmelidir.
- Async/await kullanılmalıdır.
- Gereksiz abstraction oluşturulmamalıdır.

## NFR-03: Performance

- Book.UserId gibi sık kullanılan alanlarda index bulunmalıdır.
- Book list endpoint'i pagination desteklemelidir.
- İstatistikler gereksiz aggregate tabloları oluşturmadan hesaplanmalıdır.

## NFR-04: Configuration

Configuration değerleri environment variable üzerinden yönetilmelidir.

Secret değerleri source code içerisinde tutulmamalıdır.

## NFR-05: Containerization

Local development Docker Compose üzerinden çalıştırılabilmelidir.

## NFR-06: Localization

UI Türkçe (varsayılan) ve İngilizce desteklenir; kullanıcı dil seçiciyle TR/EN arasında geçiş yapabilir ve seçim tarayıcıda hatırlanır. İlk açılışta tarayıcı dili İngilizce ise İngilizce, değilse Türkçe kullanılır. UI metinleri i18n dosyalarında tutulur. Doğrulama ve şifre sıfırlama e-postaları seçili dilde gönderilir. Kod, API ve database isimlendirmeleri İngilizcedir.

## NFR-07: Testing

Backend iş kuralları (status/tarih kuralları, istatistik ve hedef hesaplamaları) birim testleriyle; ownership ve authentication akışları PostgreSQL üzerinde entegrasyon testleriyle doğrulanmalıdır.

# 4. MVP Limitations

## 4.1. No Reading History

MVP'de detaylı okuma geçmişi tutulmayacaktır.

ReadingProgress tablosu oluşturulmayacaktır.

Örneğin:

- 10 Eylül: 100. sayfa
- 11 Eylül: 130. sayfa
- 12 Eylül: 170. sayfa

gibi geçmiş ilerleme kayıtları saklanmayacaktır.

Yalnızca CurrentPage kullanılacaktır.

## 4.2. No Statistics Table

Statistics isimli bir database tablosu oluşturulmayacaktır.

İstatistikler mevcut Book verilerinden hesaplanacaktır.

## 4.3. No Global Book Catalog

Global/shared BookDefinition tablosu oluşturulmayacaktır.

Aynı kitap farklı kullanıcılar tarafından eklendiğinde her kullanıcı için ayrı Book kaydı oluşturulacaktır.

## 4.4. No Automatic Tags

Google Books categories / subjects bilgileri kullanılmayacaktır.

Tag'ler kullanıcı tarafından manuel oluşturulacaktır.

## 4.5. No Monthly Chart

MVP'de aylık okuma grafiği bulunmayacaktır.

# 5. Technology Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- Npgsql
- ASP.NET Core Identity
- JWT Bearer authentication + refresh token
- REST API
- OpenAPI / Swagger

### Frontend

- React
- TypeScript
- Vite
- Tailwind CSS + shadcn/ui
- React Router
- TanStack Query
- react-hook-form + zod
- i18next (Türkçe + İngilizce)

### Database

- PostgreSQL

### External API

- Google Books API

### Email

- SMTP
- Production: Resend (HTTPS API; Render ücretsiz planı SMTP portlarını engeller)
- Local development: Mailpit

### Validation

- FluentValidation

### Testing

- xUnit
- Testcontainers (PostgreSQL)

### Infrastructure

- Docker
- Docker Compose

# 6. Project Deliverables

1. Çalışan .NET backend
2. Çalışan React frontend
3. PostgreSQL database
4. EF Core migrations
5. Authentication
6. Book CRUD
7. Google Books integration
8. Tags
9. Reading Goals
10. Statistics
11. API documentation
12. Docker / Docker Compose
13. Environment configuration example
14. README

README içerisinde:

- Project setup
- Requirements
- Environment variables
- Database setup
- Migration
- Local development
- Docker usage
- API execution
- Frontend execution

açıklanmalıdır.

# 7. Development Phases

## Phase 1: Project Setup

- .NET setup
- React setup
- TypeScript setup
- PostgreSQL setup
- Docker setup (PostgreSQL, Backend, Frontend, Mailpit)
- EF Core setup
- Error handling middleware, CORS, Swagger
- Tailwind + shadcn/ui kurulumu, Penpot tasarım token'ları ve temel component'ler
- i18n altyapısı

## Phase 2: Authentication

- User
- Register + e-posta doğrulama kodu
- Login
- Logout
- Refresh token
- Forgot password (kod) / Reset password
- "Oturumumu açık tut"
- TR/EN dil seçici
- Protected routes

## Phase 3: Books

- Book entity (tüm alanlar: Rating, Notes, StartDate, FinishDate dahil)
- Migration
- CRUD
- Ownership
- Reading status ve tarih kuralları
- Reading percentage
- Home page kart listesi ve pagination

## Phase 4: Google Books

- Book search
- ISBN search
- Result selection
- Autofill
- Editable form

## Phase 5: Tags

- Tag
- BookTag
- Manual tag creation
- Tag deletion
- Book/tag association

## Phase 6: Book Details UI

- Book detail düzenleme ekranı
- Reading progress
- Rating (1–10 seçici)
- Notes
- Start/finish dates
- Tag seçici
- Kitap silme

## Phase 7: Profile

- User information
- Profil güncelleme ve şifre değiştirme
- Book statistics
- Reading statistics
- 1 month / 6 months / 1 year filters

## Phase 8: Reading Goals

- Create
- Update
- Delete
- Progress calculation

## Phase 9: Final UI Integration

React UI, Penpot tasarımlarına uygun şekilde tamamlanmalıdır.

Ürün gereksinimleri değiştirilmemeli ve yeni özellikler uydurulmamalıdır.