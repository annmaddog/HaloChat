# GĐ8 — Ảnh đại diện cá nhân & modal "Hoàn tất hồ sơ" — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm ảnh đại diện cho tài khoản cá nhân (đổi ở Cài đặt, hiển
thị mọi nơi), modal "Hoàn tất hồ sơ" hiện đúng 1 lần ở lần đăng nhập
đầu tiên, và lối tắt đổi ảnh đại diện nhóm ngay tại "Thông tin nhóm".

**Architecture:** Backend thêm 2 field trên `NguoiDung`
(`DuongDanAnhDaiDien`, `DaXemHoanTatHoSo`) + 2 endpoint mới, tái dùng
nguyên endpoint upload file đã có. Frontend thêm 1 modal mới
(`ModalHoanTatHoSo.tsx`), 1 khối đổi ảnh trong Cài đặt, 1 nút camera
trong "Thông tin nhóm", và nối dây field ảnh mới vào các nơi đã dùng
sẵn component `Avatar`.

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api) + MongoDB
Driver, xUnit; React 19 + TypeScript + Vite (frontend/), Vitest.

**Spec:** `docs/superpowers/specs/2026-09-17-halochat-anh-dai-dien.md`

## Global Constraints

- Đặt tên định danh (biến/hàm/class) không dấu tiếng Việt. Văn bản hiển
  thị cho người dùng (label, thông báo lỗi) VẪN viết tiếng Việt có dấu.
- Không cắt/crop ảnh — dùng nguyên ảnh tải lên.
- Không có nút "Xóa ảnh đại diện" — đổi ảnh khác thì thay thế.
- Tái dùng nguyên `POST /api/tinnhan/upload` (`TaiLenTep` phía
  frontend) cho MỌI trường hợp upload ảnh trong plan này — không tạo
  endpoint upload mới.
- `DaXemHoanTatHoSo` là cờ ĐÃ-XEM, không phải cờ đã-hoàn-tất — set
  `true` khi đóng modal theo BẤT KỲ cách nào (Hoàn tất hay bấm X).
- `frontend/tsconfig.json` là solution-style — LUÔN dùng
  `npx tsc -b --noEmit`, KHÔNG dùng `tsc --noEmit` thường (gây false
  negative đã xác nhận nhiều lần trong dự án).
- **Quyết định giảm phạm vi thay đổi (ruling, đã xác nhận qua khảo sát
  codebase thật trước khi viết plan):** field `duongDanAnhDaiDien` trên
  interface TypeScript `NguoiDungTomTat` và 2 field mới trên
  `HoSoCaNhan` (`duongDanAnhDaiDien`, `daXemHoanTatHoSo`) đều khai báo
  **OPTIONAL** (`?`) ở phía TypeScript, dù phía backend C# (positional
  record) là bắt buộc. Lý do: có 58 chỗ literal `NguoiDungTomTat`/
  `HoSoCaNhan` rải rác trong 8 file test hiện có — nếu bắt buộc sẽ phải
  sửa toàn bộ 58 chỗ dù phần lớn không liên quan gì tới ảnh đại diện.
  Component `Avatar` đã coi `duongDanAnh` là optional và tự fallback
  đúng khi `undefined`/`null` — hành vi runtime không đổi.

---

## Task 1: Backend — model, DTO, repository, service, controller

**Files:**
- Modify: `backend/HaloChat.Api/Models/NguoiDung.cs`
- Modify: `backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs`
- Modify: `backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs`
- Create: `backend/HaloChat.Api/Dto/DoiAnhDaiDienRequest.cs`
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuKetBan.cs` (2 chỗ dựng `NguoiDungTomTatDto`)
- Modify: `backend/HaloChat.Api/Services/DichVuNhom.cs` (1 chỗ dựng `NguoiDungTomTatDto`)
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs` (1 chỗ dựng `NguoiDungTomTatDto`)
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Test: `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`
- Test: `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs`

**Interfaces:**
- Consumes: không có (task đầu tiên, độc lập).
- Produces: `IDichVuNguoiDung.DoiAnhDaiDienAsync(idHienTai, duongDanAnhDaiDien): Task<HoSoCaNhanDto?>`,
  `IDichVuNguoiDung.DanhDauHoanTatHoSoAsync(idHienTai): Task<HoSoCaNhanDto?>`;
  endpoint `PUT /api/nguoidung/anh-dai-dien`, `POST /api/nguoidung/danh-dau-hoan-tat-ho-so`;
  `HoSoCaNhanDto.DuongDanAnhDaiDien: string?`, `HoSoCaNhanDto.DaXemHoanTatHoSo: bool`,
  `NguoiDungTomTatDto.DuongDanAnhDaiDien: string?` (Task 2 và các task frontend
  sau dựa vào đúng shape JSON này).

- [ ] **Step 1: Thêm 2 field vào `NguoiDung.cs`**

Thêm vào cuối class `NguoiDung` trong `backend/HaloChat.Api/Models/NguoiDung.cs`:

```csharp
    // [GĐ8] Null = chưa từng đặt ảnh đại diện — Avatar.tsx tự fallback
    // về chữ cái đầu của TenHienThiThucTe(). Cùng kiểu dữ liệu (đường
    // dẫn tới GridFS qua endpoint upload chung) với Nhom.DuongDanAnhDaiDien.
    public string? DuongDanAnhDaiDien { get; set; }

    // [GĐ8] Cờ đã-xem, KHÔNG phải cờ đã-hoàn-tất — set true ngay khi
    // đóng modal "Hoàn tất hồ sơ" theo BẤT KỲ cách nào (bấm Hoàn tất
    // hay bấm X), để modal không bao giờ tự hiện lại sau lần đầu, kể
    // cả khi người dùng bỏ qua không nhập gì.
    public bool DaXemHoanTatHoSo { get; set; } = false;
```

- [ ] **Step 2: Sửa `HoSoCaNhanDto`/`NguoiDungTomTatDto`, tạo `DoiAnhDaiDienRequest`**

`backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs` — thay nội dung file thành:
```csharp
namespace HaloChat.Api.Dto;

public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom,
    string TenHienThi, string? DuongDanAnhDaiDien, bool DaXemHoanTatHoSo);
```

`backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs` — thay nội dung file thành:
```csharp
namespace HaloChat.Api.Dto;

public record NguoiDungTomTatDto(
    string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa,
    string TenHienThi, string? DuongDanAnhDaiDien);
```

Tạo `backend/HaloChat.Api/Dto/DoiAnhDaiDienRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record DoiAnhDaiDienRequest(
    [Required, MinLength(1)] string DuongDanAnhDaiDien);
```

- [ ] **Step 3: Build để thấy lỗi biên dịch ở mọi chỗ dựng 2 record trên**

Run: `cd backend && dotnet build`
Expected: FAIL — báo thiếu tham số ở các chỗ gọi `new HoSoCaNhanDto(...)`/`new NguoiDungTomTatDto(...)` trong `DichVuNguoiDung.cs`, `DichVuKetBan.cs`, `DichVuNhom.cs`, `DichVuTinNhan.cs`.

- [ ] **Step 4: Sửa tất cả chỗ dựng `NguoiDungTomTatDto` (5 chỗ, 4 file)**

`backend/HaloChat.Api/Services/DichVuNguoiDung.cs` dòng có
`.Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe()))`
— sửa thành:
```csharp
            .Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe(), nd.DuongDanAnhDaiDien))
```

`backend/HaloChat.Api/Services/DichVuKetBan.cs` dòng 104
(`new NguoiDungTomTatDto(ban.Id, ban.TenTaiKhoan, ban.Email, ban.ChoPhepTinNhanTuNguoiLa, ban.TenHienThiThucTe())`)
— sửa thành:
```csharp
                ketQua.Add(new NguoiDungTomTatDto(ban.Id, ban.TenTaiKhoan, ban.Email, ban.ChoPhepTinNhanTuNguoiLa, ban.TenHienThiThucTe(), ban.DuongDanAnhDaiDien));
```

