# HaloChat — Ảnh đại diện cá nhân & luồng "Hoàn tất hồ sơ" (GĐ8)

## 1. Bối cảnh & mục tiêu

Hiện tại `Nhom` đã có sẵn field `DuongDanAnhDaiDien` và đầy đủ luồng đổi
ảnh (tạo nhóm + `PanelQuanLyNhom.tsx`), nhưng `NguoiDung` (tài khoản cá
nhân) chưa có khái niệm ảnh đại diện nào — component `Avatar` đã tồn
tại và đã hỗ trợ hiển thị ảnh thật (`duongDanAnh`) hoặc fallback chữ
cái đầu, nhưng mọi nơi hiển thị avatar CÁ NHÂN hiện đều gọi nó không
kèm `duongDanAnh` nên luôn hiện chữ cái đầu.

Mục tiêu: thêm ảnh đại diện cá nhân đầy đủ (đổi ở Cài đặt + hiển thị ở
mọi nơi), thêm modal "Hoàn tất hồ sơ" hiện 1 lần duy nhất sau lần đăng
nhập đầu tiên để mời đặt avatar + tên hiển thị, và thêm lối tắt đổi
ảnh đại diện nhóm ngay tại "Thông tin nhóm" (hiện chỉ đổi được qua
"Quản lý nhóm").

## 2. Phạm vi

**Trong phạm vi:**
- Thêm field ảnh đại diện cho `NguoiDung`, đổi được qua trang Cài đặt.
- Modal "Hoàn tất hồ sơ" hiện đúng 1 lần ở lần đăng nhập đầu tiên sau
  khi đăng ký — không tự hiện lại nữa dù người dùng hoàn tất hay bấm X
  bỏ qua.
- Đổi ảnh đại diện nhóm trực tiếp từ "Thông tin nhóm" (không cần vào
  "Quản lý nhóm" nữa), chỉ admin thấy nút này.
- Cập nhật MỌI nơi đang hiển thị avatar cá nhân (5 chỗ dùng sẵn
  component `Avatar` + phần đầu đề khung chat `KhungTinNhan.tsx` hiện
  đang tự vẽ chữ cái đầu riêng) để hiển thị đúng ảnh thật khi đã có.
- Tái dùng nguyên endpoint upload file đã có (`POST /api/tinnhan/upload`)
  — không tạo endpoint upload mới.

**Ngoài phạm vi:**
- Không cắt/crop ảnh phía client hay server — dùng nguyên ảnh đã tải
  lên (giống cách nhóm đang làm).
- Không cho xóa avatar về lại chữ cái đầu sau khi đã đặt (không có nút
  "Xóa ảnh đại diện") — đổi ảnh khác thì thay thế, giống nhóm.
- Không có bước xác nhận "bạn có chắc muốn đổi ảnh" — đổi là lưu luôn
  (giống nhóm), trừ trong modal Hoàn tất hồ sơ (xem §4.2, chỉ lưu khi
  bấm "Hoàn tất").

## 3. Backend

### 3.1. Model

`Models/NguoiDung.cs` thêm 2 field:
```csharp
// [GĐ8] Null = chưa từng đặt ảnh đại diện — Avatar.tsx tự fallback về
// chữ cái đầu của TenHienThiThucTe(). Cùng kiểu dữ liệu (đường dẫn tới
// GridFS qua endpoint upload chung) với Nhom.DuongDanAnhDaiDien.
public string? DuongDanAnhDaiDien { get; set; }

// [GĐ8] Cờ đã-xem, KHÔNG phải cờ đã-hoàn-tất — set true ngay khi đóng
// modal "Hoàn tất hồ sơ" theo BẤT KỲ cách nào (bấm Hoàn tất hay bấm X),
// để modal không bao giờ tự hiện lại sau lần đầu, kể cả khi người dùng
// bỏ qua không nhập gì.
public bool DaXemHoanTatHoSo { get; set; } = false;
```

### 3.2. DTO

