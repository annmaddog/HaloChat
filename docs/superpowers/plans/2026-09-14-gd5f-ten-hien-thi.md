# GĐ5f — Tách tên hiển thị khỏi tên tài khoản — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm field `TenHienThi` cho `NguoiDung`, tách biệt hẳn khỏi
`TenTaiKhoan` (định danh đăng nhập cố định), cho phép người dùng đổi tên
hiển thị trong Cài đặt, và chuyển toàn bộ nơi hiển thị tên NGƯỜI KHÁC ở
frontend từ `.tenTaiKhoan` sang `.tenHienThi`.

**Architecture:** Backend thêm 1 field + 1 method resolve trên model
`NguoiDung`, mở rộng 2 DTO dùng chung (`NguoiDungTomTatDto`,
`HoSoCaNhanDto`) bằng cách thêm tham số cuối (không xóa `TenTaiKhoan`), và
thêm 1 endpoint đổi tên hiển thị. Frontend thêm field kiểu dữ liệu + 1 hàm
API, rồi rà từng nơi hiển thị tên người khác đổi từ `.tenTaiKhoan` sang
`.tenHienThi`, cộng thêm 1 form đổi tên hiển thị trong `TrangCaiDat.tsx`.

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api), MongoDB Driver,
xUnit (backend/HaloChat.Api.Tests); React 19 + TypeScript + Vite
(frontend/), Vitest + Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-14-halochat-ten-hien-thi.md`

## Global Constraints

- Đặt tên định danh (biến, hàm, route, class) không dấu tiếng Việt — quy
  ước xuyên suốt toàn dự án.
- Không đổi JWT — không thêm claim `tenHienThi` vào token.
- Không yêu cầu tên hiển thị duy nhất (không check trùng khi đổi).
- `TenTaiKhoan` không đổi được, không xóa khỏi bất kỳ DTO nào — chỉ thêm
  field mới bên cạnh (`NguoiDungTomTatDto`, `HoSoCaNhanDto` chỉ **thêm**
  tham số cuối).
- `TenHienThi` rỗng = chưa từng đặt → fallback về `TenTaiKhoan` (qua
  `NguoiDung.TenHienThiThucTe()`), DTO luôn trả giá trị đã resolve sẵn.
- `PUT /api/nguoidung/ten-hien-thi`: `[Authorize]`, body
  `{ tenHienThi: string }`, `[Required, MinLength(1), MaxLength(50)]` sau
  khi trim.

---

## Task 1: Backend — model, DTO, cập nhật mọi nơi khởi tạo DTO

**Files:**
- Modify: `backend/HaloChat.Api/Models/NguoiDung.cs`
- Modify: `backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs`
- Modify: `backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs:86, :102-106`
- Modify: `backend/HaloChat.Api/Services/DichVuKetBan.cs:104, :133-134`
- Modify: `backend/HaloChat.Api/Services/DichVuNhom.cs:119`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs:199`
- Test: `backend/HaloChat.Api.Tests/Models/NguoiDungTests.cs` (tạo mới nếu
  chưa có thư mục `Models/` trong test project — nếu không có, đặt file ở
  `backend/HaloChat.Api.Tests/NguoiDungTests.cs`)

**Interfaces:**
- Consumes: không có (task nền tảng, không phụ thuộc task nào khác).
- Produces:
  - `NguoiDung.TenHienThi` (`string`, mặc định `string.Empty`).
  - `NguoiDung.TenHienThiThucTe()` → `string` (rỗng/whitespace → trả
    `TenTaiKhoan`; ngược lại trả `TenHienThi`).
  - `NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa, string TenHienThi)`.
  - `HoSoCaNhanDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong, bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom, string TenHienThi)`.
  - Task 2 dùng `TenHienThiThucTe()` và cả 2 DTO trên nguyên trạng.

- [ ] **Step 1: Viết test cho `TenHienThiThucTe()`**

Tạo `backend/HaloChat.Api.Tests/NguoiDungTests.cs`:

```csharp
using HaloChat.Api.Models;
using Xunit;

namespace HaloChat.Api.Tests;

public class NguoiDungTests
{
    [Fact]
    public void TenHienThiThucTe_ChuaDatTenHienThi_TraVeTenTaiKhoan()
    {
        var nguoiDung = new NguoiDung { TenTaiKhoan = "annguyen", TenHienThi = "" };
        Assert.Equal("annguyen", nguoiDung.TenHienThiThucTe());
    }

    [Fact]
    public void TenHienThiThucTe_TenHienThiChiCoKhoangTrang_TraVeTenTaiKhoan()
    {
        var nguoiDung = new NguoiDung { TenTaiKhoan = "annguyen", TenHienThi = "   " };
        Assert.Equal("annguyen", nguoiDung.TenHienThiThucTe());
    }

    [Fact]
    public void TenHienThiThucTe_DaDatTenHienThi_TraVeTenHienThi()
    {
        var nguoiDung = new NguoiDung { TenTaiKhoan = "annguyen", TenHienThi = "An Nguyễn" };
        Assert.Equal("An Nguyễn", nguoiDung.TenHienThiThucTe());
    }
}
```

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter NguoiDungTests`
Expected: FAIL biên dịch (`TenHienThi`/`TenHienThiThucTe` chưa tồn tại).

- [ ] **Step 3: Thêm field + method vào `NguoiDung.cs`**

Thêm vào cuối class `NguoiDung` (sau field `MaOtpGuiLucNao`):

```csharp
    // [GĐ5f] Rỗng = chưa từng đặt tên hiển thị riêng — dùng TenTaiKhoan làm
    // tên hiển thị mặc định (xem TenHienThiThucTe()). Tách biệt hẳn khỏi
    // TenTaiKhoan (định danh đăng nhập duy nhất, không đổi được).
    public string TenHienThi { get; set; } = string.Empty;

    /// <summary>Tên thực sự dùng để hiển thị — TenHienThi nếu đã đặt, không thì TenTaiKhoan.</summary>
    public string TenHienThiThucTe() => string.IsNullOrWhiteSpace(TenHienThi) ? TenTaiKhoan : TenHienThi;
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter NguoiDungTests`
Expected: PASS (3/3).

- [ ] **Step 5: Mở rộng `NguoiDungTomTatDto` và `HoSoCaNhanDto`**

`backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs` — nội dung mới:

```csharp
namespace HaloChat.Api.Dto;

