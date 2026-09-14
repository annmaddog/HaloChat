# HaloChat — Redesign giao diện & tính năng bổ sung (GĐ5c)

> Spec này viết theo yêu cầu redesign giao diện của người dùng (mockup 8 màn
> hình cung cấp ngày 2026-09-14), mở rộng thêm vài tính năng thật đi kèm được
> chốt qua brainstorming. Không đụng tới GĐ6 (mã hóa RSA-AES thật).

## 1. Bối cảnh & mục tiêu

Giao diện HaloChat hiện tại (GĐ5a/GĐ5b) đã có đúng cấu trúc layout (sidebar
rail 4 mục, trang Bạn bè, trang Cài đặt có toggle...) nhưng còn nhiều điểm
thẩm mỹ kém và thiếu nhất quán:

- Không có avatar tròn dùng chung — chỉ Nhóm có, Tin nhắn/Bạn bè không có.
- Sidebar rail: các mục có kích thước không đồng đều giữa trạng thái
  chọn/không chọn (do nội dung dài ngắn khác nhau, không cố định kích thước).
- Toggle Cài đặt dùng checkbox vuông thô, không phải công tắc hiện đại.
- Chuông thông báo (`ThongBao.tsx`) có dropdown nhưng người dùng đánh giá vô
  dụng, muốn thay bằng badge số trực tiếp trên icon sidebar.
- Trang Cài đặt bố cục sơ sài, "Đổi mật khẩu" đang disable "sắp ra mắt".
- Không có chế độ giao diện Tối (dark mode).
- Nhóm chưa theo dõi tin nhắn nào từng thành viên đã đọc — không thể tính
  badge chưa đọc theo nhóm chính xác cho từng người.

Mục tiêu: làm lại giao diện theo mockup, đồng thời hoàn thiện các tính năng
thật mà mockup ngụ ý cần có để nút/toggle không bị "giả".

## 2. Phạm vi

**Trong phạm vi:**
- Component dùng chung mới: `Avatar`, `CongTac` (toggle), `Huy` (badge số).
- Sidebar (`KhungChinh.tsx`): sửa lỗi kích thước không đồng đều; bỏ
  `ThongBao.tsx`; thêm badge số trên mỗi mục (Tin nhắn/Bạn bè/Nhóm).
- Dark mode thật (CSS token thứ 2 + lưu `localStorage`).
- Trang Cài đặt viết lại theo cấu trúc 5 mục: Tài khoản / Quyền riêng tư /
  Thông báo / Bảo mật / Giao diện.
- Tính năng thật mới:
  - Đổi mật khẩu (yêu cầu mật khẩu cũ).
  - Toggle "Cho phép thêm tôi vào nhóm" (chặn `ThemThanhVienAsync` nếu tắt).
  - 3 toggle loại thông báo (Tin nhắn mới/Lời mời kết bạn/Thông báo nhóm) —
    quyết định badge tương ứng có cộng dồn hay không.
  - Theo dõi đã đọc tin nhắn nhóm theo từng thành viên (thay cơ chế
    `TinNhan.DaDoc` dùng chung hiện tại), phục vụ badge "Nhóm" chính xác.
- Redesign trực quan: `TrangChat`, `TrangBanBe`, `TrangNhom`, `TrangCaiDat`.

**Ngoài phạm vi (rõ ràng không làm đợt này):**
- Trạng thái tin nhắn đã gửi / đã nhận / đã xem (dấu tick kiểu Messenger) —
  người dùng đã từ chối rõ ràng.
