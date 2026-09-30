# HaloChat — Chữ ký số xác thực người gửi tin nhắn + Bảng "Thông tin kỹ thuật" mỗi tin nhắn

Ngày viết: 2026-09-30
Trạng thái: đã được duyệt trong phiên brainstorming, sẵn sàng chuyển sang lập kế hoạch triển khai.

## 1. Bối cảnh

HaloChat đã có tính năng mã hóa lai RSA-AES cho tin nhắn Text (xem
`docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md` và bản triển khai thật ở
`backend/HaloChat.Api/Services/DichVuTinNhan.cs`): mỗi `NguoiDung` có sẵn 1 cặp khóa RSA-2048
(`KhoaCongKhai`/`KhoaBiMat`) sinh lúc đăng ký (hoặc vá lúc đăng nhập nếu tài khoản cũ chưa có),
dùng để mã hóa/giải mã nội dung. Mã hóa hiện tại **giấu nội dung** nhưng **không xác thực được
danh tính người gửi** — không có gì chứng minh 1 tin nhắn được lưu trong MongoDB thật sự do đúng
người gửi (`NguoiGuiId`) tạo ra, hay có bị sửa nội dung sau khi lưu hay không.

Mục tiêu của tính năng này: thêm **chữ ký số** (digital signature) để mỗi tin nhắn Text mang theo
bằng chứng mật mã học rằng nó do đúng người gửi tạo ra và chưa bị sửa đổi — bổ sung cho mã hóa
(mã hóa giấu nội dung; chữ ký xác thực nguồn gốc + toàn vẹn), không thay thế.

## 2. Phạm vi

- **Chỉ áp dụng cho tin nhắn loại Text** (giống đúng phạm vi mã hóa hiện tại) — Ảnh/File giữ nguyên,
  không ký.
- **Điều kiện ký khác điều kiện mã hóa**: mã hóa yêu cầu TẤT CẢ người tham gia hội thoại có khóa;
  ký chỉ yêu cầu **người gửi** có khóa (vì ký là hành động 1 chiều của người gửi, ai cũng xác minh
  được bằng khóa công khai của người gửi — không cần biết khóa của người nhận). Vì vậy 1 tin nhắn
  hoàn toàn có thể: có chữ ký nhưng không mã hóa (người gửi có khóa, người nhận thì chưa), hoặc
  ngược lại không xảy ra (mã hóa đòi hỏi điều kiện chặt hơn ký).
- **Dùng lại đúng cặp khóa RSA hiện có** (`KhoaCongKhai`/`KhoaBiMat`) để ký — không sinh thêm khóa
  ký riêng. Đây là 1 đơn giản hóa có chủ đích (xem mục 6).
- Tin nhắn cũ (gửi trước khi tính năng này lên) không có chữ ký, không hồi tố.
- Không đổi giao thức SignalR (không thêm phương thức `invoke`/sự kiện mới) — chữ ký được gắn tự
  động vào luồng gửi/đọc tin nhắn Text đã có sẵn.

## 3. Kiến trúc: service `DichVuChuKySo` độc lập

Tách hẳn khỏi `DichVuMaHoa` (dịch vụ mã hóa AES/RSA hiện có), đặt trong thư mục con riêng — khác
với các service khác trong `HaloChat.Security` hiện đang nằm phẳng ngay trong thư mục gốc (không
có thư mục con). Lý do tách: `DichVuMaHoa` đã đảm nhiệm cả AES lẫn RSA cho mục đích mã hóa, thêm
trách nhiệm ký số vào sẽ làm file phình to và trộn 2 mối quan tâm khác nhau (giấu nội dung ≠ xác
thực nguồn gốc); tách riêng giữ ranh giới rõ, dễ test độc lập.

```
backend/HaloChat.Security/
├── DichVuMaHoa.cs / IDichVuMaHoa.cs         (đã có — không đổi)
├── DichVuMatKhau.cs / IDichVuMatKhau.cs     (đã có — không đổi)
└── DichVuChuKySo/
    ├── IDichVuChuKySo.cs
    └── DichVuChuKySo.cs
```

### `IDichVuChuKySo`

```csharp
namespace HaloChat.Security;

public interface IDichVuChuKySo
{
    string KyDuLieu(string noiDung, string khoaBiMatNguoiKy);
    bool XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy);
}
```