public record NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa, string TenHienThi);
```

`backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs` — nội dung mới:

```csharp
namespace HaloChat.Api.Dto;

public record HoSoCaNhanDto(
    string Id, string TenTaiKhoan, string Email,
    bool ChoPhepTinNhanTuNguoiLa, bool HienThiTrangThaiHoatDong,
    bool ChoPhepThemVaoNhom, bool ThongBaoTinNhanMoi, bool ThongBaoLoiMoiKetBan, bool ThongBaoNhom,
    string TenHienThi);
```

- [ ] **Step 6: Build để lộ hết các điểm gọi bị vỡ do thêm tham số**

Run: `cd backend && dotnet build`
Expected: FAIL — liệt kê đúng 6 lỗi CS7036 (thiếu tham số) tại các dòng đã
liệt kê trong "Files" ở trên (`DichVuKetBan.cs:104,133,134`,
`DichVuNguoiDung.cs:86,102`, `DichVuNhom.cs:119`, `DichVuTinNhan.cs:199`).

- [ ] **Step 7: Sửa từng điểm gọi — truyền `TenHienThiThucTe()`**

`backend/HaloChat.Api/Services/DichVuNguoiDung.cs:86` — trong
`LayDanhSachNguoiDung`, đổi:

```csharp
            .Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa))
```
thành:
```csharp
            .Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe()))
```

`backend/HaloChat.Api/Services/DichVuNguoiDung.cs:102-106` — trong
`LayThongTinCaNhanAsync`, đổi khối:

```csharp
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong,
                nguoiDung.ChoPhepThemVaoNhom, nguoiDung.ThongBaoTinNhanMoi,
                nguoiDung.ThongBaoLoiMoiKetBan, nguoiDung.ThongBaoNhom);
```
thành:
```csharp
            : new HoSoCaNhanDto(
                nguoiDung.Id, nguoiDung.TenTaiKhoan, nguoiDung.Email,
                nguoiDung.ChoPhepTinNhanTuNguoiLa, nguoiDung.HienThiTrangThaiHoatDong,
                nguoiDung.ChoPhepThemVaoNhom, nguoiDung.ThongBaoTinNhanMoi,
                nguoiDung.ThongBaoLoiMoiKetBan, nguoiDung.ThongBaoNhom, nguoiDung.TenHienThiThucTe());
```

`backend/HaloChat.Api/Services/DichVuKetBan.cs:104` (trong `LayBanBeAsync`
hoặc tương đương — dòng thêm bạn bè vào `ketQua`), đổi:
```csharp
                ketQua.Add(new NguoiDungTomTatDto(ban.Id, ban.TenTaiKhoan, ban.Email, ban.ChoPhepTinNhanTuNguoiLa));
```
thành:
```csharp
                ketQua.Add(new NguoiDungTomTatDto(ban.Id, ban.TenTaiKhoan, ban.Email, ban.ChoPhepTinNhanTuNguoiLa, ban.TenHienThiThucTe()));
```

`backend/HaloChat.Api/Services/DichVuKetBan.cs:133-134` (trong `AnhXaDto`),
đổi:
```csharp
        new NguoiDungTomTatDto(nguoiGui.Id, nguoiGui.TenTaiKhoan, nguoiGui.Email, nguoiGui.ChoPhepTinNhanTuNguoiLa),
        new NguoiDungTomTatDto(nguoiNhan.Id, nguoiNhan.TenTaiKhoan, nguoiNhan.Email, nguoiNhan.ChoPhepTinNhanTuNguoiLa),
```
thành:
```csharp
        new NguoiDungTomTatDto(nguoiGui.Id, nguoiGui.TenTaiKhoan, nguoiGui.Email, nguoiGui.ChoPhepTinNhanTuNguoiLa, nguoiGui.TenHienThiThucTe()),
        new NguoiDungTomTatDto(nguoiNhan.Id, nguoiNhan.TenTaiKhoan, nguoiNhan.Email, nguoiNhan.ChoPhepTinNhanTuNguoiLa, nguoiNhan.TenHienThiThucTe()),
```

`backend/HaloChat.Api/Services/DichVuNhom.cs:119` (trong
`AnhXaDtoAsync`), đổi:
```csharp
                thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa));
```
thành:
```csharp
                thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa, nd.TenHienThiThucTe()));
```

`backend/HaloChat.Api/Services/DichVuTinNhan.cs:199` (trong
`LayDanhSachHoiThoaiAsync`), đổi:
```csharp
            ketQua.Add(new HoiThoaiTomTatDto(
                new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa),
                xemTruoc,
                tn.ThoiGianTao,
                soChuaDoc));
```
thành:
```csharp
            ketQua.Add(new HoiThoaiTomTatDto(
                new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa, nguoiKia.TenHienThiThucTe()),
                xemTruoc,
                tn.ThoiGianTao,
                soChuaDoc));
