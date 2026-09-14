# GĐ5c — Redesign giao diện & tính năng bổ sung Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Làm lại giao diện HaloChat theo mockup mới (avatar tròn dùng chung,
công tắc bật/tắt, badge số chưa đọc trên sidebar, dark mode, trang Cài đặt
đầy đủ 5 mục) và hoàn thiện các tính năng thật đi kèm (đổi mật khẩu, chặn
thêm vào nhóm, theo dõi đã đọc tin nhắn nhóm theo từng thành viên).

**Architecture:** Backend thêm field/endpoint mới trên `NguoiDung` và
`Nhom` (không phá interface hiện có, chỉ mở rộng tham số); thêm model +
repository mới `DocNhom` để theo dõi đã đọc theo từng thành viên, thay thế
cơ chế `TinNhan.DaDoc` dùng chung hiện tại cho nhóm. Frontend thêm 3
component dùng chung (`Avatar`, `CongTac`, `Huy`) và 1 hook tổng hợp badge
(`SuDungSoLuongChuaDoc`), rồi áp dụng lại từng trang.

**Tech Stack:** Backend ASP.NET Core (.NET 9) + MongoDB Driver + xUnit.
Frontend React 19 + TypeScript + Vite + Vitest.

**Spec:** `docs/superpowers/specs/2026-09-14-halochat-redesign-giao-dien.md`

## Global Constraints

- Toàn bộ tên biến/hàm/route bằng tiếng Việt không dấu, đúng quy ước hiện có của repo (`NguoiDung`, `DichVuNhom`, v.v.) — không tự ý đổi ngôn ngữ đặt tên.
- Không đụng tới GĐ6 (mã hóa RSA-AES thật) — mục "Bảo mật" trong Cài đặt giữ nguyên placeholder "sắp ra mắt (GĐ6)".
- Không làm trạng thái tin nhắn đã gửi/nhận/xem (dấu tick) — người dùng đã từ chối rõ ràng.
- Không làm toggle "cho phép nhận lời mời kết bạn" — người dùng đã từ chối.
- Không xây hệ thống thông báo đẩy thật (push/email) — 3 toggle thông báo chỉ điều khiển badge trong ứng dụng.
- Mọi endpoint mới có `[Authorize]` trừ khi spec nói rõ khác (không có ngoại lệ nào trong plan này).
- `TinNhan.DaDoc` giữ nguyên hành vi cho tin nhắn 1-1 (`NguoiNhanId != null`) — chỉ thay đổi cách xử lý cho tin nhắn nhóm (`NhomId != null`).
- Test backend chạy bằng `dotnet test backend/HaloChat.sln`; test frontend bằng `npm test` trong `frontend/` (Vitest).

---

### Task 1: Backend — Cài đặt mở rộng + Đổi mật khẩu

**Files:**
- Modify: `backend/HaloChat.Api/Models/NguoiDung.cs`
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs`
- Modify: `backend/HaloChat.Api/Dto/CapNhatCaiDatRequest.cs`
- Create: `backend/HaloChat.Api/Dto/KetQuaDoiMatKhauDto.cs`
- Create: `backend/HaloChat.Api/Dto/DoiMatKhauRequest.cs`
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`

**Interfaces:**
- Consumes: pattern `KiemTraMatKhau(matKhau, salt, matKhauBam)` / `BamMatKhau(matKhau, salt)` / `TaoSalt()` từ `IDichVuMatKhau` (đã có, dùng nguyên).
- Produces:
  - `INguoiDungRepository.CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong, bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom)` — chữ ký mới, Task 2 và Task 3 (frontend) dùng qua service.
  - `IDichVuNguoiDung.DoiMatKhauAsync(string idHienTai, string matKhauCu, string matKhauMoi) : Task<KetQuaDoiMatKhauDto>`.
  - `HoSoCaNhanDto` có đủ 5 field bool (2 cũ + `ChoPhepThemVaoNhom`, `ThongBaoTinNhanMoi`, `ThongBaoLoiMoiKetBan`, `ThongBaoNhom`) — Task 3 frontend đọc để hiển thị Cài đặt và tính badge.
  - Endpoint `POST /api/nguoidung/doi-mat-khau` `[Authorize]`.

- [ ] **Step 1: Thêm field mới vào model `NguoiDung`**

Trong `backend/HaloChat.Api/Models/NguoiDung.cs`, thêm sau field `HienThiTrangThaiHoatDong` (dòng 30, trước khối comment `[Quên mật khẩu]`):

```csharp
    // [GĐ5c] Khi false, DichVuNhom.ThemThanhVienAsync/TaoNhomAsync từ chối
    // thêm người này vào bất kỳ nhóm nào (xem KhongChoPhepThemVaoNhomException).
    public bool ChoPhepThemVaoNhom { get; set; } = true;

    // [GĐ5c] 3 toggle quyết định badge tương ứng ở sidebar (Tin nhắn/Bạn
    // bè/Nhóm) có cộng dồn số chưa đọc hay không — không điều khiển bất kỳ
    // kênh thông báo đẩy thật nào (hệ thống chưa có kênh nào như vậy).
    public bool ThongBaoTinNhanMoi { get; set; } = true;
    public bool ThongBaoLoiMoiKetBan { get; set; } = true;
    public bool ThongBaoNhom { get; set; } = true;
```

- [ ] **Step 2: Mở rộng `INguoiDungRepository.CapNhatCaiDatAsync`**

Trong `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`, thay dòng 12:

```csharp
    Task CapNhatCaiDatAsync(string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong);
```

thành:

```csharp
    Task CapNhatCaiDatAsync(
        string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom);
```

- [ ] **Step 3: Cập nhật `NguoiDungRepository.CapNhatCaiDatAsync`**

Trong `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`, thay toàn bộ method (dòng 60-67):

```csharp
    public async Task CapNhatCaiDatAsync(
        string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.ChoPhepTinNhanTuNguoiLa, choPhepTinNhanTuNguoiLa)
            .Set(nd => nd.HienThiTrangThaiHoatDong, hienThiTrangThaiHoatDong)
            .Set(nd => nd.ChoPhepThemVaoNhom, choPhepThemVaoNhom)
            .Set(nd => nd.ThongBaoTinNhanMoi, thongBaoTinNhanMoi)
            .Set(nd => nd.ThongBaoLoiMoiKetBan, thongBaoLoiMoiKetBan)
            .Set(nd => nd.ThongBaoNhom, thongBaoNhom);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
```

- [ ] **Step 4: Cập nhật fake `NguoiDungGiaLap.CapNhatCaiDatAsync`**

Trong `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`, thay method (dòng 32-41):

```csharp
    public Task CapNhatCaiDatAsync(
        string id, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.ChoPhepTinNhanTuNguoiLa = choPhepTinNhanTuNguoiLa;
            nguoiDung.HienThiTrangThaiHoatDong = hienThiTrangThaiHoatDong;
            nguoiDung.ChoPhepThemVaoNhom = choPhepThemVaoNhom;
            nguoiDung.ThongBaoTinNhanMoi = thongBaoTinNhanMoi;
            nguoiDung.ThongBaoLoiMoiKetBan = thongBaoLoiMoiKetBan;
            nguoiDung.ThongBaoNhom = thongBaoNhom;
        }
        return Task.CompletedTask;
    }
```

- [ ] **Step 5: Cập nhật `HoSoCaNhanDto` và `LayThongTinCaNhanAsync`**

Trong `backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs`, thay toàn bộ nội dung:

```csharp
namespace HaloChat.Api.Dto;

public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom);
```

Trong `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`, thay `LayThongTinCaNhanAsync` (dòng 93-101):

```csharp
    public async Task<HoSoCaNhanDto?> LayThongTinCaNhanAsync(string id)
    {
        var nguoiDung = await _kho.TimTheoIdAsync(id);
        return nguoiDung is null
            ? null
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong,
                nguoiDung.ChoPhepThemVaoNhom, nguoiDung.ThongBaoTinNhanMoi,
                nguoiDung.ThongBaoLoiMoiKetBan, nguoiDung.ThongBaoNhom);
    }
```

Và thay `CapNhatCaiDatAsync` + interface (dòng 90-91 và `IDichVuNguoiDung.cs` dòng 10):

```csharp
    public Task CapNhatCaiDatAsync(
        string idHienTai, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom) =>
        _kho.CapNhatCaiDatAsync(
            idHienTai, choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong,
            choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom);
```

```csharp
    Task CapNhatCaiDatAsync(
        string idHienTai, bool choPhepTinNhanTuNguoiLa, bool hienThiTrangThaiHoatDong,
        bool choPhepThemVaoNhom, bool thongBaoTinNhanMoi, bool thongBaoLoiMoiKetBan, bool thongBaoNhom);
```

- [ ] **Step 6: Sửa test `CapNhatCaiDatAsync_CapNhatDungTruong`**

Trong `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`, thay test hiện có (dòng 143-153):

```csharp
    [Fact]
    public async Task CapNhatCaiDatAsync_CapNhatDungTruong()
    {
        var (dichVu, kho, _) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguoiA" });

        await dichVu.CapNhatCaiDatAsync("1", true, false, false, false, true, false);

        var daLuu = kho.DanhSach.Single();
        Assert.True(daLuu.ChoPhepTinNhanTuNguoiLa);
        Assert.False(daLuu.HienThiTrangThaiHoatDong);
        Assert.False(daLuu.ChoPhepThemVaoNhom);
        Assert.False(daLuu.ThongBaoTinNhanMoi);
        Assert.True(daLuu.ThongBaoLoiMoiKetBan);
        Assert.False(daLuu.ThongBaoNhom);
    }
```

- [ ] **Step 7: Chạy test để xác nhận Step 1-6 chưa hỏng gì**

Run: `dotnet test backend/HaloChat.sln --filter DichVuNguoiDungTests`
Expected: PASS toàn bộ.

- [ ] **Step 8: Commit**

```bash
git add backend/HaloChat.Api/Models/NguoiDung.cs backend/HaloChat.Api/Repositories/INguoiDungRepository.cs backend/HaloChat.Api/Repositories/NguoiDungRepository.cs backend/HaloChat.Api/Services/IDichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuNguoiDung.cs backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs
git commit -m "Backend: mo rong cai dat (cho phep them vao nhom, 3 toggle thong bao)"
```

- [ ] **Step 9: Viết test cho Đổi mật khẩu (TDD — viết trước khi có code)**

Thêm vào cuối `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs` (trước dấu `}` đóng class cuối file):

```csharp
    [Fact]
    public async Task DoiMatKhau_DungMatKhauCu_DoiThanhCong()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var matKhau = new DichVuMatKhau();
        var salt = matKhau.TaoSalt();
        var nguoiDung = new NguoiDung
        {
            Id = "1", TenTaiKhoan = "NguoiA", Salt = salt, MatKhauBam = matKhau.BamMatKhau("MatKhauCu123", salt),
        };
        kho.DanhSach.Add(nguoiDung);

        var ketQua = await dichVu.DoiMatKhauAsync("1", "MatKhauCu123", "MatKhauMoi456");

        Assert.True(ketQua.ThanhCong);
        Assert.True(matKhau.KiemTraMatKhau("MatKhauMoi456", nguoiDung.Salt, nguoiDung.MatKhauBam));
    }

    [Fact]
    public async Task DoiMatKhau_SaiMatKhauCu_TraVeThatBaiKhongDoiGiMatKhau()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var matKhau = new DichVuMatKhau();
        var salt = matKhau.TaoSalt();
        var matKhauBamGoc = matKhau.BamMatKhau("MatKhauCu123", salt);
        var nguoiDung = new NguoiDung { Id = "1", TenTaiKhoan = "NguoiA", Salt = salt, MatKhauBam = matKhauBamGoc };
        kho.DanhSach.Add(nguoiDung);

        var ketQua = await dichVu.DoiMatKhauAsync("1", "SaiMatKhau", "MatKhauMoi456");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal(matKhauBamGoc, nguoiDung.MatKhauBam);
    }

    [Fact]
    public async Task DoiMatKhau_NguoiDungKhongTonTai_TraVeThatBai()
    {
        var (dichVu, _, _) = TaoDichVu();

        var ketQua = await dichVu.DoiMatKhauAsync("khong-ton-tai", "MatKhauCu123", "MatKhauMoi456");

        Assert.False(ketQua.ThanhCong);
    }
```

