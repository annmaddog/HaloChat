# HaloChat — Tách tên hiển thị khỏi tên tài khoản (GĐ5f)

## 1. Bối cảnh & mục tiêu

Hiện tại `TenTaiKhoan` (định danh đăng nhập duy nhất) được dùng làm tên
hiển thị ở khắp nơi trong app (header chat, danh sách bạn bè, thành viên
nhóm, tìm kiếm...). Người dùng muốn tách biệt: giữ `TenTaiKhoan` làm định
danh đăng nhập cố định, thêm 1 "Tên hiển thị" riêng có thể đổi tùy ý trong
Cài đặt mà không ảnh hưởng đăng nhập.

## 2. Phạm vi

**Trong phạm vi:**
- `NguoiDung` thêm field `TenHienThi` (rỗng = chưa đặt, fallback về
  `TenTaiKhoan`) + method resolve `TenHienThiThucTe()`.
- Endpoint mới `PUT /api/nguoidung/ten-hien-thi` để đổi tên hiển thị.
- `NguoiDungTomTatDto`/`HoSoCaNhanDto` thêm field `TenHienThi` (giá trị đã
  resolve sẵn từ server) — **giữ nguyên** `TenTaiKhoan` trong DTO, không xóa.
- Toàn bộ nơi hiển thị tên NGƯỜI KHÁC ở frontend chuyển từ `.tenTaiKhoan`
  sang `.tenHienThi`: card bạn bè, kết quả tìm kiếm, danh sách lời mời,
  thành viên nhóm (cả `PanelThongTinNhom` và `PanelQuanLyNhom`), checkbox
  chọn thành viên khi tạo nhóm, sidebar hội thoại (`TrangChat`), tiêu đề
  khung chat khi mở cuộc trò chuyện 1-1.
- Tìm kiếm người dùng (trang Bạn bè, sidebar Tin nhắn) khớp theo
  `.tenHienThi` thay vì `.tenTaiKhoan`.
- `TrangCaiDat.tsx` mục "Tài khoản": thêm form đổi tên hiển thị; xóa nút
  "Đăng xuất" khỏi mục này (đã có sẵn ở sidebar `KhungChinh`).

**Ngoài phạm vi:**
- Không đổi JWT (không thêm claim `tenHienThi`) — đây là thuộc tính đổi
  được, đọc qua API (`LayThongTinCaNhan`), không nhúng vào token.
- Không yêu cầu tên hiển thị duy nhất (không check trùng).
- `TenTaiKhoan` (định danh đăng nhập, hiển thị tĩnh trong "Tài khoản") —
  không đổi được, không có UI đổi tên tài khoản.
- Không xóa `TenTaiKhoan` khỏi `NguoiDungTomTatDto`/`NguoiDungTomTat` —
  giữ nguyên, chỉ thêm field mới bên cạnh.

## 3. Backend

### 3.1. Model `NguoiDung`

```csharp
// [GĐ5f] Rỗng = chưa từng đặt tên hiển thị riêng — dùng TenTaiKhoan làm
// tên hiển thị mặc định (xem TenHienThiThucTe()). Tách biệt hẳn khỏi
// TenTaiKhoan (định danh đăng nhập duy nhất, không đổi được).
public string TenHienThi { get; set; } = string.Empty;

/// <summary>Tên thực sự dùng để hiển thị — TenHienThi nếu đã đặt, không thì TenTaiKhoan.</summary>
public string TenHienThiThucTe() => string.IsNullOrWhiteSpace(TenHienThi) ? TenTaiKhoan : TenHienThi;
```

### 3.2. Mở rộng DTO

```csharp
public record NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa, string TenHienThi);
public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom,
    string TenHienThi);
```

Cả 2 record chỉ **thêm** tham số cuối, không xóa tham số nào. Ở mọi nơi
khởi tạo (`DichVuNhom.AnhXaDtoAsync`, `DichVuTinNhan.LayDanhSachHoiThoaiAsync`,
`DichVuNguoiDung.LayDanhSachNguoiDung`/`LayThongTinCaNhanAsync`,
`DichVuKetBan.AnhXaDto`/`LayBanBeAsync`), truyền `nd.TenHienThiThucTe()`
vào tham số mới.

### 3.3. Đổi tên hiển thị — endpoint mới

```
PUT /api/nguoidung/ten-hien-thi
Body: { tenHienThi: string }   // [Required, MinLength(1), MaxLength(50)] sau khi trim
```