- `KyDuLieu`: nhận nội dung dạng chuỗi (chính là giá trị đang/sẽ được lưu ở `TinNhan.NoiDungTinNhan`
  — có thể là plaintext hoặc bản mã Base64 tùy tin đó có được mã hóa hay không, xem mục 4) và khóa
  bí mật RSA (Base64, cùng định dạng đang lưu trong `NguoiDung.KhoaBiMat`), trả về chữ ký Base64.
- `XacMinhChuKy`: nhận lại đúng nội dung đó, chữ ký, và khóa công khai RSA (Base64) của người ký,
  trả `true`/`false`.

### `DichVuChuKySo` — cài đặt

Dùng `System.Security.Cryptography.RSA`, băm SHA-256, đệm **RSA-PSS** (`RSASignaturePadding.Pss`)
— đệm hiện đại hơn PKCS#1 v1.5, đồng nhất tinh thần với việc đang dùng OAEP (thay vì PKCS#1) cho
mã hóa khóa phiên trong `DichVuMaHoa`.

```csharp
public string KyDuLieu(string noiDung, string khoaBiMatNguoiKy)
{
    using var rsa = RSA.Create();
    rsa.ImportRSAPrivateKey(Convert.FromBase64String(khoaBiMatNguoiKy), out _);
    var duLieu = Encoding.UTF8.GetBytes(noiDung);
    var chuKy = rsa.SignData(duLieu, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    return Convert.ToBase64String(chuKy);
}

public bool XacMinhChuKy(string noiDung, string chuKy, string khoaCongKhaiNguoiKy)
{
    try
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(khoaCongKhaiNguoiKy), out _);
        var duLieu = Encoding.UTF8.GetBytes(noiDung);
        var chuKyBytes = Convert.FromBase64String(chuKy);
        return rsa.VerifyData(duLieu, chuKyBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }
    catch (Exception ex) when (ex is CryptographicException or FormatException)
    {
        return false; // chữ ký/khóa hỏng định dạng → coi như không hợp lệ, không throw ra ngoài
    }
}
```

`Program.cs` thêm đăng ký DI ngay cạnh dòng đăng ký `IDichVuMaHoa`:

```csharp
builder.Services.AddScoped<IDichVuChuKySo, DichVuChuKySo>();
```

## 4. Tích hợp vào `DichVuTinNhan`

### Model `TinNhan` — field mới

```csharp
// [Chữ ký số] Base64 chữ ký RSA-PSS/SHA-256 của NGƯỜI GỬI trên chính nội dung đang lưu ở
// NoiDungTinNhan (plaintext hoặc bản mã, tùy tin có mã hóa hay không) — null khi người gửi
// chưa có khóa RSA lúc gửi (tài khoản cũ, dữ liệu test) hoặc tin không phải loại Text.
public string? ChuKySo { get; set; }
```

### Lúc gửi tin (`GuiTinNhanAsync`)

Sau bước `MaHoaNoiDungNeuCoThe(...)` (để chắc chắn `tinNhan.NoiDungTinNhan` đã ở giá trị CUỐI CÙNG
sẽ lưu — plaintext hoặc bản mã), thêm bước ký:

```csharp
if (tinNhan.LoaiTinNhan == LoaiTinNhan.Text && nguoiGui is not null &&
    !string.IsNullOrWhiteSpace(nguoiGui.KhoaBiMat))
{
    tinNhan.ChuKySo = _dichVuChuKySo.KyDuLieu(tinNhan.NoiDungTinNhan, nguoiGui.KhoaBiMat);
}
```

Đặt trong 1 hàm riêng `KyTinNhanNeuCoThe(...)` theo đúng phong cách đặt tên đã có (`MaHoaNoiDungNeuCoThe`).

### Lúc đọc tin (`AnhXaDto` / `GiaiMaNoiDungThucTe`)

Xác minh chạy **trước khi giải mã** (nếu tin có mã hóa) — vì chữ ký được tính trên giá trị ĐÃ LƯU
ở `NoiDungTinNhan` (bản mã), không phải trên plaintext sau khi giải mã. Vì vậy thứ tự đúng là:

1. Lấy `t.NoiDungTinNhan` **gốc như đang lưu trong Mongo** (trước khi giải mã) + `t.ChuKySo`.
2. Nếu có đủ cả 2 và tra được `NguoiGui.KhoaCongKhai` → gọi `XacMinhChuKy(...)`.
3. Rồi mới thực hiện giải mã (nếu cần) để lấy nội dung hiển thị.

Thêm hàm riêng `private async Task<bool?> XacMinhChuKyNeuCoThe(TinNhan t)`:
- `null` = tin không có chữ ký (tin cũ / không phải Text) — không hiển thị gì đặc biệt trên UI.
- `true`/`false` = có chữ ký, xác minh đúng/sai.