`backend/HaloChat.Api/Services/DichVuKetBan.cs` dòng 133-134 (2 dòng
liền nhau `new NguoiDungTomTatDto(nguoiGui...)`/`new NguoiDungTomTatDto(nguoiNhan...)`)
— sửa thành:
```csharp
        new NguoiDungTomTatDto(nguoiGui.Id, nguoiGui.TenTaiKhoan, nguoiGui.Email, nguoiGui.ChoPhepTinNhanTuNguoiLa, nguoiGui.TenHienThiThucTe(), nguoiGui.DuongDanAnhDaiDien),
        new NguoiDungTomTatDto(nguoiNhan.Id, nguoiNhan.TenTaiKhoan, nguoiNhan.Email, nguoiNhan.ChoPhepTinNhanTuNguoiLa, nguoiNhan.TenHienThiThucTe(), nguoiNhan.DuongDanAnhDaiDien),
```

`backend/HaloChat.Api/Services/DichVuNhom.cs` dòng 119
(`thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe()));`)
— sửa thành:
```csharp
                thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe(), nd.DuongDanAnhDaiDien));
```

`backend/HaloChat.Api/Services/DichVuTinNhan.cs` dòng 326
(`new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa, nguoiKia.TenHienThiThucTe()),`)
— sửa thành:
```csharp
                new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa, nguoiKia.TenHienThiThucTe(), nguoiKia.DuongDanAnhDaiDien),
```

(Đọc đúng ngữ cảnh xung quanh mỗi dòng trong file thật trước khi sửa —
số dòng nêu trên có thể lệch vài dòng do các thay đổi trước đó trong
session, nhưng nội dung dòng cần tìm là duy nhất, dùng để định vị chính
xác.)

- [ ] **Step 5: Sửa chỗ dựng `HoSoCaNhanDto` trong `DichVuNguoiDung.cs`**

Trong `LayThongTinCaNhanAsync`, sửa:
```csharp
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong,
                nguoiDung.ChoPhepThemVaoNhom, nguoiDung.ThongBaoTinNhanMoi,
                nguoiDung.ThongBaoLoiMoiKetBan, nguoiDung.ThongBaoNhom, nguoiDung.TenHienThiThucTe());
```
thành:
```csharp
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong,
                nguoiDung.ChoPhepThemVaoNhom, nguoiDung.ThongBaoTinNhanMoi,
                nguoiDung.ThongBaoLoiMoiKetBan, nguoiDung.ThongBaoNhom, nguoiDung.TenHienThiThucTe(),
                nguoiDung.DuongDanAnhDaiDien, nguoiDung.DaXemHoanTatHoSo);
```

- [ ] **Step 6: Build để thấy PASS**

Run: `cd backend && dotnet build`
Expected: 0 lỗi.

- [ ] **Step 7: Viết test service (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`
(trước dấu `}` cuối class):

```csharp
    [Fact]
    public async Task DoiAnhDaiDienAsync_ThanhCong_CapNhatDungField()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "AnhDaiDien1", Email = "anhdaidien1@gmail.com" };
        kho.DanhSach.Add(nguoiDung);

        var hoSo = await dichVu.DoiAnhDaiDienAsync(nguoiDung.Id, "/api/tinnhan/file/abc123");

        Assert.Equal("/api/tinnhan/file/abc123", hoSo!.DuongDanAnhDaiDien);
    }

    [Fact]
    public async Task DoiAnhDaiDienAsync_IdKhongTonTai_TraVeNull()
    {
        var (dichVu, _, _) = TaoDichVu();

        var hoSo = await dichVu.DoiAnhDaiDienAsync("507f1f77bcf86cd799439099", "/api/tinnhan/file/abc123");

        Assert.Null(hoSo);
    }

    [Fact]
    public async Task DanhDauHoanTatHoSoAsync_ThanhCong_SetDungCoGiuNguyenFieldKhac()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "HoanTat1", Email = "hoantat1@gmail.com", TenHienThi = "Tên Riêng" };
        kho.DanhSach.Add(nguoiDung);

        var hoSo = await dichVu.DanhDauHoanTatHoSoAsync(nguoiDung.Id);

        Assert.True(hoSo!.DaXemHoanTatHoSo);
        Assert.Equal("Tên Riêng", hoSo.TenHienThi);
    }

    [Fact]
    public async Task DanhDauHoanTatHoSoAsync_IdKhongTonTai_TraVeNull()
    {
        var (dichVu, _, _) = TaoDichVu();

        var hoSo = await dichVu.DanhDauHoanTatHoSoAsync("507f1f77bcf86cd799439099");

        Assert.Null(hoSo);
    }
```

- [ ] **Step 8: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "DoiAnhDaiDienAsync|DanhDauHoanTatHoSoAsync"`
Expected: FAIL biên dịch (2 method chưa tồn tại trên `DichVuNguoiDung`).

- [ ] **Step 9: Thêm method vào repository/fake/service**

`backend/HaloChat.Api/Repositories/INguoiDungRepository.cs` — thêm vào
cuối interface (sau `CapNhatTenHienThiAsync`):
```csharp
    Task CapNhatAnhDaiDienAsync(string id, string duongDanAnhDaiDien);
    Task DanhDauHoanTatHoSoAsync(string id);
```

`backend/HaloChat.Api/Repositories/NguoiDungRepository.cs` — thêm vào
cuối class (sau `CapNhatTenHienThiAsync`, trước dấu `}` cuối):
```csharp
    public async Task CapNhatAnhDaiDienAsync(string id, string duongDanAnhDaiDien)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.DuongDanAnhDaiDien, duongDanAnhDaiDien);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task DanhDauHoanTatHoSoAsync(string id)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.DaXemHoanTatHoSo, true);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
```

`backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs` — thêm vào cuối
class (sau `CapNhatTenHienThiAsync`, trước dấu `}` cuối):
```csharp
    public Task CapNhatAnhDaiDienAsync(string id, string duongDanAnhDaiDien)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.DuongDanAnhDaiDien = duongDanAnhDaiDien;
        }
        return Task.CompletedTask;
    }

    public Task DanhDauHoanTatHoSoAsync(string id)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.DaXemHoanTatHoSo = true;
        }
        return Task.CompletedTask;
    }
```

`backend/HaloChat.Api/Services/IDichVuNguoiDung.cs` — thêm vào cuối
interface (sau `DoiTenHienThiAsync`):
```csharp
    Task<HoSoCaNhanDto?> DoiAnhDaiDienAsync(string idHienTai, string duongDanAnhDaiDien);
    Task<HoSoCaNhanDto?> DanhDauHoanTatHoSoAsync(string idHienTai);
```

`backend/HaloChat.Api/Services/DichVuNguoiDung.cs` — thêm vào cuối
class (sau `DoiTenHienThiAsync`, trước `YeuCauOtpDatLaiMatKhauAsync`):
```csharp
    public async Task<HoSoCaNhanDto?> DoiAnhDaiDienAsync(string idHienTai, string duongDanAnhDaiDien)
    {
        await _kho.CapNhatAnhDaiDienAsync(idHienTai, duongDanAnhDaiDien);
        return await LayThongTinCaNhanAsync(idHienTai);
    }

    public async Task<HoSoCaNhanDto?> DanhDauHoanTatHoSoAsync(string idHienTai)
    {
        await _kho.DanhDauHoanTatHoSoAsync(idHienTai);
        return await LayThongTinCaNhanAsync(idHienTai);
    }
```

- [ ] **Step 10: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "DoiAnhDaiDienAsync|DanhDauHoanTatHoSoAsync"`
Expected: PASS (4/4).

- [ ] **Step 11: Viết test controller (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs`
(trước dấu `}` cuối class):

