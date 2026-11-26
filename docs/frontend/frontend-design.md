# Frontend Design

# 1. Overview

Kitaphane frontend uygulaması React, TypeScript ve Vite kullanılarak geliştirilecektir.

Frontend backend ile REST API üzerinden iletişim kurmalıdır.

UI ve business logic birbirinden mümkün olduğunca ayrılmalıdır.

Uygulamanın nihai UI/UX tasarımları Penpot üzerinden sağlanacaktır.

UI Türkçe (varsayılan) ve İngilizce desteklenir; metinler i18n dosyalarında tutulur.

# 2. Technology Stack

- React
- TypeScript
- Vite
- Tailwind CSS
- shadcn/ui (Penpot tasarım sistemi shadcn/ui tabanlıdır)
- React Router (routing, protected routes)
- TanStack Query (server state, cache, pagination)
- react-hook-form + zod (formlar ve frontend validation)
- i18next + react-i18next (Türkçe ve İngilizce metinler)

Backend communication:

- REST API
- Merkezi `fetch` tabanlı HTTP client (`lib/http.ts`): base URL, Authorization ve Accept-Language header'ları, 401'de refresh denemesi, hata formatının parse edilmesi.

# 3. Project Structure

Feature-based organization kullanılır:

text

```
src/
├── app/
│   ├── router.tsx          (route tanımları, protected routes)
│   ├── providers.tsx       (QueryClient, AuthProvider, i18n)
│   └── main.tsx
├── components/
│   ├── ui/                 (shadcn/ui component'leri)
│   └── ...                 (ortak component'ler: EmptyState, ErrorMessage, ConfirmDialog vb.)
├── layouts/
│   ├── AuthLayout.tsx
│   └── AppLayout.tsx
├── features/
│   ├── auth/               (AuthProvider, token yönetimi, login/register/verify/reset formları)
│   ├── books/              (BookCard, BookForm, status kuralları, readingPercentage)
│   ├── google-books/       (arama, sonuç listesi)
│   ├── tags/               (TagPicker)
│   ├── profile/            (profil formu, şifre değiştirme)
│   ├── statistics/
│   └── reading-goals/
├── pages/                  (route seviyesindeki ince bileşenler)
├── lib/                    (http client, i18n, utils)
├── assets/                 (Penpot'tan alınan görseller)
└── locales/
    ├── tr.json
    └── en.json
```

