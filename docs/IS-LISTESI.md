# ProKantar iş listesi

Son güncelleme: 05.11.2026. Bu dosya kaldığımız yeri ve sıradaki işleri tutar; yeni bir oturuma buradan başlanır.
Repolar: `DropDeart/ProKantar` (backend) ve `DropDeart/ProKantarUI` (ön yüz).

---

## 1. Sıradaki iş: sevkiyat yükü ve kantarın sevkiyattan beslenmesi (KARARLAŞTI)

Kullanıcı kararları (05.11):

- **Yük, teslimat durağına yazılır.** Her teslimat durağında farklı ürünler olabilir. Bir durağa birden çok satır eklenebilir: malzeme (tanımlı malzemelerden), planlanan miktar ve birim (ton / m³ / adet / kg).
- **Kantar kesinlikle sevkiyattan beslenir.** Tartım sırasında malzeme bilgisi sorulmaz; kantarın tek işi ölçüm yapıp sonucu göndermek. Malzeme, alıcı ve teslim adresi sevkiyat planından gelir.
- **İrsaliye şoföre e-postayla gider.** Sefer yayınlanınca irsaliye şoföre otomatik e-postayla gönderilir.
- **Yazdırma isteğe bağlı.** Çıktı alınacaksa tanımlara gerekirse yazıcı eklenir.

Netleşmesi gerekenler (başlamadan önce kullanıcıya sor):

- [ ] **İrsaliyenin kesilme anı.** Kullanıcı "irsaliye sefere yayınlanınca" dedi. Kodda ise şu an çıkış tartımı tamamlanınca kesiliyor (o anda net ağırlık belli). İki seçenek var:
  - Yayınlanınca planlanan miktarla kesilir; tartım sonrası fark varsa uyarı çıkar.
  - Yayın ile tartımın sırası korunur: yayın → çıkış tartımı → irsaliye otomatik kesilir → şoföre e-posta.
- [ ] **Şoföre e-posta.** Platform Driver kaydında e-posta alanı yok, yalnız telefon var. E-posta alanı mı eklenecek, yoksa SMS / mobil uygulama bildirimi mi?
- [ ] **Çok duraklı sevkiyatta miktar.** Tek kantar neti birden çok durağa nasıl bölünecek? Öneri: her durağın irsaliyesine planlanan miktar yazılır, net ile plan toplamı belirli bir oranın üzerinde saparsa (ör. %2) uyarı verilir.

Yapılacaklar (öneri):

- [ ] kantar: `ShipmentStopLine` (ShipmentStopId, MaterialId, PlannedQuantity, Unit) ve plan kaydetme komutu (`Custom/Shipments/SaveShipmentPlanCommand.cs`).
- [ ] UI: `ShipmentForm` → `StopsEditor` içinde teslimat durağı başına yük satırları.
- [ ] Kantar: plaka ile sevkiyata bağlanan tartımda malzeme plandan gelir. Tartım ekranında malzeme seçimi kaldırılır ya da salt okunur yapılır.
- [ ] İrsaliye satırları: tartımın malzemesinden değil, sevkiyat durağının yük satırlarından kurulur (`kantar/Custom/EDespatch/Issuing/DespatchDraftBuilder.cs`). Teslimat durağı başına bir irsaliye kesilir, `ShipmentStop.WaybillId` doldurulur.
- [ ] Sevkiyat detayında planlanan ve tartılan miktar yan yana gösterilir.
- [ ] Otomatik e-posta (yukarıdaki karara göre).
- [ ] Tanımlara yazıcı (isteğe bağlı).

## 2. Unutulmuş iş: yol haritası ve tahmini varış süresi

- [ ] Sefer için rota çizimi (sevkiyat detayı ve haritası) ile durak başına tahmini varış süresi.
  - Planlanırken (sevkiyat formunda) ve seyir sırasında, son konuma göre güncellenen.
  - Yönlendirme servisi seçilmeli: OSRM / GraphHopper (kendi sunucumuzda) ya da ücretli bir API. Şu an konum araması Nominatim ile yapılıyor; production için ücretli sağlayıcı gerekecek.

## 3. e-İrsaliye: yapıldı, açık kalanlar

Yapıldı (05.11):
- Sağlayıcıdan bağımsız altyapı, `kantar/Custom/EDespatch/`. Üç yöntem var:
  - Yalnız kâğıt
  - XML indir, elle yükle (GİB portalı ya da başka entegratör)
  - e-Logo
- Ayarlar: `/panel/ayarlar/e-irsaliye`. Şifre DataProtection ile şifreli saklanıyor.
- Tartım detayı `/panel/tartimlar/[id]` üzerinden irsaliye kesme, XML indirme, durum işaretleme ve yazdırma.
- Üretilen XML GİB XSD ve şematronundan 0 hatayla geçiyor.