```csharp
    [Fact]
    public async Task DoiAnhDaiDien_DangNhap_CapNhatVaTraVeHoSoMoi()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky",
            new { tenTaiKhoan = "doianh1", email = "doianh1@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap",
            new { tenDangNhap = "doianh1", matKhau = "MatKhau123!" });
        var ketQuaDangNhap = await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ketQuaDangNhap!.Token);

        var phanHoi = await _client.PutAsJsonAsync("/api/nguoidung/anh-dai-dien",
            new { duongDanAnhDaiDien = "/api/tinnhan/file/abc123" });

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var hoSo = await phanHoi.Content.ReadFromJsonAsync<HoSoCaNhanDto>();
        Assert.Equal("/api/tinnhan/file/abc123", hoSo!.DuongDanAnhDaiDien);
    }

    [Fact]
    public async Task DoiAnhDaiDien_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.PutAsJsonAsync("/api/nguoidung/anh-dai-dien",
            new { duongDanAnhDaiDien = "/api/tinnhan/file/abc123" });

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task DoiAnhDaiDien_ChuoiRong_TraVe400()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky",
            new { tenTaiKhoan = "doianh2", email = "doianh2@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap",
            new { tenDangNhap = "doianh2", matKhau = "MatKhau123!" });
        var ketQuaDangNhap = await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ketQuaDangNhap!.Token);

        var phanHoi = await _client.PutAsJsonAsync("/api/nguoidung/anh-dai-dien",
            new { duongDanAnhDaiDien = "" });

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task DanhDauHoanTatHoSo_DangNhap_SetDungCoVaTraVeHoSoMoi()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky",
            new { tenTaiKhoan = "hoantat2", email = "hoantat2@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap",
            new { tenDangNhap = "hoantat2", matKhau = "MatKhau123!" });
        var ketQuaDangNhap = await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ketQuaDangNhap!.Token);

        var phanHoi = await _client.PostAsync("/api/nguoidung/danh-dau-hoan-tat-ho-so", null);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var hoSo = await phanHoi.Content.ReadFromJsonAsync<HoSoCaNhanDto>();
        Assert.True(hoSo!.DaXemHoanTatHoSo);
    }

    [Fact]
    public async Task DanhDauHoanTatHoSo_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.PostAsync("/api/nguoidung/danh-dau-hoan-tat-ho-so", null);

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }
```

- [ ] **Step 12: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "DoiAnhDaiDien|DanhDauHoanTatHoSo"`
Expected: FAIL với 404 "route not found" (2 endpoint chưa tồn tại).

- [ ] **Step 13: Thêm 2 endpoint vào `NguoiDungController.cs`**

Thêm vào cuối class (sau `DoiTenHienThi`, trước `LayTrangThaiHoatDong`):

```csharp
    [HttpPut("anh-dai-dien")]
    [Authorize]
    public async Task<IActionResult> DoiAnhDaiDien([FromBody] DoiAnhDaiDienRequest yeuCau)
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(yeuCau.DuongDanAnhDaiDien))
        {
            return BadRequest(new { thongBao = "Đường dẫn ảnh không hợp lệ." });
        }

        var hoSo = await _dichVu.DoiAnhDaiDienAsync(idHienTai, yeuCau.DuongDanAnhDaiDien);
        return hoSo is null ? NotFound() : Ok(hoSo);
    }

    [HttpPost("danh-dau-hoan-tat-ho-so")]
    [Authorize]
    public async Task<IActionResult> DanhDauHoanTatHoSo()
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        var hoSo = await _dichVu.DanhDauHoanTatHoSoAsync(idHienTai);
        return hoSo is null ? NotFound() : Ok(hoSo);
    }
```

- [ ] **Step 14: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "DoiAnhDaiDien|DanhDauHoanTatHoSo"`
Expected: PASS (6/6).

- [ ] **Step 15: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: 0 lỗi build; toàn bộ test PASS (bao gồm mọi test cũ dùng
`NguoiDungTomTatDto`/`HoSoCaNhanDto` qua constructor — không có test
nào tự dựng 2 record này trực tiếp ngoài các service/controller đã sửa
ở trên, nhưng vẫn chạy toàn bộ suite để chắc chắn không bỏ sót).

- [ ] **Step 16: Commit**

```bash
git add backend/HaloChat.Api/Models/NguoiDung.cs backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs backend/HaloChat.Api/Dto/DoiAnhDaiDienRequest.cs backend/HaloChat.Api/Repositories/INguoiDungRepository.cs backend/HaloChat.Api/Repositories/NguoiDungRepository.cs backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs backend/HaloChat.Api/Services/IDichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuKetBan.cs backend/HaloChat.Api/Services/DichVuNhom.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Controllers/NguoiDungController.cs backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs
git commit -m "feat(backend): anh dai dien ca nhan + co da-xem-hoan-tat-ho-so (GD8)"
```

---

## Task 2: Frontend — kiểu dữ liệu, API, component `ModalHoanTatHoSo`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Create: `frontend/src/ThanhPhan/ModalHoanTatHoSo.tsx`
- Create: `frontend/src/ThanhPhan/ModalHoanTatHoSo.css`
- Test: `frontend/src/ThanhPhan/ModalHoanTatHoSo.test.tsx`

**Interfaces:**
- Consumes: JSON `HoSoCaNhanDto` (có `duongDanAnhDaiDien`/`daXemHoanTatHoSo`)
  từ Task 1, hàm `TaiLenTep`/`DoiTenHienThi` đã có sẵn trong `DichVuApi.ts`.
- Produces: `DoiAnhDaiDien(token, duongDanAnhDaiDien): Promise<HoSoCaNhan>`,
  `DanhDauHoanTatHoSo(token): Promise<HoSoCaNhan>`; component
  `ModalHoanTatHoSo` với props
  `{ tenHienThiBanDau: string; onDong: (hoSoMoi: HoSoCaNhan | null) => void }`
  — Task 3 (TrangChat.tsx) dùng component này.

- [ ] **Step 1: Thêm field vào `KieuDuLieu.ts`**

Sửa `interface NguoiDungTomTat` (thêm dòng cuối, optional theo ruling ở
Global Constraints):
```typescript
export interface NguoiDungTomTat {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  tenHienThi: string;
  duongDanAnhDaiDien?: string | null;
}
```

Sửa `interface HoSoCaNhan` (thêm 2 dòng cuối, optional theo cùng
ruling):
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
  tenHienThi: string;
  duongDanAnhDaiDien?: string | null;
  daXemHoanTatHoSo?: boolean;
}
```

- [ ] **Step 2: Viết test cho 2 hàm API mới (FAIL trước)**

Thêm vào `frontend/src/DichVuApi.test.ts`:

```typescript
it('DoiAnhDaiDien goi dung endpoint kem duong dan anh', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({}), { status: 200 })));

  await DoiAnhDaiDien('token-gia-lap', '/api/tinnhan/file/abc123');

  expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/nguoidung/anh-dai-dien'),
    expect.objectContaining({ method: 'PUT', body: JSON.stringify({ duongDanAnhDaiDien: '/api/tinnhan/file/abc123' }) }),
  );
});