Giá trị này được truyền vào `TinNhanDto` mới.

### `TinNhanDto` — field mới

```csharp
public record TinNhanDto(
    ...,
    bool? DaXacThucChuKy   // null = không có chữ ký; true/false = kết quả xác minh
);
```

Thêm đúng 1 tham số vào cuối record hiện có (giữ nguyên thứ tự các tham số cũ để không phá vỡ các
lệnh gọi positional-constructor đã có ở mọi nơi khác trong `DichVuTinNhan.cs`).

## 5. Frontend — hiển thị

`KhungTinNhan.tsx`/`.css`: tại đúng chỗ đang hiện giờ:phút khi bấm vào 1 tin nhắn (`dinhDangGio`,
xem `tinDangMoId`), thêm 1 icon nhỏ ngay cạnh:

- `daXacThucChuKy === true` → icon ✓ nhỏ màu xanh (`--mau-chinh-dam`), tooltip "Đã xác thực chữ ký người gửi".
- `daXacThucChuKy === false` → icon cảnh báo màu đỏ (`--mau-loi`), tooltip "Chữ ký không hợp lệ — nội dung có thể đã bị thay đổi".
- `daXacThucChuKy === null/undefined` → không hiện gì (tin cũ chưa có chữ ký).

`KieuDuLieu.ts`: thêm `daXacThucChuKy: boolean | null` vào interface `TinNhan`.

## 6. Giới hạn đã biết (ghi vào báo cáo, mục "Đánh giá mức độ an toàn")

- **Dùng chung 1 cặp khóa RSA cho cả mã hóa (OAEP) lẫn ký (PSS)** không phải thực hành mật mã học
  lý tưởng (khuyến nghị chuẩn là tách khóa mã hóa và khóa ký riêng biệt) — nhưng cùng 1 khóa RSA có
  thể dùng an toàn với 2 sơ đồ đệm khác nhau (OAEP cho mã hóa, PSS cho ký) mà không xung đột về mặt
  toán học; chấp nhận được ở quy mô đồ án, nhất quán với việc Private Key đã lưu ở server (mục 9
  của spec RSA-AES gốc).
- **Không phải chữ ký "không thể chối bỏ" (non-repudiation) theo đúng nghĩa pháp lý**: vì Private
  Key nằm ở server (server tự ký hộ người dùng khi họ gửi tin qua API), một quản trị viên server có
  quyền truy cập database về lý thuyết có thể tự ký giả danh bất kỳ ai. Tính năng này xác thực được
  "tin nhắn đi ra đúng từ hệ thống dưới danh nghĩa người gửi đó, chưa bị sửa sau khi lưu" chứ không
  chống được chính server bị xâm phạm.
- Tin nhắn cũ (trước khi tính năng này triển khai) không có chữ ký, không hồi tố.
- Ảnh/File không được ký (ngoài phạm vi, giống mã hóa).

## 7. Kế hoạch test

- `DichVuChuKySoTests` (mới, trong `HaloChat.Api.Tests/Services/` — dự án này là project test duy
  nhất của solution, kể cả cho `HaloChat.Security`, đúng như `DichVuMaHoaTests` hiện có): round-trip
  ký/xác minh đúng cặp khóa; xác minh
  thất bại khi đổi 1 ký tự nội dung; xác minh thất bại khi dùng sai khóa công khai (không phải của
  người ký); xác minh thất bại khi chữ ký là chuỗi rác (không throw ra ngoài).
- `DichVuTinNhanTests`: gửi tin bởi người gửi có khóa → `ChuKySo` khác null, `DaXacThucChuKy` trả
  về true khi đọc; gửi tin bởi người gửi KHÔNG có khóa → `ChuKySo` null, `DaXacThucChuKy` null khi
  đọc; giả lập tin bị sửa trực tiếp trong kho (đổi `NoiDungTinNhan` sau khi đã ký) → đọc lại phải
  trả `DaXacThucChuKy = false`.
- Frontend: test `KhungTinNhan.test.tsx` thêm case hiển thị đúng icon theo 3 trạng thái
  `daXacThucChuKy`.