- [ ] **Step 10: Chạy test để xác nhận thất bại (chưa có `DoiMatKhauAsync`)**

Run: `dotnet test backend/HaloChat.sln --filter DichVuNguoiDungTests`
Expected: FAIL biên dịch — `'DichVuNguoiDung' does not contain a definition for 'DoiMatKhauAsync'`.

- [ ] **Step 11: Tạo DTO cho Đổi mật khẩu**

Tạo `backend/HaloChat.Api/Dto/KetQuaDoiMatKhauDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record KetQuaDoiMatKhauDto(bool ThanhCong, string ThongBao);
```

Tạo `backend/HaloChat.Api/Dto/DoiMatKhauRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record DoiMatKhauRequest(
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ.")]
    string MatKhauCu,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    string MatKhauMoi);
```

- [ ] **Step 12: Cài đặt `DoiMatKhauAsync` trong `DichVuNguoiDung`**

Thêm vào `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`, cuối interface:

```csharp
    Task<KetQuaDoiMatKhauDto> DoiMatKhauAsync(string idHienTai, string matKhauCu, string matKhauMoi);
```

Thêm vào `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`, sau method `DatLaiMatKhauAsync` (trước `TaoMaOtp`):

```csharp
    public async Task<KetQuaDoiMatKhauDto> DoiMatKhauAsync(string idHienTai, string matKhauCu, string matKhauMoi)
    {
        var nguoiDung = await _kho.TimTheoIdAsync(idHienTai);
        if (nguoiDung is null)
        {
            return new KetQuaDoiMatKhauDto(false, "Không tìm thấy tài khoản.");
        }

        if (!_dichVuMatKhau.KiemTraMatKhau(matKhauCu, nguoiDung.Salt, nguoiDung.MatKhauBam))
        {
            return new KetQuaDoiMatKhauDto(false, "Mật khẩu cũ không đúng.");
        }

        var saltMoi = _dichVuMatKhau.TaoSalt();
        var matKhauBamMoi = _dichVuMatKhau.BamMatKhau(matKhauMoi, saltMoi);
        await _kho.DatLaiMatKhauAsync(nguoiDung.Id, matKhauBamMoi, saltMoi);

        return new KetQuaDoiMatKhauDto(true, "Đã đổi mật khẩu thành công.");
    }
```

(Dùng lại `INguoiDungRepository.DatLaiMatKhauAsync` đã có — nó set `MatKhauBam`/`Salt` và dọn sạch mọi trạng thái OTP, đúng ý muốn ở đây.)

- [ ] **Step 13: Chạy test để xác nhận PASS**

Run: `dotnet test backend/HaloChat.sln --filter DichVuNguoiDungTests`
Expected: PASS toàn bộ (bao gồm 3 test mới).

- [ ] **Step 14: Thêm endpoint controller**

Trong `backend/HaloChat.Api/Controllers/NguoiDungController.cs`, thêm sau method `LayTrangThaiHoatDong` (trước `QuenMatKhau`):

```csharp
    [HttpPost("doi-mat-khau")]
    [Authorize]
    public async Task<IActionResult> DoiMatKhau([FromBody] DoiMatKhauRequest yeuCau)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var ketQua = await _dichVu.DoiMatKhauAsync(IdHienTai, yeuCau.MatKhauCu, yeuCau.MatKhauMoi);
        if (!ketQua.ThanhCong)
        {
            return BadRequest(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }
```

- [ ] **Step 15: Build toàn bộ solution để xác nhận không lỗi biên dịch**

Run: `dotnet build backend/HaloChat.sln`
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 16: Chạy toàn bộ test backend**

Run: `dotnet test backend/HaloChat.sln`
Expected: PASS toàn bộ (không có test nào bị hỏng bởi thay đổi này).

- [ ] **Step 17: Commit**

```bash
git add backend/HaloChat.Api/Dto/KetQuaDoiMatKhauDto.cs backend/HaloChat.Api/Dto/DoiMatKhauRequest.cs backend/HaloChat.Api/Services/IDichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuNguoiDung.cs backend/HaloChat.Api/Controllers/NguoiDungController.cs backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs
git commit -m "Backend: them doi mat khau that (POST /api/nguoidung/doi-mat-khau)"
```

---

### Task 2: Backend — Theo dõi đã đọc tin nhắn nhóm theo thành viên + chặn thêm vào nhóm

**Files:**
- Create: `backend/HaloChat.Api/Models/DocNhom.cs`
- Create: `backend/HaloChat.Api/Repositories/IDocNhomRepository.cs`
- Create: `backend/HaloChat.Api/Repositories/DocNhomRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNhom.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNhom.cs`
- Modify: `backend/HaloChat.Api/Services/NgoaiLeNhom.cs`
- Modify: `backend/HaloChat.Api/Controllers/NhomController.cs`
- Modify: `backend/HaloChat.Api/Program.cs`
- Create: `backend/HaloChat.Api.Tests/Fakes/DocNhomGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`
- Create: `backend/HaloChat.Api.Tests/Services/DichVuNhomTests.cs`

**Interfaces:**
- Consumes: `INhomRepository.LayTheoThanhVienAsync`, `INguoiDungRepository.TimTheoIdAsync` (đã có, dùng nguyên trong `DichVuNhom`).
- Produces:
  - `IDocNhomRepository { Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId); Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId); }`.
  - `ITinNhanRepository.DemTinNhanSauIdAsync(string nhomId, string? sauId) : Task<int>` — thay thế `DanhDauDaDocNhomAsync` (bị xóa khỏi interface này).
  - `IDichVuNhom.DemTongChuaDocAsync(string nguoiDungId) : Task<int>` — Task 8/9 (frontend) gọi qua endpoint `GET /api/nhom/so-tin-chua-doc`.
  - `KhongChoPhepThemVaoNhomException` — ném khi thêm người đã tắt `ChoPhepThemVaoNhom`.

- [ ] **Step 1: Tạo model `DocNhom`**

Tạo `backend/HaloChat.Api/Models/DocNhom.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

// [GĐ5c] Theo dõi mỗi thành viên đã đọc tới tin nhắn nào trong mỗi nhóm —
// thay thế TinNhan.DaDoc dùng chung cho cả nhóm (không chính xác theo
// từng người). Không đụng TinNhan.DaDoc của nhánh 1-1.
public class DocNhom
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string NhomId { get; set; } = string.Empty;

    // Id tin nhắn mới nhất người này đã đọc trong nhóm này. Null = chưa
    // từng mở nhóm này — mọi tin nhắn trong nhóm đều tính là chưa đọc.
    public string? TinNhanCuoiDaDocId { get; set; }

    public DateTime ThoiGianDoc { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Tạo `IDocNhomRepository` + `DocNhomRepository`**

Tạo `backend/HaloChat.Api/Repositories/IDocNhomRepository.cs`:

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface IDocNhomRepository
{
    /// <summary>Ghi nhận nguoiDungId đã đọc tới tinNhanCuoiId trong nhomId (upsert).</summary>
    Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId);

    /// <summary>Id tin nhắn cuối nguoiDungId đã đọc trong nhomId, null nếu chưa từng đọc.</summary>
    Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId);
}
```

Tạo `backend/HaloChat.Api/Repositories/DocNhomRepository.cs`:

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class DocNhomRepository : IDocNhomRepository
{
    private readonly IMongoCollection<DocNhom> _collection;

    public DocNhomRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<DocNhom>("DocNhom");
    }

    public async Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId)
    {
        var boLoc = Builders<DocNhom>.Filter.And(
            Builders<DocNhom>.Filter.Eq(d => d.NguoiDungId, nguoiDungId),
            Builders<DocNhom>.Filter.Eq(d => d.NhomId, nhomId));
        var capNhat = Builders<DocNhom>.Update
            .Set(d => d.TinNhanCuoiDaDocId, tinNhanCuoiId)
            .Set(d => d.ThoiGianDoc, DateTime.UtcNow)
            .SetOnInsert(d => d.NguoiDungId, nguoiDungId)
            .SetOnInsert(d => d.NhomId, nhomId);
        await _collection.UpdateOneAsync(boLoc, capNhat, new UpdateOptions { IsUpsert = true });
    }

    public async Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId)
    {
        var boLoc = Builders<DocNhom>.Filter.And(
            Builders<DocNhom>.Filter.Eq(d => d.NguoiDungId, nguoiDungId),
            Builders<DocNhom>.Filter.Eq(d => d.NhomId, nhomId));
        var ketQua = await _collection.Find(boLoc).FirstOrDefaultAsync();
        return ketQua?.TinNhanCuoiDaDocId;
    }
}
```

- [ ] **Step 3: Đăng ký DI + tạo chỉ mục cho `DocNhom`**

Trong `backend/HaloChat.Api/Program.cs`, thêm sau dòng `builder.Services.AddScoped<IDichVuNhom, DichVuNhom>();`:

```csharp
builder.Services.AddScoped<IDocNhomRepository, DocNhomRepository>();
```

Trong khối tạo chỉ mục (`if (!app.Configuration.GetValue<bool>("BoQuaKhoiTaoChiMuc"))`), thêm sau khối tạo chỉ mục `NguoiDung` hiện có (trước dấu `}` đóng khối `if`):

```csharp
    var docNhomCollection = csdl.GetCollection<HaloChat.Api.Models.DocNhom>("DocNhom");
    var indexKeysDocNhom = Builders<HaloChat.Api.Models.DocNhom>.IndexKeys
        .Ascending(d => d.NguoiDungId).Ascending(d => d.NhomId);
    await docNhomCollection.Indexes.CreateOneAsync(
        new CreateIndexModel<HaloChat.Api.Models.DocNhom>(indexKeysDocNhom, new CreateIndexOptions { Unique = true }));
```

- [ ] **Step 4: Thay `ITinNhanRepository.DanhDauDaDocNhomAsync` bằng `DemTinNhanSauIdAsync`**

Trong `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`, xóa dòng 25 (`Task DanhDauDaDocNhomAsync(string nhomId);` và comment phía trên nó), thay bằng:

```csharp
    /// <summary>Đếm số tin nhắn của 1 nhóm có Id lớn hơn sauId (mới hơn) — null thì đếm tất cả tin nhắn của nhóm.</summary>
    Task<int> DemTinNhanSauIdAsync(string nhomId, string? sauId);
```

Trong `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`, xóa method `DanhDauDaDocNhomAsync` (dòng 72-79), thay bằng:

```csharp
    public async Task<int> DemTinNhanSauIdAsync(string nhomId, string? sauId)
    {
        var boLocNhom = Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId);
        var boLoc = sauId is null
            ? boLocNhom
            : Builders<TinNhan>.Filter.And(boLocNhom, Builders<TinNhan>.Filter.Gt(t => t.Id, sauId));
        return (int)await _collection.CountDocumentsAsync(boLoc);
    }
```

- [ ] **Step 5: Cập nhật fake `TinNhanGiaLap`**

Trong `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`, xóa method `DanhDauDaDocNhomAsync` (dòng 57-64), thay bằng:

```csharp
    public Task<int> DemTinNhanSauIdAsync(string nhomId, string? sauId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId)
            .Where(t => sauId is null || string.CompareOrdinal(t.Id, sauId) > 0)
            .Count();
        return Task.FromResult(ketQua);
    }
```

- [ ] **Step 6: Tạo fake `DocNhomGiaLap` + đăng ký trong fixture tích hợp**

Tạo `backend/HaloChat.Api.Tests/Fakes/DocNhomGiaLap.cs`:

```csharp
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class DocNhomGiaLap : IDocNhomRepository
{
    // Khóa "nguoiDungId|nhomId" -> id tin nhắn cuối đã đọc.
    public Dictionary<string, string> DaDoc { get; } = new();

    public Task DanhDauDaDocAsync(string nguoiDungId, string nhomId, string tinNhanCuoiId)
    {
        DaDoc[$"{nguoiDungId}|{nhomId}"] = tinNhanCuoiId;
        return Task.CompletedTask;
    }

    public Task<string?> LayTinNhanCuoiDaDocAsync(string nguoiDungId, string nhomId)
    {
        DaDoc.TryGetValue($"{nguoiDungId}|{nhomId}", out var ketQua);
        return Task.FromResult(ketQua);
    }
}
```

Trong `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`, thêm property mới sau `KhoNhomGiaLap` (dòng 33):

```csharp
    public DocNhomGiaLap KhoDocNhomGiaLap { get; } = new();