it('DanhDauHoanTatHoSo goi dung endpoint bang POST', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({}), { status: 200 })));

  await DanhDauHoanTatHoSo('token-gia-lap');

  expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/nguoidung/danh-dau-hoan-tat-ho-so'),
    expect.objectContaining({ method: 'POST' }),
  );
});
```

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: FAIL (2 hàm chưa tồn tại).

- [ ] **Step 4: Thêm 2 hàm vào `DichVuApi.ts`**

Thêm vào cuối file (sau `DoiTenHienThi`):
```typescript
export async function DoiAnhDaiDien(token: string, duongDanAnhDaiDien: string): Promise<HoSoCaNhan> {
  return goiApi<HoSoCaNhan>('/nguoidung/anh-dai-dien', {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
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

- [ ] **Step 5: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: PASS.

- [ ] **Step 6: Viết test cho `ModalHoanTatHoSo` (FAIL trước)**

Tạo `frontend/src/ThanhPhan/ModalHoanTatHoSo.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ModalHoanTatHoSo } from './ModalHoanTatHoSo';
import * as DichVuApi from '../DichVuApi';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';

function renderModal(onDong = vi.fn()) {
  localStorage.setItem('haloChatToken', 'token-gia-lap');
  return {
    onDong,
    ...render(
      <NhaCungCapXacThuc>
        <ModalHoanTatHoSo tenHienThiBanDau="NguyenAn" onDong={onDong} />
      </NhaCungCapXacThuc>,
    ),
  };
}

describe('ModalHoanTatHoSo', () => {
  it('hien dung tieu de, phu de va gia tri ten hien thi ban dau', () => {
    renderModal();

    expect(screen.getByText('Hoàn tất hồ sơ')).toBeInTheDocument();
    expect(screen.getByText(/Hãy thiết lập hồ sơ của bạn/)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập tên của bạn...')).toHaveValue('NguyenAn');
    expect(screen.getByText('9/50')).toBeInTheDocument();
  });

  it('go ten hien thi cap nhat bo dem dung', async () => {
    renderModal();
    const oNhap = screen.getByPlaceholderText('Nhập tên của bạn...');

    await userEvent.clear(oNhap);
    await userEvent.type(oNhap, 'Tên Mới');

    expect(screen.getByText('7/50')).toBeInTheDocument();
  });

  it('chon anh hien preview ngay, chua goi API', async () => {
    const taiLenTep = vi.spyOn(DichVuApi, 'TaiLenTep');
    renderModal();
    const tep = new File(['noi-dung'], 'avatar.png', { type: 'image/png' });

    await userEvent.upload(screen.getByLabelText('Chọn ảnh'), tep);

    expect(taiLenTep).not.toHaveBeenCalled();
  });

  it('bam Hoan tat khi co doi anh va ten thi goi du chuoi API roi dong', async () => {
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/abc123', tenFileGoc: 'avatar.png', kichThuocFile: 100, loaiFile: 'image/png' });
    vi.spyOn(DichVuApi, 'DoiAnhDaiDien').mockResolvedValue({ id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', duongDanAnhDaiDien: '/api/tinnhan/file/abc123' });
    vi.spyOn(DichVuApi, 'DoiTenHienThi').mockResolvedValue({ id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới' });
    const hoSoCuoiCung = { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới', duongDanAnhDaiDien: '/api/tinnhan/file/abc123', daXemHoanTatHoSo: true };
    vi.spyOn(DichVuApi, 'DanhDauHoanTatHoSo').mockResolvedValue(hoSoCuoiCung);
    const onDong = vi.fn();
    renderModal(onDong);
    const tep = new File(['noi-dung'], 'avatar.png', { type: 'image/png' });
    await userEvent.upload(screen.getByLabelText('Chọn ảnh'), tep);
    const oNhap = screen.getByPlaceholderText('Nhập tên của bạn...');
    await userEvent.clear(oNhap);
    await userEvent.type(oNhap, 'Tên Mới');

    await userEvent.click(screen.getByRole('button', { name: 'Hoàn tất' }));

    expect(DichVuApi.TaiLenTep).toHaveBeenCalledWith('token-gia-lap', tep);
    expect(DichVuApi.DoiAnhDaiDien).toHaveBeenCalledWith('token-gia-lap', '/api/tinnhan/file/abc123');
    expect(DichVuApi.DoiTenHienThi).toHaveBeenCalledWith('token-gia-lap', 'Tên Mới');
    expect(DichVuApi.DanhDauHoanTatHoSo).toHaveBeenCalledWith('token-gia-lap');
    expect(onDong).toHaveBeenCalledWith(hoSoCuoiCung);
  });

  it('bam X khong goi DoiAnhDaiDien/DoiTenHienThi nhung van goi DanhDauHoanTatHoSo', async () => {
    const daXem = { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', daXemHoanTatHoSo: true };
    vi.spyOn(DichVuApi, 'DanhDauHoanTatHoSo').mockResolvedValue(daXem);
    const taiLenTep = vi.spyOn(DichVuApi, 'TaiLenTep');
    const doiTenHienThi = vi.spyOn(DichVuApi, 'DoiTenHienThi');
    const onDong = vi.fn();
    renderModal(onDong);
    const oNhap = screen.getByPlaceholderText('Nhập tên của bạn...');
    await userEvent.clear(oNhap);
    await userEvent.type(oNhap, 'Tên gõ dở');

    await userEvent.click(screen.getByRole('button', { name: 'Đóng' }));

    expect(taiLenTep).not.toHaveBeenCalled();
    expect(doiTenHienThi).not.toHaveBeenCalled();
    expect(DichVuApi.DanhDauHoanTatHoSo).toHaveBeenCalledWith('token-gia-lap');
    expect(onDong).toHaveBeenCalledWith(daXem);
  });
});
```

- [ ] **Step 7: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run ModalHoanTatHoSo`
Expected: FAIL (component chưa tồn tại).

- [ ] **Step 8: Viết `ModalHoanTatHoSo.tsx`**

```tsx
import { useEffect, useRef, useState } from 'react';
import { DoiAnhDaiDien, DoiTenHienThi, DanhDauHoanTatHoSo, TaiLenTep } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { Avatar } from './Avatar';
import { BieuTuongMayAnh, BieuTuongDong } from './BieuTuong';
import type { HoSoCaNhan } from '../KieuDuLieu';
import './ModalHoanTatHoSo.css';

const GIOI_HAN_TEN = 50;

interface PropsModalHoanTatHoSo {
  tenHienThiBanDau: string;
  onDong: (hoSoMoi: HoSoCaNhan | null) => void;
}

export function ModalHoanTatHoSo({ tenHienThiBanDau, onDong }: PropsModalHoanTatHoSo) {
  const { token } = useXacThuc();
  const [tepAnhDaChon, setTepAnhDaChon] = useState<File | null>(null);
  const [duongDanPreview, setDuongDanPreview] = useState<string | null>(null);
  const [tenHienThi, setTenHienThi] = useState(tenHienThiBanDau);
  const [dangLuu, setDangLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);
  const inputTepRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    return () => {
      if (duongDanPreview) URL.revokeObjectURL(duongDanPreview);
    };
  }, [duongDanPreview]);

  function chonAnh(tep: File) {
    if (duongDanPreview) URL.revokeObjectURL(duongDanPreview);
    setTepAnhDaChon(tep);
    setDuongDanPreview(URL.createObjectURL(tep));
  }

  async function xuLyHoanTat() {
    if (!token) return;
    setLoi(null);
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

  async function xuLyDong() {
    if (!token) {
      onDong(null);
      return;
    }
    const hoSoMoi = await DanhDauHoanTatHoSo(token).catch(() => null);
    onDong(hoSoMoi);
  }

  return (
    <div className="modal-hoan-tat-ho-so-nen">
      <div className="modal-hoan-tat-ho-so">
        <button className="modal-hoan-tat-ho-so__dong" onClick={xuLyDong} aria-label="Đóng">
          <BieuTuongDong />
        </button>
        <h3>Hoàn tất hồ sơ</h3>
        <p className="modal-hoan-tat-ho-so__phu-de">
          Hãy thiết lập hồ sơ của bạn để mọi người có thể dễ dàng nhận ra bạn trong HaloChat.
        </p>

        {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

        <div className="modal-hoan-tat-ho-so__anh-cum">
          <div className="modal-hoan-tat-ho-so__anh-vong">
            <Avatar id={tenHienThiBanDau} ten={tenHienThi || '?'} kichThuoc="lon" duongDanAnh={duongDanPreview} />
            <button
              type="button"
              className="modal-hoan-tat-ho-so__nut-camera"
              aria-label="Đổi ảnh đại diện"
              onClick={() => inputTepRef.current?.click()}
            >
              <BieuTuongMayAnh />
            </button>
          </div>
          <label className="nut-phu modal-hoan-tat-ho-so__nut-chon-anh">
            Chọn ảnh
            <input
              ref={inputTepRef}
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              hidden
              onChange={(su) => {
                const tep = su.target.files?.[0];
                if (tep) chonAnh(tep);
                su.target.value = '';
              }}
            />
          </label>
        </div>

        <label className="modal-hoan-tat-ho-so__nhan">
          Tên hiển thị
          <input
            type="text"
            value={tenHienThi}
            onChange={(su) => setTenHienThi(su.target.value)}
            placeholder="Nhập tên của bạn..."
            maxLength={GIOI_HAN_TEN}
            disabled={dangLuu}
          />
          <span className="modal-hoan-tat-ho-so__dem-ky-tu">{tenHienThi.length}/{GIOI_HAN_TEN}</span>
        </label>

        <button className="nut-chinh modal-hoan-tat-ho-so__nut-hoan-tat" onClick={xuLyHoanTat} disabled={dangLuu}>
          {dangLuu ? 'Đang lưu...' : 'Hoàn tất'}
        </button>
      </div>
    </div>
  );
}
```

Lưu ý: `<label className="modal-hoan-tat-ho-so__nut-chon-anh">Chọn
ảnh<input type="file" hidden /></label>` — `<label>` bọc `<input>`
khiến Testing Library's `getByLabelText('Chọn ảnh')` tìm đúng input ẩn
này (đúng pattern accessibility đã dùng ở `PanelQuanLyNhom.tsx`).

- [ ] **Step 9: Viết `ModalHoanTatHoSo.css`**

Tạo `frontend/src/ThanhPhan/ModalHoanTatHoSo.css`:
```css
.modal-hoan-tat-ho-so-nen {
  position: fixed;
  inset: 0;
  background: rgba(16, 27, 51, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 30;
}

.modal-hoan-tat-ho-so {
  position: relative;
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-the);
  width: 420px;
  max-width: calc(100vw - 32px);
  padding: 28px 24px 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 12px;
}

.modal-hoan-tat-ho-so__dong {
  position: absolute;
  top: 16px;
  right: 16px;
  border: none;
  background: none;
  cursor: pointer;
  color: var(--mau-chu-phu);
}

.modal-hoan-tat-ho-so h3 {
  margin: 0;
}

.modal-hoan-tat-ho-so__phu-de {
  margin: 0;
  font-size: 13px;
  color: var(--mau-chu-phu);
}

.modal-hoan-tat-ho-so__anh-cum {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
}

.modal-hoan-tat-ho-so__anh-vong {
  position: relative;
}

.modal-hoan-tat-ho-so__nut-camera {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: 2px solid var(--mau-nen-the);
  background: var(--mau-chinh-dam);
  color: #fff;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal-hoan-tat-ho-so__nut-chon-anh {
  display: inline-block;
  cursor: pointer;
  font-size: 13px;
}

.modal-hoan-tat-ho-so__nhan {
  display: block;
  width: 100%;
  text-align: left;
  font-size: 13px;
  color: var(--mau-chu-phu);
  position: relative;
  margin-top: 4px;
}

.modal-hoan-tat-ho-so__nhan input {
  display: block;
  width: 100%;
  margin-top: 6px;
  padding: 10px 12px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 14px;
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  box-sizing: border-box;
}

.modal-hoan-tat-ho-so__dem-ky-tu {
  position: absolute;
  right: 0;
  bottom: -18px;
  font-size: 11px;
}

.modal-hoan-tat-ho-so__nut-hoan-tat {
  width: 100%;
  margin-top: 20px;
}
```

- [ ] **Step 10: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run ModalHoanTatHoSo`
Expected: PASS toàn bộ.

- [ ] **Step 11: `tsc -b --noEmit`**

Run: `cd frontend && npx tsc -b --noEmit`
Expected: 0 lỗi trong phạm vi file của task này (`KieuDuLieu.ts`,
`DichVuApi.ts`/`.test.ts`, `ModalHoanTatHoSo.tsx`/`.test.tsx`). Có thể
CHƯA có lỗi nào phát sinh ở nơi khác vì 2 field mới đều optional theo
ruling ở Global Constraints — xác nhận đúng như vậy, không có lỗi lan
sang file khác.

- [ ] **Step 12: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts frontend/src/ThanhPhan/ModalHoanTatHoSo.tsx frontend/src/ThanhPhan/ModalHoanTatHoSo.css frontend/src/ThanhPhan/ModalHoanTatHoSo.test.tsx
git commit -m "feat(frontend): component ModalHoanTatHoSo + API doi anh/danh dau hoan tat (GD8)"
```

---

## Task 3: Frontend — hiện modal ở `TrangChat.tsx` sau lần đăng nhập đầu

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`

**Interfaces:**
- Consumes: `ModalHoanTatHoSo` (Task 2), `LayThongTinCaNhan` (đã có sẵn
  trong `DichVuApi.ts` — dùng để lấy `daXemHoanTatHoSo` ban đầu).
- Produces: không có task nào phụ thuộc.

- [ ] **Step 1: Thêm mock mặc định `LayThongTinCaNhan` vào `beforeEach` của `TrangChat.test.tsx`**

Sửa `beforeEach` hiện có (thêm 1 dòng sau
`vi.spyOn(DichVuApi, 'LayTrangThaiHoatDong').mockResolvedValue({});`):
```typescript
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', daXemHoanTatHoSo: true,
    });
```
(Đặt `daXemHoanTatHoSo: true` làm mặc định để KHÔNG hiện modal onboarding
trong mọi test hiện có của file — các test riêng ở Step 3 dưới đây sẽ
tự override lại giá trị này khi cần kiểm tra đúng hành vi hiện modal.)

- [ ] **Step 2: Thêm state + hiện modal trong `TrangChat.tsx`**

Thêm import ở đầu file: `import { ModalHoanTatHoSo } from '../ThanhPhan/ModalHoanTatHoSo';`
và thêm `LayThongTinCaNhan` vào dòng import từ `'../DichVuApi'` (nếu
chưa có).

Thêm state (cạnh các state khác đã có ở đầu component):
```typescript
  const [hoSo, setHoSo] = useState<HoSoCaNhan | null>(null);
  const [hienModalHoanTatHoSo, setHienModalHoanTatHoSo] = useState(false);
```
(Thêm `HoSoCaNhan` vào dòng import type từ `'../KieuDuLieu'` nếu chưa
có.)

Thêm effect mới (đặt cạnh các effect khác đã có ở đầu component, chạy
khi có `token`):
```typescript
  useEffect(() => {
    if (!token) return;
    LayThongTinCaNhan(token).then((ketQua) => {
      setHoSo(ketQua);
      setHienModalHoanTatHoSo(!ketQua.daXemHoanTatHoSo);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);
```

Thêm JSX vào cuối phần tử gốc trả về của component (ngay trước dấu
đóng `</div>`/Fragment cuối cùng của return — đọc kỹ file thật để xác
định đúng vị trí, không lồng bên trong bất kỳ điều kiện `{nguoiDangChon && (...)}`
nào vì modal phải hiện độc lập, không phụ thuộc đã chọn hội thoại nào
chưa):
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

- [ ] **Step 3: Viết test cho `TrangChat.test.tsx` (FAIL trước)**

Thêm vào cuối `describe('TrangChat', ...)`:

```typescript
it('chua xem hoan tat ho so thi hien modal ngay khi vao trang', async () => {
  vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
    id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', daXemHoanTatHoSo: false,
  });

  renderTrangChat();

  expect(await screen.findByText('Hoàn tất hồ sơ')).toBeInTheDocument();
});

it('da xem hoan tat ho so thi khong hien modal', async () => {
  renderTrangChat();

  await screen.findByText('TranBinh');
  expect(screen.queryByText('Hoàn tất hồ sơ')).not.toBeInTheDocument();
});

it('dong modal hoan tat ho so thi an modal va cap nhat ten hien thi tren giao dien', async () => {
  vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
    id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', daXemHoanTatHoSo: false,
  });
  vi.spyOn(DichVuApi, 'DanhDauHoanTatHoSo').mockResolvedValue({
    id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', daXemHoanTatHoSo: true,
  });

  renderTrangChat();
  await screen.findByText('Hoàn tất hồ sơ');

  await userEvent.click(screen.getByRole('button', { name: 'Đóng' }));

  expect(screen.queryByText('Hoàn tất hồ sơ')).not.toBeInTheDocument();
});
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run TrangChat`
Expected: PASS toàn bộ (bao gồm mọi test cũ trong file — nhờ mock mặc
định `daXemHoanTatHoSo: true` thêm ở Step 1).

- [ ] **Step 5: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangChat.test.tsx
git commit -m "feat(frontend): hien ModalHoanTatHoSo o TrangChat khi chua xem (GD8)"
```

---

## Task 4: Frontend — đổi ảnh đại diện cá nhân ở Cài đặt

**Files:**
- Modify: `frontend/src/Trang/TrangCaiDat.tsx`
- Modify: `frontend/src/Trang/TrangCaiDat.css`
- Test: `frontend/src/Trang/TrangCaiDat.test.tsx`

**Interfaces:**
- Consumes: `DoiAnhDaiDien` (Task 2).
- Produces: không có task nào phụ thuộc.

- [ ] **Step 1: Viết test (FAIL trước)**

Thêm vào cuối `describe('TrangCaiDat', ...)` trong
`frontend/src/Trang/TrangCaiDat.test.tsx` (dùng đúng cấu trúc mock
`LayThongTinCaNhan` đã lặp lại trong file — thêm `duongDanAnhDaiDien: null`
vào mock đầu tiên trong test mới để có giá trị khởi tạo rõ ràng, KHÔNG
cần sửa 10 mock hiện có vì field optional):

```typescript
it('doi anh dai dien ca nhan luu ngay khi chon file', async () => {
  vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
    id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', duongDanAnhDaiDien: null,
  });
  vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/xyz789', tenFileGoc: 'a.png', kichThuocFile: 100, loaiFile: 'image/png' });
  vi.spyOn(DichVuApi, 'DoiAnhDaiDien').mockResolvedValue({
    id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', duongDanAnhDaiDien: '/api/tinnhan/file/xyz789',
  });
  render(<TrangCaiDat />);
  await screen.findByText('Tên hiển thị');
  const tep = new File(['noi-dung'], 'a.png', { type: 'image/png' });

  await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện'), tep);

  await waitFor(() => expect(DichVuApi.DoiAnhDaiDien).toHaveBeenCalledWith('token-gia-lap', '/api/tinnhan/file/xyz789'));
});

it('chon file khong phai anh thi bao loi, khong goi TaiLenTep', async () => {
  render(<TrangCaiDat />);
  await screen.findByText('Tên hiển thị');
  const taiLenTep = vi.spyOn(DichVuApi, 'TaiLenTep');
  const tep = new File(['noi-dung'], 'a.pdf', { type: 'application/pdf' });

  await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện'), tep);

  expect(await screen.findByText('Chỉ chấp nhận file ảnh.')).toBeInTheDocument();
  expect(taiLenTep).not.toHaveBeenCalled();
});
```

(Kiểm tra đầu file `TrangCaiDat.test.tsx` đã có `import userEvent from
'@testing-library/user-event';`/`waitFor` — nếu thiếu, thêm vào dòng
import từ `@testing-library/react`.)

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run TrangCaiDat`
Expected: FAIL (chưa có UI đổi ảnh đại diện).

- [ ] **Step 3: Thêm state + hàm `doiAnhDaiDien` vào `TrangCaiDat.tsx`**

Thêm `DoiAnhDaiDien`, `TaiLenTep` vào dòng import từ `'../DichVuApi'`.

Thêm hằng số ở đầu file (sau import, trước `type MucCaiDat`):
```typescript
const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
```

Thêm state (cạnh `tenHienThi`/`tenHienThiGoc`):
```typescript
  const [duongDanAnhDaiDien, setDuongDanAnhDaiDien] = useState<string | null>(null);
  const [dangTaiAnh, setDangTaiAnh] = useState(false);
  const [loiAnh, setLoiAnh] = useState<string | null>(null);
```

Trong `useEffect` gọi `LayThongTinCaNhan` hiện có, thêm dòng (cạnh
`setTenHienThi(hoSo.tenHienThi)`):
```typescript
        setDuongDanAnhDaiDien(hoSo.duongDanAnhDaiDien ?? null);
```

Thêm hàm (cạnh `xuLyDoiTenHienThi`):
```typescript
  function doiAnhDaiDien(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoiAnh('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoiAnh(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setLoiAnh(null);
    setDangTaiAnh(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => DoiAnhDaiDien(token, daTaiLen.duongDanFile))
      .then((hoSoMoi) => setDuongDanAnhDaiDien(hoSoMoi.duongDanAnhDaiDien ?? null))
      .catch(() => setLoiAnh('Đổi ảnh đại diện thất bại.'))
      .finally(() => setDangTaiAnh(false));
  }
```

- [ ] **Step 4: Thêm JSX vào tab "Tài khoản"**

Thêm import `Avatar` từ `'../ThanhPhan/Avatar'` ở đầu file.

Trong nhánh `{mucDangChon === 'tai-khoan' && (...)}`, thêm khối JSX
này ngay TRÊN `<div className="trang-cai-dat__form-ten-hien-thi">` đã
có (giữa 2 dòng `<p><strong>Email:</strong>...` và
`<div className="trang-cai-dat__form-ten-hien-thi">`):

```tsx
            <div className="trang-cai-dat__doi-anh">
              <Avatar id={nguoiDungHienTai?.id ?? ''} ten={tenHienThi || '?'} kichThuoc="lon" duongDanAnh={duongDanAnhDaiDien} />
              {loiAnh && <p className="thong-bao-loi" role="alert">{loiAnh}</p>}
              <label className="nut-phu trang-cai-dat__nut-doi-anh">
                {dangTaiAnh ? 'Đang tải...' : 'Đổi ảnh đại diện'}
                <input
                  type="file"
                  accept="image/jpeg,image/png,image/gif,image/webp"
                  hidden
                  disabled={dangTaiAnh}
                  onChange={(su) => {
                    const tep = su.target.files?.[0];
                    if (tep) doiAnhDaiDien(tep);
                    su.target.value = '';
                  }}
                />
              </label>
            </div>
```

- [ ] **Step 5: Thêm CSS**

Thêm vào cuối `frontend/src/Trang/TrangCaiDat.css`:
```css
.trang-cai-dat__doi-anh {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 20px;
}

.trang-cai-dat__nut-doi-anh {
  display: inline-block;
  cursor: pointer;
  font-size: 13px;
}
```

- [ ] **Step 6: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run TrangCaiDat`
Expected: PASS toàn bộ.

- [ ] **Step 7: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/Trang/TrangCaiDat.tsx frontend/src/Trang/TrangCaiDat.css frontend/src/Trang/TrangCaiDat.test.tsx
git commit -m "feat(frontend): doi anh dai dien ca nhan trong Cai dat (GD8)"
```

---

## Task 5: Frontend — lối tắt đổi ảnh đại diện nhóm ở "Thông tin nhóm"

**Files:**
- Modify: `frontend/src/Trang/PanelThongTinNhom.tsx`
- Modify: `frontend/src/Trang/PanelThongTinNhom.css`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Test: `frontend/src/Trang/PanelThongTinNhom.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `CapNhatNhom`/`TaiLenTep` (đã có sẵn trong `DichVuApi.ts`,
  đã dùng trong `PanelQuanLyNhom.tsx`).
- Produces: `PropsPanelThongTinNhom.onCapNhatNhom: (nhomMoi: Nhom) => void`
  — không có task nào khác phụ thuộc (task độc lập với Task 2-4).

- [ ] **Step 1: Viết test cho `PanelThongTinNhom.test.tsx` (FAIL trước)**

Đọc đầu file `PanelThongTinNhom.test.tsx` để lấy đúng fixture `Nhom`
mẫu đang dùng trong các test có sẵn (biến `NHOM_MAU` hoặc tương đương)
và pattern render hiện tại, rồi thêm vào cuối `describe(...)`:

```tsx
it('admin thay nut camera doi anh dai dien, non-admin khong thay', () => {
  const { rerender } = render(
    <PanelThongTinNhom nhom={NHOM_MAU} laAdmin={true} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} onCapNhatNhom={() => {}} />,
  );
  expect(screen.getByRole('button', { name: 'Đổi ảnh đại diện nhóm' })).toBeInTheDocument();

  rerender(
    <PanelThongTinNhom nhom={NHOM_MAU} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} onCapNhatNhom={() => {}} />,
  );
  expect(screen.queryByRole('button', { name: 'Đổi ảnh đại diện nhóm' })).not.toBeInTheDocument();
});

