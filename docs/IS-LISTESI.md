# ProKantar iş listesi

Son güncelleme: 06.11.2026. Bu dosya kaldığımız yeri ve sıradaki işleri tutar; yeni bir oturuma buradan başlanır.
Repolar: `DropDeart/ProKantar` (backend) ve `DropDeart/ProKantarUI` (ön yüz).

---

## 1. Sıradaki iş: sevkiyat yükü ve kantarın sevkiyattan beslenmesi (KARARLAŞTI)

Kullanıcı kararları (05.11):

- **Yük, teslimat durağına yazılır.** Her teslimat durağında farklı ürünler olabilir. Bir durağa birden çok satır eklenebilir: malzeme (tanımlı malzemelerden), planlanan miktar ve birim (ton / m³ / adet / kg).
- **Kantar kesinlikle sevkiyattan beslenir.** Tartım sırasında malzeme bilgisi sorulmaz; kantarın tek işi ölçüm yapıp sonucu göndermek. Malzeme, alıcı ve teslim adresi sevkiyat planından gelir.
- **İrsaliye şoföre ulaştırılır** (06.11 kararıyla e-posta yerine push → SMS zinciri, aşağıda).
- **Yazdırma isteğe bağlı.** Çıktı alınacaksa tanımlara gerekirse yazıcı eklenir.

Netleşen kararlar (06.11):

- [x] **İrsaliye tartımdan sonra kesilir.** Sıra: yayın → çıkış tartımı → irsaliye otomatik kesilir → şoföre bildirim. Net ağırlık irsaliyede gerçek değerdir.
- [x] **Şoföre bildirim bir zincirle (pipeline) gider:** SignalR → push → SMS. E-posta kullanılmaz.
  1. **SignalR:** şoförün uygulaması platform hub'ına bağlıysa irsaliye oradan gider; uygulama "aldım" onayı döner. 10 sn içinde onay yoksa sonraki adım.
  2. **Push (FCM):** `Device.pushToken` doluysa bildirim gönderilir.
  3. **SMS:** push token yoksa ya da push başarısızsa irsaliye linki SMS ile gider.
  - Hub platform servisinde (Driver/Device orada); kullanıcı JWT'deki kimlikten tanınır. Her denemenin kanalı ve sonucu kaydedilir, sevkiyat detayında gösterilir.
  - Birden çok platform kopyasına geçilirse SignalR için Redis backplane eklenir.
  - Mobil ekip: Flutter `signalr_netcore` ile bağlanır, irsaliye mesajına onay (ack) gönderir.
  - Link: süreli ve imzalı, giriş istemeyen salt okunur irsaliye sayfası.
  - Sağlayıcılar arayüz arkasında, varsayılan Log: push için FCM, SMS için `ISmsSender` (Netgsm vb.).
- [x] **Çok duraklı sevkiyatta** her durağın irsaliyesine planlanan miktar yazılır. Net ile plan toplamı %2'den fazla saparsa uyarı verilir.

Yapılacaklar (öneri):