```

và thêm vào `ConfigureServices` (sau dòng đăng ký `INhomRepository`):

```csharp
            dichVu.RemoveAll<IDocNhomRepository>();
            dichVu.AddSingleton<IDocNhomRepository>(KhoDocNhomGiaLap);
```

- [ ] **Step 7: Cập nhật `IDichVuTinNhan`/`DichVuTinNhan.DanhDauDaDocNhomAsync` dùng `DocNhom`**

Trong `backend/HaloChat.Api/Services/DichVuTinNhan.cs`, thêm field + constructor param `IDocNhomRepository khoDocNhom` (dòng 17-29):

```csharp
    private readonly ITinNhanRepository _khoTinNhan;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ILoiMoiKetBanRepository _khoLoiMoiKetBan;
    private readonly INhomRepository _khoNhom;
    private readonly IDocNhomRepository _khoDocNhom;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IDocNhomRepository khoDocNhom, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _khoDocNhom = khoDocNhom;
        _quanLyKetNoi = quanLyKetNoi;
    }
```

Thay method `DanhDauDaDocNhomAsync` (dòng 147-156):

```csharp
    public async Task DanhDauDaDocNhomAsync(string nguoiHienTaiId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var tinMoiNhat = await _khoTinNhan.LayLichSuNhomAsync(nhomId, null, 1);
        if (tinMoiNhat.Count > 0)
        {
            await _khoDocNhom.DanhDauDaDocAsync(nguoiHienTaiId, nhomId, tinMoiNhat[0].Id);
        }
    }
```

- [ ] **Step 8: Thêm `DemTongChuaDocAsync` vào `IDichVuNhom`/`DichVuNhom`**

Trong `backend/HaloChat.Api/Services/IDichVuNhom.cs`, thêm cuối interface:

```csharp
    Task<int> DemTongChuaDocAsync(string nguoiDungId);
```

Trong `backend/HaloChat.Api/Services/DichVuNhom.cs`, thêm field + constructor param `IDocNhomRepository khoDocNhom` (dòng 10-19):

```csharp
    private readonly INhomRepository _khoNhom;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ITinNhanRepository _khoTinNhan;
    private readonly IDocNhomRepository _khoDocNhom;

    public DichVuNhom(
        INhomRepository khoNhom, INguoiDungRepository khoNguoiDung, ITinNhanRepository khoTinNhan,
        IDocNhomRepository khoDocNhom)
    {
        _khoNhom = khoNhom;
        _khoNguoiDung = khoNguoiDung;
        _khoTinNhan = khoTinNhan;
        _khoDocNhom = khoDocNhom;
    }
```

Thêm method mới (cuối class, trước dấu `}` đóng):

```csharp
    public async Task<int> DemTongChuaDocAsync(string nguoiDungId)
    {
        var danhSachNhom = await _khoNhom.LayTheoThanhVienAsync(nguoiDungId);
        var tong = 0;
        foreach (var nhom in danhSachNhom)
        {
            var tinCuoiDaDoc = await _khoDocNhom.LayTinNhanCuoiDaDocAsync(nguoiDungId, nhom.Id);
            tong += await _khoTinNhan.DemTinNhanSauIdAsync(nhom.Id, tinCuoiDaDoc);
        }
        return tong;
    }