- `DichVuTinNhanTests` (thêm cho `LayThongTinKyThuatAsync`): tin đã mã hóa → `DaMaHoa = true`,
  `KichThuocMaHoaByte > KichThuocGocByte`, `ThoiGianMaHoaMs`/`ThoiGianGiaiMaMs` khác null; tin không
  mã hóa → `DaMaHoa = false`, các field mã hóa/thời gian null; tin đã thu hồi hoặc loại Ảnh/File →
  `ApDungDuoc = false`; người không thuộc cuộc trò chuyện gọi API → ném đúng exception quyền hạn
  (giống `KiemTraQuyenTrenTinNhanAsync` ở các hành động khác).
- `TinNhanControllerTests`: gọi endpoint `GET /api/tinnhan/{id}/thong-tin-ky-thuat` trả đúng
  `200`/dữ liệu khi có quyền, `403`/`404` khi không.
- Frontend: test mới `ModalThongTinKyThuat.test.tsx` — hiện đúng badge theo từng trạng thái
  (đã mã hóa/không mã hóa, có chữ ký hợp lệ/không hợp lệ/không có, không áp dụng).

## 8. Bảng "Thông tin kỹ thuật" cho mỗi tin nhắn

Mục mới trong menu "..." đã có sẵn trên mỗi tin nhắn (cạnh "Lưu về thiết bị"/"Ghim"/"Thu hồi"/"Xóa"
trong `KhungTinNhan.tsx`), mở 1 modal hiển thị gọn 3 nhóm thông tin phục vụ trực tiếp 3 mục "Đánh
giá" của đề bài (thời gian mã hóa/giải mã, kích thước dữ liệu sau mã hóa, mức độ an toàn):

```
┌─ Thông tin kỹ thuật ───────────────────────── ✕ ┐
│ Người gửi: <tên> · Loại: Text                   │
│                                                  │
│ 🔒 MÃ HÓA                    [Đã mã hóa]        │
│   Thuật toán: AES-256-GCM                       │
│   Kích thước gốc:     .. byte                   │
│   Kích thước sau mã hóa: .. byte (~x.xx lần)    │
│   Bản mã (rút gọn): ........·10 ký tự đầu/cuối  │
│   Nonce / AuthTag (rút gọn tương tự)            │
│                                                  │
│ ⏱ THỜI GIAN (đo lại ngay lúc bấm xem)           │
│   Mã hóa lại để đo:   .. ms                     │
│   Giải mã:            .. ms                     │
│                                                  │
│ ✒ CHỮ KÝ SỐ              [✓ Hợp lệ / ...]       │
│   Thuật toán: RSA-PSS / SHA-256                 │
└──────────────────────────────────────────────────┘
```

- Tin **không mã hóa**: phần MÃ HÓA chỉ hiện badge `[Không mã hóa]`, ẩn Ciphertext/Nonce/AuthTag và
  cả phần THỜI GIAN (không có gì để đo).
- Tin **không có chữ ký** (tin cũ): phần CHỮ KÝ SỐ hiện `[Không có chữ ký]`, không có badge màu.
- Tin **đã thu hồi** hoặc **không phải Text** (Ảnh/File): cả bảng hiện "Không áp dụng cho loại tin
  nhắn này" — không phân tích nội dung đã bị thu hồi hay file đính kèm.
- **Thời gian đo lại ngay lúc bấm xem**, không lưu số liệu lịch sử: khi bấm "Thông tin kỹ thuật",
  server giải mã tin đó (nếu có mã hóa) để lấy lại plaintext + đo `Stopwatch` quanh bước giải mã,
  rồi **mã hóa lại plaintext đó bằng 1 khóa phiên AES mới** (không dùng lại khóa cũ, không ghi đè gì
  vào MongoDB — chỉ để đo, kết quả bỏ đi) và đo `Stopwatch` quanh bước mã hóa. Cách này không cần
  thêm field lưu trữ, áp dụng đồng nhất cho mọi tin nhắn kể cả tin cũ.

### Backend

**DTO mới** `ThongTinKyThuatDto.cs`:

```csharp
public record ThongTinKyThuatDto(
    bool ApDungDuoc,             // false = tin thu hồi/không phải Text — các field dưới đều null/mặc định
    bool DaMaHoa,
    string? ThuatToanMaHoa,      // "AES-256-GCM"
    int? KichThuocGocByte,
    int? KichThuocMaHoaByte,
    double? TyLePhinh,
    string? CiphertextRutGon,
    string? NonceRutGon,
    string? AuthTagRutGon,
    double? ThoiGianMaHoaMs,
    double? ThoiGianGiaiMaMs,
    bool CoChuKy,
    bool? DaXacThucChuKy,
    string? ThuatToanChuKy);     // "RSA-PSS / SHA-256"
```