- [x] kantar: `ShipmentStopLine` (elle, `Custom/Shipments/`; malzeme kod/ad snapshot, miktar, birim) + plan komutunda durak başına `lines` + `GET api/Shipments/{id}/loads`. Çıkış sevkiyatında yüksüz teslimat durağı yayınlanamaz. Toplam yük (kg'a çevrilerek) kantar durağı istasyonundaki en büyük aktif kantarın kapasitesini aşamaz; kayıtta ve yayında denetlenir (`ShipmentLoadCapacity`). Tablo açılışta `ShipmentLoadSchema` ile kurulur (06.11).
- [x] UI: `ShipmentForm` → `StopsEditor` içinde durak başına yük satırları (`StopLoadsEditor`); malzeme seçilince birim gelir, düzenlemede yükler geri yüklenir (06.11).
- [ ] Kantar: plaka ile sevkiyata bağlanan tartımda malzeme plandan gelir. Tartım ekranında malzeme seçimi kaldırılır ya da salt okunur yapılır.
- [ ] İrsaliye satırları: tartımın malzemesinden değil, sevkiyat durağının yük satırlarından kurulur (`kantar/Custom/EDespatch/Issuing/DespatchDraftBuilder.cs`). Teslimat durağı başına bir irsaliye kesilir, `ShipmentStop.WaybillId` doldurulur.
- [ ] Sevkiyat detayında planlanan ve tartılan miktar yan yana gösterilir.
- [x] Araç kataloğu + "Araçlarımız" (06.11). Uygun ücretsiz API yok (NHTSA vPIC ABD odaklı, ağırlığı sınıf aralığı veriyor; AB yakıt verisi VECTO kapalı), kendi kataloğumuz:
  - platform `VehicleModel` (marka, model, yapı, dingil, azami yüklü ağırlık, tipik dara, ort. yakıt; onaylı/öneri) — açılışta 44 modellik başlangıç kataloğu (`VehicleModelCatalog`, tipik değerler, Türkiye dingil sınırına göre)
  - platform `VehicleSpec` (araç 1-1: model, ruhsattaki azami ağırlık, `FleetTenantId`); generate edilen `Vehicle`'a dokunulmadı
  - uçlar: `GET VehicleModels/brands`, `GET VehicleModels?brand=&bodyType=&search=`, `POST VehicleModels/suggestions` (öneri, onaya kadar yalnız önerene görünür), `PUT VehicleModels/{id}` (SuperAdmin onay/düzeltme), `GET Vehicles/mine` (mobil: şoförün araçları), `GET/PUT Vehicles/{id}/spec`, `GET/POST Fleet`, `DELETE Fleet/{vehicleId}`
  - yetki (spec yazma): sahibi; firma personeli araç kendi filosundaysa ya da sahiplenilmemiş ve başka filoda değilse; SuperAdmin
  - panel: `/panel/araclar` (marka → model seçimi, katalog değerleri, ruhsat değeri)
- [x] Plakadan şoför (06.11, kullanıcı kararı: firma ataması şoför onayı istemez):
  - `VehicleSpec.DefaultDriverId` — filo aracının varsayılan şoförü, `PUT Fleet/{vehicleId}/driver` (null kaldırır); Araçlarımız'da "Şoför ata"
  - `GET Drivers/for-plate?plate=` — filodaki varsayılan şoför (ilk sırada) + aracı aktif kullananlar
  - sevkiyat formu: plaka alanında "Araçlarımız" listesi; plaka girilince varsayılan şoför, yoksa aracı kullanan tek şoför otomatik gelir, birden çoksa seçim çipleri; elle seçilen şoförün üzerine yazılmaz
- [ ] **Mobil:** şoför araç eklerken `VehicleModels/brands` → `VehicleModels?brand=` → `PUT Vehicles/{id}/spec`; listede yoksa `POST VehicleModels/suggestions`.
- [ ] Yönetici paneli: onay bekleyen model önerileri (`GET VehicleModels?pending=true` + `PUT VehicleModels/{id}`).
- [ ] Araç kapasitesi denetimi: kantar, platformdan aracın azami yüklü ağırlığını (gRPC) alıp planlanan yük + kayıtlı dara (`TenantVehicle.StoredTareKg`) ile karşılaştırsın; çıkış tartımında ölçülen brüt azami ağırlığı aşarsa engel/uyarı (karar bekliyor). `VehicleType.maxGrossKg` bununla gereksizleşiyor.
- [ ] Katalog değerleri tipik; yasal dingil sınırları (18/26/32/40 t) yönetmelikten teyit edilmeli.
- [ ] Şoför bildirim zinciri: kantar `kantar/WaybillIssued` olayı → platform SignalR (ack) → push → SMS ile imzalı irsaliye linki.
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
  - kantar: `Kantar.csproj`, `Data/KantarDbContext.cs` (`ShipmentStopLines` DbSet dahil), üretilmiş Entity/Dto/Command dosyalarına eklenen alanlar
  - identity: `Program.cs`
  - platform: `Program.cs`, `Grpc/DriverGrpcService.cs`, `Data/PlatformDbContext.cs` (`VehicleModels`/`VehicleSpecs` DbSet + `VehicleBodyType` dönüşümü)

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