```

- [ ] **Step 8: Build + chạy toàn bộ test backend để xác nhận không vỡ gì**

Run: `cd backend && dotnet build && dotnet test`
Expected: build 0 lỗi; toàn bộ test PASS (các test hiện có deserialize JSON
qua `ReadFromJsonAsync<HoSoCaNhanDto>`/`<List<NguoiDungTomTatDto>>` nên
không cần sửa gì thêm — trường mới chỉ được thêm, không xóa).

- [ ] **Step 9: Commit**

```bash
git add backend/HaloChat.Api/Models/NguoiDung.cs backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs backend/HaloChat.Api/Dto/HoSoCaNhanDto.cs backend/HaloChat.Api/Services/DichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuKetBan.cs backend/HaloChat.Api/Services/DichVuNhom.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api.Tests/NguoiDungTests.cs
git commit -m "feat(backend): them TenHienThi cho NguoiDung, mo rong DTO dung chung"
```

---

## Task 2: Backend — endpoint đổi tên hiển thị

**Files:**
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Create: `backend/HaloChat.Api/Dto/DoiTenHienThiRequest.cs`
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Test: `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs`

**Interfaces:**
- Consumes: `NguoiDung.TenHienThiThucTe()`, `HoSoCaNhanDto` (Task 1).
- Produces: `PUT /api/nguoidung/ten-hien-thi` — `[Authorize]`, body
  `DoiTenHienThiRequest { string TenHienThi }`, trả `200 OK` với body
  `HoSoCaNhanDto` mới nhất, hoặc `401` nếu chưa đăng nhập, hoặc `400` nếu
  validation body thất bại (rỗng sau trim / quá 50 ký tự).
  - `INguoiDungRepository.CapNhatTenHienThiAsync(string id, string tenHienThi): Task`.
  - `IDichVuNguoiDung.DoiTenHienThiAsync(string idHienTai, string tenHienThiMoi): Task<HoSoCaNhanDto?>`.

- [ ] **Step 1: Tạo request DTO với validation**

Tạo `backend/HaloChat.Api/Dto/DoiTenHienThiRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record DoiTenHienThiRequest(
    [property: Required, MinLength(1), MaxLength(50)] string TenHienThi);