`HoSoCaNhanDto` thêm 2 tham số cuối:
```csharp
public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom,
    string TenHienThi, string? DuongDanAnhDaiDien, bool DaXemHoanTatHoSo);
```

`NguoiDungTomTatDto` thêm tham số cuối (đây là DTO dùng cho danh sách
bạn bè/thành viên nhóm/đối tác chat — nơi mọi avatar cá nhân khác đọc
dữ liệu từ đây):
```csharp
public record NguoiDungTomTatDto(
    string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa,
    string TenHienThi, string? DuongDanAnhDaiDien);
```
Cả 2 record đều CHỈ thêm tham số cuối — không xóa/sắp xếp lại tham số
hiện có.

Mọi nơi trong `DichVuNguoiDung.cs` đang dựng `HoSoCaNhanDto`/
`NguoiDungTomTatDto` (map từ `NguoiDung`) đều cần cập nhật để truyền
đúng `nd.DuongDanAnhDaiDien`/`nd.DaXemHoanTatHoSo`.

### 3.3. Controller — 2 endpoint mới trong `NguoiDungController.cs`

```csharp
public record DoiAnhDaiDienRequest(string DuongDanAnhDaiDien);

[HttpPut("anh-dai-dien")]
[Authorize]
public async Task<IActionResult> DoiAnhDaiDien([FromBody] DoiAnhDaiDienRequest yeuCau)
{
    if (IdHienTai is null) return Unauthorized();
    if (string.IsNullOrWhiteSpace(yeuCau.DuongDanAnhDaiDien))
    {
        return BadRequest(new { thongBao = "Đường dẫn ảnh không hợp lệ." });
    }
    var hoSo = await _dichVu.DoiAnhDaiDienAsync(IdHienTai, yeuCau.DuongDanAnhDaiDien);
    return hoSo is null ? NotFound() : Ok(hoSo);
}

[HttpPost("danh-dau-hoan-tat-ho-so")]
[Authorize]
public async Task<IActionResult> DanhDauHoanTatHoSo()
{
    if (IdHienTai is null) return Unauthorized();
    var hoSo = await _dichVu.DanhDauHoanTatHoSoAsync(IdHienTai);
    return hoSo is null ? NotFound() : Ok(hoSo);
}
```
`IDichVuNguoiDung`/`DichVuNguoiDung` thêm 2 method tương ứng, cùng
pattern với `DoiTenHienThiAsync` đã có (tìm theo id, cập nhật field,
lưu, trả về `HoSoCaNhanDto` map lại — không cần validate gì thêm cho
`DanhDauHoanTatHoSoAsync` vì đây là hành động không tham số).

### 3.4. Nhóm — không cần thay đổi backend

`Nhom`/`NhomDto`/`CapNhatNhomRequest`/`DichVuNhom.CapNhatAsync` đã hỗ
trợ đầy đủ đổi `DuongDanAnhDaiDien` từ trước (dùng trong
`PanelQuanLyNhom.tsx`) — lối tắt ở "Thông tin nhóm" (§4.4) chỉ là UI
mới gọi lại đúng các hàm/endpoint đã có, không cần sửa backend.

## 4. Frontend

### 4.1. Kiểu dữ liệu

`KieuDuLieu.ts`:
```typescript
export interface NguoiDungTomTat {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  tenHienThi: string;
  duongDanAnhDaiDien: string | null;
}
```
`HoSoCaNhan` (kiểu trả về của `LayThongTinCaNhan`) thêm
`duongDanAnhDaiDien: string | null;` và `daXemHoanTatHoSo: boolean;`.