**Endpoint mới** `GET /api/tinnhan/{id}/thong-tin-ky-thuat` trong `TinNhanController` (`[Authorize]`),
gọi `DichVuTinNhan.LayThongTinKyThuatAsync(idHienTai, tinNhanId)` — dùng lại đúng
`KiemTraQuyenTrenTinNhanAsync` đã có (chỉ người trong cuộc trò chuyện mới xem được, giống các hành
động khác trên tin nhắn).

**`DichVuTinNhan.LayThongTinKyThuatAsync`** — hàm mới, KHÔNG tái dùng `GiaiMaNoiDungThucTe`/
`AnhXaDto` (2 hàm đó phục vụ đường hiển thị bình thường, không nên chỉnh sửa chỉ để đo lường):
1. Tìm tin nhắn, kiểm tra quyền.
2. Nếu `DaThuHoi` hoặc `LoaiTinNhan != Text` → trả `ApDungDuoc = false`, các field còn lại mặc định.
3. `DaMaHoa = t.DanhSachKhoaPhien.Count > 0`.
4. Nếu `DaMaHoa`: tìm bản khóa phiên của `idHienTai` trong `DanhSachKhoaPhien`, giải mã ra khóa AES
   bằng khóa bí mật người xem — bọc `Stopwatch` quanh đúng lời gọi `GiaiMaTinNhan` (không tính thời
   gian tìm khóa/giải mã khóa phiên RSA) để lấy `ThoiGianGiaiMaMs` + plaintext. Sau đó
   `SinhKhoaPhienAes()` + `MaHoaTinNhan(plaintext, khoaMoi)` bọc `Stopwatch` riêng để lấy
   `ThoiGianMaHoaMs` (kết quả mã hóa lại này không lưu, chỉ lấy số đo). `KichThuocGocByte` = độ dài
   byte UTF-8 của plaintext; `KichThuocMaHoaByte` = độ dài byte sau `Convert.FromBase64String` của
   `t.CiphertextTinNhan`; `TyLePhinh` = tỷ lệ 2 số trên. Cắt rút gọn Ciphertext/Nonce/AuthTag
   (10 ký tự đầu + "..." + 10 ký tự cuối, hoặc nguyên chuỗi nếu ngắn hơn 24 ký tự).
5. Nếu không mã hóa: `KichThuocGocByte` = độ dài byte UTF-8 của `t.NoiDungTinNhan` (chính là
   plaintext); các field mã hóa/thời gian còn lại null.
6. `CoChuKy = t.ChuKySo is not null`; nếu có, gọi lại `IDichVuChuKySo.XacMinhChuKy(...)` với khóa
   công khai người gửi (`nguoiGui.KhoaCongKhai`) → `DaXacThucChuKy`.

### Frontend

- `DichVuApi.ts`: thêm `LayThongTinKyThuat(token, tinNhanId)` gọi endpoint trên.
- `KieuDuLieu.ts`: thêm interface `ThongTinKyThuat` khớp DTO trên.
- `KhungTinNhan.tsx`: thêm mục "Thông tin kỹ thuật" vào menu "..." đã có, gọi callback mới
  `onXemThongTinKyThuat(id)` (theo đúng pattern các callback khác — `onGhim`, `onThuHoi`...).
- `TrangChat.tsx`/`TrangNhom.tsx`: thêm state (id đang xem + dữ liệu đã tải), gọi API khi mở modal.
- Component mới `ModalThongTinKyThuat.tsx` (+ `.css`): hiện đúng bố cục ở trên, đặt cạnh
  `ModalHoanTatHoSo.tsx` trong `ThanhPhan/` (theo đúng quy ước modal hiện có).

## 9. Ngoài phạm vi (không làm trong lần này)

- Ký cho tin nhắn nhóm theo mô hình khác (tin nhóm dùng lại đúng cơ chế trên, không cần xử lý riêng
  vì việc ký chỉ phụ thuộc người gửi, không phụ thuộc số người nhận).
- Ký/xác thực cho Ảnh/File đính kèm.
- Sinh cặp khóa ký riêng biệt khỏi khóa mã hóa (có thể cân nhắc trong 1 lần cải tiến sau nếu cần
  đúng chuẩn mật mã học hơn).
- Cơ chế thu hồi/xoay khóa (key rotation) khi nghi ngờ khóa bị lộ.
- Lưu lại lịch sử số liệu thời gian mã hóa/giải mã qua nhiều lần đo (chỉ hiện số đo tức thời).