```

- [ ] **Step 2: Viết test tích hợp cho endpoint (FAIL trước)**

Thêm vào `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs` (đặt
cạnh các test `LayThongTinCaNhan` hiện có — theo đúng pattern đăng ký +
đăng nhập + set `Authorization` header rồi gọi API mà các test khác trong
file này đã dùng):

```csharp
    [Fact]
    public async Task DoiTenHienThi_DangNhap_CapNhatVaTraVeHoSoMoi()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky",
            new { tenTaiKhoan = "doiten1", email = "doiten1@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap",
            new { tenDangNhap = "doiten1", matKhau = "MatKhau123!" });
        var ketQuaDangNhap = await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ketQuaDangNhap!.Token);

        var phanHoi = await _client.PutAsJsonAsync("/api/nguoidung/ten-hien-thi",
            new { tenHienThi = "Tên Mới Của Tôi" });

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var hoSo = await phanHoi.Content.ReadFromJsonAsync<HoSoCaNhanDto>();
        Assert.Equal("Tên Mới Của Tôi", hoSo!.TenHienThi);
        Assert.Equal("doiten1", hoSo.TenTaiKhoan);
    }

    [Fact]
    public async Task DoiTenHienThi_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.PutAsJsonAsync("/api/nguoidung/ten-hien-thi",
            new { tenHienThi = "Tên Mới" });

        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task DoiTenHienThi_ChuoiRongSauTrim_TraVe400()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky",
            new { tenTaiKhoan = "doiten2", email = "doiten2@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap",
            new { tenDangNhap = "doiten2", matKhau = "MatKhau123!" });
        var ketQuaDangNhap = await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ketQuaDangNhap!.Token);

        var phanHoi = await _client.PutAsJsonAsync("/api/nguoidung/ten-hien-thi",
            new { tenHienThi = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }
```

Kiểm tra đầu file test đã có `using System.Net;`, `using System.Net.Http.Json;`
và namespace DTO cần thiết (`HaloChat.Api.Dto`) — nếu thiếu, thêm vào.

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter DoiTenHienThi`
Expected: FAIL biên dịch/404 (endpoint và các method chưa tồn tại).

- [ ] **Step 4: Thêm method vào repository**

`backend/HaloChat.Api/Repositories/INguoiDungRepository.cs` — thêm vào
cuối interface (trước dấu `}`):

```csharp
    Task CapNhatTenHienThiAsync(string id, string tenHienThi);
```

`backend/HaloChat.Api/Repositories/NguoiDungRepository.cs` — thêm method
mới vào cuối class (trước dấu `}` cuối file):

```csharp
    public async Task CapNhatTenHienThiAsync(string id, string tenHienThi)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Set(nd => nd.TenHienThi, tenHienThi);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
```

- [ ] **Step 5: Thêm method vào service**

`backend/HaloChat.Api/Services/IDichVuNguoiDung.cs` — thêm vào cuối
interface:

```csharp
    Task<HoSoCaNhanDto?> DoiTenHienThiAsync(string idHienTai, string tenHienThiMoi);
```

`backend/HaloChat.Api/Services/DichVuNguoiDung.cs` — thêm method mới (đặt
cạnh `LayThongTinCaNhanAsync`, trước `YeuCauOtpDatLaiMatKhauAsync`):

```csharp
    public async Task<HoSoCaNhanDto?> DoiTenHienThiAsync(string idHienTai, string tenHienThiMoi)
    {
        await _kho.CapNhatTenHienThiAsync(idHienTai, tenHienThiMoi.Trim());
        return await LayThongTinCaNhanAsync(idHienTai);
    }
```

- [ ] **Step 6: Thêm endpoint vào controller**

`backend/HaloChat.Api/Controllers/NguoiDungController.cs` — thêm action
mới (đặt cạnh `CapNhatCaiDat`, sau nó):

```csharp
    [HttpPut("ten-hien-thi")]
    [Authorize]
    public async Task<IActionResult> DoiTenHienThi([FromBody] DoiTenHienThiRequest yeuCau)
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        var hoSo = await _dichVu.DoiTenHienThiAsync(idHienTai, yeuCau.TenHienThi);
        return hoSo is null ? NotFound() : Ok(hoSo);
    }
```

Model validation (`[Required, MinLength(1), MaxLength(50)]` trên
`DoiTenHienThiRequest`) đã tự động trả `400` qua `[ApiController]` khi
`ModelState` không hợp lệ — không cần code kiểm tra thủ công. Lưu ý:
`MinLength(1)` chỉ kiểm tra độ dài chuỗi thô, chuỗi `"   "` (3 khoảng
trắng) vẫn qua được validation này; endpoint dựa vào việc
`DoiTenHienThiAsync` gọi `.Trim()` trước khi lưu — nghĩa là test
`DoiTenHienThi_ChuoiRongSauTrim_TraVe400` ở Step 2 cần `[Required]` kết
hợp thêm kiểm tra thủ công. Vì `[Required]` trên `string` không chặn
chuỗi toàn khoảng trắng, thêm kiểm tra thủ công đầu action:

```csharp
    [HttpPut("ten-hien-thi")]
    [Authorize]
    public async Task<IActionResult> DoiTenHienThi([FromBody] DoiTenHienThiRequest yeuCau)
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(yeuCau.TenHienThi))
        {
            return BadRequest(new { thongBao = "Tên hiển thị không được để trống." });
        }

        var hoSo = await _dichVu.DoiTenHienThiAsync(idHienTai, yeuCau.TenHienThi);
        return hoSo is null ? NotFound() : Ok(hoSo);
    }
```

(Đây là bản action cuối cùng — thay hẳn bản nháp phía trên, không giữ cả
hai.)

- [ ] **Step 7: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter DoiTenHienThi`
Expected: PASS (3/3).

- [ ] **Step 8: Chạy toàn bộ test backend**

Run: `cd backend && dotnet test`
Expected: PASS toàn bộ.

- [ ] **Step 9: Commit**

```bash
git add backend/HaloChat.Api/Repositories/INguoiDungRepository.cs backend/HaloChat.Api/Repositories/NguoiDungRepository.cs backend/HaloChat.Api/Services/IDichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuNguoiDung.cs backend/HaloChat.Api/Dto/DoiTenHienThiRequest.cs backend/HaloChat.Api/Controllers/NguoiDungController.cs backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs
git commit -m "feat(backend): them endpoint PUT /api/nguoidung/ten-hien-thi"
```

---

## Task 3: Frontend — kiểu dữ liệu, API, và toàn bộ hiển thị tên người khác

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/Trang/TrangBanBe.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/PanelThongTinNhom.tsx`
- Modify: `frontend/src/Trang/PanelQuanLyNhom.tsx`
- Test: `frontend/src/DichVuApi.test.ts`
- Test: `frontend/src/Trang/TrangBanBe.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`
- Test: `frontend/src/Trang/PanelThongTinNhom.test.tsx`
- Test: `frontend/src/Trang/PanelQuanLyNhom.test.tsx`
- Test: `frontend/src/NguCanh/SuDungSoLuongChuaDoc.test.tsx`

**Interfaces:**
- Consumes: backend `NguoiDungTomTatDto`/`HoSoCaNhanDto` đã có field
  `tenHienThi` (Task 1); backend endpoint `PUT /nguoidung/ten-hien-thi`
  (Task 2).
- Produces:
  - `NguoiDungTomTat.tenHienThi: string`, `HoSoCaNhan.tenHienThi: string`.
  - `DoiTenHienThi(token: string, tenHienThiMoi: string): Promise<HoSoCaNhan>`
    trong `DichVuApi.ts` — Task 4 (TrangCaiDat) gọi hàm này.

- [ ] **Step 1: Thêm field vào kiểu dữ liệu**

`frontend/src/KieuDuLieu.ts` — sửa `NguoiDungTomTat` (dòng 1-6):

```typescript
export interface NguoiDungTomTat {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  tenHienThi: string;
}
```

Và `HoSoCaNhan` (dòng 56-66):

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
}
```

- [ ] **Step 2: Build TypeScript để lộ hết chỗ thiếu field (fixture test)**

Run: `cd frontend && npx tsc --noEmit`
Expected: FAIL — liệt kê lỗi TS2741/TS2739 "thiếu tenHienThi" tại mọi nơi
tạo object `NguoiDungTomTat`/`HoSoCaNhan` literal (test fixtures) trong:
`DichVuApi.test.ts`, `TrangBanBe.test.tsx`, `TrangNhom.test.tsx`,
`TrangChat.test.tsx`, `PanelThongTinNhom.test.tsx`,
`PanelQuanLyNhom.test.tsx`, `SuDungSoLuongChuaDoc.test.tsx`. Dùng danh
sách lỗi này làm checklist cho Step 3.

- [ ] **Step 3: Thêm `tenHienThi` vào mọi fixture bị báo lỗi**

Với mỗi file trên, tìm từng object literal tạo `NguoiDungTomTat` (hoặc
lồng trong `HoSoCaNhan`, `HoiThoaiTomTat`, `Nhom.thanhVien`,
`LoiMoiKetBan.nguoiGui/nguoiNhan`) — nhận diện qua field `tenTaiKhoan:`
đứng cạnh `id:`/`email:`/`choPhepTinNhanTuNguoiLa:`. Thêm dòng
`tenHienThi: '<giá trị>',` ngay sau `tenTaiKhoan: '<giá trị>',` dùng
CHÍNH giá trị của `tenTaiKhoan` đó (vd. `tenTaiKhoan: 'nguoia'` →
`tenHienThi: 'nguoia'`) trừ khi test đó cụ thể đang kiểm tra hiển thị tên
(trường hợp đó xử lý riêng ở Task 4/5, không phải ở đây — task này chỉ
làm cho toàn bộ suite biên dịch được, không đổi ý nghĩa test hiện có).

Ví dụ áp dụng cho `TrangBanBe.test.tsx` — mọi fixture kiểu:
```typescript
{ id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false }
```
sửa thành:
```typescript
{ id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false, tenHienThi: 'nguoia' }
```

Lặp lại cho tất cả object literal tương tự trong 7 file test ở trên, và
trong `HoSoCaNhan` fixture (thêm `tenHienThi: '<tenTaiKhoan tương ứng>'`).

- [ ] **Step 4: Build TypeScript lại để xác nhận hết lỗi**

Run: `cd frontend && npx tsc --noEmit`
Expected: 0 lỗi.

- [ ] **Step 5: Chạy test hiện có để xác nhận chưa vỡ hành vi**

Run: `cd frontend && npm test -- --run`
Expected: PASS toàn bộ (fixture thêm field không đổi hành vi vì các test
hiện có so sánh/hiển thị theo `tenTaiKhoan`, chưa đổi sang `tenHienThi`).

- [ ] **Step 6: Thêm hàm API `DoiTenHienThi`**

`frontend/src/DichVuApi.ts` — thêm vào cuối file (sau `XoaBanBe`):

```typescript
export async function DoiTenHienThi(token: string, tenHienThiMoi: string): Promise<HoSoCaNhan> {
  return goiApi<HoSoCaNhan>('/nguoidung/ten-hien-thi', {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenHienThi: tenHienThiMoi }),
  });
}
```

- [ ] **Step 7: Viết test cho `DoiTenHienThi` (FAIL trước đã có hàm ở Step 6,
  nên viết test trước khi dùng — chạy để xác nhận PASS ngay vì hàm đã tồn
  tại là hợp lệ trong task này do đây là hàm mới độc lập không cần chu
  trình đỏ/xanh riêng)**

Thêm vào `frontend/src/DichVuApi.test.ts` (theo đúng pattern mock `fetch`
mà các test hàm khác trong file này dùng, ví dụ pattern của `DoiMatKhau`):

```typescript
it('DoiTenHienThi goi dung endpoint PUT va tra ve ho so moi', async () => {
  const hoSoMoi = {
    id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới',
  };
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: true,
    json: () => Promise.resolve(hoSoMoi),
  }));

  const ketQua = await DoiTenHienThi('token-gia-lap', 'Tên Mới');

  expect(ketQua).toEqual(hoSoMoi);
  expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/nguoidung/ten-hien-thi'),
    expect.objectContaining({ method: 'PUT' }),
  );
});
```

Kiểm tra file đã import `DoiTenHienThi` (thêm vào dòng import từ
`./DichVuApi` nếu import theo named list) và đã có `vi`/`expect` sẵn từ
Vitest setup hiện có trong file.

- [ ] **Step 8: Chạy test này để xác nhận PASS**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: PASS.

- [ ] **Step 9: Đổi hiển thị tên người khác — `TrangBanBe.tsx`**

Đổi từng dòng sau (tất cả đều hiển thị tên MỘT NGƯỜI KHÁC, không phải
`nguoiDungHienTai`):
- Dòng 98: `` `Xóa ${b.tenTaiKhoan} khỏi danh sách bạn bè?` `` → `` `Xóa ${b.tenHienThi} khỏi danh sách bạn bè?` ``
- Dòng 116: `.filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(...))` → `.filter((nd) => nd.tenHienThi.toLowerCase().includes(tuKhoaTimKiem.trim().toLowerCase()))`
- Dòng 163: `<Avatar id={nd.id} ten={nd.tenTaiKhoan} />` → `<Avatar id={nd.id} ten={nd.tenHienThi} />`
- Dòng 165: `<span className="trang-ban-be__card-ten">{nd.tenTaiKhoan}</span>` → `{nd.tenHienThi}`
- Dòng 207: `<Avatar id={b.id} ten={b.tenTaiKhoan} />` → `ten={b.tenHienThi}`
- Dòng 209: `<span className="trang-ban-be__card-ten">{b.tenTaiKhoan}</span>` → `{b.tenHienThi}`
- Dòng 221: `` aria-label={`Thêm thao tác cho ${b.tenTaiKhoan}`} `` → `` `Thêm thao tác cho ${b.tenHienThi}` ``
- Dòng 252: `<Avatar id={l.nguoiGui.id} ten={l.nguoiGui.tenTaiKhoan} />` → `ten={l.nguoiGui.tenHienThi}`
- Dòng 254: `<span className="trang-ban-be__card-ten">{l.nguoiGui.tenTaiKhoan}</span>` → `{l.nguoiGui.tenHienThi}`
- Dòng 273: `<Avatar id={hoSoDangXem.id} ten={hoSoDangXem.tenTaiKhoan} kichThuoc="lon" />` → `ten={hoSoDangXem.tenHienThi}`
- Dòng 274: `<h3>{hoSoDangXem.tenTaiKhoan}</h3>` → `<h3>{hoSoDangXem.tenHienThi}</h3>`

(Số dòng chính xác tại thời điểm viết plan — nếu lệch do các sửa trước
đó, dùng nội dung chuỗi để định vị chính xác, không dùng số dòng mù
quáng.)

- [ ] **Step 10: Đổi hiển thị tên người khác — `TrangNhom.tsx`**

Dòng ~324, trong checkbox chọn thành viên lúc tạo nhóm:
```tsx
                    {nd.tenTaiKhoan}