- `[Authorize]`.
- `INguoiDungRepository` thêm `Task CapNhatTenHienThiAsync(string id, string tenHienThi)`.
- `IDichVuNguoiDung` thêm `Task<HoSoCaNhanDto?> DoiTenHienThiAsync(string idHienTai, string tenHienThiMoi)`:
  trim đầu vào, gọi repo, trả về hồ sơ mới nhất (dùng lại
  `LayThongTinCaNhanAsync` sau khi cập nhật) để frontend cập nhật UI ngay
  không cần gọi thêm request.
- Controller trả `200 OK` kèm `HoSoCaNhanDto` mới, hoặc `401` nếu chưa đăng nhập.

## 4. Frontend

### 4.1. Kiểu dữ liệu + API

- `NguoiDungTomTat` thêm `tenHienThi: string`.
- `HoSoCaNhan` thêm `tenHienThi: string`.
- `DichVuApi.ts` thêm `DoiTenHienThi(token: string, tenHienThiMoi: string): Promise<HoSoCaNhan>`
  → `PUT /nguoidung/ten-hien-thi`.

### 4.2. Đổi hiển thị tên người khác

Rà từng file, đổi `.tenTaiKhoan` → `.tenHienThi` khi hiển thị tên 1 người
dùng KHÁC (không phải chính mình qua `nguoiDungHienTai`):
- `TrangBanBe.tsx`: card bạn bè, card kết quả tìm kiếm, card lời mời đến,
  khung hồ sơ (`hoSoDangXem`).
- `TrangNhom.tsx`: checkbox chọn thành viên lúc tạo nhóm.
- `TrangChat.tsx`: sidebar danh sách hội thoại, prop `tenHienThi` truyền
  vào `KhungTinNhan` (dùng `nguoiDangChon.tenHienThi` thay vì `.tenTaiKhoan`
  — lưu ý tên prop `tenHienThi` của `KhungTinNhan` là tên chung cho tiêu đề
  header, trùng tên nhưng khác khái niệm với field mới này).
- `PanelThongTinNhom.tsx`: `Avatar` cho từng thành viên trong hàng avatar.
- `PanelQuanLyNhom.tsx`: danh sách thành viên (tên + nhãn "(Admin)"),
  dropdown thêm thành viên.

Component `Avatar` không đổi (vẫn nhận `ten` để tính chữ cái đầu — chỉ đổi
GIÁ TRỊ truyền vào từ `.tenTaiKhoan` sang `.tenHienThi` ở nơi gọi).

### 4.3. Tìm kiếm khớp theo tên hiển thị

- `TrangBanBe.tsx`: `ketQuaTimKiem` filter đổi từ
  `nd.tenTaiKhoan.toLowerCase().includes(...)` sang `nd.tenHienThi...`.
- `TrangChat.tsx`: filter sidebar theo `tuKhoaTimKiem` đổi tương tự.

### 4.4. `TrangCaiDat.tsx` — mục Tài khoản

- Thêm form đổi tên hiển thị (theo đúng mẫu form đổi tên nhóm ở
  `PanelQuanLyNhom.tsx`): 1 input pre-fill `hoSo.tenHienThi`, nút "Lưu"
  disabled khi rỗng/không đổi/đang lưu, gọi `DoiTenHienThi`, cập nhật lại
  state `hoSo` cục bộ từ response.
- Dòng `<p><strong>Tên tài khoản:</strong> {nguoiDungHienTai?.tenTaiKhoan}</p>`
  **giữ nguyên tĩnh**, không có nút đổi (đây là định danh đăng nhập).
- **Xóa** nút `<button className="trang-cai-dat__nut-dang-xuat" onClick={dangXuat}>Đăng xuất</button>`
  khỏi mục Tài khoản (đã có sẵn trong `KhungChinh` sidebar).

## 5. Kiểm thử

- Backend: unit test `NguoiDung.TenHienThiThucTe()` (rỗng → fallback,
  có giá trị → dùng giá trị); `DoiTenHienThiAsync` (đổi thành công, input
  rỗng sau trim → lỗi validation ở tầng DTO); test các DTO đã có sẵn cập
  nhật để không gãy vì thêm tham số.
- Frontend: test cho form đổi tên hiển thị trong `TrangCaiDat.test.tsx`
  (đổi thành công, nút Lưu disabled đúng lúc); cập nhật fixture
  `NguoiDungTomTat`/`HoSoCaNhan` ở TẤT CẢ test hiện có (rất nhiều file,
  giống việc thêm `choPhepTinNhanTuNguoiLa` ở GĐ5d) thêm field
  `tenHienThi`; test tìm kiếm khớp theo tên hiển thị thay vì tên tài khoản.