- Toggle "Cho phép nhận lời mời kết bạn" (khác với "cho phép thêm vào
  nhóm") — từ chối, không làm.
- Hệ thống thông báo đẩy thật (push/email) — 3 toggle thông báo ở trên chỉ
  điều khiển badge trong ứng dụng, không có kênh thông báo nào khác tồn tại.
- "Quản lý khóa" RSA / mã hóa thật — thuộc GĐ6, do nhóm khác phụ trách.

## 3. Backend

### 3.1. `NguoiDung` — field mới

```csharp
public bool ChoPhepThemVaoNhom { get; set; } = true;
public bool ThongBaoTinNhanMoi { get; set; } = true;
public bool ThongBaoLoiMoiKetBan { get; set; } = true;
public bool ThongBaoNhom { get; set; } = true;
```

`CapNhatCaiDatAsync` (repository + service + `PUT /api/nguoidung/cai-dat`)
mở rộng nhận đủ 5 field bool này (2 cũ + 3 mới), giữ nguyên tên tham số cũ,
thêm tham số mới theo đúng thứ tự khai báo ở trên. `HoSoCaNhanDto`/
`HoSoCaNhan` (frontend) cũng thêm đủ field để trang Cài đặt đọc/hiển thị.

### 3.2. Đổi mật khẩu thật

Endpoint mới, có `[Authorize]` (khác hẳn quên-mật-khẩu — người dùng đã đăng
nhập nên không cần giấu email tồn tại hay không):

```
POST /api/nguoidung/doi-mat-khau
Body: { matKhauCu: string, matKhauMoi: string }
```

- `IDichVuNguoiDung` thêm `Task<KetQuaDoiMatKhauDto> DoiMatKhauAsync(string idHienTai, string matKhauCu, string matKhauMoi)`.
- Logic: lấy `NguoiDung` theo id → `_dichVuMatKhau.KiemTraMatKhau(matKhauCu, nguoiDung.Salt, nguoiDung.MatKhauBam)` — sai thì trả `ThanhCong = false, ThongBao = "Mật khẩu cũ không đúng."`.
- Đúng thì sinh `Salt` mới, `_dichVuMatKhau.BamMatKhau(matKhauMoi, saltMoi)`, cập nhật `MatKhauBam`/`Salt` — dùng lại đúng pattern `DatLaiMatKhauAsync` đã có ở module Quên mật khẩu.
- Validate độ dài `matKhauMoi` tối thiểu (dùng lại rule đã áp cho đăng ký/đặt lại mật khẩu, nếu có).
- `KetQuaDoiMatKhauDto { bool ThanhCong, string ThongBao }`.
- Controller: `matKhauCu` sai → `400 BadRequest`; thành công → `200 OK`.

### 3.3. Theo dõi đã đọc tin nhắn nhóm theo từng thành viên

Bỏ cách dùng `TinNhan.DaDoc` chung cho tin nhắn nhóm (giữ nguyên cho tin
nhắn 1-1 — không đụng `DanhDauDaDocAsync`/`DaDoc` ở nhánh 1-1).

**Model mới** `DocNhom` (collection Mongo riêng `"DocNhom"`):

```csharp
public class DocNhom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string NhomId { get; set; } = string.Empty;

    // Id của tin nhắn mới nhất mà người này đã xem trong nhóm này. Null
    // nghĩa là chưa từng mở nhóm này lần nào — mọi tin nhắn đều là chưa đọc.
    public string? TinNhanCuoiDaDocId { get; set; }

    public DateTime ThoiGianDoc { get; set; } = DateTime.UtcNow;
}
```

Index unique ghép `(NguoiDungId, NhomId)` tạo ở `Program.cs` theo đúng cách
2 index hiện có của `NguoiDung` được tạo (`CreateManyAsync` trong khối
`BoQuaKhoiTaoChiMuc`).

**Repository mới** `IDocNhomRepository`/`DocNhomRepository`:
```csharp
Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId);
Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId);
Task<int> DemChuaDocAsync(string nguoiDungId, string nhomId, ITinNhanRepository tinNhanRepo);
```
(hoặc tính unread trực tiếp trong `DichVuNhom`/`DichVuTinNhan` bằng cách kết
hợp `IDocNhomRepository.LayTinNhanCuoiDaDocAsync` +
`ITinNhanRepository.DemTinNhanSauIdAsync(nhomId, tinNhanCuoiDaDocId)` — thêm
phương thức đếm mới vào `ITinNhanRepository`, dùng `Filter.Gt(t => t.Id, ...)`
khi có mốc, hoặc đếm tất cả khi mốc là null).

`ITinNhanRepository.DanhDauDaDocNhomAsync(nhomId)` **bị xóa** — thay bằng gọi
`IDocNhomRepository.DanhDauDaDocAsync` từ `DichVuNhom` khi người dùng mở
nhóm. `TinNhan.DaDoc` không còn dùng cho tin nhắn nhóm — field vẫn giữ (dùng
cho 1-1), nhưng không set nữa với tin nhắn có `NhomId != null`.

**Endpoint mở nhóm (đánh dấu đã đọc):** thêm vào `NhomController`:
```
POST /api/nhom/{id}/danh-dau-da-doc
```
gọi `DichVuNhom.DanhDauDaDocAsync(idHienTai, id)` → lấy tin nhắn mới nhất
của nhóm (`ITinNhanRepository.LayLichSuNhomAsync(nhomId, null, 1)`), rồi
`IDocNhomRepository.DanhDauDaDocAsync(idHienTai, nhomId, tinMoiNhat.Id)`.
Nếu nhóm chưa có tin nhắn nào thì không làm gì (không có gì để đánh dấu).

**Endpoint badge tổng:**
```
GET /api/nhom/so-tin-chua-doc
Response: { soTinChuaDoc: number }
```
`DichVuNhom.DemTongChuaDocAsync(idHienTai)`: lấy tất cả nhóm của user →
với mỗi nhóm, đếm tin nhắn có `Id > TinNhanCuoiDaDocId` (hoặc tất cả nếu
null) → cộng dồn. Trả tổng.

### 3.4. Tôn trọng toggle "Cho phép thêm tôi vào nhóm"

`DichVuNhom.ThemThanhVienAsync` — trước khi thêm, kiểm tra
`NguoiDung.ChoPhepThemVaoNhom` của `thanhVienMoiId`; nếu `false`, ném
exception mới `KhongChoPhepThemVaoNhomException` → `NhomController` bắt,
trả `400 BadRequest { thongBao = "Người này không cho phép thêm vào nhóm." }`.
Áp dụng cả khi tạo nhóm (`TaoNhomAsync` — lọc `thanhVienIds` đầu vào, hoặc
báo lỗi rõ nếu 1 người trong danh sách chọn không cho phép — quyết định:
**báo lỗi rõ**, để người tạo biết mà bỏ người đó ra thay vì âm thầm bỏ qua).

### 3.5. Badge Tin nhắn & Bạn bè

Không cần endpoint mới — dùng lại:
- `GET /api/tinnhan/hoi-thoai` (đã trả `soTinChuaDoc` mỗi hội thoại — cộng
  dồn ở frontend).
- `GET /api/ketban/loi-moi-den` (đã có — đếm số lượng ở frontend).

### 3.6. Áp dụng 3 toggle thông báo vào badge

Đây là bước duy nhất khiến 3 toggle "có tác dụng thật": frontend đọc
`HoSoCaNhan.ThongBaoTinNhanMoi/ThongBaoLoiMoiKetBan/ThongBaoNhom` — nếu
toggle nào `false`, badge tương ứng luôn hiển thị 0 (ẩn), bất kể số liệu
thật từ API là bao nhiêu. Không cần đổi gì backend thêm ngoài việc trả các
field này trong `HoSoCaNhanDto`.

## 4. Frontend

### 4.1. Component dùng chung mới (`ThanhPhan/`)

**`Avatar.tsx`** — vòng tròn có chữ cái đầu tên viết hoa, màu nền lấy từ
bảng 6 màu cố định theo hash đơn giản của `id` (`id.charCodeAt(0) % 6`,
không cần thư viện hash phức tạp) để cùng 1 người luôn ra cùng 1 màu.
```tsx
interface AvatarProps { id: string; ten: string; kichThuoc?: 'nho' | 'vua' | 'lon' }
export function Avatar({ id, ten, kichThuoc = 'vua' }: AvatarProps) { ... }
```

**`CongTac.tsx`** — công tắc bật/tắt dạng viên thuốc, thay checkbox:
```tsx
interface CongTacProps { batTat: boolean; onDoi: (giaTri: boolean) => void; disabled?: boolean; nhan?: string }
export function CongTac({ batTat, onDoi, disabled, nhan }: CongTacProps) { ... }
```

**`Huy.tsx`** — số tròn đỏ nhỏ (badge), ẩn hẳn khi số = 0:
```tsx
export function Huy({ soLuong }: { soLuong: number }) {
  if (soLuong <= 0) return null;
  return <span className="huy">{soLuong > 99 ? '99+' : soLuong}</span>;
}
```

### 4.2. Hook `SuDungSoLuongChuaDoc` (thay thế logic trong `ThongBao.tsx`)

`NguCanh/` hoặc `ThanhPhan/` — hook mới tính 3 số:
```tsx
function SuDungSoLuongChuaDoc(): { tinNhan: number; banBe: number; nhom: number }
```
- `tinNhan`: tổng `soTinChuaDoc` từ `LayDanhSachHoiThoai` — 0 nếu
  `hoSoCaNhan.thongBaoTinNhanMoi === false`.
- `banBe`: `LayLoiMoiDen().length` — 0 nếu `thongBaoLoiMoiKetBan === false`.
- `nhom`: `GET /api/nhom/so-tin-chua-doc` (hàm `DichVuApi` mới
  `LaySoTinNhomChuaDoc`) — 0 nếu `thongBaoNhom === false`.
- Tải lại khi mount + khi nhận sự kiện Hub: `NhanTinNhan`,
  `NhanLoiMoiKetBan`, `LoiMoiKetBanDuocChapNhan` (đã có), cộng thêm
  `NhomDaCapNhat` cho phần nhóm (đủ để làm mới badge nhóm gần đúng thời
  gian thực — không cần sự kiện mới riêng cho unread nhóm).

### 4.3. `KhungChinh.tsx` + `KhungChinh.css`

- Xóa `<ThongBao />`, xóa file `ThongBao.tsx`/`ThongBao.css`/`ThongBao.test.tsx` (chuyển test còn hữu ích, nếu có, sang test của hook mới).
- Mỗi `NavLink` bọc icon + label + `<Huy soLuong={...} />` từ hook trên.
- **Sửa lỗi kích thước:** `.khung-chinh__muc` đặt `width`/`min-height` cố
  định (không phụ thuộc độ dài label), `justify-content: center`, icon cùng
  1 kích thước (`width/height: 22px` cho mọi `BieuTuong*`), padding cố định
  áp dụng như nhau cho trạng thái chọn/không chọn — chỉ đổi `background`/
  `color` khi `--dang-chon`.

### 4.4. Dark mode

`index.css` thêm khối theme tối:
```css
:root[data-theme='toi'] {
  --mau-nen-tren: #0f1729;
  --mau-nen-duoi: #0a0f1c;
  --mau-nen-the: #16213a;
  --mau-chu-dam: #e8ecf5;
  --mau-chu-phu: #8b96ab;
  --mau-vien: #263452;
}
```
(giữ nguyên `--mau-chinh*`/`--mau-loi` — chỉ đổi nền/chữ/viền).

`main.tsx` hoặc 1 hook nhỏ `SuDungGiaoDien()`: đọc `localStorage.getItem('halochat-giao-dien')`
(`'sang' | 'toi'`, mặc định `'sang'`), set `document.documentElement.dataset.theme`
khi mount và mỗi khi đổi. Trang Cài đặt gọi hook này để đổi + lưu lại.

### 4.5. `TrangCaiDat.tsx` — viết lại theo cấu trúc

5 mục bên trái (icon + tên), giữ đúng cấu trúc `MucCaiDat` union type mở
rộng thêm `'giao-dien'`:

```
TÀI KHOẢN        → Thông tin cá nhân (tên/email, không sửa được) + Đổi mật khẩu (form thật)
QUYỀN RIÊNG TƯ   → 3 CongTac: người lạ nhắn tin / hiển thị online / cho phép thêm vào nhóm
THÔNG BÁO        → 3 CongTac: tin nhắn mới / lời mời kết bạn / thông báo nhóm (đều có tác dụng thật lên badge)
BẢO MẬT          → giữ nguyên placeholder "sắp ra mắt (GĐ6)"
GIAO DIỆN        → 2 nút Sáng/Tối, đổi ngay lập tức
```

Form Đổi mật khẩu: 3 ô (mật khẩu cũ, mật khẩu mới, xác nhận mật khẩu mới —
xác nhận chỉ so khớp phía client, không gửi lên server), nút submit gọi
`DichVuApi.DoiMatKhau(token, matKhauCu, matKhauMoi)`, hiện lỗi rõ nếu sai
mật khẩu cũ, hiện "Đã đổi mật khẩu." khi thành công, tự xóa 3 ô sau khi
thành công.

### 4.6. Redesign trực quan các trang còn lại

- **`TrangChat.tsx`**: `Avatar` cho từng người trong danh sách hội thoại +
  header khung chat; `Huy` cạnh mỗi dòng hội thoại chưa đọc (dùng
  `soTinChuaDoc` có sẵn, không phải hook mới).
- **`TrangNhom.tsx`**: thay khối `<span className="trang-nhom__avatar">`
  bằng `<Avatar>` dùng chung; modal tạo nhóm bo góc/spacing lại theo mockup
  ảnh 4 (không đổi logic chọn thành viên).
- **`TrangBanBe.tsx`**: `Avatar` cho mỗi người ở cả 3 danh sách; thêm khung
  "hồ sơ" bên phải khi bấm vào 1 người trong danh sách Bạn bè (avatar lớn +
  tên + nút Nhắn tin), thay vì chỉ có nút trong dòng danh sách như hiện tại.
- **`KhungTinNhan.tsx`**: không đổi logic, chỉ thêm `Avatar` cạnh tin nhắn
  nếu cần cho đúng bố cục mockup ảnh 3.

## 5. Kiểm thử

- Backend: unit test cho `DoiMatKhauAsync` (sai mật khẩu cũ / đúng /
  mật khẩu mới quá ngắn), `DocNhomRepository` (đánh dấu + đếm chưa đọc),
  `ThemThanhVienAsync` khi `ChoPhepThemVaoNhom = false`, endpoint
  `GET /api/nhom/so-tin-chua-doc` (tích hợp).
- Frontend: test cho `Avatar`/`CongTac`/`Huy` (component thuần), hook
  `SuDungSoLuongChuaDoc` (mock `DichVuApi`), `TrangCaiDat` (đổi mật khẩu
  thành công/thất bại, đổi dark mode lưu `localStorage`), `KhungChinh` (badge
  hiện đúng số, kích thước 4 mục bằng nhau — snapshot hoặc kiểm tra class).

## 6. Ngoài phạm vi — nhắc lại

Trạng thái tin nhắn đã gửi/nhận/xem, toggle "cho phép nhận lời mời kết
bạn", hệ thống thông báo đẩy thật, quản lý khóa RSA — không làm ở spec này.