it('admin bam nut camera chon anh thi goi TaiLenTep roi CapNhatNhom', async () => {
  vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/nhom123', tenFileGoc: 'a.png', kichThuocFile: 100, loaiFile: 'image/png' });
  vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({ ...NHOM_MAU, duongDanAnhDaiDien: '/api/tinnhan/file/nhom123' });
  localStorage.setItem('haloChatToken', 'token-gia-lap');
  const onCapNhatNhom = vi.fn();
  render(
    <PanelThongTinNhom nhom={NHOM_MAU} laAdmin={true} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} onCapNhatNhom={onCapNhatNhom} />,
  );
  const tep = new File(['noi-dung'], 'a.png', { type: 'image/png' });

  await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện nhóm'), tep);

  await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', NHOM_MAU.id, NHOM_MAU.tenNhom, NHOM_MAU.moTa, '/api/tinnhan/file/nhom123'));
  await waitFor(() => expect(onCapNhatNhom).toHaveBeenCalledWith({ ...NHOM_MAU, duongDanAnhDaiDien: '/api/tinnhan/file/nhom123' }));
});
```

(Đối chiếu tên biến fixture thật trong file — `NHOM_MAU` là tên giả
định, THAY bằng tên biến/hằng số Nhom mẫu thực tế đang dùng trong file
đó; nếu file dùng object literal trực tiếp trong từng test thay vì 1
hằng số dùng chung, viết literal `Nhom` đầy đủ tương tự các test khác
trong file thay vì tham chiếu `NHOM_MAU`. Kiểm tra `token` cần set qua
`localStorage.setItem('haloChatToken', ...)` hay qua context khác —
đọc cách các test hiện có trong file setup `token`, nếu component này
dùng `useXacThuc()` cần bọc `NhaCungCapXacThuc` giống cách
`PanelQuanLyNhom.test.tsx` đã làm.)

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run PanelThongTinNhom`
Expected: FAIL (chưa có nút camera, chưa có prop `onCapNhatNhom`).