```

- [ ] **Step 9: Thêm ngoại lệ `KhongChoPhepThemVaoNhomException`**

Trong `backend/HaloChat.Api/Services/NgoaiLeNhom.cs`, thêm cuối file (trước dấu `}` đóng namespace nếu có, hoặc cuối file):

```csharp
/// <summary>Ném ra khi cố thêm 1 người đã tắt "cho phép thêm vào nhóm" vào bất kỳ nhóm nào.</summary>
public class KhongChoPhepThemVaoNhomException : Exception
{
    public KhongChoPhepThemVaoNhomException(string tenTaiKhoan)
        : base($"{tenTaiKhoan} không cho phép người khác thêm vào nhóm.")
    {
    }
}
```

- [ ] **Step 10: Viết test cho `ThemThanhVienAsync`/`TaoNhomAsync` chặn theo `ChoPhepThemVaoNhom` + `DemTongChuaDocAsync`**

Tạo `backend/HaloChat.Api.Tests/Services/DichVuNhomTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNhomTests
{
    private static (DichVuNhom DichVu, NhomGiaLap KhoNhom, NguoiDungGiaLap KhoNguoiDung, TinNhanGiaLap KhoTinNhan, DocNhomGiaLap KhoDocNhom) TaoDichVu()
    {
        var khoNhom = new NhomGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var khoTinNhan = new TinNhanGiaLap();
        var khoDocNhom = new DocNhomGiaLap();
        var dichVu = new DichVuNhom(khoNhom, khoNguoiDung, khoTinNhan, khoDocNhom);
        return (dichVu, khoNhom, khoNguoiDung, khoTinNhan, khoDocNhom);
    }

    [Fact]
    public async Task ThemThanhVien_NguoiNhanTatChoPhepThemVaoNhom_NemLoi()
    {
        var (dichVu, khoNhom, khoNguoiDung, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "admin", TenTaiKhoan = "Admin" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "b", TenTaiKhoan = "NguoiB", ChoPhepThemVaoNhom = false });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "Nhom1", NguoiTaoId = "admin", ThanhVienIds = new() { "admin" } });

        await Assert.ThrowsAsync<KhongChoPhepThemVaoNhomException>(() => dichVu.ThemThanhVienAsync("admin", "n1", "b"));
    }

    [Fact]
    public async Task ThemThanhVien_NguoiNhanChoPhep_ThemThanhCong()
    {
        var (dichVu, khoNhom, khoNguoiDung, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "admin", TenTaiKhoan = "Admin" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "b", TenTaiKhoan = "NguoiB", ChoPhepThemVaoNhom = true });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "Nhom1", NguoiTaoId = "admin", ThanhVienIds = new() { "admin" } });

        var nhom = await dichVu.ThemThanhVienAsync("admin", "n1", "b");

        Assert.Contains(nhom.ThanhVien, tv => tv.Id == "b");
    }

    [Fact]
    public async Task TaoNhom_MotThanhVienTatChoPhepThemVaoNhom_NemLoi()
    {
        var (dichVu, _, khoNguoiDung, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "admin", TenTaiKhoan = "Admin" });
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "b", TenTaiKhoan = "NguoiB", ChoPhepThemVaoNhom = false });

        await Assert.ThrowsAsync<KhongChoPhepThemVaoNhomException>(
            () => dichVu.TaoNhomAsync("admin", "NhomMoi", null, null, new List<string> { "b" }));
    }

    [Fact]
    public async Task DemTongChuaDoc_ChuaTungDoc_DemTatCaTinNhanCuaMoiNhom()
    {
        var (dichVu, khoNhom, khoNguoiDung, khoTinNhan, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "toi", TenTaiKhoan = "Toi" });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "N1", NguoiTaoId = "toi", ThanhVienIds = new() { "toi" } });
        khoNhom.DanhSach.Add(new Nhom { Id = "n2", TenNhom = "N2", NguoiTaoId = "toi", ThanhVienIds = new() { "toi" } });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "1", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "2", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "3", NhomId = "n2" });

        var tong = await dichVu.DemTongChuaDocAsync("toi");

        Assert.Equal(3, tong);
    }

    [Fact]
    public async Task DemTongChuaDoc_DaDocMotPhan_ChiDemTinMoiHonMocDaDoc()
    {
        var (dichVu, khoNhom, khoNguoiDung, khoTinNhan, khoDocNhom) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "toi", TenTaiKhoan = "Toi" });
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", TenNhom = "N1", NguoiTaoId = "toi", ThanhVienIds = new() { "toi" } });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "1", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "2", NhomId = "n1" });
        khoTinNhan.DanhSach.Add(new TinNhan { Id = "3", NhomId = "n1" });
        await khoDocNhom.DanhDauDaDocAsync("toi", "n1", "1");

        var tong = await dichVu.DemTongChuaDocAsync("toi");

        Assert.Equal(2, tong);
    }
}
```

- [ ] **Step 11: Chạy test để xác nhận thất bại (chưa sửa `DichVuNhom`)**

Run: `dotnet test backend/HaloChat.sln --filter DichVuNhomTests`
Expected: FAIL biên dịch — constructor `DichVuNhom` chưa nhận `IDocNhomRepository`, chưa có `KhongChoPhepThemVaoNhomException`/`DemTongChuaDocAsync`.

(Lưu ý: Step 1-9 ở trên đã viết đủ code cho `DichVuNhom`/exception/`DemTongChuaDocAsync` — bước này chỉ để xác nhận trình tự TDD đúng nếu làm tuần tự; nếu Step 1-9 đã áp dụng trước thì bỏ qua bước FAIL này, chạy thẳng Step 12.)

- [ ] **Step 12: Thêm kiểm tra `ChoPhepThemVaoNhom` vào `ThemThanhVienAsync`/`TaoNhomAsync`**

Trong `backend/HaloChat.Api/Services/DichVuNhom.cs`, thay `TaoNhomAsync` (dòng 21-31):

```csharp
    public async Task<NhomDto> TaoNhomAsync(
        string nguoiTaoId, string tenNhom, string? moTa, string? duongDanAnhDaiDien, List<string> thanhVienIds)
    {
        var idThanhVien = new HashSet<string>(thanhVienIds) { nguoiTaoId };
        foreach (var id in idThanhVien)
        {
            if (!ObjectId.TryParse(id, out _))
            {
                throw new ThanhVienKhongTonTaiException(id);
            }

            var nguoiDung = await _khoNguoiDung.TimTheoIdAsync(id);
            if (nguoiDung is null)
            {
                throw new ThanhVienKhongTonTaiException(id);
            }

            if (id != nguoiTaoId && !nguoiDung.ChoPhepThemVaoNhom)
            {
                throw new KhongChoPhepThemVaoNhomException(nguoiDung.TenTaiKhoan);
            }
        }
```

(Giữ nguyên phần còn lại của method — chỉ thay khối `foreach` này.)

Thay `ThemThanhVienAsync` (dòng 110-126):

```csharp
    public async Task<NhomDto> ThemThanhVienAsync(string nguoiGoiId, string nhomId, string thanhVienMoiId)
    {
        var nhom = await LayNhomKiemTraQuanTriAsync(nguoiGoiId, nhomId);

        if (!ObjectId.TryParse(thanhVienMoiId, out _))
        {
            throw new ThanhVienKhongTonTaiException(thanhVienMoiId);
        }

        var thanhVienMoi = await _khoNguoiDung.TimTheoIdAsync(thanhVienMoiId);
        if (thanhVienMoi is null)
        {
            throw new ThanhVienKhongTonTaiException(thanhVienMoiId);
        }

        if (!thanhVienMoi.ChoPhepThemVaoNhom)
        {
            throw new KhongChoPhepThemVaoNhomException(thanhVienMoi.TenTaiKhoan);
        }

        if (!nhom.ThanhVienIds.Contains(thanhVienMoiId))
        {
            await _khoNhom.ThemThanhVienAsync(nhomId, thanhVienMoiId);
            nhom.ThanhVienIds.Add(thanhVienMoiId);
        }

        return await AnhXaDtoAsync(nhom);
    }
```

- [ ] **Step 13: Chạy test để xác nhận PASS**

Run: `dotnet test backend/HaloChat.sln --filter DichVuNhomTests`
Expected: PASS toàn bộ 5 test.

- [ ] **Step 14: Thêm endpoint badge + bắt ngoại lệ mới trong `NhomController`**

Trong `backend/HaloChat.Api/Controllers/NhomController.cs`, thêm sau method `TaoNhom` catch block hiện có, thêm 1 catch mới cho `TaoNhom` (dòng 56-59):

```csharp
        catch (ThanhVienKhongTonTaiException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
        catch (KhongChoPhepThemVaoNhomException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
```

Tương tự thêm catch mới vào method `ThemThanhVien` (sau dòng bắt `ThanhVienKhongTonTaiException`, dòng 146-149):

```csharp
        catch (ThanhVienKhongTonTaiException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
        catch (KhongChoPhepThemVaoNhomException loi)
        {
            return BadRequest(new { thongBao = loi.Message });
        }
```

Thêm endpoint mới cuối class (trước dấu `}` đóng class):

```csharp
    [HttpGet("so-tin-chua-doc")]
    public async Task<IActionResult> LaySoTinChuaDoc()
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var soTinChuaDoc = await _dichVu.DemTongChuaDocAsync(IdHienTai);
        return Ok(new { soTinChuaDoc });
    }
```

- [ ] **Step 15: Build + chạy toàn bộ test backend**

Run: `dotnet build backend/HaloChat.sln`
Expected: `Build succeeded. 0 Error(s)`.

Run: `dotnet test backend/HaloChat.sln`
Expected: PASS toàn bộ. Nếu `DichVuTinNhanTests.cs` có test cũ gọi `DanhDauDaDocNhomAsync` trên `ITinNhanRepository` trực tiếp (không qua service) — sửa test đó để gọi `DemTinNhanSauIdAsync`/kiểm tra qua `DocNhomGiaLap` thay vì field `DaDoc` của tin nhắn nhóm.

- [ ] **Step 16: Commit**

```bash
git add backend/HaloChat.Api/Models/DocNhom.cs backend/HaloChat.Api/Repositories/IDocNhomRepository.cs backend/HaloChat.Api/Repositories/DocNhomRepository.cs backend/HaloChat.Api/Repositories/ITinNhanRepository.cs backend/HaloChat.Api/Repositories/TinNhanRepository.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Services/IDichVuNhom.cs backend/HaloChat.Api/Services/DichVuNhom.cs backend/HaloChat.Api/Services/NgoaiLeNhom.cs backend/HaloChat.Api/Controllers/NhomController.cs backend/HaloChat.Api/Program.cs backend/HaloChat.Api.Tests/Fakes/DocNhomGiaLap.cs backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs backend/HaloChat.Api.Tests/Services/DichVuNhomTests.cs
git commit -m "Backend: theo doi da doc tin nhan nhom theo thanh vien + chan them vao nhom"
```

---

### Task 3: Frontend — Mở rộng `KieuDuLieu.ts` + `DichVuApi.ts`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/DichVuApi.test.ts`

**Interfaces:**
- Consumes: endpoint `POST /api/nguoidung/doi-mat-khau`, `GET /api/nhom/so-tin-chua-doc`, `PUT /api/nguoidung/cai-dat` (7 tham số) từ Task 1/2.
- Produces:
  - `HoSoCaNhan` có đủ 4 field mới (`choPhepThemVaoNhom`, `thongBaoTinNhanMoi`, `thongBaoLoiMoiKetBan`, `thongBaoNhom`) — Task 4 (hook) và Task 6 (Cài đặt) dùng.
  - `SoTinNhomChuaDoc { soTinChuaDoc: number }`.
  - `DoiMatKhau(token, matKhauCu, matKhauMoi): Promise<KetQuaThongBao>`.
  - `LaySoTinNhomChuaDoc(token): Promise<SoTinNhomChuaDoc>`.
  - `CapNhatCaiDat(token, choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom): Promise<void>` — chữ ký mới, thay chữ ký cũ 3 tham số.

- [ ] **Step 1: Mở rộng `HoSoCaNhan` + thêm `SoTinNhomChuaDoc` trong `KieuDuLieu.ts`**

Trong `frontend/src/KieuDuLieu.ts`, thay `HoSoCaNhan` (dòng 55-61):

```typescript
export interface HoSoCaNhan {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  hienThiTrangThaiHoatDong: boolean;
  choPhepThemVaoNhom: boolean;
  thongBaoTinNhanMoi: boolean;
  thongBaoLoiMoiKetBan: boolean;
  thongBaoNhom: boolean;
}
```

Thêm cuối file:

```typescript

export interface SoTinNhomChuaDoc {
  soTinChuaDoc: number;
}
```

- [ ] **Step 2: Sửa `CapNhatCaiDat` trong `DichVuApi.ts`**

Thay method (dòng 180-188):

```typescript
export async function CapNhatCaiDat(
  token: string,
  choPhepTinNhanTuNguoiLa: boolean,
  hienThiTrangThaiHoatDong: boolean,
  choPhepThemVaoNhom: boolean,
  thongBaoTinNhanMoi: boolean,
  thongBaoLoiMoiKetBan: boolean,
  thongBaoNhom: boolean,
): Promise<void> {
  await goiApi('/nguoidung/cai-dat', {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({
      choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong,
      choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom,
    }),
  });
}
```

- [ ] **Step 3: Thêm `DoiMatKhau` + `LaySoTinNhomChuaDoc`**

Thêm vào cuối `frontend/src/DichVuApi.ts`:

```typescript

export async function DoiMatKhau(token: string, matKhauCu: string, matKhauMoi: string): Promise<KetQuaThongBao> {
  return goiApi<KetQuaThongBao>('/nguoidung/doi-mat-khau', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ matKhauCu, matKhauMoi }),
  });
}

export async function LaySoTinNhomChuaDoc(token: string): Promise<SoTinNhomChuaDoc> {
  return goiApi<SoTinNhomChuaDoc>('/nhom/so-tin-chua-doc', {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

Thêm `SoTinNhomChuaDoc` vào import đầu file (dòng 1-4):

```typescript
import type {
  KetQuaDangKy, KetQuaDangNhap, NguoiDungTomTat, TinNhan, TepTinDaTaiLen,
  LoiMoiKetBan, HoiThoaiTomTat, HoSoCaNhan, Nhom, KetQuaRoiNhom, KetQuaThongBao, SoTinNhomChuaDoc,
} from './KieuDuLieu';
```

- [ ] **Step 4: Sửa test hiện có gọi `CapNhatCaiDat` cũ (nếu có) trong `DichVuApi.test.ts`**

Tìm mọi lời gọi `CapNhatCaiDat(token, ...)` trong `frontend/src/DichVuApi.test.ts` và thêm đủ 4 tham số mới, ví dụ đổi:

```typescript
await CapNhatCaiDat('token123', true, false);
```

thành:

```typescript
await CapNhatCaiDat('token123', true, false, true, true, true, true);
```

(Áp dụng cho mọi lời gọi tương tự tìm thấy trong file — giữ nguyên phần assertion phía sau, chỉ sửa tham số truyền vào.)

- [ ] **Step 5: Thêm test cho `DoiMatKhau`/`LaySoTinNhomChuaDoc`**

Thêm vào `frontend/src/DichVuApi.test.ts` (theo đúng pattern `fetchMock`/`vi.fn` mà file này đã dùng cho các hàm khác — copy cấu trúc 1 test hiện có của hàm tương tự, ví dụ test của `GuiYeuCauQuenMatKhau`, và đổi tên hàm/đường dẫn):

```typescript
describe('DoiMatKhau', () => {
  it('goi dung endpoint POST /nguoidung/doi-mat-khau voi body dung', async () => {
    fetchMock.mockResolvedValueOnce(phanHoiThanhCong({ thongBao: 'Đã đổi mật khẩu thành công.' }));

    const ketQua = await DoiMatKhau('token123', 'MatKhauCu', 'MatKhauMoi');

    expect(ketQua.thongBao).toBe('Đã đổi mật khẩu thành công.');
    const [duongDan, tuyChon] = fetchMock.mock.calls[0];
    expect(duongDan).toContain('/nguoidung/doi-mat-khau');
    expect(JSON.parse(tuyChon.body as string)).toEqual({ matKhauCu: 'MatKhauCu', matKhauMoi: 'MatKhauMoi' });
  });
});

describe('LaySoTinNhomChuaDoc', () => {
  it('goi dung endpoint GET /nhom/so-tin-chua-doc', async () => {
    fetchMock.mockResolvedValueOnce(phanHoiThanhCong({ soTinChuaDoc: 5 }));

    const ketQua = await LaySoTinNhomChuaDoc('token123');

    expect(ketQua.soTinChuaDoc).toBe(5);
    expect(fetchMock.mock.calls[0][0]).toContain('/nhom/so-tin-chua-doc');
  });
});
```

(Nếu file dùng tên helper khác `phanHoiThanhCong`/`fetchMock` — dùng đúng tên helper thực tế đã có trong file, giữ cùng shape mock response như các test khác trong cùng file.)

- [ ] **Step 6: Chạy test frontend**

Run: `cd frontend && npm test -- DichVuApi`
Expected: PASS toàn bộ.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts
git commit -m "Frontend: mo rong KieuDuLieu + DichVuApi cho GD5c (cai dat, doi mat khau, badge nhom)"
```

---

### Task 4: Frontend — Component dùng chung (`Avatar`, `CongTac`, `Huy`)

**Files:**
- Create: `frontend/src/ThanhPhan/Avatar.tsx`
- Create: `frontend/src/ThanhPhan/Avatar.css`
- Create: `frontend/src/ThanhPhan/Avatar.test.tsx`
- Create: `frontend/src/ThanhPhan/CongTac.tsx`
- Create: `frontend/src/ThanhPhan/CongTac.css`
- Create: `frontend/src/ThanhPhan/CongTac.test.tsx`
- Create: `frontend/src/ThanhPhan/Huy.tsx`
- Create: `frontend/src/ThanhPhan/Huy.css`
- Create: `frontend/src/ThanhPhan/Huy.test.tsx`

**Interfaces:**
- Produces:
  - `Avatar({ id, ten, kichThuoc? }: { id: string; ten: string; kichThuoc?: 'nho' | 'vua' | 'lon' })` — Task 6/7/8/9 dùng.
  - `CongTac({ batTat, onDoi, disabled?, nhan? }: { batTat: boolean; onDoi: (giaTri: boolean) => void; disabled?: boolean; nhan?: string })` — Task 7 dùng.
  - `Huy({ soLuong }: { soLuong: number })` — Task 5/7/8 dùng.

- [ ] **Step 1: Viết test cho `Avatar`**

Tạo `frontend/src/ThanhPhan/Avatar.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Avatar } from './Avatar';

describe('Avatar', () => {
  it('hien chu cai dau viet hoa cua ten', () => {
    render(<Avatar id="abc" ten="nguyen" />);
    expect(screen.getByText('N')).toBeInTheDocument();
  });

  it('hien dau hoi neu ten rong', () => {
    render(<Avatar id="abc" ten="" />);
    expect(screen.getByText('?')).toBeInTheDocument();
  });

  it('cung mot id luon ra cung mau nen', () => {
    const { container: c1 } = render(<Avatar id="user-1" ten="A" />);
    const { container: c2 } = render(<Avatar id="user-1" ten="B" />);
    const mau1 = (c1.querySelector('.avatar') as HTMLElement).style.background;
    const mau2 = (c2.querySelector('.avatar') as HTMLElement).style.background;
    expect(mau1).toBe(mau2);
  });
});
```

- [ ] **Step 2: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- Avatar`
Expected: FAIL — không tìm thấy module `./Avatar`.

- [ ] **Step 3: Cài đặt `Avatar`**

Tạo `frontend/src/ThanhPhan/Avatar.css`:

```css
.avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  color: #fff;
  font-weight: 700;
  flex-shrink: 0;
  user-select: none;
}

.avatar--nho {
  width: 28px;
  height: 28px;
  font-size: 12px;
}

.avatar--vua {
  width: 40px;
  height: 40px;
  font-size: 16px;
}

.avatar--lon {
  width: 72px;
  height: 72px;
  font-size: 28px;
}
```

Tạo `frontend/src/ThanhPhan/Avatar.tsx`:

```tsx
import './Avatar.css';

const BANG_MAU = ['#2f7bf6', '#00b894', '#e17055', '#a29bfe', '#fdcb6e', '#00cec9'];

function laySoTuChuoi(chuoi: string): number {
  let tong = 0;
  for (let i = 0; i < chuoi.length; i++) {
    tong += chuoi.charCodeAt(i);
  }
  return tong;
}

interface AvatarProps {
  id: string;
  ten: string;
  kichThuoc?: 'nho' | 'vua' | 'lon';
}

export function Avatar({ id, ten, kichThuoc = 'vua' }: AvatarProps) {
  const mau = BANG_MAU[laySoTuChuoi(id) % BANG_MAU.length];
  const chuCaiDau = ten.trim().charAt(0).toUpperCase() || '?';
  return (
    <span className={`avatar avatar--${kichThuoc}`} style={{ background: mau }}>
      {chuCaiDau}
    </span>
  );
}
```

- [ ] **Step 4: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- Avatar`
Expected: PASS toàn bộ 3 test.

- [ ] **Step 5: Viết test cho `CongTac`**

Tạo `frontend/src/ThanhPhan/CongTac.test.tsx`:

```tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { CongTac } from './CongTac';

describe('CongTac', () => {
  it('goi onDoi voi gia tri nguoc lai khi bam', () => {
    const onDoi = vi.fn();
    render(<CongTac batTat={false} onDoi={onDoi} nhan="Vi du" />);
    fireEvent.click(screen.getByRole('switch'));
    expect(onDoi).toHaveBeenCalledWith(true);
  });

  it('phan anh dung trang thai bat/tat qua aria-checked', () => {
    render(<CongTac batTat onDoi={() => {}} nhan="Vi du" />);
    expect(screen.getByRole('switch')).toHaveAttribute('aria-checked', 'true');
  });

  it('khong goi onDoi khi disabled', () => {
    const onDoi = vi.fn();
    render(<CongTac batTat={false} onDoi={onDoi} disabled nhan="Vi du" />);
    fireEvent.click(screen.getByRole('switch'));
    expect(onDoi).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 6: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- CongTac`
Expected: FAIL — không tìm thấy module `./CongTac`.

- [ ] **Step 7: Cài đặt `CongTac`**

Tạo `frontend/src/ThanhPhan/CongTac.css`:

```css
.cong-tac {
  position: relative;
  width: 40px;
  height: 22px;
  border-radius: 999px;
  border: none;
  background: var(--mau-vien);
  cursor: pointer;
  padding: 2px;
  flex-shrink: 0;
  transition: background 0.15s ease;
}

.cong-tac--bat {
  background: var(--mau-chinh);
}

.cong-tac:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.cong-tac__nut {
  display: block;
  width: 18px;
  height: 18px;
  border-radius: 999px;
  background: #fff;
  transition: transform 0.15s ease;
}

.cong-tac--bat .cong-tac__nut {
  transform: translateX(18px);
}
```

Tạo `frontend/src/ThanhPhan/CongTac.tsx`:

```tsx
import './CongTac.css';

interface CongTacProps {
  batTat: boolean;
  onDoi: (giaTri: boolean) => void;
  disabled?: boolean;
  nhan?: string;
}

export function CongTac({ batTat, onDoi, disabled, nhan }: CongTacProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={batTat}
      aria-label={nhan}
      className={`cong-tac${batTat ? ' cong-tac--bat' : ''}`}
      disabled={disabled}
      onClick={() => onDoi(!batTat)}
    >
      <span className="cong-tac__nut" />
    </button>
  );
}
```

- [ ] **Step 8: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- CongTac`
Expected: PASS toàn bộ 3 test.

- [ ] **Step 9: Viết test cho `Huy`**

Tạo `frontend/src/ThanhPhan/Huy.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Huy } from './Huy';