```
đổi thành:
```tsx
                    {nd.tenHienThi}
```

- [ ] **Step 11: Đổi hiển thị tên người khác — `TrangChat.tsx`**

- Dòng 222: `.filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))` → `.filter((nd) => nd.tenHienThi.toLowerCase().includes(tuKhoaTimKiem.toLowerCase()))`
- Dòng 229: `<Avatar id={nd.id} ten={nd.tenTaiKhoan} kichThuoc="nho" />` → `ten={nd.tenHienThi}`
- Dòng 230: `<span className="trang-chat__ten">{nd.tenTaiKhoan}</span>` → `{nd.tenHienThi}`
- Dòng 247: `tenHienThi={nguoiDangChon.tenTaiKhoan}` → `tenHienThi={nguoiDangChon.tenHienThi}`
  (LƯU Ý: đây là prop `tenHienThi` của `KhungTinNhan` — tên trùng với
  field mới nhưng là khái niệm khác, tiêu đề chung của header; chỉ đổi
  GIÁ TRỊ truyền vào, không đổi tên prop).

- [ ] **Step 12: Đổi hiển thị tên người khác — `PanelThongTinNhom.tsx`**

Dòng 40:
```tsx
<Avatar key={tv.id} id={tv.id} ten={tv.tenTaiKhoan} kichThuoc="nho" />
```
đổi thành:
```tsx
<Avatar key={tv.id} id={tv.id} ten={tv.tenHienThi} kichThuoc="nho" />
```

- [ ] **Step 13: Đổi hiển thị tên người khác — `PanelQuanLyNhom.tsx`**

- Dòng 95: `<Avatar id={tv.id} ten={tv.tenTaiKhoan} kichThuoc="nho" />` → `ten={tv.tenHienThi}`
- Dòng 96: `` <span>{tv.tenTaiKhoan}{tv.id === nhom.nguoiTaoId ? ' (Admin)' : ''}</span> `` → `` {tv.tenHienThi} ``
- Dòng 98: `` aria-label={`Xóa ${tv.tenTaiKhoan}`} `` → `` `Xóa ${tv.tenHienThi}` ``
- Dòng 111: `<option key={nd.id} value={nd.id}>{nd.tenTaiKhoan}</option>` → `{nd.tenHienThi}`

- [ ] **Step 14: Cập nhật test đã có để phản ánh việc chuyển sang `tenHienThi`**

Trong các file test đã sửa fixture ở Step 3, tìm các assertion đang tìm
kiếm text bằng giá trị `tenTaiKhoan` để hiển thị tên người khác trên màn
hình (`screen.getByText(...)`, `screen.findByText(...)` khớp tên hiển
thị) — vì Step 3 đặt `tenHienThi` giá trị GIỐNG `tenTaiKhoan`, các
assertion này vẫn PASS nguyên trạng, không cần sửa gì thêm. Chỉ sửa nếu
một test cụ thể đặt `tenHienThi` khác `tenTaiKhoan` một cách cố ý để kiểm
tra đúng field mới được dùng — nếu plan tới đây chưa có test nào như vậy,
thêm đúng 1 test khẳng định hành vi mới cho `TrangBanBe.test.tsx`:

```typescript
it('the ket qua tim kiem hien thi tenHienThi thay vi tenTaiKhoan', async () => {
  const nguoiDung = {
    id: '9', tenTaiKhoan: 'tentaikhoan9', email: 'x@vi.du',
    choPhepTinNhanTuNguoiLa: true, tenHienThi: 'Tên Hiển Thị Chín',
  };
  // ... dựng lại phần setup/mock LayDanhSachNguoiDung/LayBanBe/LayLoiMoiDen/LayLoiMoiGui
  // theo đúng pattern các test tìm kiếm khác đã có sẵn trong file này,
  // trả về nguoiDung ở trên qua LayDanhSachNguoiDung.
  // Gõ vào ô tìm kiếm bằng MỘT PHẦN của tenHienThi (không phải tenTaiKhoan):
  // fireEvent.change(oTimKiem, { target: { value: 'Hiển Thị' } });
  // Kỳ vọng: hiển thị 'Tên Hiển Thị Chín'; và gõ 'tentaikhoan9' (chỉ khớp
  // tenTaiKhoan) KHÔNG cho ra kết quả nào.
});
```

(Viết đầy đủ test này theo cấu trúc render/mock thực tế đã tồn tại trong
`TrangBanBe.test.tsx` — đọc file để lấy đúng helper dựng props/mock đã có
trước khi điền phần "..." ở trên; đây là điểm duy nhất trong task cần đọc
thêm ngữ cảnh cụ thể của file test khi thực thi.)

- [ ] **Step 15: Chạy toàn bộ test frontend**

Run: `cd frontend && npm test -- --run`
Expected: PASS toàn bộ.

- [ ] **Step 16: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts frontend/src/Trang/TrangBanBe.tsx frontend/src/Trang/TrangBanBe.test.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangNhom.test.tsx frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/PanelThongTinNhom.tsx frontend/src/Trang/PanelThongTinNhom.test.tsx frontend/src/Trang/PanelQuanLyNhom.tsx frontend/src/Trang/PanelQuanLyNhom.test.tsx frontend/src/NguCanh/SuDungSoLuongChuaDoc.test.tsx
git commit -m "feat(frontend): dung tenHienThi thay tenTaiKhoan de hien thi ten nguoi khac"
```