- [ ] **Step 3: Sửa `PanelThongTinNhom.tsx`**

Thêm import `useState` (nếu file component dùng function component
không hook — kiểm tra hiện đã import `useState` cho
`hienHetThanhVien`, dùng chung).
Thêm import `TaiLenTep, CapNhatNhom, LoiGoiApi` từ `'../DichVuApi'`,
`useXacThuc` từ `'../NguCanh/NguCanhXacThuc'`, `BieuTuongMayAnh` từ
`'../ThanhPhan/BieuTuong'`.

Thêm hằng số ở đầu file (sau import, trước
`const SO_AVATAR_HIEN_MAC_DINH`):
```typescript
const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
```

Sửa `interface PropsPanelThongTinNhom` — thêm tham số cuối:
```typescript
  onCapNhatNhom: (nhomMoi: Nhom) => void;
```
Thêm `onCapNhatNhom` vào destructure tham số hàm component.

Thêm bên trong component (cạnh `hienHetThanhVien`):
```typescript
  const { token } = useXacThuc();
  const [dangTaiAnh, setDangTaiAnh] = useState(false);
  const [loiAnh, setLoiAnh] = useState<string | null>(null);

  function doiAnhNhom(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoiAnh('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoiAnh(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setLoiAnh(null);
    setDangTaiAnh(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => CapNhatNhom(token, nhom.id, nhom.tenNhom, nhom.moTa, daTaiLen.duongDanFile))
      .then(onCapNhatNhom)
      .catch((loiBat) => setLoiAnh(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi ảnh đại diện thất bại.'))
      .finally(() => setDangTaiAnh(false));
  }
```

