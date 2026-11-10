# ProKantar iş listesi

Son güncelleme: 09.11.2026. Bu dosya kaldığımız yeri ve sıradaki işleri tutar; yeni bir oturuma buradan başlanır.
Repolar: `DropDeart/ProKantar` (backend) ve `DropDeart/ProKantarUI` (ön yüz).

---

## 0. Devir notu (07.11): başka bilgisayardan devam

**Durum:** Şoför daveti + Şoförlerimiz + yakındaki müsait şoförler ve Netgsm SMS entegrasyonu commit'lendi ve push'landı (iki repo). Tek engel Netgsm gönderici adı.

**Netgsm (SMS) — nerede kaldık:**
- Hesap: abone no `8505103285`. API alt kullanıcısı açıldı; giriş **çalışıyor** (`GET sms/rest/v2/msgheader` → kod `00`).
- **Engel:** hesapta onaylı gönderici adı (msgheader) **yok**; liste boş. `8505103285` ve `PROKANTAR` ikisi de `40 header problem` (07.11'de geçersiz numarayla, kontör harcamadan denendi).
- PROKANTAR başlığı talep edildi; onay için E-Tuğra KEP hesabından Netgsm'e evrak gönderilecek. KEP girişi: kepkur.com.tr → "KEP Hesabı Giriş" (webmail.kepkur.com.tr), e-imza ile; aktivasyon cm2.e-tugra.com.tr/Login (ETugraSigner). Kullanıcının e-imzası **e-Güven**'den; E-Tuğra kabul ediyor mu denenmedi, olmazsa E-Tuğra destek.
- OTP ve toplu SMS paketlerinde kontör var.

**Yeni bilgisayarda yapılacaklar:**
1. `.env` dosyaları git'te yok (secret). `identity/.env` ve `platform/.env`'i eski makineden taşı ya da `.env.example`'dan yeniden doldur:
   - identity: `Auth__Messaging__SmsProvider=Netgsm`, `Auth__Messaging__Netgsm__Usercode=8505103285`, `..._Password=<API alt kullanıcısı şifresi>`, `..._MsgHeader=<onaylı başlık>`.
   - platform: `Netgsm__Usercode`, `Netgsm__Password`, `Netgsm__MsgHeader`; davet linkinin web adresi `Invitations__WebUrl` (SMS: `{WebUrl}/davet/{kod}`). Şifre boşsa SMS'ler log'a yazılır.
   - **Başlık onaylanana kadar identity'de `SmsProvider=Log` bırak**; Netgsm seçiliyken başlık geçersizse OTP gönderimi hata verir (giriş çalışmaz).
   - API'de IP kısıtı açılırsa yeni makinenin dış IP'si Netgsm paneline eklenmeli (yoksa kod 30).
2. Başlık onaylanınca: msgheader sorgula → `.env`'lere yaz → `docker compose up -d --build identity platform` → kendi numarana OTP (`POST /api/account/otp/send`) ve telefonla şoför daveti (panel `/panel/soforler`) ile uçtan uca dene.

**Netgsm API özeti** (dokümantasyon JS ile yükleniyor; okumak için tarayıcı gerekir: netgsm.com.tr/dokuman):
- Basic Auth = abone no + API alt kullanıcısı şifresi. Hata cevabı HTTP 406 + JSON gövde (`code`).
- OTP: `POST https://api.netgsm.com.tr/sms/rest/v2/otp` `{msgheader,msg,no}` → `{code:"00",jobid}`. Türkçe karakter yok, ≤155 karakter, yalnız TR cep `5XXXXXXXXX`, 3 dk içinde teslim. Kodlar: 20 metin, 30 kimlik/API izni/IP, 40-41 başlık, 50-52 numara, 60 OTP paketi yok, 70 parametre, 100 sistem.
- Toplu: `POST .../sms/rest/v2/send` `{msgheader, messages:[{msg,no}], encoding:"TR", iysfilter:"0", appname}` → `{code:"00",jobid}`. Kodlar: 20, 30, 40, 50-51 İYS, 70, 80 sınır, 85 aynı numaraya 1 dk'da >20.
- Başlık listesi: `GET .../sms/rest/v2/msgheader`.

---

## 0.1 Sevkiyat zinciri ve simülasyon (09.11) — commit'lenmedi

Yapıldı ve simülasyonla uçtan uca doğrulandı (iki duraklı / tek duraklı, %5 sapma, push dalı):
- kantar: çıkış tartımı plakadan yayındaki sevkiyata bağlanır; malzeme, cari, şoför plandan gelir; plana bağlı tartımda başka
  malzeme reddedilir, plansızda malzeme zorunlu (`Custom/Weighings/WeighingShipmentLink.cs`, `CreateWeighingGuard`).
- kantar: teslimat durağı başına irsaliye; satırlar durağın yükünden (tek durak + tek satır + kg/ton → gerçek net, aksi halde plan);
  net ile plan toplamı %2'den fazla saparsa ön izlemede uyarı. Benzersiz indeks (tartım, teslim noktası) oldu
  (`UX_Waybills_TenantId_WeighingId_Location_Valid`). Uçlar: `GET Weighings/{id}/despatches`, `GET Shipments/{id}/progress`.
- kantar: `kantar/WaybillIssued` olayı + giriş istemeyen süreli link `{WebUrl}/irsaliye/{token}` (43 karakterlik HMAC token, anahtar
  `EDespatchSecrets`'te DataProtection ile şifreli; 30 gün), `GET api/public/waybills/{token}`.
- platform: RabbitMQ aboneliği + şoför bildirim zinciri: SignalR hub `/hubs/driver` (Driver rolü, `?access_token=`, 10 sn "Ack")
  → push (`IPushSender`, şimdilik log) → SMS (`ISmsGateway`, irsaliye linkli). Her deneme `DriverNotificationAttempts`'e yazılır.
  Uçlar: `GET api/DriverNotifications?shipmentId=`, şoför: `GET api/me/notifications`, `POST api/me/notifications/{id}/ack`.
- UI: tartım ayrıntısında durak başına "İrsaliye kes" (uyarılarla); sevkiyat detayında "Yük ve irsaliyeler" kartı (plan/tartılan/fark,
  tartımlar, durak irsaliyesi, bildirim zinciri); `/irsaliye/[token]` (giriş istemez, mobil); `/panel/simulasyon` (yalnız dev:
  şoför uygulaması + kantar cihazı yerine). Bireysel kayıtta "Şoförüm" seçeneği (Driver rolü + profil).
- Betik: `tools/simulate/yerel_firma_hazirla.py` (yerel test firması; onay yerel DB'de SQL ile) ve
  `tools/simulate/sevkiyat_simulasyonu.py [--sapma 0.05] [--push] [--tek-durak]`.

Kalanlar:
- [ ] İrsaliye çıkış tartımından sonra **otomatik** kesilmiyor (karar 06.11); şimdilik tartım ayrıntısından elle. Otomatik kesimde
  GİB eksikleri (TCKN, posta kodu) olursa ne olacağı kararlaştırılmalı.
- [ ] FCM bağlantısı (push gerçek gönderim); `LogPushSender` yerine.
- [ ] Platform birden çok kopyaya çıkarsa SignalR için Redis backplane; `DriverPresence` kopya içi.
- [ ] Kayıtta şoför profili SMS doğrulamasından önce kayıttaki numarayla açılıyor: başkasının numarasıyla kayıt o numaranın davet
  kabulünü engelleyebilir. Profil telefonu doğrulanmış `phone_number` ile eşleşmeli.
- [ ] Sosyal girişle bireysel kayıt şoför rolü vermiyor.

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
- [x] Kantar: plaka ile sevkiyata bağlanan tartımda malzeme plandan gelir (09.11; arayüzde tartım oluşturma ekranı yok, kantar API'den).
- [x] İrsaliye satırları sevkiyat durağının yük satırlarından; teslimat durağı başına bir irsaliye, `ShipmentStop.WaybillId` (09.11).
- [x] Sevkiyat detayında planlanan ve tartılan miktar yan yana (09.11).
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
- [x] Şoför daveti + "Şoförlerimiz" + yakındaki müsait şoförler (06.11). Kullanıcı kararları: önce davet, sonra iş teklifi (ayrı adım); konum yalnız "Müsaitim" modunda; kayıtsız numaraya SMS ile uygulama linki; kabul öncesi firma ad + araç tipi + yaklaşık mesafe görür, telefon maskeli.
  - platform `TenantDriver` (firma, şoför?, telefon, Invited/Active/Declined/Removed, kaynak Search/Phone/Nearby); firmada aynı numaraya tek açık kayıt (kısmi benzersiz indeks)
  - firma: `GET TenantDrivers`, `POST TenantDrivers/invite` (`driverId` ya da `phone`), `DELETE TenantDrivers/{id}` (geri çek / çıkar), `POST TenantDrivers/summaries` (yakındakilerin adı + aracı + bağ durumu)
  - şoför (mobil): `GET me/invitations`, `POST me/invitations/{id}/accept|decline` — davet şoför kaydına ya da token'daki **doğrulanmış telefona** (`phone_number`, identity'ye eklendi) gelir; kabul için şoför profili gerekir
  - tracking: `PUT/DELETE api/driver/availability` (Müsaitim; yalnız son konum, 30 dk ping gelmezse düşer), `GET api/availability/nearby?latitude&longitude&radiusKm` (personel; yalnız kullanıcı kimliği + 0,5 km'ye yuvarlı mesafe)
  - bildirim `IDriverNotifier` → şimdilik `LogDriverNotifier` (uygulama bildirimi / SMS log'a yazılır)
  - şoför telefonu ve plakası kayıtta anahtar biçimine çevrilir; kullanıcı başına tek şoför profili (`CreateDriverGuard`)
  - panel: `/panel/soforler` (liste, kayıtlı şoför / telefonla davet, istasyona yakın müsait şoförler)
- [ ] **Mobil:** "Müsaitim" anahtarı (açıkken birkaç dakikada bir `PUT driver/availability`, kapatınca `DELETE`), davetler ekranı (kabul/red), push bildirimi.
- [ ] **İş teklifi (Uber mantığı, 2. adım):** sevkiyatı yakındaki müsait şoförlere teklif et, ilk kabul eden alır; teklif süresi, ücret alanı, kabul yarışı (kilit). Karar: teklif yalnız Şoförlerimiz'e mi, herkese mi?
- [ ] Sevkiyat formunda şoför aramasında önce Şoförlerimiz gelsin.
- [x] Netgsm OTP SMS (06.11): identity `NetgsmSmsSender` (`sms/rest/v2/otp`, Basic Auth); `Auth__Messaging__SmsProvider=Netgsm` + Usercode/Password/MsgHeader `.env`'de. Metin Türkçe karaktersize çevrilir, ≤155 karakter, yalnız TR cep. Gerçek gönderim gönderici adı onayını bekliyor (bkz. §0).
- [x] Davet SMS'i (06.11): platform `ISmsGateway` → `NetgsmSmsGateway` (`sms/rest/v2/send`, n:n toplu, `encoding=TR`, `iysfilter=0`); `Netgsm__*` .env'de yoksa `LogSmsGateway`. `SmsDriverNotifier` push gelene kadar kayıtlı/kayıtsız her davete SMS atar; SMS hatası daveti bozmaz. Gerçek gönderim gönderici adı onayını bekliyor (bkz. §0).
- [x] İrsaliye linki SMS'i aynı `ISmsGateway` ile (09.11); push arayüzü hazır, FCM bağlanmadı.
- [ ] **Mobil:** şoför araç eklerken `VehicleModels/brands` → `VehicleModels?brand=` → `PUT Vehicles/{id}/spec`; listede yoksa `POST VehicleModels/suggestions`.
- [ ] Yönetici paneli: onay bekleyen model önerileri (`GET VehicleModels?pending=true` + `PUT VehicleModels/{id}`).
- [ ] Araç kapasitesi denetimi: kantar, platformdan aracın azami yüklü ağırlığını (gRPC) alıp planlanan yük + kayıtlı dara (`TenantVehicle.StoredTareKg`) ile karşılaştırsın; çıkış tartımında ölçülen brüt azami ağırlığı aşarsa engel/uyarı (karar bekliyor). `VehicleType.maxGrossKg` bununla gereksizleşiyor.
- [ ] Katalog değerleri tipik; yasal dingil sınırları (18/26/32/40 t) yönetmelikten teyit edilmeli.
- [x] Şoför bildirim zinciri: kantar `kantar/WaybillIssued` olayı → platform SignalR (ack) → push → SMS ile imzalı irsaliye linki (09.11).
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
- [ ] **platform artık RabbitMQ'ya bağlı:** `platform/.env`'de `RABBITMQ_PASSWORD` (kökteki broker ile aynı). İrsaliye linkinin
  web adresi kantar'da `WEB_URL` (→ `WaybillLinks__WebUrl`), davet linki platformda `Invitations__WebUrl`.
- [ ] **SignalR arkasında proxy:** nginx'te `/hubs/driver` için WebSocket upgrade başlıkları (`Upgrade`, `Connection`) açılmalı.
- [ ] **Yeniden başlatma sırası:** identity ve platform → kantar → tracking. Tracking önce kalkarsa sefer durumu olayları kaybolur.
- [ ] Eski "Yayında" sevkiyatlar sefer durumuyla otomatik güncellenmez; tek seferlik SQL gerekebilir.
- [ ] Yeni kolonlar ve tablolar servis açılışında idempotent SQL ile kuruluyor (EnsureCreated var olan DB'ye eklemez).
- [ ] **Regen riski.** Üretici yeniden çalışırsa şu elle değişiklikler ezilir ve yeniden uygulanmalı:
  - kantar: `Kantar.csproj`, `Data/KantarDbContext.cs` (`ShipmentStopLines` DbSet dahil), üretilmiş Entity/Dto/Command dosyalarına eklenen alanlar
  - kantar (10.11): `Features/Stations/StationQueries.cs` ve `Features/DeliveryLocations/DeliveryLocationQueries.cs` arama koşuluna
    `City`/`District`/`PostalCode` elle eklendi (il/ilçe 05.11'de spec'e girmiş, sorgu üretilmemişti). Üretici spec'ten aynısını üretir;
    ezilse de sonuç aynı. `Waybill` ve `Weighbridge` için spec'e `searchFields` kondu: yeniden üretim TCKN, UBL XML ve
    `ConnectionInfo`'yu aramaya katmasın.
  - platform (10.11): sayfalı uçlar elle yazıldı (generate dokunmaz): `GET api/Fleet/paged`, `GET api/TenantDrivers/paged`,
    `GET api/demo-requests` (artık sayfalı, `openCount` ile). Tam liste uçları seçiciler ve mobil için aynen duruyor.
  - identity: `Program.cs`, `Controllers/AuthorizationController.cs` (`phone_number` claim'i), `Messaging/MessagingRegistration.cs` + `Configuration/AuthOptions.cs` (Netgsm sağlayıcısı; `Messaging/NetgsmSmsSender.cs` elle yazıldı)
  - platform: `Program.cs` (09.11: `EnableRabbitMq` + `kantar/WaybillIssued` aboneliği), `Grpc/DriverGrpcService.cs`, `Data/PlatformDbContext.cs`
    (`VehicleModels`/`VehicleSpecs`/`TenantDrivers`/`DriverNotifications`/`DriverNotificationAttempts` DbSet + enum dönüşümleri)

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

- Simülasyon: önce `python tools/simulate/yerel_firma_hazirla.py` (bir kez; `tools/simulate/.env.local` yazar), sonra
  `python tools/simulate/sevkiyat_simulasyonu.py`. Panelde aynı akış: `/panel/simulasyon` (şoför + kantar) ve Sevkiyatlar.

- Demo kiracı: `E2E Lojistik 1793804329539`, tenant `49bc6b5d-bf9a-4d8e-b635-5f99f47ea5e0`, e-posta `e2e1793804329539@test.com` (şifre seed / test notlarında).
- Demo şoförler: Mehmet Yılmaz ve Ayşe Demir, deneme amaçlı geçerli test TCKN'leri girili. Plaka 41KL4471'e ikisi de bağlı.
- Yerel DB'de 3 test irsaliyesi var: PKN2026000000001–003.
- e-İrsaliye doğrulama araçları bu oturumun geçici klasöründeydi (`build_despatch.py`, `validate.py`, `elogo_client.py`; GİB paketleri ebelge.gib.gov.tr `UBL-TR1.2.1_Paketi.zip` ve `e-FaturaPaketi.zip`). Yeni makinede gerekirse yeniden indirilir.