---

## Task 4: Frontend — `TrangCaiDat.tsx`: form đổi tên hiển thị + xóa nút Đăng xuất

**Files:**
- Modify: `frontend/src/Trang/TrangCaiDat.tsx`
- Modify: `frontend/src/Trang/TrangCaiDat.css`
- Test: `frontend/src/Trang/TrangCaiDat.test.tsx`

**Interfaces:**
- Consumes: `DoiTenHienThi(token, tenHienThiMoi): Promise<HoSoCaNhan>`,
  `HoSoCaNhan.tenHienThi: string` (Task 3); `LoiGoiApi` (đã import sẵn
  trong file, dùng lại nguyên trạng).
- Produces: không có task nào phụ thuộc task này (task cuối).

- [ ] **Step 1: Viết test cho form đổi tên hiển thị (FAIL trước)**

Thêm vào `frontend/src/Trang/TrangCaiDat.test.tsx` (đọc file trước để
dùng đúng pattern mock `LayThongTinCaNhan`/`useXacThuc`/render đã có sẵn
cho các test mục "Tài khoản"/"Đổi mật khẩu" hiện tại):

```typescript
it('doi ten hien thi thanh cong cap nhat lai input va thong bao', async () => {
  vi.mocked(LayThongTinCaNhan).mockResolvedValue({
    id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Cũ',
  });
  vi.mocked(DoiTenHienThi).mockResolvedValue({
    id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới',
  });

  render(<TrangCaiDat />);
  const oNhap = await screen.findByDisplayValue('Tên Cũ');
  fireEvent.change(oNhap, { target: { value: 'Tên Mới' } });
  fireEvent.click(screen.getByRole('button', { name: /lưu tên hiển thị/i }));

  expect(await screen.findByText(/đã lưu tên hiển thị/i)).toBeInTheDocument();
  expect(DoiTenHienThi).toHaveBeenCalledWith(expect.any(String), 'Tên Mới');
});

it('nut luu ten hien thi bi disable khi rong hoac khong doi', async () => {
  vi.mocked(LayThongTinCaNhan).mockResolvedValue({
    id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Cũ',
  });

  render(<TrangCaiDat />);
  const oNhap = await screen.findByDisplayValue('Tên Cũ');
  const nutLuu = screen.getByRole('button', { name: /lưu tên hiển thị/i });
  expect(nutLuu).toBeDisabled();

  fireEvent.change(oNhap, { target: { value: '   ' } });
  expect(nutLuu).toBeDisabled();

  fireEvent.change(oNhap, { target: { value: 'Tên Cũ' } });
  expect(nutLuu).toBeDisabled();
});

it('khong con nut Dang xuat trong muc Tai khoan', async () => {
  vi.mocked(LayThongTinCaNhan).mockResolvedValue({
    id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
    hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
    thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Cũ',
  });

  render(<TrangCaiDat />);
  await screen.findByDisplayValue('Tên Cũ');

  expect(screen.queryByRole('button', { name: /đăng xuất/i })).not.toBeInTheDocument();
});
```