`DichVuApi.ts` thêm:
```typescript
export async function DoiAnhDaiDien(token: string, duongDanAnhDaiDien: string): Promise<HoSoCaNhan> {
  return goiApi<HoSoCaNhan>('/nguoidung/anh-dai-dien', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
    body: JSON.stringify({ duongDanAnhDaiDien }),
  });
}

export async function DanhDauHoanTatHoSo(token: string): Promise<HoSoCaNhan> {
  return goiApi<HoSoCaNhan>('/nguoidung/danh-dau-hoan-tat-ho-so', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

### 4.2. Modal `ModalHoanTatHoSo.tsx` (mới)

Component mới, tự quản lý state, nhận props:
```typescript
interface PropsModalHoanTatHoSo {
  tenHienThiBanDau: string;
  onDong: (hoSoMoi: HoSoCaNhan | null) => void; // null = không có gì thay đổi cần cập nhật lên state cha
}
```

Cấu trúc đúng theo mockup: nút X góc phải, tiêu đề "Hoàn tất hồ sơ",
phụ đề "Hãy thiết lập hồ sơ của bạn để mọi người có thể dễ dàng nhận
ra bạn trong HaloChat.", avatar tròn (dùng component `Avatar`, preview
ảnh vừa chọn qua `URL.createObjectURL` — CHƯA gọi API) với 1 nút camera
nhỏ đè góc dưới phải (bấm mở file input, giống nút "Chọn ảnh" bên dưới
— cả 2 đều trigger cùng 1 `<input type="file" hidden>`), ô nhập "Tên
hiển thị" (label riêng, `maxLength={50}`, hiện đếm `{tenHienThi.length}/50`),
nút xanh lớn "Hoàn tất".

State nội bộ: `tepAnhDaChon: File | null`, `duongDanPreview: string | null`
(qua `URL.createObjectURL`, revoke khi unmount/đổi ảnh khác),
`tenHienThi: string` (khởi tạo từ `tenHienThiBanDau`), `dangLuu: boolean`,
`loi: string | null`.

Bấm "Hoàn tất":
```typescript
async function xuLyHoanTat() {
  setDangLuu(true);
  try {
    let hoSoMoi: HoSoCaNhan | null = null;
    if (tepAnhDaChon) {
      const daTaiLen = await TaiLenTep(token, tepAnhDaChon);
      hoSoMoi = await DoiAnhDaiDien(token, daTaiLen.duongDanFile);
    }
    if (tenHienThi.trim() && tenHienThi.trim() !== tenHienThiBanDau) {
      hoSoMoi = await DoiTenHienThi(token, tenHienThi.trim());
    }
    hoSoMoi = await DanhDauHoanTatHoSo(token);
    onDong(hoSoMoi);
  } catch {
    setLoi('Lưu hồ sơ thất bại, vui lòng thử lại.');
  } finally {
    setDangLuu(false);
  }
}
```
(Gọi `DanhDauHoanTatHoSo` LUÔN ở cuối bất kể có đổi ảnh/tên hay không —
đây chính là cờ "đã xem", không phụ thuộc có thay đổi gì hay không.)

Bấm X:
```typescript
async function xuLyDong() {
  const hoSoMoi = await DanhDauHoanTatHoSo(token).catch(() => null);
  onDong(hoSoMoi);
}
```
Không lưu `tepAnhDaChon`/`tenHienThi` đang gõ dở — chỉ đánh dấu đã xem
rồi đóng.

### 4.3. `TrangChat.tsx` — nơi hiện modal

Thêm state `hoSo: HoSoCaNhan | null` và `hienModalHoanTatHoSo: boolean`.
Trong effect mount (chạy 1 lần khi có `token`), gọi
`LayThongTinCaNhan(token)`, lưu vào `hoSo`, và set
`hienModalHoanTatHoSo = !ketQua.daXemHoanTatHoSo`.

```tsx
{hienModalHoanTatHoSo && hoSo && (
  <ModalHoanTatHoSo
    tenHienThiBanDau={hoSo.tenHienThi}
    onDong={(hoSoMoi) => {
      if (hoSoMoi) setHoSo(hoSoMoi);
      setHienModalHoanTatHoSo(false);
    }}
  />
)}
```

### 4.4. Cài đặt (`TrangCaiDat.tsx`) — đổi ảnh đại diện cá nhân

`TrangCaiDat.tsx` hiện tách từng field thành 1 `useState` riêng (không
có 1 object "hoSo" gộp chung) — thêm theo đúng pattern đó:
`const [duongDanAnhDaiDien, setDuongDanAnhDaiDien] = useState<string | null>(null);`,
gán trong đúng `useEffect` đang gọi `LayThongTinCaNhan` hiện có (cạnh
dòng `setTenHienThi(hoSo.tenHienThi)`).

Thêm 1 khối ngay TRÊN mục "Tên hiển thị" đã có trong tab "Tài khoản",
cùng pattern `doiAnh`/UI với `PanelQuanLyNhom.tsx` (đọc file đó để bám
đúng): `Avatar` lớn hiển thị `duongDanAnhDaiDien`, nút "Đổi ảnh đại
diện" (label bọc `<input type="file" hidden>`), lưu NGAY khi chọn file
(không preview/chờ xác nhận — khác với modal onboarding). Cũng cần
thêm 1 state lỗi riêng (`loiAnh`) và 1 state `dangTaiAnh` — không dùng
chung `loi`/`dangLuu` đã có (2 biến đó gắn với luồng lưu cài đặt
dạng toggle riêng, không liên quan avatar):
```typescript
function doiAnhDaiDien(tep: File) {
  if (!token) return;
  if (!tep.type.startsWith('image/')) { setLoiAnh('Chỉ chấp nhận file ảnh.'); return; }
  if (tep.size > GIOI_HAN_ANH_BYTES) { setLoiAnh(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`); return; }
  setDangTaiAnh(true);
  TaiLenTep(token, tep)
    .then((daTaiLen) => DoiAnhDaiDien(token, daTaiLen.duongDanFile))
    .then((hoSoMoi) => setDuongDanAnhDaiDien(hoSoMoi.duongDanAnhDaiDien))
    .catch(() => setLoiAnh('Đổi ảnh đại diện thất bại.'))
    .finally(() => setDangTaiAnh(false));
}
```
`GIOI_HAN_ANH_BYTES` dùng lại đúng giá trị 5MB đã định nghĩa trong
`PanelQuanLyNhom.tsx` (copy hằng số, không cần export dùng chung vì
2 file độc lập theo cấu trúc hiện có của dự án).

### 4.5. `PanelThongTinNhom.tsx` — lối tắt đổi ảnh đại diện nhóm

Thêm prop `onCapNhatNhom: (nhomMoi: Nhom) => void` (giống hệt prop đã
có ở `PanelQuanLyNhom`). `TrangNhom.tsx` truyền cùng 1 hàm xử lý đang
truyền cho `PanelQuanLyNhom` xuống cả `PanelThongTinNhom`.

Chỉ khi `laAdmin`, bọc `<Avatar>` hiện có (dòng có
`duongDanAnh={nhom.duongDanAnhDaiDien}`) trong 1 `div` quan hệ định vị
tương đối, thêm nút camera nhỏ tuyệt đối góc dưới phải (class mới
`.panel-thong-tin-nhom__nut-doi-anh`, dùng lại icon camera đã có —
kiểm tra `BieuTuong.tsx` xem đã có icon camera chưa, nếu chưa thêm
`BieuTuongMayAnh` đã có sẵn từ trước — xác nhận lại tên icon thật khi
viết plan), bấm mở `<input type="file" hidden>`, dùng lại NGUYÊN VĂN
logic `doiAnh` đã có trong `PanelQuanLyNhom.tsx` (copy sang, không tạo
hook dùng chung — 2 file độc lập theo cấu trúc hiện có):
```typescript
function doiAnh(tep: File) {
  if (!token) return;
  if (!tep.type.startsWith('image/')) { setLoi('Chỉ chấp nhận file ảnh.'); return; }
  if (tep.size > GIOI_HAN_ANH_BYTES) { setLoi(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`); return; }
  setDangTaiAnh(true);
  TaiLenTep(token, tep)
    .then((daTaiLen) => CapNhatNhom(token, nhom.id, nhom.tenNhom, nhom.moTa, daTaiLen.duongDanFile))
    .then(onCapNhatNhom)
    .catch(() => setLoi('Đổi ảnh đại diện thất bại.'))
    .finally(() => setDangTaiAnh(false));
}
```

### 4.6. Cập nhật 5 chỗ dùng sẵn `<Avatar>` cho avatar cá nhân

Sau khi `NguoiDungTomTat` có `duongDanAnhDaiDien`, thêm
`duongDanAnh={x.duongDanAnhDaiDien}` vào các lệnh gọi `<Avatar>` đang
render NGƯỜI DÙNG (không phải nhóm) tại: `TrangChat.tsx`,
`TrangBanBe.tsx`, `PanelQuanLyNhom.tsx` (danh sách thành viên — dòng
95, khác với avatar NHÓM ở dòng 63 đã có sẵn `duongDanAnh`),
`PanelThongTinNhom.tsx` (danh sách thành viên — dòng 40, khác avatar
nhóm ở dòng 25 đã có sẵn). Đọc kỹ từng file thật để xác định đúng biến
chứa `NguoiDungTomTat` tại từng lệnh gọi trước khi sửa — không suy
đoán tên biến.

### 4.7. `KhungTinNhan.tsx` — nâng cấp đầu đề khung chat

Đầu đề khung chat hiện có 2 chỗ dùng
`<span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>`
(1 cho nhánh `onBamTieuDe`, 1 cho nhánh không có — xem GĐ7b Task 2 đã
từng sửa đúng khối JSX này). Thêm prop mới `duongDanAnh?: string | null`
vào `PropsKhungTinNhan`, thay CẢ 2 span đó bằng
`<Avatar id={idHienTaiCuaCuocTroChuyen} ten={tenHienThi} duongDanAnh={duongDanAnh} />`
(cần xác định đúng giá trị `id` truyền vào — với 1-1 là id đối tác, với
nhóm là id nhóm; kiểm tra prop nào của `KhungTinNhan` đang giữ giá trị
này, có thể đã có sẵn 1 prop `id`/`idCuocTroChuyen` dùng cho mục đích
khác, đọc file thật trước khi quyết định thêm prop mới hay tái dùng).

`TrangChat.tsx` truyền `duongDanAnh={nguoiDangChon?.duongDanAnhDaiDien}`,
`TrangNhom.tsx` truyền `duongDanAnh={nhomDangChon?.duongDanAnhDaiDien}`.

## 5. Kiểm thử

- Backend: test `DoiAnhDaiDienAsync` (thành công, id không tồn tại trả
  null), test `DanhDauHoanTatHoSoAsync` (set đúng cờ, giữ nguyên các
  field khác), test controller cho 2 endpoint mới (401 khi chưa đăng
  nhập, 400 khi thiếu `DuongDanAnhDaiDien`, 200 kèm đúng DTO).
- Frontend: test `ModalHoanTatHoSo` (chọn ảnh hiện preview đúng, gõ tên
  cập nhật bộ đếm, bấm Hoàn tất gọi đúng chuỗi API khi có/không có thay
  đổi ảnh hoặc tên, bấm X không gọi `DoiAnhDaiDien`/`DoiTenHienThi`
  nhưng vẫn gọi `DanhDauHoanTatHoSo`); test `TrangChat.tsx` hiện modal
  đúng khi `daXemHoanTatHoSo === false` và không hiện khi `true`; test
  `TrangCaiDat.tsx` đổi ảnh đại diện cá nhân lưu ngay; test
  `PanelThongTinNhom.tsx` nút camera chỉ hiện khi `laAdmin`, bấm đổi
  ảnh gọi đúng `CapNhatNhom` qua `onCapNhatNhom`; test các avatar cá
  nhân đã cập nhật hiển thị đúng ảnh khi có `duongDanAnhDaiDien`.