Açık kalanlar:
- [ ] **e-Logo ile gerçek gönderim denenmedi**, demo test hesabı gerekiyor.
  - Önce ayarlardan "Bağlantıyı test et".
  - Elle deneme betiği: `elogo_client.py send` (scratchpad'te; gerekirse repoya `tools/` olarak alınabilir).
- [ ] e-Logo durum kodu eşlemesi tahmine dayanıyor; kodda "DOĞRULANMALI" notlu. Canlı URL de tahmin.
- [ ] GİB'e kayıtlı olmayan alıcı için varsayılan etiket `urn:mail:irsaliyepk@gib.gov.tr` teyit edilmeli (`Providers/GibAliases.cs`).
- [ ] **Ürün kararı:** GİB kuralları (TCKN, posta kodu) kâğıt irsaliyeye de uygulanıyor. Kâğıtta gevşetilsin mi?
- [ ] Şoför TCKN'si için ekran yok. Şu an operatör irsaliye keserken giriyor; mobil uygulamada kayıt sırasında sorulmalı (mobil ekip).
- [ ] GİB doğrudan entegrasyonu (kiracının kendi mali mührüyle) ve diğer entegratörler: yeni bir `IEDespatchProvider` yazılarak eklenir.
- [ ] e-İrsaliye iptali: bildiğimiz kadarıyla GİB'de iptal yok, düzeltme yeni irsaliyeyle yapılıyor. Doğrulanmalı.

## 4. Sunucuya taşırken (deploy notları)

- [ ] **Servis hesabı şifresi.** identity `.env` → `Auth__Clients__2__Secret` ile kantar `.env` → `Auth__ServiceClient__ClientSecret` aynı değer olmalı (`kantar-service` istemcisi). Girilmezse irsaliye keserken 500 döner.
- [ ] **Proxy ağı.** Servisler nginx/Docker arkasındaysa `ForwardedHeaders__KnownNetworks__0=<proxy ağı>` girilmeli (identity ve platform). Girilmezse bütün kullanıcılar tek bir deneme sınırını paylaşır.
- [ ] **identity portu.** `identity/docker-compose.yml` 8081'i bütün arayüzlere açıyor (`127.0.0.1:` önekiyle kısıtlanmalı).
- [ ] **Yeniden başlatma sırası:** identity ve platform → kantar → tracking. Tracking önce kalkarsa sefer durumu olayları kaybolur.
- [ ] Eski "Yayında" sevkiyatlar sefer durumuyla otomatik güncellenmez; tek seferlik SQL gerekebilir.
- [ ] Yeni kolonlar ve tablolar servis açılışında idempotent SQL ile kuruluyor (EnsureCreated var olan DB'ye eklemez).
- [ ] **Regen riski.** Üretici yeniden çalışırsa şu elle değişiklikler ezilir ve yeniden uygulanmalı:
  - kantar: `Kantar.csproj`, `Data/KantarDbContext.cs`, üretilmiş Entity/Dto/Command dosyalarına eklenen alanlar
  - identity: `Program.cs`
  - platform: `Program.cs`, `Grpc/DriverGrpcService.cs`

## 5. Diğer açık işler

- [ ] Admin sayfalarında (`src/app/(admin)/admin/page.tsx`, `admin/basvurular/[tenantId]`) hata/403'te bitmeyen yükleme ekranı. Panel için yazılan `useAsync` + `LoadError` ile çevrilmeli.
- [ ] Aynı fiş numarasıyla iki istasyon aynı anda planlanmamış çıkış tartımı yaparsa benzersiz indeks yüzünden 500 dönebilir (`CompleteWeighingCommand`). Çok düşük ihtimal.
- [ ] **Adım 3** (mobil ekiple):
  - şoför atamayı uygulamada kabul etsin;
  - konum izni (LocationConsent) sunucuda denetlensin;
  - geç gelen konumlar kayıt zamanına göre işlensin (sıralama düzeltmesi yapıldı, kabul akışı yok).
- [ ] **Karar:** şoför ad soyad araması tüm platformda mı, yalnız firmayla çalışmış şoförlerde mi?
- [ ] Haritaya tıklayınca il/ilçe/adres/posta kodu otomatik dolsun.
- [ ] İç modal açılınca dış formda "Nokta adı" hatası görünmesin.
- [ ] Tartım detayında irsaliye kartı için ayrıca tartım iptal düğmesi (backend ucu var: `POST api/Weighings/{id}/cancel`).
- [ ] Tablolarda posta kodu kolonu yok, yalnız formlarda var.
- [ ] Menüye "Ayarlar" eklenince üst çubukta e-posta yalnız 2xl ve üzerinde gösteriliyor; gözden geçirilmeli.
- [ ] BaseForge: istemcinin iptal ettiği istekler ERR olarak loglanıyor; BaseForge backlog'una.
- [ ] Production'da Nominatim yerine ücretli bir adres/konum sağlayıcısı.

## 6. Yerel test notları

- Demo kiracı: `E2E Lojistik 1793804329539`, tenant `49bc6b5d-bf9a-4d8e-b635-5f99f47ea5e0`, e-posta `e2e1793804329539@test.com` (şifre seed / test notlarında).
- Demo şoförler: Mehmet Yılmaz ve Ayşe Demir, deneme amaçlı geçerli test TCKN'leri girili. Plaka 41KL4471'e ikisi de bağlı.
- Yerel DB'de 3 test irsaliyesi var: PKN2026000000001–003.
- e-İrsaliye doğrulama araçları bu oturumun geçici klasöründeydi (`build_despatch.py`, `validate.py`, `elogo_client.py`; GİB paketleri ebelge.gib.gov.tr `UBL-TR1.2.1_Paketi.zip` ve `e-FaturaPaketi.zip`). Yeni makinede gerekirse yeniden indirilir.