Đảm bảo `DoiTenHienThi` và `LayThongTinCaNhan` được import + mock ở đầu
file theo đúng cách các hàm `DichVuApi` khác trong file này đã được mock
(vd. `vi.mock('../DichVuApi', ...)` với factory liệt kê từng hàm, hoặc
`vi.mocked(...)` nếu file dùng cách đó — đọc phần đầu file để khớp đúng
kiểu mock hiện có trước khi thêm).

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run TrangCaiDat`
Expected: FAIL (form chưa tồn tại, nút Đăng xuất vẫn còn).

- [ ] **Step 3: Thêm state + hàm xử lý đổi tên hiển thị**

`frontend/src/Trang/TrangCaiDat.tsx` — sửa import ở dòng 2:

```typescript
import { LayThongTinCaNhan, CapNhatCaiDat, DoiMatKhau, DoiTenHienThi, LoiGoiApi } from '../DichVuApi';
```

Thêm state mới sau dòng 19 (`const [thongBaoNhom, setThongBaoNhom] = useState(true);`):

```typescript
  const [tenHienThi, setTenHienThi] = useState('');
  const [tenHienThiGoc, setTenHienThiGoc] = useState('');
  const [dangLuuTen, setDangLuuTen] = useState(false);
  const [daLuuTen, setDaLuuTen] = useState(false);
  const [loiDoiTen, setLoiDoiTen] = useState<string | null>(null);
```

Trong `useEffect` tải hồ sơ (dòng 35-49), thêm 2 dòng set state sau dòng
`setThongBaoNhom(hoSo.thongBaoNhom);`:

```typescript
        setTenHienThi(hoSo.tenHienThi);
        setTenHienThiGoc(hoSo.tenHienThi);