Her feature klasörü kendi `api.ts` (endpoint çağrıları), `hooks.ts` (TanStack Query hook'ları), `types.ts` ve component'lerini içerir.

## components/

Birden fazla ekranda kullanılabilecek ortak UI component'leri. shadcn/ui component'leri `components/ui/` altındadır.

## pages/

Uygulamanın sayfa seviyesindeki ekranları. İş mantığı içermez; feature component'lerini birleştirir.

## layouts/

Ortak sayfa layout'ları.

## features/

Book, Tag, Profile, Reading Goals gibi feature bazlı organizasyon.

## lib/

HTTP client ve ortak yardımcı fonksiyonlar.

## locales/

i18n metin dosyaları (`tr.json`, `en.json`; anahtar setleri aynı olmalıdır). API hata `code` değerlerinin karşılıkları da burada tutulur.

Dil seçimi `localStorage` içinde (`kitaphane.language`) hatırlanır. İlk açılışta tarayıcı dili `en` ise İngilizce, değilse Türkçe kullanılır. Seçili dil API'ye `Accept-Language` ile iletilir; e-postalar bu dilde gönderilir.

Dil seçici (TR/EN) auth ekranlarının ve uygulama header'ının sağ üstünde bulunur.

# 4. Frontend Requirements

Frontend'de aşağıdaki özellikler bulunmalıdır:

- API service layer
- Authentication state
- Protected routes
- Loading states
- Error states
- Form validation
- API error handling

API URL gibi configuration değerleri environment variable üzerinden alınmalıdır.

Frontend validation yalnızca kullanıcı deneyimi amacıyla kullanılmalıdır.

Backend validation her zaman esas alınmalıdır.

# 5. Authentication

## 5.1. Screens

Aşağıdaki ekranlar bulunmalıdır:

| Route | Ekran (Penpot) |
|---|---|
| `/login` | Giriş Yap ("Oturumumu açık tut" checkbox'ı ile) |
| `/register` | Kayıt Ol (Ad Soyad, E-posta, Şifre, Şifreyi Onayla) |
| `/verify-email` | Kodu Doğrula (kayıt) |
| `/forgot-password` | Şifremi Unuttum |
| `/forgot-password/verify` | Kodu Doğrula (şifre sıfırlama) |
| `/reset-password` | Yeni Şifre Oluştur |

E-posta ve reset token adımlar arasında URL yerine router state ile taşınır (kişisel veri URL'de yer almaz). State olmadan açılan ara ekranlar akışın başına yönlendirir.

Auth ekranları ortak `AuthLayout` kullanır: solda Penpot'taki kitap illüstrasyonu (`assets/auth-illustration.webp`), sağda form. Dar ekranlarda illüstrasyon gizlenir.

Giriş yapmış kullanıcı auth ekranlarına girerse ana sayfaya yönlendirilir.

Logout işlemi desteklenmelidir.

Kayıt akışı:

text

```
Register formu → POST /register → Email Verification ekranı
→ kod girilir → POST /verify-email → hesap oluşturulur, kullanıcı giriş yapmış olarak ana sayfaya yönlendirilir
```

Şifre sıfırlama akışı:

text

```
Şifremi Unuttum → POST /forgot-password → Kodu Doğrula → POST /verify-reset-code (resetToken)
→ Yeni Şifre Oluştur → POST /reset-password → login ekranı (e-posta önceden dolu)
```

"Kodu tekrar gönder" butonu 60 saniye bekleme süresi gösterir.

## 5.2. Token Management

- Access token yalnızca bellekte (AuthProvider state) tutulur; localStorage/sessionStorage kullanılmaz.
- Refresh token HttpOnly cookie'dedir; frontend tarafından okunamaz.
- Uygulama açılışında `POST /api/auth/refresh` çağrılarak oturum sessizce yenilenir; bu sırada loading gösterilir.
- 401 alındığında HTTP client bir kez refresh dener ve isteği tekrarlar; aynı anda gelen 401'ler tek refresh isteğini bekler. Auth akışı endpoint'leri (login, register, refresh vb.) bu mekanizmanın dışındadır.
- Refresh token her kullanımda değiştiği için sekmeler arası refresh Web Locks API ile sıraya konur.
- Refresh başarısız olursa auth state temizlenir ve kullanıcı login ekranına yönlendirilir.
- Refresh cookie'sinin gönderilmesi için auth istekleri `credentials: "include"` ile yapılır.

Authenticated olmayan kullanıcılar protected routes üzerinden korunan sayfalara erişememelidir.

# 6. Home Page

Ana sayfanın amacı kullanıcının kendi kitaplarını göstermektir.

Ana sayfada yalnızca authenticated user's books gösterilmelidir.

Kitaplar CARD layout ile, son güncellenen en üstte olacak şekilde gösterilmelidir.

Her kitap kartında yalnızca şu bilgiler bulunmalıdır:

- Cover image
- Book title
- Author
- Reading percentage

Kart üzerinde başka kitap bilgisi gösterilmemelidir.

Kapak görseli yoksa placeholder gösterilir.

Örnek:

text

```
┌──────────────────┐
│                  │
│      COVER       │
│                  │
├──────────────────┤
│ Suç ve Ceza      │
│ Fyodor Dostoyevski│
│                  │
│ ████████░░ 80%   │
└──────────────────┘
```

Kitap kartına tıklandığında ilgili Book Detail ekranı açılmalıdır.

Liste pagination ile yüklenir (TanStack Query `useInfiniteQuery`). Sonraki sayfa, listenin sonuna yaklaşıldığında (IntersectionObserver) otomatik yüklenir.

Uygulama kabuğu Penpot "Ana Sayfa" yerleşimini izler, boyutlar ise standart web ölçülerindedir (Penpot 1920px tuval için çizildiğinden birebir ölçüler fazla büyük kalıyordu):

Uygulama ekranları (sol menü + üst bar olan sayfalar) %125 ölçekte gösterilir: `AppLayout` açıkken `<html>` öğesine `app-scale` sınıfı eklenir ve kök yazı boyutu %125 olur. Bu ekranlarda tüm boyutlar rem ile verildiği için yazı, boşluk ve ikonlar birlikte büyür (tarayıcı yakınlaştırmasıyla aynı etki). Giriş/kayıt ekranları Penpot ölçülerinde (%100) kalır. Aşağıdaki ölçüler %100 değerleridir; ekranda 1,25 katı görünür.

- Sol menü: 72px genişlik, 1px kenarlık, üstte 32px logo ikonu; dikeyde ortalanmış 40px butonlar (24px ikon): Ana Sayfa, Okuduğum Kitaplar, Profil. Aktif buton `#FF6D6D` zemin + beyaz ikon. Kitap ekranları "Ana Sayfa" altında sayılır. Mobilde alt bar olur.
- Üst bar: 64px. Solda arama kutusu (20px ikon, 14px metin), sağda dil seçici (36px yükseklik, TR/EN + bayrak) ve kullanıcı (36px avatar + ad + küçük aşağı ok; menü açıkken ok yukarı döner); kullanıcıya tıklanınca Profil ve Çıkış yap menüsü açılır.
- Kitap grid'i: en fazla 170px genişliğinde kartlar, 24px sütun ve 32px satır aralığı, sola hizalı (telefonda 2 sütun). Kartta başlık 15px kalın, yazar 14px, yüzde 12px ve 6px yüksekliğinde ilerleme çubuğu.
- Arama terimi ana sayfanın adresinde tutulur (`/?q=...`); arama sırasında "Kitap Ekle" kartı gizlenir ve sonuç sayısı gösterilir.
- Profil ekranlarında (`/profile`, `/profile/edit`) üst bardaki arama kutusu gösterilmez.
- Okuduğum Kitaplar ekranında üst bardaki arama o ekranın listesini (yalnızca okunmuş kitaplar) kitap adı veya yazara göre filtreler; terim `/read-books?q=...` adresinde tutulur ve sonuç sayısı gösterilir. Diğer ekranlarda yazılan arama ana sayfayı açar.
- Profil (`/profile`) §10'da anlatılır.

## 6.1. Okuduğum Kitaplar (`/read-books`)

Penpot "Okunan Kitaplar" ekranı. Sol menüdeki saat ikonuyla açılır.

- Bu ekran Penpot ölçülerinin %90'ını kullanır (değerler rem = Penpot px / 20 × 0,9; uygulamanın %125 ölçeğinde 1rem = 20px). Aşağıdaki değerler Penpot'taki tasarım değerleridir; ekranda 0,9 katı görünür. Sol menü ve üst bar bu ölçekten etkilenmez.
- Başlık "OKUDUĞUM KİTAPLAR" (28px, kalın, büyük harf), 30px altında satırlar; satırlar 50px aralıklı ve içerik alanının tamamı genişliğinde.
- Her satır: 219×321 kapak (10px köşe, gölge), 50px sağında başlık (28px kalın), 5px altında puan satırı (44px: 24px sayı + 10 adet 24px yıldız, 10px aralık; dolu `#FBBC05`, boş `#181821` çizgi), 40px altında Yazar / ISBN / Sayfa Sayısı / Bitiş Tarihi (etiket 20px kalın, değer 20px; sütunlar arası 100px). Tarih `gg/aa/yyyy` biçimindedir.
- X: 40px kutu içinde 25px ikon, satır üstünden 141px aşağıda. Satırlar içerik alanını kaplar; X'in yatay ortası üst bardaki TR/EN dil seçicisinin ortasıyla hizalıdır. Dil seçicinin yeri kullanıcı adının uzunluğuna göre değiştiği için konum çalışma anında ölçülür (`useOffsetFromRight`, `data-language-switcher`) ve pencere/öğe boyutu değişince güncellenir. Mobilde X satırın sağ üst köşesindedir. Yıldız ve X ikonları Penpot'tan alınan SVG'lerdir.
- Mobilde kapak küçülür ve bilgiler iki sütunda gösterilir.
- Sağdaki X butonu kitabı silmez: onay penceresinden sonra durumu "Okunuyor" yapar (bitiş tarihi temizlenir) ve kitap listeden kalkar.
- Kapak ve başlık kitap ekranına gider. Liste sonsuz kaydırma ile 18'lik gruplar halinde yüklenir.

Okuma yüzdesi dile göre biçimlenir (Türkçe `%60`, İngilizce `60%`). Kapak görseli yoksa kitap adını gösteren bir yer tutucu gösterilir.

Sayfa boyutu seçicisi yoktur; kitaplar aşağı kaydırıldıkça 18'lik gruplar halinde yüklenir.

Kullanıcının hiç kitabı yoksa empty state ile birlikte yalnızca "Kitap Ekle" kartı gösterilir.

# 7. Add Book Card

Home Page'de kitapların sonunda özel bir:

text

```
+ Kitap Ekle
```

kartı bulunmalıdır.

Bu kart yeni kitap ekleme ekranını açmalıdır.

Kullanıcının kitap sayısı arttığında ve yeni sayfalar yüklendiğinde "Kitap Ekle" kartı her zaman yüklenmiş kitapların sonunda kalmalıdır.

Örnek:

text

```
Kitap 1
Kitap 2
Kitap 3
Kitap 4
+ Kitap Ekle
(sonuna gelince sonraki kitaplar yüklenir)
```

# 8. Book Detail Page

Kullanıcı kitap kartına (veya Okuduğum Kitaplar'daki bir kitaba) tıkladığında `/books/:id` adresinde Penpot "Kitap Bilgileri 1 / 2" ekranı açılır. Ekran Penpot ölçülerinin %90'ını kullanır (rem = Penpot px / 20 × 0,9; uygulamanın %125 ölçeğinde 1rem = 20px). Aşağıdaki değerler tasarım değerleridir. Ekran yalnızca görüntüleme içindir:

- "< Geri" (50px ok + 24/600 metin): önceki sayfaya döner. Sağda küçük bir "Düzenle" butonu (tasarımda yok) düzenleme formunu açar.
- 50px altında: 312×457 kapak (10px köşe, gölge); 50px sağında başlık (32/700) ve puan satırı (24px sayı + 10 yıldız).
- 20px altında tek bir durum etiketi (206×50): kitabın mevcut durumunu (OKUNUYOR / OKUNACAK / OKUNDU) kendi renginde gösterir (Okunuyor `#2563EB`, Okunacak `#F59E0B`, Okundu `#10B981`; "Kitap Bilgileri 2"). Tıklanamaz.
- 50px altında bilgiler: 1. satır Yazar, ISBN; 2. satır Toplam Sayfa, Okunan Sayfa, Başlangıç Tarihi, Bitiş Tarihi (sütunlar 285 : 288 : 302 : 105 oranında, satırlar arası 22px; etiket 20/700, değer 20/500). Tarih `gg/aa/yyyy`.
- 52px altında notlar (18/500): en fazla 6 satır + "...Devamını oku"; açılınca tamamı + "Gizle". Not yoksa bölüm gösterilmez.
- Penpot'taki gibi bilgi alanı (sütunlar ve notlar) üst bardaki profil resminin sol kenarına kadar uzanır. Profil resminin konumu kullanıcı adına göre değiştiği için çalışma anında ölçülür (`useOffsetFromRight`, `data-user-avatar`); geniş ekranlarda (lg) uygulanır.

Puan, durum ve diğer tüm alanlar yalnızca `/books/:id/edit` adresindeki form ile değiştirilir; kayıttan sonra detay ekranına dönülür. Kitap silme bu formdadır. Düzenleme ve Kitap Ekle (`/books/new`, Google Books araması dahil) sayfaları da detay ekranı gibi %90 ölçekte gösterilir ("< Geri" butonu hariç) ve içerik sağda üst bilgideki profil resminin sol kenarına kadar genişler (lg ve üzeri). Geniş ekranda (≥ 1536 px) her iki sayfa da kaydırmadan tek ekrana sığar: Google Books bölümü iki satırdır (başlık + açıklama; arama türü + arama kutusu + buton), formda Kitap Adı | Yazar, ISBN | Durum | Toplam Sayfa | Okunan Sayfa ve Puan | Başlangıç | Bitiş | Kapak Görseli yan yana durur, Notlar alanı daha kısadır. Daha dar ekranlarda Kapak Görseli ayrı satıra iner.

Reading percentage CurrentPage ve PageCount üzerinden hesaplanmalıdır.

# 9. Add Book Page

Kullanıcı yeni kitap ekleme ekranına erişebilmelidir.

Kitap ekleme sırasında iki arama yöntemi desteklenmelidir:

- Kitap adı ile ara.
- ISBN ile ara.

Google Books sonuçları kullanıcıya sunulmalıdır.

Kullanıcı bir sonuç seçtiğinde aşağıdaki alanlar forma otomatik doldurulabilmelidir:

- Title
- Author
- PageCount
- ISBN
- CoverImage

Kullanıcı bu bilgileri kaydetmeden önce değiştirebilmelidir.

Ekleme ekranının üstünde "Google Books'ta ara" bölümü bulunur: arama türü (Kitap adı / yazar veya ISBN), arama kutusu ve sonuç listesi (küçük kapak, başlık, yazar, sayfa, ISBN, "Seç"). ISBN aramasında ISBN istemci tarafında doğrulanır. "Seç" formu önerilen değerlerle yeniden doldurur ve forma kaydırır; eksik alan varsa kullanıcı uyarılır. Google Books'a ulaşılamazsa hata mesajı gösterilir ve form elle doldurulabilir.

Google Books sayfa sayısı döndürmediyse PageCount alanı boş gelir ve zorunlu alan olarak işaretlenir.

Kullanıcı Google Books araması yapmadan formu elle de doldurabilir. Elle ekleme formu Phase 3'te `/books/new` adresinde hazırdır; Phase 4 bu formun üstüne Google Books aramasını ekler.

Form, status değişince backend kurallarını önizler (Okunuyor → başlangıç tarihi bugün, Okundu → okunan sayfa = toplam sayfa ve bitiş tarihi bugün); tarihler kullanıcının yerel gününe göre doldurulur.

Aynı ISBN'e sahip kitap zaten varsa backend'in döndürdüğü `BOOK_ISBN_EXISTS` hatası kullanıcıya gösterilir.

Google Books categories / subjects kullanılmayacaktır.

Kullanıcı onayı olmadan kitap kaydedilmemelidir.

# 10. Profile Page

Profile ekranında aşağıdaki bölümler bulunmalıdır.

Penpot'taki "Profil" board'u boş olduğundan ekran uygulamanın kart stiliyle tasarlanmıştır (kullanıcı kararı, Faz 7). Diğer uygulama ekranları gibi %90 ölçekte gösterilir ve geniş ekranda içerik üst bardaki profil resminin sol kenarına kadar uzanır.

`/profile` ekranında görünür bir "Profil" başlığı yoktur (yalnızca ekran okuyucular için); üstte kullanıcı kartı (avatar, ad, e-posta, sağda "Düzenle" butonu; mobilde yalnızca kalem ikonu) ve altında istatistikler yer alır:

- "Kitaplarım" başlığı altında 4 kitap istatistiği kartı (Toplam / Okunan / Okunuyor / Okunacak; renk şeritleri durum renkleriyle aynı).
- Okuma İstatistikleri: dönem seçici (Son 1 ay / Son 6 ay / Son 1 yıl, varsayılan 1 ay), dönem tarih aralığı, 3 sayı kartı (tamamlanan kitap, okunan sayfa, ortalama kitap uzunluğu) ve yazarlara göre tamamlanan kitaplar listesi (çoktan aza; kitap yoksa boş durum metni).

"Düzenle" hesap ayarlarını `/profile/edit` ekranında açar ("< Geri" + "Profili Düzenle"): "Profil bilgileri" formu (Ad Soyad, salt okunur e-posta, profil görseli URL'si, canlı avatar önizlemesi, Kaydet) ve "Şifre değiştir" formu (mevcut şifre, yeni şifre, yeni şifre tekrar). Profil kaydedilince üst bardaki avatar ve ad hemen güncellenir ve profil ekranına dönülür; şifre değişince ekranda kalınır. Çıkış, üst bardaki kullanıcı menüsünden yapılır.

İstatistikler sayfa her açıldığında yeniden yüklenir. Okuma Hedefleri bölümü "Kitaplarım" ile Okuma İstatistikleri arasındadır (bkz. aşağıdaki Reading Goals).

## User Information

- Name
- Email
- Profile image
- Account settings

Account settings:

- Name güncelleme
- Profile image URL güncelleme (dosya yükleme yok; görsel yoksa baş harf avatarı gösterilir)
- Şifre değiştirme (mevcut şifre + yeni şifre)
- Logout (üst bardaki kullanıcı menüsünden)

Email değiştirilemez, yalnızca görüntülenir.

## Book Statistics

- Total books
- Completed books
- Reading books
- To-read books

## Reading Statistics

Zaman aralığı seçimi:

text

```
Son 1 ay
Son 6 ay
Son 1 yıl
```

Seçilen döneme göre:

- Bu dönemde tamamlanan kitap sayısı
- Bu dönemde okunan sayfa sayısı
- Ortalama kitap uzunluğu
- Hangi yazarlardan kaç kitap okunduğu

gösterilmelidir.

Hesaplama backend'de yapılır (`GET /api/statistics/reading?period=...`).

MVP'de aylık grafik bulunmamaktadır.

## Reading Goals

Profil ekranında kullanıcı okuma hedeflerini yönetebilmelidir.

Desteklenen işlemler:

- Create
- Update
- Delete
- View progress

Hedef türleri:

- BOOK_COUNT (Kitap sayısı)
- PAGE_COUNT (Sayfa sayısı)

Hedef başlığı tür, hedef değer ve tarih aralığından üretilir (ör. "1 Oca 2026 – 31 Ara 2026: 30 kitap").

İlerleme progress bar ile gösterilir; hedef aşıldıysa bar %100'de sınırlanır, sayısal değer gerçek ilerlemeyi gösterir.

Uygulama (Faz 8): Hedefler kart ızgarasında listelenir (başlık, tür, ilerleme çubuğu, "12 / 30 kitap" ve yüzde). Hedefe ulaşılınca çubuk yeşil olur ve "Hedefe ulaşıldı" yazar. "Hedef ekle" ve kalem ikonu aynı form penceresini açar (Hedef türü, Hedef, Başlangıç / Bitiş tarihi; yeni hedef varsayılan olarak bu takvim yılı için kitap sayısıdır; form üretilecek başlığı canlı gösterir). Çöp kutusu ikonu onay penceresinden sonra hedefi siler. Hedef yoksa açıklamalı boş durum gösterilir.

# 11. Tags UI

Kullanıcı kitap detay ekranından manuel olarak tag ekleyebilmelidir.

Tag seçici (TagPicker) üzerinden:

- Mevcut tag'ler aranıp kitaba atanabilir.
- Yeni tag oluşturulup kitaba atanabilir.
- Tag kitaptan kaldırılabilir.
- Tag tamamen silinebilir (onay dialogu ile; tag tüm kitaplardan kaldırılır).

Tag yeniden adlandırma MVP kapsamında değildir.

Google Books kategorileri otomatik olarak kullanılmamalıdır.

Yeni oluşturulan kitap başlangıçta tagsiz olmalıdır.

# 12. Reading Progress UI

Kullanıcı kitap detay ekranından CurrentPage değerini güncelleyebilmelidir.

Reading percentage:

text

```
(CurrentPage / PageCount) * 100
```

formülü üzerinden hesaplanmalı ve tam sayıya yuvarlanmalıdır.

Division by zero engellenmelidir.

Database'de ReadingPercentage saklanmamalıdır.

Kitap COMPLETED olduğunda CurrentPage PageCount ile eşit olmalıdır (backend tarafından zorunlu uygulanır).

# 13. UI/UX Design Reference

Uygulamanın nihai UI/UX tasarımları PENPOT MCP üzerinden sağlanacaktır.

Ekran tasarımları (referans dosya):

https://design.penpot.app/?screen=workspace&team-id=d8ac01df-6646-81d2-8008-a5b3acef92c8&file-id=a64f76ef-45a2-80c7-8008-cc7b1aaeddfb&page-id=ce6a0147-2492-8048-8008-cc7a0ef2c741&board-id=4d119694-5a44-52db-996d-c0510bb9d680

Tasarım sistemi shadcn/ui tabanlıdır.

Agent bu tasarımları, component'leri ve ikonları incelemelidir.

Uygulama bu tasarım sistemi temel alınarak geliştirilmelidir.

Tasarım token'ları (renkler, tipografi, radius, spacing) Phase 1'de Tailwind/shadcn temasına aktarılır; ekranlar ilgili fazlarda tasarıma uygun geliştirilir.

Business logic ve UI birbirinden mümkün olduğunca ayrılmalıdır.

Tasarım uygulanırken mevcut ürün gereksinimleri değiştirilmemeli ve yeni özellikler uydurulmamalıdır.

# 14. Environment Configuration

API URL gibi configuration değerleri environment variable üzerinden yönetilmelidir.

Örnek:

env

```
VITE_API_BASE_URL=/api
```

Local development'ta Vite dev server `/api` isteklerini backend'e proxy'ler (frontend ve backend aynı origin'den görünür).

Backend secret değerleri frontend environment dosyalarına aktarılmamalıdır.

Google Books API key frontend'e kesinlikle gönderilmemelidir.

# 15. UX States

Uygulamada gerekli durumlar desteklenmelidir:

- Loading
- Error
- Empty state
- Form validation
- API error feedback

API hata mesajları kullanıcıya anlaşılır şekilde gösterilmelidir.

Hata metinleri API'nin döndürdüğü `code` değerine göre `locales/tr.json` ya da `locales/en.json` içinden (seçili dile göre) seçilir; bilinmeyen kodlar için genel bir hata metni gösterilir. Validation hataları (`errors`) ilgili form alanlarının altında gösterilir.

# 16. Frontend Development Principles

- Strong TypeScript typing kullanılmalıdır.
- Business logic component'lerin içine gereksiz şekilde dağıtılmamalıdır.
- Reusable component'ler tercih edilmelidir.
- Gereksiz abstraction oluşturulmamalıdır.
- API işlemleri service layer (feature `api.ts` + TanStack Query hook'ları) üzerinden yürütülmelidir.
- Authentication state merkezi ve tutarlı yönetilmelidir.
- Protected routes uygulanmalıdır.
- UI metinleri component içine gömülmemeli, i18n üzerinden kullanılmalıdır.
- UI tasarımları Penpot referansına uygun olmalıdır.

# 17. Final UI Integration

Final aşamada React UI, sağlanan Penpot tasarımlarına göre tamamlanmalıdır.

Mevcut ürün gereksinimleri korunmalıdır.

Tasarım entegrasyonu sırasında yeni işlevler eklenmemeli veya mevcut gereksinimler kaldırılmamalıdır.