Sửa dòng `<Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon"
duongDanAnh={nhom.duongDanAnhDaiDien} />` — bọc trong `div` mới, chỉ
thêm nút camera khi `laAdmin`:
```tsx
      <div className="panel-thong-tin-nhom__anh-cum">
        <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" duongDanAnh={nhom.duongDanAnhDaiDien} />
        {laAdmin && (
          <label className="panel-thong-tin-nhom__nut-doi-anh" aria-label="Đổi ảnh đại diện nhóm">
            {dangTaiAnh ? '...' : <BieuTuongMayAnh />}
            <input
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              hidden
              disabled={dangTaiAnh}
              onChange={(su) => {
                const tep = su.target.files?.[0];
                if (tep) doiAnhNhom(tep);
                su.target.value = '';
              }}
            />
          </label>
        )}
      </div>
      {loiAnh && <p className="thong-bao-loi" role="alert">{loiAnh}</p>}
```
(`<label>` có `aria-label` VÀ bọc `<input>` — Testing Library's
`getByRole('button', {...})` sẽ KHÔNG khớp `<label>`; đổi cách viết
test ở Step 1 nếu cần dùng `getByLabelText('Đổi ảnh đại diện nhóm')`
thay vì `getByRole('button', ...)` cho việc BẤM/upload, chỉ dùng
`getByRole`/`queryByRole` khi cần assert nút camera có tồn tại hay
không thực chất là kiểm tra `label` — thống nhất lại: dùng
`screen.getByLabelText('Đổi ảnh đại diện nhóm')` cho MỌI thao tác liên
quan tới `<label>` này trong Step 1, kể cả kiểm tra tồn tại/không tồn
tại, để nhất quán với cách `PanelQuanLyNhom.tsx`/`TrangCaiDat.tsx` đã
làm — SỬA LẠI Step 1 theo đúng cách này trước khi implement, dùng
`queryByLabelText`/`getByLabelText` xuyên suốt thay vì
`getByRole('button', ...)`.)

- [ ] **Step 4: Thêm CSS**

Thêm vào cuối `frontend/src/Trang/PanelThongTinNhom.css`:
```css
.panel-thong-tin-nhom__anh-cum {
  position: relative;
}

.panel-thong-tin-nhom__nut-doi-anh {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  border: 2px solid var(--mau-nen-the);
  background: var(--mau-chinh-dam);
  color: #fff;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
```

- [ ] **Step 5: Nối dây `onCapNhatNhom` từ `TrangNhom.tsx`**

Tìm chỗ `<PanelThongTinNhom` được render trong `TrangNhom.tsx` (trong
khối `panelDangMo === 'thong-tin'`) và chỗ `<PanelQuanLyNhom` đang
nhận prop `onCapNhatNhom` — dùng ĐÚNG hàm xử lý đang truyền cho
`PanelQuanLyNhom` (tìm tên hàm đó trong file thật) truyền thêm xuống
`PanelThongTinNhom`:
```tsx
onCapNhatNhom={/* tên hàm y hệt đang truyền cho PanelQuanLyNhom */}
```

- [ ] **Step 6: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run PanelThongTinNhom TrangNhom`
Expected: PASS toàn bộ.

- [ ] **Step 7: Viết test cho `TrangNhom.test.tsx`**

Thêm vào cuối `describe('TrangNhom', ...)`:

```typescript
it('doi anh dai dien nhom tu Thong tin nhom cap nhat dung state', async () => {
  vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
    { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
  ]);
  vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);
  vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/nhom456', tenFileGoc: 'a.png', kichThuocFile: 100, loaiFile: 'image/png' });
  vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({
    id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: '/api/tinnhan/file/nhom456', nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z',
  });

  const { container } = renderTrangNhom();
  await userEvent.click(await screen.findByText('Nhóm CNTT'));
  await userEvent.click(container.querySelector('.khung-tin-nhan__tieu-de-bam') as Element);
  const tep = new File(['noi-dung'], 'a.png', { type: 'image/png' });

  await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện nhóm'), tep);

  await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', 'n1', 'Nhóm CNTT', null, '/api/tinnhan/file/nhom456'));
});
```
(`idHienTai` trong `TrangNhom.test.tsx` là `'1'` theo `beforeEach` đã
có — `nguoiTaoId: '1'` trong fixture nhóm ở trên đảm bảo
`laAdmin === true` để nút camera hiện ra; đối chiếu đúng cách file này
mở panel "Thông tin nhóm" — dòng `container.querySelector('.khung-tin-nhan__tieu-de-bam')`
là suy đoán dựa theo pattern `onBamTieuDe` đã thấy ở
`KhungTinNhan.tsx`, xác nhận lại đúng selector/luồng mở panel thật
trong file `TrangNhom.tsx`/test hiện có trước khi dùng.)

- [ ] **Step 8: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run TrangNhom`
Expected: PASS toàn bộ.