```

Thêm hàm xử lý mới sau hàm `xuLyDoiMatKhau` (trước dấu `return (` của
component):

```typescript
  async function xuLyDoiTenHienThi() {
    if (!token || !tenHienThi.trim() || tenHienThi.trim() === tenHienThiGoc) return;
    setLoiDoiTen(null);
    setDaLuuTen(false);
    setDangLuuTen(true);
    try {
      const hoSoMoi = await DoiTenHienThi(token, tenHienThi.trim());
      setTenHienThi(hoSoMoi.tenHienThi);
      setTenHienThiGoc(hoSoMoi.tenHienThi);
      setDaLuuTen(true);
    } catch (loiBat) {
      setLoiDoiTen(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi tên hiển thị thất bại.');
    } finally {
      setDangLuuTen(false);
    }
  }
```

- [ ] **Step 4: Thêm form vào JSX mục "Tài khoản" + xóa nút Đăng xuất**

Trong khối `{mucDangChon === 'tai-khoan' && (...)}` (dòng 149-193), đổi
toàn bộ khối thành:

```tsx
        {mucDangChon === 'tai-khoan' && (
          <>
            <h2>Tài khoản</h2>
            <p><strong>Tên tài khoản:</strong> {nguoiDungHienTai?.tenTaiKhoan}</p>
            <p><strong>Email:</strong> {nguoiDungHienTai?.email}</p>

            <div className="trang-cai-dat__form-ten-hien-thi">
              <h3>Tên hiển thị</h3>
              {loiDoiTen && (
                <p className="thong-bao-loi" role="alert">
                  {loiDoiTen}
                </p>
              )}
              {daLuuTen && <p className="trang-cai-dat__da-luu">Đã lưu tên hiển thị.</p>}
              <div className="panel-quan-ly-nhom__hang-ten">
                <input
                  type="text"
                  value={tenHienThi}
                  onChange={(su) => { setTenHienThi(su.target.value); setDaLuuTen(false); }}
                  disabled={dangLuuTen}
                  maxLength={50}
                />
                <button
                  className="nut-chinh"
                  onClick={xuLyDoiTenHienThi}
                  disabled={dangLuuTen || !tenHienThi.trim() || tenHienThi.trim() === tenHienThiGoc}
                >
                  {dangLuuTen ? 'Đang lưu...' : 'Lưu tên hiển thị'}
                </button>
              </div>
            </div>

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
          </>
        )}
```

(Khác biệt so với bản gốc: thêm khối `trang-cai-dat__form-ten-hien-thi`
ngay sau 2 dòng `<p>` tĩnh, và XÓA hẳn dòng
`<button className="trang-cai-dat__nut-dang-xuat" onClick={dangXuat}>Đăng xuất</button>`
ở cuối khối.)

Class `panel-quan-ly-nhom__hang-ten` được tái dùng nguyên trạng từ
`PanelQuanLyNhom.css` để giữ đúng layout input+nút cạnh nhau đã có sẵn
trong app — không cần định nghĩa CSS mới cho riêng layout hàng
input/nút này, nhưng `PanelQuanLyNhom.css` chưa được import trong
`TrangCaiDat.tsx`. Thay vì thêm import chéo giữa 2 trang, copy đúng rule
đó sang `TrangCaiDat.css` dưới tên riêng của trang này ở Step 5.

- [ ] **Step 5: Thêm CSS cho form mới**

`frontend/src/Trang/TrangCaiDat.css` — đọc file trước để xác định vị trí
thêm hợp lý (cạnh các rule `.trang-cai-dat__form-mat-khau` nếu đã tồn
tại), thêm:

```css
.trang-cai-dat__form-ten-hien-thi {
  margin-bottom: 28px;
}

.trang-cai-dat__form-ten-hien-thi .panel-quan-ly-nhom__hang-ten {
  display: flex;
  gap: 8px;
  align-items: center;
}

.trang-cai-dat__form-ten-hien-thi input {
  flex: 1;
  padding: 10px 14px;
  border: 1px solid var(--mau-vien);
  border-radius: 10px;
  font-family: inherit;
  font-size: 14px;
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  box-sizing: border-box;
}
```

(Class được đặt tên `.trang-cai-dat__form-ten-hien-thi .panel-quan-ly-nhom__hang-ten`
chỉ để tái dùng flex layout hàng ngang qua tên class hiện có trong JSX ở
Step 4 — không phụ thuộc `PanelQuanLyNhom.css` phải được import, vì rule
này tự định nghĩa lại trong phạm vi `TrangCaiDat.css`.)

- [ ] **Step 6: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run TrangCaiDat`
Expected: PASS toàn bộ 3 test mới + mọi test cũ trong file.

- [ ] **Step 7: Chạy toàn bộ test frontend + kiểm tra TypeScript**

Run: `cd frontend && npx tsc --noEmit && npm test -- --run`
Expected: 0 lỗi TypeScript; toàn bộ test PASS.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/Trang/TrangCaiDat.tsx frontend/src/Trang/TrangCaiDat.css frontend/src/Trang/TrangCaiDat.test.tsx
git commit -m "feat(frontend): them form doi ten hien thi, xoa nut Dang xuat khoi muc Tai khoan"
```

---

## Ghi chú cho reviewer / executor

- Task 1 và Task 2 là backend, có thể review độc lập với frontend.
- Task 3 phụ thuộc Task 1 (cần `tenHienThi` trong DTO JSON) nhưng có thể
  code song song về mặt tĩnh — chỉ cần chạy thử cuối cùng phụ thuộc
  backend đã deploy/đang chạy local đúng field mới nếu review bằng test
  tích hợp thật; test đơn vị frontend dùng mock nên không cần backend
  chạy thật.
- Task 4 phụ thuộc Task 3 (cần `DoiTenHienThi` đã tồn tại trong
  `DichVuApi.ts`).
- Không có task nào đụng chung 1 file với task khác trong cùng vùng code
  (Task 3 và Task 4 cùng đụng `TrangCaiDat.test.tsx`? — không, Task 3
  không đụng `TrangCaiDat.*`; xác nhận: không xung đột file giữa các
  task).