describe('Huy', () => {
  it('khong render gi khi soLuong = 0', () => {
    const { container } = render(<Huy soLuong={0} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('hien dung so khi <= 99', () => {
    render(<Huy soLuong={7} />);
    expect(screen.getByText('7')).toBeInTheDocument();
  });

  it('hien "99+" khi > 99', () => {
    render(<Huy soLuong={150} />);
    expect(screen.getByText('99+')).toBeInTheDocument();
  });
});
```

- [ ] **Step 10: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- Huy`
Expected: FAIL — không tìm thấy module `./Huy`.

- [ ] **Step 11: Cài đặt `Huy`**

Tạo `frontend/src/ThanhPhan/Huy.css`:

```css
.huy {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 3px;
  border-radius: 999px;
  background: var(--mau-loi);
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  line-height: 1;
}
```

Tạo `frontend/src/ThanhPhan/Huy.tsx`:

```tsx
import './Huy.css';

export function Huy({ soLuong }: { soLuong: number }) {
  if (soLuong <= 0) return null;
  return <span className="huy">{soLuong > 99 ? '99+' : soLuong}</span>;
}
```

- [ ] **Step 12: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- Huy`
Expected: PASS toàn bộ 3 test.

- [ ] **Step 13: Commit**

```bash
git add frontend/src/ThanhPhan/Avatar.tsx frontend/src/ThanhPhan/Avatar.css frontend/src/ThanhPhan/Avatar.test.tsx frontend/src/ThanhPhan/CongTac.tsx frontend/src/ThanhPhan/CongTac.css frontend/src/ThanhPhan/CongTac.test.tsx frontend/src/ThanhPhan/Huy.tsx frontend/src/ThanhPhan/Huy.css frontend/src/ThanhPhan/Huy.test.tsx
git commit -m "Frontend: them component dung chung Avatar, CongTac, Huy"
```

---

### Task 5: Frontend — Hook `SuDungSoLuongChuaDoc` + Dark mode

**Files:**
- Create: `frontend/src/NguCanh/SuDungSoLuongChuaDoc.ts`
- Create: `frontend/src/NguCanh/SuDungSoLuongChuaDoc.test.tsx`
- Create: `frontend/src/NguCanh/GiaoDien.ts`
- Create: `frontend/src/NguCanh/GiaoDien.test.ts`
- Modify: `frontend/src/main.tsx`
- Modify: `frontend/src/index.css`

**Interfaces:**
- Consumes: `LayThongTinCaNhan`, `LayDanhSachHoiThoai`, `LayLoiMoiDen`, `LaySoTinNhomChuaDoc` (Task 3), `useXacThuc()`, `useChat()` (đã có).
- Produces:
  - `SuDungSoLuongChuaDoc(): { tinNhan: number; banBe: number; nhom: number }` — Task 6 (`KhungChinh`) dùng.
  - `layGiaoDienDaLuu(): 'sang' | 'toi'`, `apDungGiaoDien(giaoDien: 'sang' | 'toi'): void` — Task 6 (`main.tsx`) và Task 7 (`TrangCaiDat`) dùng.

- [ ] **Step 1: Viết test cho `GiaoDien`**

Tạo `frontend/src/NguCanh/GiaoDien.test.ts`:

```typescript
import { beforeEach, describe, expect, it } from 'vitest';
import { apDungGiaoDien, layGiaoDienDaLuu } from './GiaoDien';

describe('GiaoDien', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('mac dinh la sang khi chua luu gi', () => {
    expect(layGiaoDienDaLuu()).toBe('sang');
  });

  it('apDungGiaoDien dat data-theme tren the html', () => {
    apDungGiaoDien('toi');
    expect(document.documentElement.dataset.theme).toBe('toi');
  });

  it('apDungGiaoDien luu lai lua chon, doc lai dung', () => {
    apDungGiaoDien('toi');
    expect(layGiaoDienDaLuu()).toBe('toi');
  });
});
```

- [ ] **Step 2: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- GiaoDien`
Expected: FAIL — không tìm thấy module `./GiaoDien`.

- [ ] **Step 3: Cài đặt `GiaoDien.ts`**

Tạo `frontend/src/NguCanh/GiaoDien.ts`:

```typescript
const KHOA_LUU = 'halochat-giao-dien';

export type GiaoDien = 'sang' | 'toi';

export function layGiaoDienDaLuu(): GiaoDien {
  try {
    return localStorage.getItem(KHOA_LUU) === 'toi' ? 'toi' : 'sang';
  } catch {
    return 'sang';
  }
}

export function apDungGiaoDien(giaoDien: GiaoDien): void {
  document.documentElement.dataset.theme = giaoDien;
  try {
    localStorage.setItem(KHOA_LUU, giaoDien);
  } catch {
    // localStorage có thể bị chặn (chế độ ẩn danh) — chấp nhận mất khả năng nhớ lựa chọn.
  }
}
```

- [ ] **Step 4: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- GiaoDien`
Expected: PASS toàn bộ 3 test.

- [ ] **Step 5: Thêm token CSS dark mode vào `index.css`**

Thêm vào cuối `frontend/src/index.css`:

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

- [ ] **Step 6: Áp dụng giao diện lúc khởi động trong `main.tsx`**

Thay toàn bộ `frontend/src/main.tsx`:

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { apDungGiaoDien, layGiaoDienDaLuu } from './NguCanh/GiaoDien'

apDungGiaoDien(layGiaoDienDaLuu());

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
```

- [ ] **Step 7: Viết test cho hook `SuDungSoLuongChuaDoc`**

`NguCanhChat.test.tsx` (đã có trong repo) cho thấy cách giả lập SignalR đúng cho
`NhaCungCapChat`: mock `@microsoft/signalr` để `HubConnectionBuilder().build()`
trả về 1 object giả có `on`/`off`/`invoke`, và đặt `localStorage.setItem('haloChatToken', ...)`
trước khi render để `NhaCungCapXacThuc` coi như đã đăng nhập. Dùng lại đúng
cách đó ở đây.

Tạo `frontend/src/NguCanh/SuDungSoLuongChuaDoc.test.tsx`:

```tsx
import { renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import type { ReactNode } from 'react';
import * as DichVuApi from '../DichVuApi';
import { NhaCungCapXacThuc } from './NguCanhXacThuc';
import { NhaCungCapChat } from './NguCanhChat';
import { SuDungSoLuongChuaDoc } from './SuDungSoLuongChuaDoc';

const ketNoiGiaLap = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  onclose: vi.fn(),
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue(ketNoiGiaLap),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function boc({ children }: { children: ReactNode }) {
  return (
    <NhaCungCapXacThuc>
      <NhaCungCapChat>{children}</NhaCungCapChat>
    </NhaCungCapXacThuc>
  );
}

describe('SuDungSoLuongChuaDoc', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('cong dung so tin chua doc cua tat ca hoi thoai vao tinNhan', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com',
      choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([
      { nguoiDung: { id: 'a', tenTaiKhoan: 'A', email: 'a@gmail.com' }, tinNhanCuoi: 'hi', thoiGianTinNhanCuoi: '', soTinChuaDoc: 2 },
      { nguoiDung: { id: 'b', tenTaiKhoan: 'B', email: 'b@gmail.com' }, tinNhanCuoi: 'hi', thoiGianTinNhanCuoi: '', soTinChuaDoc: 3 },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LaySoTinNhomChuaDoc').mockResolvedValue({ soTinChuaDoc: 0 });

    const { result } = renderHook(() => SuDungSoLuongChuaDoc(), { wrapper: boc });

    await waitFor(() => expect(result.current.tinNhan).toBe(5));
  });

  it('tra ve 0 cho tinNhan khi thongBaoTinNhanMoi tat, khong goi LayDanhSachHoiThoai', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com',
      choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: false, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    const spyHoiThoai = vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai');
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LaySoTinNhomChuaDoc').mockResolvedValue({ soTinChuaDoc: 0 });

    const { result } = renderHook(() => SuDungSoLuongChuaDoc(), { wrapper: boc });

    await waitFor(() => expect(result.current.banBe).toBe(0));
    expect(spyHoiThoai).not.toHaveBeenCalled();
    expect(result.current.tinNhan).toBe(0);
  });
});
```

- [ ] **Step 8: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- SuDungSoLuongChuaDoc`
Expected: FAIL — không tìm thấy module.

- [ ] **Step 9: Cài đặt hook `SuDungSoLuongChuaDoc`**

Tạo `frontend/src/NguCanh/SuDungSoLuongChuaDoc.ts`:

```typescript
import { useCallback, useEffect, useState } from 'react';
import { LayDanhSachHoiThoai, LayLoiMoiDen, LaySoTinNhomChuaDoc, LayThongTinCaNhan } from '../DichVuApi';
import { useXacThuc } from './NguCanhXacThuc';
import { useChat } from './NguCanhChat';

export interface SoLuongChuaDoc {
  tinNhan: number;
  banBe: number;
  nhom: number;
}

const RONG: SoLuongChuaDoc = { tinNhan: 0, banBe: 0, nhom: 0 };

export function SuDungSoLuongChuaDoc(): SoLuongChuaDoc {
  const { token } = useXacThuc();
  const { ketNoi } = useChat();
  const [soLuong, setSoLuong] = useState<SoLuongChuaDoc>(RONG);

  const taiLai = useCallback(() => {
    if (!token) {
      setSoLuong(RONG);
      return;
    }

    LayThongTinCaNhan(token)
      .then((hoSo) =>
        Promise.all([
          hoSo.thongBaoTinNhanMoi ? LayDanhSachHoiThoai(token) : Promise.resolve([]),
          hoSo.thongBaoLoiMoiKetBan ? LayLoiMoiDen(token) : Promise.resolve([]),
          hoSo.thongBaoNhom ? LaySoTinNhomChuaDoc(token) : Promise.resolve({ soTinChuaDoc: 0 }),
        ]).then(([hoiThoai, loiMoi, nhom]) => {
          setSoLuong({
            tinNhan: hoiThoai.reduce((tong, h) => tong + h.soTinChuaDoc, 0),
            banBe: loiMoi.length,
            nhom: nhom.soTinChuaDoc,
          });
        }),
      )
      .catch(() => {});
  }, [token]);

  useEffect(() => {
    taiLai();
  }, [taiLai]);

  useEffect(() => {
    if (!ketNoi) return;

    ketNoi.on('NhanTinNhan', taiLai);
    ketNoi.on('NhanLoiMoiKetBan', taiLai);
    ketNoi.on('LoiMoiKetBanDuocChapNhan', taiLai);
    ketNoi.on('NhomDaCapNhat', taiLai);
    return () => {
      ketNoi.off('NhanTinNhan', taiLai);
      ketNoi.off('NhanLoiMoiKetBan', taiLai);
      ketNoi.off('LoiMoiKetBanDuocChapNhan', taiLai);
      ketNoi.off('NhomDaCapNhat', taiLai);
    };
  }, [ketNoi, taiLai]);

  return soLuong;
}
```

- [ ] **Step 10: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- SuDungSoLuongChuaDoc GiaoDien`
Expected: PASS toàn bộ.

- [ ] **Step 11: Chạy toàn bộ test frontend**

Run: `cd frontend && npm test`
Expected: PASS toàn bộ (không có test nào bị hỏng).

- [ ] **Step 12: Commit**

```bash
git add frontend/src/NguCanh/SuDungSoLuongChuaDoc.ts frontend/src/NguCanh/SuDungSoLuongChuaDoc.test.tsx frontend/src/NguCanh/GiaoDien.ts frontend/src/NguCanh/GiaoDien.test.ts frontend/src/main.tsx frontend/src/index.css
git commit -m "Frontend: hook SuDungSoLuongChuaDoc + dark mode (GiaoDien)"
```

---

### Task 6: Frontend — `KhungChinh.tsx` redesign (bỏ chuông, thêm badge, sửa lỗi kích thước)

**Files:**
- Modify: `frontend/src/ThanhPhan/KhungChinh.tsx`
- Modify: `frontend/src/ThanhPhan/KhungChinh.css`
- Modify: `frontend/src/ThanhPhan/KhungChinh.test.tsx`
- Delete: `frontend/src/ThanhPhan/ThongBao.tsx`
- Delete: `frontend/src/ThanhPhan/ThongBao.css`
- Delete: `frontend/src/ThanhPhan/ThongBao.test.tsx`

**Interfaces:**
- Consumes: `SuDungSoLuongChuaDoc()` (Task 5), `Huy` (Task 4).
- Produces: không có interface mới cho task khác — đây là trang lá, không ai import `KhungChinh` để lấy thêm gì ngoài component.

- [ ] **Step 1: Đọc `KhungChinh.test.tsx` hiện có để biết cần sửa gì**

Mở `frontend/src/ThanhPhan/KhungChinh.test.tsx`, xác định các chỗ test import/mock `ThongBao` hoặc kỳ vọng chuông thông báo xuất hiện — các test đó cần đổi sang kỳ vọng `Huy`/badge trên từng `NavLink` thay vì dropdown.

- [ ] **Step 2: Xóa `ThongBao.tsx`/`ThongBao.css`/`ThongBao.test.tsx`**

```bash
git rm frontend/src/ThanhPhan/ThongBao.tsx frontend/src/ThanhPhan/ThongBao.css frontend/src/ThanhPhan/ThongBao.test.tsx
```

- [ ] **Step 3: Viết lại `KhungChinh.tsx`**

Thay toàn bộ nội dung `frontend/src/ThanhPhan/KhungChinh.tsx`:

```tsx
import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { BieuTuongTinNhan, BieuTuongBanBe, BieuTuongNhom, BieuTuongCaiDat } from './BieuTuong';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { SuDungSoLuongChuaDoc } from '../NguCanh/SuDungSoLuongChuaDoc';
import { Huy } from './Huy';
import './KhungChinh.css';

function lopMuc({ isActive }: { isActive: boolean }): string {
  return `khung-chinh__muc${isActive ? ' khung-chinh__muc--dang-chon' : ''}`;
}

export function KhungChinh({ children }: { children: ReactNode }) {
  const { dangXuat } = useXacThuc();
  const soLuong = SuDungSoLuongChuaDoc();

  return (
    <div className="khung-chinh">
      <nav className="khung-chinh__rail">
        <div className="khung-chinh__logo">HaloChat</div>
        <NavLink to="/nguoi-dung" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongTinNhan />
            <Huy soLuong={soLuong.tinNhan} />
          </span>
          <span>Tin nhắn</span>
        </NavLink>
        <NavLink to="/ban-be" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongBanBe />
            <Huy soLuong={soLuong.banBe} />
          </span>
          <span>Bạn bè</span>
        </NavLink>
        <NavLink to="/nhom" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongNhom />
            <Huy soLuong={soLuong.nhom} />
          </span>
          <span>Nhóm</span>
        </NavLink>
        <NavLink to="/cai-dat" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongCaiDat />
          </span>
          <span>Cài đặt</span>
        </NavLink>
        <button className="khung-chinh__dang-xuat" onClick={dangXuat}>
          Đăng xuất
        </button>
      </nav>
      <div className="khung-chinh__noi-dung">{children}</div>
    </div>
  );
}
```

- [ ] **Step 4: Sửa CSS — cố định kích thước 4 mục, bỏ style chuông**

Thay toàn bộ `frontend/src/ThanhPhan/KhungChinh.css`:

```css
.khung-chinh {
  display: flex;
  height: 100vh;
}

.khung-chinh__rail {
  width: 76px;
  flex-shrink: 0;
  background: var(--mau-nen-the);
  border-right: 1px solid var(--mau-vien);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 20px 0;
}

.khung-chinh__muc {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  width: 60px;
  height: 52px;
  box-sizing: border-box;
  border-radius: var(--ban-kinh-o);
  color: var(--mau-chu-phu);
  text-decoration: none;
  font-size: 11px;
  cursor: pointer;
}

.khung-chinh__muc svg {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}

.khung-chinh__icon-cum {
  position: relative;
  display: flex;
}

.khung-chinh__icon-cum .huy {
  position: absolute;
  top: -6px;
  right: -8px;
}

.khung-chinh__muc:hover,
.khung-chinh__muc--dang-chon {
  background: var(--mau-nen-tren);
  color: var(--mau-chinh-dam);
}

.khung-chinh__noi-dung {
  flex: 1;
  min-width: 0;
}

@media (max-width: 640px) {
  .khung-chinh {
    flex-direction: column-reverse;
    height: auto;
    min-height: 100vh;
  }

  .khung-chinh__rail {
    width: 100%;
    flex-direction: row;
    justify-content: space-around;
    padding: 8px 0;
  }
}

.khung-chinh__logo {
  font-size: 11px;
  font-weight: 700;
  color: var(--mau-chinh-dam);
  margin-bottom: 12px;
  text-align: center;
}

.khung-chinh__dang-xuat {
  margin-top: auto;
  border: 1px solid var(--mau-vien);
  background: #fff;
  color: var(--mau-chu-dam);
  border-radius: 999px;
  padding: 6px 10px;
  font-family: inherit;
  font-weight: 600;
  font-size: 11px;
  cursor: pointer;
}
```

- [ ] **Step 5: Sửa `KhungChinh.test.tsx`**

Đọc lại toàn bộ file `frontend/src/ThanhPhan/KhungChinh.test.tsx` hiện có (sau khi Step 1 đã khảo sát) và sửa:
- Xóa mọi `vi.mock('./ThongBao', ...)` hoặc import liên quan `ThongBao`.
- Xóa mọi test kỳ vọng phần tử chuông (`aria-label="Thông báo"` hoặc tương tự).
- Thêm test mới xác nhận badge hiển thị đúng khi mock `SuDungSoLuongChuaDoc` trả về số > 0 (dùng `vi.mock('../NguCanh/SuDungSoLuongChuaDoc', () => ({ SuDungSoLuongChuaDoc: () => ({ tinNhan: 3, banBe: 0, nhom: 0 }) }))` ở đầu file test), ví dụ:

```tsx
it('hien badge so tin nhan chua doc tren muc Tin nhan', () => {
  render(/* dựng theo đúng cách file này đang dựng Provider/Router hiện có */);
  expect(screen.getByText('3')).toBeInTheDocument();
});
```

- [ ] **Step 6: Chạy test**

Run: `cd frontend && npm test -- KhungChinh`
Expected: PASS toàn bộ.

- [ ] **Step 7: Build frontend để chắc chắn không còn tham chiếu `ThongBao` ở đâu khác**

Run: `cd frontend && npm run build`
Expected: build thành công, không lỗi `Cannot find module './ThongBao'`.

- [ ] **Step 8: Chạy toàn bộ test frontend**

Run: `cd frontend && npm test`
Expected: PASS toàn bộ.

- [ ] **Step 9: Commit**

```bash
git add -A frontend/src/ThanhPhan/KhungChinh.tsx frontend/src/ThanhPhan/KhungChinh.css frontend/src/ThanhPhan/KhungChinh.test.tsx frontend/src/ThanhPhan/ThongBao.tsx frontend/src/ThanhPhan/ThongBao.css frontend/src/ThanhPhan/ThongBao.test.tsx
git commit -m "Frontend: bo chuong thong bao, them badge tren sidebar, sua loi kich thuoc muc"
```

---

### Task 7: Frontend — `TrangCaiDat.tsx` viết lại hoàn chỉnh

**Files:**
- Modify: `frontend/src/Trang/TrangCaiDat.tsx`
- Modify: `frontend/src/Trang/TrangCaiDat.css`
- Modify: `frontend/src/Trang/TrangCaiDat.test.tsx`

**Interfaces:**
- Consumes: `CongTac` (Task 4), `apDungGiaoDien`/`layGiaoDienDaLuu` (Task 5), `DoiMatKhau`/`CapNhatCaiDat` (Task 3).

- [ ] **Step 1: Đọc `TrangCaiDat.test.tsx` hiện có**

Mở `frontend/src/Trang/TrangCaiDat.test.tsx`, ghi nhận cách file dựng `token`/mock `DichVuApi` hiện có (copy nguyên cách mock `LayThongTinCaNhan`/`CapNhatCaiDat` đã có, chỉ mở rộng object trả về và tham số gọi).

- [ ] **Step 2: Viết lại `TrangCaiDat.tsx`**

Thay toàn bộ nội dung `frontend/src/Trang/TrangCaiDat.tsx`:

```tsx
import { useEffect, useState } from 'react';
import { LayThongTinCaNhan, CapNhatCaiDat, DoiMatKhau, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { CongTac } from '../ThanhPhan/CongTac';
import { apDungGiaoDien, layGiaoDienDaLuu, type GiaoDien } from '../NguCanh/GiaoDien';
import './TrangCaiDat.css';

type MucCaiDat = 'tai-khoan' | 'quyen-rieng-tu' | 'thong-bao' | 'bao-mat' | 'giao-dien';

export function TrangCaiDat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const [mucDangChon, setMucDangChon] = useState<MucCaiDat>('tai-khoan');

  const [choPhepTinNhanTuNguoiLa, setChoPhepTinNhanTuNguoiLa] = useState(false);
  const [hienThiTrangThaiHoatDong, setHienThiTrangThaiHoatDong] = useState(true);
  const [choPhepThemVaoNhom, setChoPhepThemVaoNhom] = useState(true);
  const [thongBaoTinNhanMoi, setThongBaoTinNhanMoi] = useState(true);
  const [thongBaoLoiMoiKetBan, setThongBaoLoiMoiKetBan] = useState(true);
  const [thongBaoNhom, setThongBaoNhom] = useState(true);

  const [dangTai, setDangTai] = useState(true);
  const [dangLuu, setDangLuu] = useState(false);
  const [daLuu, setDaLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  const [giaoDien, setGiaoDien] = useState<GiaoDien>(layGiaoDienDaLuu());

  const [matKhauCu, setMatKhauCu] = useState('');
  const [matKhauMoi, setMatKhauMoi] = useState('');
  const [xacNhanMatKhauMoi, setXacNhanMatKhauMoi] = useState('');
  const [loiDoiMatKhau, setLoiDoiMatKhau] = useState<string | null>(null);
  const [thanhCongDoiMatKhau, setThanhCongDoiMatKhau] = useState(false);
  const [dangDoiMatKhau, setDangDoiMatKhau] = useState(false);

  useEffect(() => {
    if (!token) return;
    LayThongTinCaNhan(token)
      .then((hoSo) => {
        setChoPhepTinNhanTuNguoiLa(hoSo.choPhepTinNhanTuNguoiLa);
        setHienThiTrangThaiHoatDong(hoSo.hienThiTrangThaiHoatDong);
        setChoPhepThemVaoNhom(hoSo.choPhepThemVaoNhom);
        setThongBaoTinNhanMoi(hoSo.thongBaoTinNhanMoi);
        setThongBaoLoiMoiKetBan(hoSo.thongBaoLoiMoiKetBan);
        setThongBaoNhom(hoSo.thongBaoNhom);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được cài đặt.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function luu(giaTriMoi: {
    choPhepTinNhanTuNguoiLa: boolean;
    hienThiTrangThaiHoatDong: boolean;
    choPhepThemVaoNhom: boolean;
    thongBaoTinNhanMoi: boolean;
    thongBaoLoiMoiKetBan: boolean;
    thongBaoNhom: boolean;
  }) {
    if (!token) return;
    const truoc = {
      choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom,
      thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom,
    };
    setChoPhepTinNhanTuNguoiLa(giaTriMoi.choPhepTinNhanTuNguoiLa);
    setHienThiTrangThaiHoatDong(giaTriMoi.hienThiTrangThaiHoatDong);
    setChoPhepThemVaoNhom(giaTriMoi.choPhepThemVaoNhom);
    setThongBaoTinNhanMoi(giaTriMoi.thongBaoTinNhanMoi);
    setThongBaoLoiMoiKetBan(giaTriMoi.thongBaoLoiMoiKetBan);
    setThongBaoNhom(giaTriMoi.thongBaoNhom);
    setDangLuu(true);
    setDaLuu(false);
    try {
      await CapNhatCaiDat(
        token, giaTriMoi.choPhepTinNhanTuNguoiLa, giaTriMoi.hienThiTrangThaiHoatDong,
        giaTriMoi.choPhepThemVaoNhom, giaTriMoi.thongBaoTinNhanMoi,
        giaTriMoi.thongBaoLoiMoiKetBan, giaTriMoi.thongBaoNhom,
      );
      setDaLuu(true);
    } catch (loiBat) {
      setChoPhepTinNhanTuNguoiLa(truoc.choPhepTinNhanTuNguoiLa);
      setHienThiTrangThaiHoatDong(truoc.hienThiTrangThaiHoatDong);
      setChoPhepThemVaoNhom(truoc.choPhepThemVaoNhom);
      setThongBaoTinNhanMoi(truoc.thongBaoTinNhanMoi);
      setThongBaoLoiMoiKetBan(truoc.thongBaoLoiMoiKetBan);
      setThongBaoNhom(truoc.thongBaoNhom);
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Lưu cài đặt thất bại.');
    } finally {
      setDangLuu(false);
    }
  }

  function doiGiaoDien(moi: GiaoDien) {
    setGiaoDien(moi);
    apDungGiaoDien(moi);
  }

  async function xuLyDoiMatKhau(su: React.FormEvent) {
    su.preventDefault();
    setLoiDoiMatKhau(null);
    setThanhCongDoiMatKhau(false);

    if (matKhauMoi !== xacNhanMatKhauMoi) {
      setLoiDoiMatKhau('Xác nhận mật khẩu mới không khớp.');
      return;
    }
    if (!token) return;

    setDangDoiMatKhau(true);
    try {
      await DoiMatKhau(token, matKhauCu, matKhauMoi);
      setThanhCongDoiMatKhau(true);
      setMatKhauCu('');
      setMatKhauMoi('');
      setXacNhanMatKhauMoi('');
    } catch (loiBat) {
      setLoiDoiMatKhau(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi mật khẩu thất bại.');
    } finally {
      setDangDoiMatKhau(false);
    }
  }

  return (
    <div className="trang-cai-dat">
      <nav className="trang-cai-dat__menu">
        <button className={mucDangChon === 'tai-khoan' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('tai-khoan')}>
          Tài khoản
        </button>
        <button className={mucDangChon === 'quyen-rieng-tu' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('quyen-rieng-tu')}>
          Quyền riêng tư
        </button>
        <button className={mucDangChon === 'thong-bao' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('thong-bao')}>
          Thông báo
        </button>
        <button className={mucDangChon === 'bao-mat' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('bao-mat')}>
          Bảo mật
        </button>
        <button className={mucDangChon === 'giao-dien' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('giao-dien')}>
          Giao diện
        </button>
      </nav>

      <div className="trang-cai-dat__noi-dung">
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}

        {mucDangChon === 'tai-khoan' && (
          <>
            <h2>Tài khoản</h2>
            <p><strong>Tên tài khoản:</strong> {nguoiDungHienTai?.tenTaiKhoan}</p>
            <p><strong>Email:</strong> {nguoiDungHienTai?.email}</p>

            <form className="trang-cai-dat__form-mat-khau" onSubmit={xuLyDoiMatKhau}>
              <h3>Đổi mật khẩu</h3>
              {loiDoiMatKhau && (
                <p className="thong-bao-loi" role="alert">
                  {loiDoiMatKhau}
                </p>
              )}
              {thanhCongDoiMatKhau && <p className="trang-cai-dat__da-luu">Đã đổi mật khẩu thành công.</p>}
              <input
                type="password"
                placeholder="Mật khẩu cũ"
                value={matKhauCu}
                onChange={(su) => setMatKhauCu(su.target.value)}
                required
              />
              <input
                type="password"
                placeholder="Mật khẩu mới"
                value={matKhauMoi}
                onChange={(su) => setMatKhauMoi(su.target.value)}
                required
                minLength={6}
              />
              <input
                type="password"
                placeholder="Xác nhận mật khẩu mới"
                value={xacNhanMatKhauMoi}
                onChange={(su) => setXacNhanMatKhauMoi(su.target.value)}
                required
                minLength={6}
              />
              <button type="submit" className="nut-chinh" disabled={dangDoiMatKhau}>
                {dangDoiMatKhau ? 'Đang đổi...' : 'Đổi mật khẩu'}
              </button>
            </form>

            <button className="trang-cai-dat__nut-dang-xuat" onClick={dangXuat}>Đăng xuất</button>
          </>
        )}

        {mucDangChon === 'quyen-rieng-tu' && (
          <>
            <h2>Quyền riêng tư</h2>
            <label className="trang-cai-dat__dong">
              <span>Cho phép người lạ (chưa kết bạn) nhắn tin cho tôi</span>
              <CongTac
                batTat={choPhepTinNhanTuNguoiLa}
                disabled={dangTai || dangLuu}
                nhan="Cho phép người lạ nhắn tin cho tôi"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa: gt, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Hiển thị trạng thái hoạt động (online/offline) cho bạn bè</span>
              <CongTac
                batTat={hienThiTrangThaiHoatDong}
                disabled={dangTai || dangLuu}
                nhan="Hiển thị trạng thái hoạt động"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong: gt, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Cho phép người khác thêm tôi vào nhóm</span>
              <CongTac
                batTat={choPhepThemVaoNhom}
                disabled={dangTai || dangLuu}
                nhan="Cho phép thêm tôi vào nhóm"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom: gt, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
          </>
        )}

        {mucDangChon === 'thong-bao' && (
          <>
            <h2>Thông báo</h2>
            <label className="trang-cai-dat__dong">
              <span>Tin nhắn mới</span>
              <CongTac
                batTat={thongBaoTinNhanMoi}
                disabled={dangTai || dangLuu}
                nhan="Thông báo tin nhắn mới"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi: gt, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Lời mời kết bạn</span>
              <CongTac
                batTat={thongBaoLoiMoiKetBan}
                disabled={dangTai || dangLuu}
                nhan="Thông báo lời mời kết bạn"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan: gt, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Thông báo nhóm</span>
              <CongTac
                batTat={thongBaoNhom}
                disabled={dangTai || dangLuu}
                nhan="Thông báo nhóm"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom: gt })}
              />
            </label>
            {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
          </>
        )}

        {mucDangChon === 'bao-mat' && (
          <>
            <h2>Bảo mật</h2>
            <p className="trang-cai-dat__sap-ra-mat">🔒 Mã hóa tin nhắn AES-256-GCM + quản lý khóa RSA — sắp ra mắt (GĐ6).</p>
          </>
        )}

        {mucDangChon === 'giao-dien' && (
          <>
            <h2>Giao diện</h2>
            <div className="trang-cai-dat__giao-dien">
              <button
                className={`trang-cai-dat__nut-giao-dien${giaoDien === 'sang' ? ' trang-cai-dat__nut-giao-dien--chon' : ''}`}
                onClick={() => doiGiaoDien('sang')}
              >
                Sáng
              </button>
              <button
                className={`trang-cai-dat__nut-giao-dien${giaoDien === 'toi' ? ' trang-cai-dat__nut-giao-dien--chon' : ''}`}
                onClick={() => doiGiaoDien('toi')}
              >
                Tối
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
```

- [ ] **Step 3: Cập nhật CSS cho form đổi mật khẩu + nút giao diện**

Thêm vào cuối `frontend/src/Trang/TrangCaiDat.css`:

```css
.trang-cai-dat__dong {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px;
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-o);
  border: 1px solid var(--mau-vien);
  margin-bottom: 12px;
}

.trang-cai-dat__form-mat-khau {
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 320px;
  margin: 16px 0;
  padding: 16px;
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-o);
  border: 1px solid var(--mau-vien);
}

.trang-cai-dat__form-mat-khau h3 {
  margin: 0 0 4px;
  font-size: 14px;
}

.trang-cai-dat__form-mat-khau input {
  padding: 10px 12px;
  border: 1px solid var(--mau-vien);
  border-radius: 10px;
  font-family: inherit;
  font-size: 14px;
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
}

.trang-cai-dat__giao-dien {
  display: flex;
  gap: 12px;
}

.trang-cai-dat__nut-giao-dien {
  padding: 10px 20px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  font-family: inherit;
  cursor: pointer;
}

.trang-cai-dat__nut-giao-dien--chon {
  background: var(--mau-chinh);
  border-color: var(--mau-chinh);
  color: #fff;
  font-weight: 700;
}
```

Xóa rule `.trang-cai-dat__dong input` cũ (checkbox 18px, không còn dùng — thay bằng `CongTac`) và rule `.trang-cai-dat__nut-sap-ra-mat` (nút Đổi mật khẩu disabled cũ, không còn dùng) khỏi `frontend/src/Trang/TrangCaiDat.css` nếu còn tồn tại từ trước (dòng cũ 58-62 và 70-79 trong bản gốc).

- [ ] **Step 4: Cập nhật `TrangCaiDat.test.tsx`**

Đọc lại toàn bộ `frontend/src/Trang/TrangCaiDat.test.tsx` hiện có, sửa:
- Mọi mock `LayThongTinCaNhan` trả về object phải có đủ 4 field mới (`choPhepThemVaoNhom`, `thongBaoTinNhanMoi`, `thongBaoLoiMoiKetBan`, `thongBaoNhom` — mặc định `true`).
- Mọi assertion `expect(CapNhatCaiDat).toHaveBeenCalledWith(token, a, b)` sửa thành đủ 6 tham số boolean theo đúng thứ tự chữ ký mới.
- Test cũ về "Đổi mật khẩu" disabled/"sắp ra mắt" (nếu có) — xóa, thay bằng test mới:

```tsx
it('doi mat khau thanh cong hien thong bao', async () => {
  vi.mocked(DichVuApi.DoiMatKhau).mockResolvedValue({ thongBao: 'Đã đổi mật khẩu thành công.' });
  // dựng render giống các test khác trong file, chuyển sang mục "Tài khoản" nếu cần

  fireEvent.change(screen.getByPlaceholderText('Mật khẩu cũ'), { target: { value: 'Cu123456' } });
  fireEvent.change(screen.getByPlaceholderText('Mật khẩu mới'), { target: { value: 'Moi123456' } });
  fireEvent.change(screen.getByPlaceholderText('Xác nhận mật khẩu mới'), { target: { value: 'Moi123456' } });
  fireEvent.click(screen.getByRole('button', { name: 'Đổi mật khẩu' }));

  await waitFor(() => expect(screen.getByText('Đã đổi mật khẩu thành công.')).toBeInTheDocument());
});

it('xac nhan mat khau moi khong khop hien loi, khong goi API', () => {
  // dựng render giống trên
  fireEvent.change(screen.getByPlaceholderText('Mật khẩu mới'), { target: { value: 'Moi123456' } });
  fireEvent.change(screen.getByPlaceholderText('Xác nhận mật khẩu mới'), { target: { value: 'Khac123456' } });
  fireEvent.click(screen.getByRole('button', { name: 'Đổi mật khẩu' }));

  expect(screen.getByText('Xác nhận mật khẩu mới không khớp.')).toBeInTheDocument();
  expect(DichVuApi.DoiMatKhau).not.toHaveBeenCalled();
});
```

- [ ] **Step 5: Chạy test**

Run: `cd frontend && npm test -- TrangCaiDat`
Expected: PASS toàn bộ.

- [ ] **Step 6: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/Trang/TrangCaiDat.tsx frontend/src/Trang/TrangCaiDat.css frontend/src/Trang/TrangCaiDat.test.tsx
git commit -m "Frontend: viet lai TrangCaiDat (5 muc, cong tac, doi mat khau that, dark mode)"
```

---

### Task 8: Frontend — `TrangChat.tsx` + `TrangNhom.tsx` (Avatar + đánh dấu đã đọc)

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangChat.css`
- Modify: `frontend/src/Trang/TrangChat.test.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Modify: `frontend/src/Trang/TrangNhom.css`
- Modify: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `Avatar` (Task 4).

- [ ] **Step 1: `TrangChat.tsx` — dùng `Avatar`, gọi đánh dấu đã đọc khi chọn hội thoại**

Trong `frontend/src/Trang/TrangChat.tsx`, thêm import `Avatar` (đầu file, sau import `KhungTinNhan`):

```tsx
import { Avatar } from '../ThanhPhan/Avatar';
```

Thay dòng `<span className="trang-chat__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>` (dòng 210) bằng:

```tsx
<Avatar id={nd.id} ten={nd.tenTaiKhoan} kichThuoc="nho" />
```

Thay `onClick={() => setNguoiDangChon(nd)}` (dòng 208) bằng:

```tsx
onClick={() => {
  setNguoiDangChon(nd);
  ketNoi?.invoke('DanhDauDaDoc', nd.id, null).catch(() => {});
}}
```

- [ ] **Step 2: Xóa CSS avatar cũ không còn dùng trong `TrangChat.css`**

Mở `frontend/src/Trang/TrangChat.css`, tìm rule `.trang-chat__avatar` (chữ cái đơn tự vẽ) và xóa (giờ dùng `Avatar` dùng chung, style nằm ở `Avatar.css`).

- [ ] **Step 3: Sửa `TrangChat.test.tsx`**

Mở `frontend/src/Trang/TrangChat.test.tsx`, tìm test nào assert nội dung `.trang-chat__avatar` hoặc chữ cái đầu tự vẽ — sửa sang kiểm tra class `.avatar` (từ component `Avatar` dùng chung) thay vì class cũ. Nếu có test click chọn 1 người trong danh sách và assert `ketNoi.invoke` được gọi để lấy lịch sử — không cần sửa gì thêm; nếu muốn kiểm tra thêm lời gọi `DanhDauDaDoc`, thêm assertion:

```tsx
expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('DanhDauDaDoc', 'id-nguoi-duoc-chon', null);
```

(dùng đúng biến mock `ketNoi`/tên hàm giả lập mà file test này đã dựng sẵn cho SignalR — không tạo mock mới nếu file đã có).

- [ ] **Step 4: `TrangNhom.tsx` — dùng `Avatar`, gọi đánh dấu đã đọc khi chọn nhóm**

Trong `frontend/src/Trang/TrangNhom.tsx`, thêm import (đầu file):

```tsx
import { Avatar } from '../ThanhPhan/Avatar';
```

Thay `<span className="trang-nhom__avatar">{n.tenNhom.charAt(0).toUpperCase()}</span>` (dòng 217) bằng:

```tsx
<Avatar id={n.id} ten={n.tenNhom} kichThuoc="nho" />
```

Thay `onClick={() => setNhomDangChonId(n.id)}` (dòng 215) bằng:

```tsx
onClick={() => {
  setNhomDangChonId(n.id);
  ketNoi?.invoke('DanhDauDaDoc', null, n.id).catch(() => {});
}}
```

- [ ] **Step 5: Xóa CSS avatar cũ trong `TrangNhom.css`**

Mở `frontend/src/Trang/TrangNhom.css`, xóa rule `.trang-nhom__avatar`.

- [ ] **Step 6: Sửa `TrangNhom.test.tsx`**

Tương tự Step 3 — sửa mọi assertion về class avatar cũ (`.trang-nhom__avatar`) sang `.avatar`, và (tùy chọn) thêm assertion `ketNoi.invoke` được gọi với `'DanhDauDaDoc', null, <idNhom>` khi bấm chọn 1 nhóm trong sidebar.

- [ ] **Step 7: Chạy test**

Run: `cd frontend && npm test -- TrangChat TrangNhom`
Expected: PASS toàn bộ.

- [ ] **Step 8: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangChat.css frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangNhom.css frontend/src/Trang/TrangNhom.test.tsx
git commit -m "Frontend: TrangChat + TrangNhom dung Avatar dung chung, goi danh dau da doc khi mo hoi thoai/nhom"
```

---

### Task 9: Frontend — `TrangBanBe.tsx` (Avatar + khung hồ sơ)

**Files:**
- Modify: `frontend/src/Trang/TrangBanBe.tsx`
- Modify: `frontend/src/Trang/TrangBanBe.css`
- Modify: `frontend/src/Trang/TrangBanBe.test.tsx`

**Interfaces:**
- Consumes: `Avatar` (Task 4).

- [ ] **Step 1: Thêm state chọn hồ sơ + import `Avatar`**

Trong `frontend/src/Trang/TrangBanBe.tsx`, thêm import (đầu file, sau import `LoiMoiKetBan`):

```tsx
import { Avatar } from '../ThanhPhan/Avatar';
```

Thêm state mới sau dòng `const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');`:

```tsx
  const [hoSoDangXem, setHoSoDangXem] = useState<NguoiDungTomTat | null>(null);
```

- [ ] **Step 2: Thêm `Avatar` vào 3 danh sách + bấm vào 1 người trong "Bạn bè" mở khung hồ sơ**

Thay dòng trong danh sách "Lời mời kết bạn" (`<span>{l.nguoiGui.tenTaiKhoan}</span>`, dòng 108):

```tsx
<span className="trang-ban-be__hang">
  <Avatar id={l.nguoiGui.id} ten={l.nguoiGui.tenTaiKhoan} kichThuoc="nho" />
  {l.nguoiGui.tenTaiKhoan}
</span>
```

Thay khối `<li>` trong danh sách "Bạn bè" (dòng 125-128):

```tsx
<li key={b.id} className="trang-ban-be__muc">
  <button className="trang-ban-be__hang trang-ban-be__hang--bam-duoc" onClick={() => setHoSoDangXem(b)}>
    <Avatar id={b.id} ten={b.tenTaiKhoan} kichThuoc="nho" />
    <span>{b.tenTaiKhoan}</span>
  </button>
  <button onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: b } })}>Nhắn tin</button>
</li>
```

Thay dòng trong danh sách "Tìm người để kết bạn" (`<span>{nd.tenTaiKhoan}</span>`, dòng 141):

```tsx
<span className="trang-ban-be__hang">
  <Avatar id={nd.id} ten={nd.tenTaiKhoan} kichThuoc="nho" />
  {nd.tenTaiKhoan}
</span>
```

- [ ] **Step 3: Thêm khung hồ sơ bên phải**

Thêm ngay trước dấu `</div>` đóng cuối cùng của component (sau `</section>` cuối):

```tsx
      {hoSoDangXem && (
        <aside className="trang-ban-be__ho-so">
          <button className="trang-ban-be__dong-ho-so" onClick={() => setHoSoDangXem(null)} aria-label="Đóng hồ sơ">×</button>
          <Avatar id={hoSoDangXem.id} ten={hoSoDangXem.tenTaiKhoan} kichThuoc="lon" />
          <h3>{hoSoDangXem.tenTaiKhoan}</h3>
          <p className="trang-ban-be__email-ho-so">{hoSoDangXem.email}</p>
          <button
            className="nut-chinh"
            onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: hoSoDangXem } })}
          >
            Nhắn tin
          </button>
        </aside>
      )}
```

- [ ] **Step 4: Thêm CSS cho hàng có avatar + khung hồ sơ**

Thêm vào cuối `frontend/src/Trang/TrangBanBe.css`:

```css
.trang-ban-be__hang {
  display: flex;
  align-items: center;
  gap: 10px;
}

.trang-ban-be__hang--bam-duoc {
  border: none;
  background: none;
  padding: 0;
  font-family: inherit;
  font-size: inherit;
  color: inherit;
  cursor: pointer;
  text-align: left;
}

.trang-ban-be__ho-so {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  width: 280px;
  background: var(--mau-nen-the);
  border-left: 1px solid var(--mau-vien);
  padding: 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  text-align: center;
}

.trang-ban-be__dong-ho-so {
  align-self: flex-end;
  border: none;
  background: none;
  font-size: 20px;
  cursor: pointer;
  color: var(--mau-chu-phu);
}

.trang-ban-be__email-ho-so {
  color: var(--mau-chu-phu);
  font-size: 13px;
  margin: 0 0 12px;
}
```

- [ ] **Step 5: Sửa `TrangBanBe.test.tsx`**

Mở `frontend/src/Trang/TrangBanBe.test.tsx`, thêm test mới:

```tsx
it('bam vao 1 nguoi trong danh sach ban be mo khung ho so', async () => {
  // dựng render giống các test khác trong file, đảm bảo danh sách banBe có ít nhất 1 người (vd { id: '1', tenTaiKhoan: 'NguoiA', email: 'a@gmail.com' })

  fireEvent.click(screen.getByRole('button', { name: /NguoiA/i }));

  expect(screen.getByText('a@gmail.com')).toBeInTheDocument();
});
```

(Điều chỉnh đúng tên biến mock/dữ liệu giả mà file này đã dùng cho `LayBanBe` — không tạo lại toàn bộ setup, chỉ thêm test mới nối vào cấu trúc `describe` đã có.)

- [ ] **Step 6: Chạy test**

Run: `cd frontend && npm test -- TrangBanBe`
Expected: PASS toàn bộ.

- [ ] **Step 7: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/Trang/TrangBanBe.tsx frontend/src/Trang/TrangBanBe.css frontend/src/Trang/TrangBanBe.test.tsx
git commit -m "Frontend: TrangBanBe dung Avatar dung chung + khung ho so ben phai"
```

---

## Ghi chú cho người thực thi plan (subagent-driven-development)

- Task 1 và Task 2 (backend) có thể phát hiện thêm test cũ ở `DichVuTinNhanTests.cs`/`NhomControllerTests.cs` gọi trực tiếp các phương thức đã đổi chữ ký (`DanhDauDaDocNhomAsync` trên repository, constructor `DichVuNhom`/`DichVuTinNhan`) — đây là việc BÌNH THƯỜNG khi đổi interface có nhiều nơi dùng; implementer tự sửa các lời gọi đó cho khớp chữ ký mới, không cần hỏi lại.
- Mọi Task frontend đọc file test hiện có TRƯỚC khi sửa để bắt đúng pattern mock/dựng Provider của repo (đừng đoán) — các bước trên đã nhắc rõ ở từng chỗ cần việc này.
- Task 3-9 phụ thuộc tuần tự vào Task 1-2 (backend) đã xong trước, và Task 4-5 (component/hook dùng chung) phải xong trước Task 6-9 (áp dụng vào trang) — thực hiện đúng thứ tự 1→9, không nhảy cóc.