- [ ] **Step 9: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 10: Commit**

```bash
git add frontend/src/Trang/PanelThongTinNhom.tsx frontend/src/Trang/PanelThongTinNhom.css frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/PanelThongTinNhom.test.tsx frontend/src/Trang/TrangNhom.test.tsx
git commit -m "feat(frontend): loi tat doi anh dai dien nhom tu Thong tin nhom (GD8)"
```

---

## Task 6: Frontend — hiển thị ảnh đại diện cá nhân ở mọi nơi còn lại

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangBanBe.tsx`
- Modify: `frontend/src/Trang/PanelQuanLyNhom.tsx`
- Modify: `frontend/src/Trang/PanelThongTinNhom.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Test: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`

**Interfaces:**
- Consumes: `NguoiDungTomTat.duongDanAnhDaiDien` (Task 1 backend + Task 2
  kiểu dữ liệu).
- Produces: không có task nào phụ thuộc (task cuối GĐ8).

- [ ] **Step 1: Đọc 4 lệnh gọi `<Avatar>` cho NGƯỜI DÙNG (không phải
  nhóm) và thêm `duongDanAnh`**

Đọc từng file thật, tìm đúng biến `NguoiDungTomTat`/tương đương tại
mỗi lệnh gọi, rồi thêm `duongDanAnh={<biến>.duongDanAnhDaiDien}`:

`frontend/src/Trang/TrangChat.tsx` — dòng có
`<Avatar id={nd.id} ten={nd.tenHienThi} kichThuoc="nho" />` (trong
danh sách hội thoại sidebar) sửa thành:
```tsx
<Avatar id={nd.id} ten={nd.tenHienThi} kichThuoc="nho" duongDanAnh={nd.duongDanAnhDaiDien} />
```

`frontend/src/Trang/TrangBanBe.tsx` — đọc file để tìm lệnh gọi
`<Avatar>` render từng người trong danh sách bạn bè/lời mời, thêm
`duongDanAnh={<biến>.duongDanAnhDaiDien}` cho MỖI lệnh gọi (có thể có
nhiều hơn 1 nếu file có nhiều tab bạn bè/lời mời đến/lời mời gửi).

`frontend/src/Trang/PanelQuanLyNhom.tsx` — dòng có
`<Avatar id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" />` (danh sách
thành viên, KHÔNG phải dòng avatar nhóm ở đầu file đã có sẵn
`duongDanAnh`) sửa thành:
```tsx
<Avatar id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" duongDanAnh={tv.duongDanAnhDaiDien} />
```

`frontend/src/Trang/PanelThongTinNhom.tsx` — dòng có
`<Avatar key={tv.id} id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" />`
(danh sách thành viên) sửa thành:
```tsx
<Avatar key={tv.id} id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" duongDanAnh={tv.duongDanAnhDaiDien} />
```

- [ ] **Step 2: Viết test cho đầu đề khung chat trong `KhungTinNhan.test.tsx` (FAIL trước)**

Thêm `duongDanAnh: null` vào `PROPS_MAC_DINH` (nếu file dùng
`PROPS_MAC_DINH` cho props mặc định — đọc file để xác nhận đúng tên
hằng số này còn hiệu lực ở phiên bản hiện tại). Thêm vào cuối
`describe('KhungTinNhan', ...)`:

```tsx
it('co duongDanAnh thi hien anh that trong avatar dau de, khong hien chu cai dau', () => {
  render(<KhungTinNhan {...PROPS_MAC_DINH} duongDanAnh="/api/tinnhan/file/abc" />);

  const anh = screen.getByAltText('TranBinh');
  expect(anh).toHaveAttribute('src', expect.stringContaining('/api/tinnhan/file/abc'));
});

it('khong co duongDanAnh thi hien chu cai dau nhu cu', () => {
  render(<KhungTinNhan {...PROPS_MAC_DINH} duongDanAnh={null} />);

  expect(screen.queryByAltText('TranBinh')).not.toBeInTheDocument();
});
```
(`'TranBinh'` là giá trị `tenHienThi` trong `PROPS_MAC_DINH` — đối
chiếu đúng giá trị thật trong file, thay nếu khác.)

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: FAIL (prop `duongDanAnh` chưa tồn tại).

- [ ] **Step 4: Sửa `KhungTinNhan.tsx`**

Thêm import `Avatar` từ `'./Avatar'` (nếu chưa có).

Thêm vào `PropsKhungTinNhan`:
```typescript
  duongDanAnh?: string | null;
```
Thêm `duongDanAnh` vào destructure tham số hàm component.

Thay CẢ 2 dòng
`<span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>`
bằng:
```tsx
<Avatar id={tenHienThi} ten={tenHienThi} duongDanAnh={duongDanAnh} />
```

- [ ] **Step 5: Xóa CSS cũ không còn dùng, chạy test để thấy PASS**

Xóa khối `.khung-tin-nhan__avatar { ... }` trong `KhungTinNhan.css`
(component `Avatar` tự mang CSS riêng qua `Avatar.css`, không cần
class này nữa — xác nhận không còn tham chiếu `khung-tin-nhan__avatar`
nào khác trong file trước khi xóa bằng cách grep lại).

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: PASS toàn bộ.

- [ ] **Step 6: Truyền `duongDanAnh` từ `TrangChat.tsx`/`TrangNhom.tsx`**

`frontend/src/Trang/TrangChat.tsx` — thêm vào lệnh gọi `<KhungTinNhan>`:
```tsx
            duongDanAnh={nguoiDangChon.duongDanAnhDaiDien}
```

`frontend/src/Trang/TrangNhom.tsx` — thêm vào lệnh gọi `<KhungTinNhan>`:
```tsx
            duongDanAnh={nhomDangChon.duongDanAnhDaiDien}
```

- [ ] **Step 7: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangBanBe.tsx frontend/src/Trang/PanelQuanLyNhom.tsx frontend/src/Trang/PanelThongTinNhom.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "feat(frontend): hien anh dai dien ca nhan o moi noi con lai + nang cap avatar dau de khung chat (GD8)"
```

---

## Ghi chú cho reviewer / executor

- Task 1 độc lập. Task 2 phụ thuộc Task 1 chỉ qua JSON shape (test
  dùng mock). Task 3/4 phụ thuộc Task 2. Task 5 độc lập với Task 2-4
  (chỉ dùng API nhóm đã có sẵn từ trước). Task 6 phụ thuộc Task 1+2 (
  field `duongDanAnhDaiDien` trên `NguoiDungTomTat`) và có thể chạy
  sau cùng vì không task nào phụ thuộc nó.
- 2 field mới trên `NguoiDungTomTat`/`HoSoCaNhan` phía TypeScript đều
  optional (xem ruling ở Global Constraints) — nếu reviewer thấy 1 test
  cũ không tự thêm field mới mà vẫn compile được, đó là ĐÚNG THEO THIẾT
  KẾ, không phải thiếu sót.
- Task 5 Step 3 có 1 điểm cần implementer tự xác nhận lại trước khi
  code (tên biến fixture `Nhom` mẫu thật trong `PanelThongTinNhom.test.tsx`,
  cách `getByLabelText` vs `getByRole` cho `<label>` bọc input ẩn) — đã
  ghi rõ trong step, không phải placeholder bỏ trống.
