# Quên Mật Khẩu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép người dùng chưa đăng nhập tự đặt lại mật khẩu qua email: nhập email → nhận mã OTP 6 số qua email thật → nhập OTP + mật khẩu mới → đăng nhập lại được.

**Architecture:** Lưu 4 field OTP trực tiếp trên document `NguoiDung` (không tạo collection riêng — mỗi người chỉ cần 1 OTP hiệu lực tại 1 thời điểm). Dịch vụ gửi email mới (`IDichVuEmail`, dùng MailKit qua Gmail SMTP) độc lập với `HaloChat.Security` (project đó chỉ dành cho mã hóa/băm). 2 endpoint REST công khai (không `[Authorize]`) nối vào `NguoiDungController` sẵn có. Frontend thêm 1 trang mới với luồng 2 bước trên cùng 1 form.

**Tech Stack:** ASP.NET Core 9 (backend/HaloChat.Api), thư viện **MailKit** (SMTP client), MongoDB.Driver, React 19 + TypeScript + Vite (frontend/), xUnit, Vitest + Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md` §11, §11.1 (đọc cả 2 mục trước khi bắt đầu bất kỳ task nào).

## Global Constraints

- Mã OTP là 6 chữ số, sinh bằng `System.Security.Cryptography.RandomNumberGenerator` — KHÔNG dùng `System.Random`.
- `POST /api/nguoidung/quen-mat-khau` LUÔN trả về cùng 1 thông báo thành công chung, bất kể email có tồn tại trong hệ thống hay không, và bất kể có bị chặn bởi cooldown 60 giây hay không — không được để lộ qua bất kỳ response nào (khác thời gian phản hồi rõ rệt, mã lỗi khác nhau, v.v.) rằng 1 email cụ thể có đăng ký tài khoản hay không.
- Khi nhập sai OTP, response KHÔNG được tiết lộ còn bao nhiêu lần thử — chỉ 1 trong 2 thông báo cố định: "Mã OTP không đúng." (còn hiệu lực, còn lượt thử) hoặc "Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới." (hết hạn / vượt quá 5 lần sai / chưa từng yêu cầu OTP).
- Băm OTP và mật khẩu mới đều tái dùng `HaloChat.Security.IDichVuMatKhau` (`BamMatKhau`/`TaoSalt`) đã có sẵn — KHÔNG viết logic băm mới. OTP băm bằng `Salt` sẵn có của chính user đó (không cần field salt riêng cho OTP).
- KHÔNG BAO GIỜ commit giá trị App Password/tài khoản Gmail thật vào bất kỳ file nào (`appsettings.json`, code, plan, docs) — chỉ cấu hình qua `dotnet user-secrets` (local) và biến môi trường trên Render (production), theo đúng pattern `MongoDb__.../Jwt__...` đã có trong dự án.
- Mọi endpoint REST GIỮ NGUYÊN pattern `{ thongBao }` cho cả thành công lẫn lỗi, đúng style hiện có trong `NguoiDungController`.
- Cuối MỌI task backend: `dotnet build backend/HaloChat.sln` phải 0 lỗi và `dotnet test backend/HaloChat.sln` phải xanh toàn bộ trước khi commit.
- Cuối MỌI task frontend: cả `npm run test -- --run` VÀ `npm run build` (chạy `tsc -b` full type-check) phải xanh trước khi commit.
- Toàn bộ chuỗi hiển thị cho người dùng viết bằng tiếng Việt, đúng văn phong hiện có (câu hoàn chỉnh có dấu chấm cho thông báo lỗi/thành công).

---

## Task 1: Model + Repository — 4 field OTP trên `NguoiDung`

**Files:**
- Modify: `backend/HaloChat.Api/Models/NguoiDung.cs`
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Create: `backend/HaloChat.Api.Tests/Repositories/NguoiDungRepositoryOtpTests.cs` (kiểm thử đơn vị cho fake — xem Step 5, không cần MongoDB thật vì test trên `NguoiDungGiaLap`)

**Interfaces:**
- Consumes: không có.
- Produces: `NguoiDung` có thêm `MaOtpBam: string?`, `MaOtpHetHan: DateTime?`, `SoLanThuSai: int` (mặc định 0), `MaOtpGuiLucNao: DateTime?`. `INguoiDungRepository` thêm 3 method: `LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao)`, `TangSoLanThuSaiOtpAsync(string id)`, `DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi)` (method này set `MatKhauBam`/`Salt` VÀ xóa sạch cả 4 field OTP về `null`/`0` trong CÙNG 1 lần update) — Task 3 (`DichVuNguoiDung`) gọi đúng 3 method này.

- [ ] **Step 1: Thêm field vào model**

Trong `backend/HaloChat.Api/Models/NguoiDung.cs`, thêm ngay dưới dòng `public bool HienThiTrangThaiHoatDong { get; set; } = true;`:

```csharp
    // [Quên mật khẩu] Mã OTP băm bằng Salt sẵn có của chính user (không cần
    // field salt riêng) — xem IDichVuMatKhau.BamMatKhau. Null khi chưa từng
    // yêu cầu OTP hoặc đã đặt lại mật khẩu thành công.
    public string? MaOtpBam { get; set; }
    public DateTime? MaOtpHetHan { get; set; }
    public int SoLanThuSai { get; set; } = 0;

    // Thời điểm gửi OTP gần nhất — dùng để chặn spam gửi lại liên tục
    // (cooldown 60 giây), tách biệt với MaOtpHetHan (thời điểm OTP đó
    // hết hạn sử dụng, 10 phút sau khi gửi).
    public DateTime? MaOtpGuiLucNao { get; set; }
```

- [ ] **Step 2: Thêm 3 method vào `INguoiDungRepository`**

```csharp
    Task LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao);
    Task TangSoLanThuSaiOtpAsync(string id);
    Task DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi);
```

- [ ] **Step 3: Cài đặt trong `NguoiDungRepository`**

```csharp
    public async Task LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.MaOtpBam, maOtpBam)
            .Set(nd => nd.MaOtpHetHan, hetHan)
            .Set(nd => nd.MaOtpGuiLucNao, guiLucNao)
            .Set(nd => nd.SoLanThuSai, 0);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task TangSoLanThuSaiOtpAsync(string id)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update.Inc(nd => nd.SoLanThuSai, 1);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi)
    {
        var boLoc = Builders<NguoiDung>.Filter.Eq(nd => nd.Id, id);
        var capNhat = Builders<NguoiDung>.Update
            .Set(nd => nd.MatKhauBam, matKhauBamMoi)
            .Set(nd => nd.Salt, saltMoi)
            .Set(nd => nd.MaOtpBam, null)
            .Set(nd => nd.MaOtpHetHan, null)
            .Set(nd => nd.MaOtpGuiLucNao, null)
            .Set(nd => nd.SoLanThuSai, 0);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }
```

- [ ] **Step 4: Cập nhật fake `NguoiDungGiaLap`**

Thêm vào `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`:

```csharp
    public Task LuuOtpAsync(string id, string maOtpBam, DateTime hetHan, DateTime guiLucNao)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.MaOtpBam = maOtpBam;
            nguoiDung.MaOtpHetHan = hetHan;
            nguoiDung.MaOtpGuiLucNao = guiLucNao;
            nguoiDung.SoLanThuSai = 0;
        }
        return Task.CompletedTask;
    }

    public Task TangSoLanThuSaiOtpAsync(string id)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.SoLanThuSai++;
        }
        return Task.CompletedTask;
    }

    public Task DatLaiMatKhauAsync(string id, string matKhauBamMoi, string saltMoi)
    {
        var nguoiDung = DanhSach.FirstOrDefault(nd => nd.Id == id);
        if (nguoiDung is not null)
        {
            nguoiDung.MatKhauBam = matKhauBamMoi;
            nguoiDung.Salt = saltMoi;
            nguoiDung.MaOtpBam = null;
            nguoiDung.MaOtpHetHan = null;
            nguoiDung.MaOtpGuiLucNao = null;
            nguoiDung.SoLanThuSai = 0;
        }
        return Task.CompletedTask;
    }
```

- [ ] **Step 5: Viết kiểm thử đơn vị cho hành vi fake (đảm bảo Task 3 dựa vào đúng ngữ nghĩa)**

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class NguoiDungRepositoryOtpTests
{
    [Fact]
    public async Task LuuOtpAsync_GhiDungCaBonFieldVaResetSoLanThuSai()
    {
        var kho = new NguoiDungGiaLap();
        var nguoiDung = new NguoiDung { SoLanThuSai = 3 };
        kho.DanhSach.Add(nguoiDung);
        var hetHan = DateTime.UtcNow.AddMinutes(10);
        var guiLucNao = DateTime.UtcNow;

        await kho.LuuOtpAsync(nguoiDung.Id, "ma-bam", hetHan, guiLucNao);

        Assert.Equal("ma-bam", nguoiDung.MaOtpBam);
        Assert.Equal(hetHan, nguoiDung.MaOtpHetHan);
        Assert.Equal(guiLucNao, nguoiDung.MaOtpGuiLucNao);
        Assert.Equal(0, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task TangSoLanThuSaiOtpAsync_TangDung1DonVi()
    {
        var kho = new NguoiDungGiaLap();
        var nguoiDung = new NguoiDung { SoLanThuSai = 2 };
        kho.DanhSach.Add(nguoiDung);

        await kho.TangSoLanThuSaiOtpAsync(nguoiDung.Id);

        Assert.Equal(3, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task DatLaiMatKhauAsync_CapNhatMatKhauVaXoaSachOtp()
    {
        var kho = new NguoiDungGiaLap();
        var nguoiDung = new NguoiDung
        {
            MatKhauBam = "cu", Salt = "salt-cu",
            MaOtpBam = "ma-bam", MaOtpHetHan = DateTime.UtcNow, MaOtpGuiLucNao = DateTime.UtcNow, SoLanThuSai = 4,
        };
        kho.DanhSach.Add(nguoiDung);

        await kho.DatLaiMatKhauAsync(nguoiDung.Id, "moi", "salt-moi");

        Assert.Equal("moi", nguoiDung.MatKhauBam);
        Assert.Equal("salt-moi", nguoiDung.Salt);
        Assert.Null(nguoiDung.MaOtpBam);
        Assert.Null(nguoiDung.MaOtpHetHan);
        Assert.Null(nguoiDung.MaOtpGuiLucNao);
        Assert.Equal(0, nguoiDung.SoLanThuSai);
    }
}
```

- [ ] **Step 6: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass, bao gồm 3 test mới ở Step 5.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Them 4 field OTP tren NguoiDung + repository methods (Quen mat khau)"
```

---

## Task 2: `IDichVuEmail` — gửi email OTP qua Gmail SMTP (MailKit)

**Files:**
- Create: `backend/HaloChat.Api/Options/TuyChonSmtpEmail.cs`
- Create: `backend/HaloChat.Api/Services/IDichVuEmail.cs`
- Create: `backend/HaloChat.Api/Services/DichVuEmail.cs`
- Create: `backend/HaloChat.Api.Tests/Fakes/DichVuEmailGiaLap.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (thêm `Configure<TuyChonSmtpEmail>` + đăng ký DI)
- Modify: `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs` (đăng ký fake)
- Modify: `backend/HaloChat.Api/HaloChat.Api.csproj` (thêm package MailKit)

**Interfaces:**
- Consumes: không có (task độc lập, không phụ thuộc Task 1).
- Produces: `IDichVuEmail.GuiEmailOtpAsync(string diaChiNhan, string maOtp): Task` — Task 3 (`DichVuNguoiDung`) tiêm và gọi method này.

- [ ] **Step 1: Thêm package MailKit**

Run: `cd backend/HaloChat.Api && dotnet add package MailKit`

(Không tự gõ số phiên bản — để `dotnet add package` tự chọn bản ổn định mới nhất và ghi vào `.csproj`. Sau khi chạy, mở `HaloChat.Api.csproj` để xác nhận dòng `<PackageReference Include="MailKit" Version="..." />` đã xuất hiện trong `<ItemGroup>` cạnh các package khác.)

- [ ] **Step 2: Tạo `TuyChonSmtpEmail`**

```csharp
namespace HaloChat.Api.Options;

public class TuyChonSmtpEmail
{
    public const string TenMuc = "SmtpEmail";

    public string MayChu { get; set; } = "smtp.gmail.com";
    public int Cong { get; set; } = 587;
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauUngDung { get; set; } = string.Empty;
    public string NguoiGuiHienThi { get; set; } = "HaloChat";
}
```

Lưu vào `backend/HaloChat.Api/Options/TuyChonSmtpEmail.cs`.

- [ ] **Step 3: Tạo `IDichVuEmail`**

```csharp
namespace HaloChat.Api.Services;

public interface IDichVuEmail
{
    Task GuiEmailOtpAsync(string diaChiNhan, string maOtp);
}
```

- [ ] **Step 4: Tạo `DichVuEmail`**

```csharp
using HaloChat.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HaloChat.Api.Services;

public class DichVuEmail : IDichVuEmail
{
    private readonly TuyChonSmtpEmail _tuyChon;

    public DichVuEmail(IOptions<TuyChonSmtpEmail> tuyChon)
    {
        _tuyChon = tuyChon.Value;
    }

    public async Task GuiEmailOtpAsync(string diaChiNhan, string maOtp)
    {
        var thongDiep = new MimeMessage();
        thongDiep.From.Add(new MailboxAddress(_tuyChon.NguoiGuiHienThi, _tuyChon.TenDangNhap));
        thongDiep.To.Add(MailboxAddress.Parse(diaChiNhan));
        thongDiep.Subject = "Mã OTP đặt lại mật khẩu HaloChat";
        thongDiep.Body = new TextPart("plain")
        {
            Text =
                $"Xin chào,\n\n" +
                $"Mã OTP để đặt lại mật khẩu HaloChat của bạn là: {maOtp}\n\n" +
                "Mã này có hiệu lực trong 10 phút. Không chia sẻ mã này với bất kỳ ai.\n\n" +
                "Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.",
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_tuyChon.MayChu, _tuyChon.Cong, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_tuyChon.TenDangNhap, _tuyChon.MatKhauUngDung);
        await client.SendAsync(thongDiep);
        await client.DisconnectAsync(true);
    }
}
```

- [ ] **Step 5: Đăng ký cấu hình + DI trong `Program.cs`**

Thêm cạnh dòng `builder.Services.Configure<TuyChonJwt>(...)`:

```csharp
builder.Services.Configure<TuyChonSmtpEmail>(builder.Configuration.GetSection(TuyChonSmtpEmail.TenMuc));
builder.Services.AddScoped<IDichVuEmail, DichVuEmail>();
```

Thêm `using HaloChat.Api.Options;` đã có sẵn ở đầu file — không cần thêm mới. Không cần `using MailKit`/`using MimeKit` trong `Program.cs` (chỉ dùng ở `DichVuEmail.cs`).

- [ ] **Step 6: Tạo fake cho test**

```csharp
using HaloChat.Api.Services;

namespace HaloChat.Api.Tests.Fakes;

public class DichVuEmailGiaLap : IDichVuEmail
{
    public List<(string DiaChiNhan, string MaOtp)> DaGui { get; } = new();

    public Task GuiEmailOtpAsync(string diaChiNhan, string maOtp)
    {
        DaGui.Add((diaChiNhan, maOtp));
        return Task.CompletedTask;
    }
}
```

Lưu vào `backend/HaloChat.Api.Tests/Fakes/DichVuEmailGiaLap.cs`.

- [ ] **Step 7: Đăng ký fake trong fixture kiểm thử**

Trong `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`, thêm property:

```csharp
    public DichVuEmailGiaLap KhoEmailGiaLap { get; } = new();
```

Và trong `ConfigureWebHost`, thêm:

```csharp
            dichVu.RemoveAll<IDichVuEmail>();
            dichVu.AddSingleton<IDichVuEmail>(KhoEmailGiaLap);
```

(Thêm `using HaloChat.Api.Services;` ở đầu file nếu chưa có — kiểm tra trước khi thêm trùng.)

- [ ] **Step 8: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi. Nếu lỗi vì thiếu `using MailKit.Net.Smtp;`/`MailKit.Security`/`MimeKit`, xác nhận namespace chính xác từ package MailKit vừa cài (namespace có thể lệch nhẹ theo phiên bản — sửa theo lỗi biên dịch báo, không đoán mò).

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass (chưa có test nào gọi `IDichVuEmail` trực tiếp ở task này — task chỉ dựng hạ tầng, Task 3 mới dùng tới).

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "Them IDichVuEmail (MailKit + Gmail SMTP) gui OTP qua email"
```

---

## Task 3: `DichVuNguoiDung` + `NguoiDungController` — 2 endpoint quên mật khẩu

**Files:**
- Create: `backend/HaloChat.Api/Dto/QuenMatKhauRequest.cs`
- Create: `backend/HaloChat.Api/Dto/DatLaiMatKhauRequest.cs`
- Create: `backend/HaloChat.Api/Dto/KetQuaDatLaiMatKhauDto.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs` (cập nhật helper `TaoDichVu()` cho constructor mới + thêm test)
- Modify: `backend/HaloChat.Api.Tests/NguoiDungControllerTests.cs` (thêm test tích hợp)

**Interfaces:**
- Consumes: `INguoiDungRepository.LuuOtpAsync`/`TangSoLanThuSaiOtpAsync`/`DatLaiMatKhauAsync` (Task 1), `IDichVuEmail.GuiEmailOtpAsync` (Task 2), `IDichVuMatKhau.BamMatKhau`/`TaoSalt` (đã có).
- Produces: `IDichVuNguoiDung.YeuCauOtpDatLaiMatKhauAsync(string email): Task`, `IDichVuNguoiDung.DatLaiMatKhauAsync(string email, string maOtp, string matKhauMoi): Task<KetQuaDatLaiMatKhauDto>` — Task 4 (frontend) không gọi trực tiếp interface này, chỉ gọi qua 2 endpoint REST bên dưới.

- [ ] **Step 1: Tạo 3 DTO**

`backend/HaloChat.Api/Dto/QuenMatKhauRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record QuenMatKhauRequest(
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    string Email);
```

`backend/HaloChat.Api/Dto/DatLaiMatKhauRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record DatLaiMatKhauRequest(
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    string Email,
    [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP phải gồm đúng 6 chữ số.")]
    string MaOtp,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    string MatKhauMoi);
```

`backend/HaloChat.Api/Dto/KetQuaDatLaiMatKhauDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record KetQuaDatLaiMatKhauDto(bool ThanhCong, string ThongBao);
```

- [ ] **Step 2: Mở rộng `IDichVuNguoiDung`**

Thêm vào interface:

```csharp
    Task YeuCauOtpDatLaiMatKhauAsync(string email);
    Task<KetQuaDatLaiMatKhauDto> DatLaiMatKhauAsync(string email, string maOtp, string matKhauMoi);
```

- [ ] **Step 3: Cài đặt trong `DichVuNguoiDung`**

Đổi constructor để tiêm thêm `IDichVuEmail`:

```csharp
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;
    private readonly IDichVuJwt _dichVuJwt;
    private readonly IDichVuEmail _dichVuEmail;

    public DichVuNguoiDung(
        INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau, IDichVuJwt dichVuJwt, IDichVuEmail dichVuEmail)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
        _dichVuJwt = dichVuJwt;
        _dichVuEmail = dichVuEmail;
    }
```

Thêm 2 method mới + 1 helper riêng vào cuối class (trước dấu `}` cuối):

```csharp
    private const int HetHanOtpPhut = 10;
    private const int CooldownOtpGiay = 60;
    private const int SoLanSaiToiDa = 5;

    public async Task YeuCauOtpDatLaiMatKhauAsync(string email)
    {
        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(email);
        if (nguoiDung is null)
        {
            return; // Không tiết lộ email không tồn tại — controller luôn trả thông báo chung.
        }

        if (nguoiDung.MaOtpGuiLucNao is not null &&
            (DateTime.UtcNow - nguoiDung.MaOtpGuiLucNao.Value).TotalSeconds < CooldownOtpGiay)
        {
            return; // Chặn spam gửi lại liên tục — vẫn không tiết lộ gì ra ngoài.
        }

        var maOtp = TaoMaOtp();
        var maOtpBam = _dichVuMatKhau.BamMatKhau(maOtp, nguoiDung.Salt);
        var hetHan = DateTime.UtcNow.AddMinutes(HetHanOtpPhut);
        var guiLucNao = DateTime.UtcNow;

        await _kho.LuuOtpAsync(nguoiDung.Id, maOtpBam, hetHan, guiLucNao);
        await _dichVuEmail.GuiEmailOtpAsync(nguoiDung.Email, maOtp);
    }

    public async Task<KetQuaDatLaiMatKhauDto> DatLaiMatKhauAsync(string email, string maOtp, string matKhauMoi)
    {
        const string ThongBaoOtpKhongDung = "Mã OTP không đúng.";
        const string ThongBaoOtpHetHan = "Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới.";

        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(email);
        if (nguoiDung is null || nguoiDung.MaOtpBam is null || nguoiDung.MaOtpHetHan is null)
        {
            return new KetQuaDatLaiMatKhauDto(false, ThongBaoOtpHetHan);
        }

        if (nguoiDung.MaOtpHetHan.Value < DateTime.UtcNow || nguoiDung.SoLanThuSai >= SoLanSaiToiDa)
        {
            return new KetQuaDatLaiMatKhauDto(false, ThongBaoOtpHetHan);
        }

        var maOtpBamNhapVao = _dichVuMatKhau.BamMatKhau(maOtp, nguoiDung.Salt);
        if (maOtpBamNhapVao != nguoiDung.MaOtpBam)
        {
            await _kho.TangSoLanThuSaiOtpAsync(nguoiDung.Id);
            return new KetQuaDatLaiMatKhauDto(false, ThongBaoOtpKhongDung);
        }

        var saltMoi = _dichVuMatKhau.TaoSalt();
        var matKhauBamMoi = _dichVuMatKhau.BamMatKhau(matKhauMoi, saltMoi);
        await _kho.DatLaiMatKhauAsync(nguoiDung.Id, matKhauBamMoi, saltMoi);

        return new KetQuaDatLaiMatKhauDto(true, "Đặt lại mật khẩu thành công.");
    }

    private static string TaoMaOtp()
    {
        var so = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1_000_000);
        return so.ToString("D6");
    }
```

Thêm `using HaloChat.Api.Services;` KHÔNG cần (cùng namespace) — nhưng cần đảm bảo `IDichVuEmail` (cùng namespace `HaloChat.Api.Services`) resolve được, không cần using thêm vì `DichVuNguoiDung.cs` đã ở namespace đó.

- [ ] **Step 4: Thêm 2 action vào `NguoiDungController`**

Thêm vào cuối class (trước dấu `}` cuối), KHÔNG có `[Authorize]` (đây là luồng công khai cho người chưa đăng nhập):

```csharp
    [HttpPost("quen-mat-khau")]
    public async Task<IActionResult> QuenMatKhau([FromBody] QuenMatKhauRequest yeuCau)
    {
        await _dichVu.YeuCauOtpDatLaiMatKhauAsync(yeuCau.Email);
        return Ok(new { thongBao = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi." });
    }

    [HttpPost("dat-lai-mat-khau")]
    public async Task<IActionResult> DatLaiMatKhau([FromBody] DatLaiMatKhauRequest yeuCau)
    {
        var ketQua = await _dichVu.DatLaiMatKhauAsync(yeuCau.Email, yeuCau.MaOtp, yeuCau.MatKhauMoi);
        if (!ketQua.ThanhCong)
        {
            return BadRequest(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }
```

- [ ] **Step 5: Cập nhật helper trong `DichVuNguoiDungTests.cs`**

Đổi `TaoDichVu()` để tạo và trả về thêm fake email:

```csharp
    private static (DichVuNguoiDung DichVu, NguoiDungGiaLap Kho, DichVuEmailGiaLap Email) TaoDichVu()
    {
        var kho = new NguoiDungGiaLap();
        var email = new DichVuEmailGiaLap();
        var dichVuJwt = new DichVuJwt(Microsoft.Extensions.Options.Options.Create(new TuyChonJwt
        {
            ChuoiBiMat = "khoa-bi-mat-du-dai-danh-cho-kiem-thu-toi-thieu-32-ky-tu",
        }));
        var dichVu = new DichVuNguoiDung(kho, new DichVuMatKhau(), dichVuJwt, email);
        return (dichVu, kho, email);
    }
```

Cập nhật MỌI lời gọi hiện có `var (dichVu, kho) = TaoDichVu();` trong file thành `var (dichVu, kho, _) = TaoDichVu();` (dùng `_` bỏ qua vì các test cũ không cần email). Thêm `using HaloChat.Api.Tests.Fakes;` nếu chưa có ở đầu file (đã có sẵn theo Read trước đó).

Thêm các test mới vào cuối class:

```csharp
    [Fact]
    public async Task YeuCauOtp_EmailTonTai_GuiEmailVaLuuOtp()
    {
        var (dichVu, kho, email) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser1", Email = "otpuser1@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);

        await dichVu.YeuCauOtpDatLaiMatKhauAsync("otpuser1@gmail.com");

        Assert.NotNull(nguoiDung.MaOtpBam);
        Assert.NotNull(nguoiDung.MaOtpHetHan);
        Assert.Single(email.DaGui);
        Assert.Equal("otpuser1@gmail.com", email.DaGui[0].DiaChiNhan);
        Assert.Matches(@"^\d{6}$", email.DaGui[0].MaOtp);
    }

    [Fact]
    public async Task YeuCauOtp_EmailKhongTonTai_KhongGuiEmailKhongNemLoi()
    {
        var (dichVu, _, email) = TaoDichVu();

        await dichVu.YeuCauOtpDatLaiMatKhauAsync("khong-ton-tai@gmail.com");

        Assert.Empty(email.DaGui);
    }

    [Fact]
    public async Task YeuCauOtp_ConTrongCooldown_KhongGuiLaiEmail()
    {
        var (dichVu, kho, email) = TaoDichVu();
        var nguoiDung = new NguoiDung
        {
            TenTaiKhoan = "otpuser2", Email = "otpuser2@gmail.com", Salt = "salt",
            MaOtpGuiLucNao = DateTime.UtcNow,
        };
        kho.DanhSach.Add(nguoiDung);

        await dichVu.YeuCauOtpDatLaiMatKhauAsync("otpuser2@gmail.com");

        Assert.Empty(email.DaGui);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpDungConHieuLuc_DoiMatKhauThanhCong()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser3", Email = "otpuser3@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(5);

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser3@gmail.com", "123456", "MatKhauMoi123");

        Assert.True(ketQua.ThanhCong);
        Assert.True(matKhau.KiemTraMatKhau("MatKhauMoi123", nguoiDung.Salt, nguoiDung.MatKhauBam));
        Assert.Null(nguoiDung.MaOtpBam);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpSai_TangSoLanThuSaiVaTraVeThatBai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser4", Email = "otpuser4@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(5);

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser4@gmail.com", "000000", "MatKhauMoi123");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal("Mã OTP không đúng.", ketQua.ThongBao);
        Assert.Equal(1, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpDaHetHan_TraVeThatBaiKhongTangSoLanThuSai()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung { TenTaiKhoan = "otpuser5", Email = "otpuser5@gmail.com", Salt = "salt" };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(-1); // đã hết hạn 1 phút trước

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser5@gmail.com", "123456", "MatKhauMoi123");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal("Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới.", ketQua.ThongBao);
        Assert.Equal(0, nguoiDung.SoLanThuSai);
    }

    [Fact]
    public async Task DatLaiMatKhau_QuaSoLanSaiToiDa_TuChoiDuKhiOtpDung()
    {
        var (dichVu, kho, _) = TaoDichVu();
        var nguoiDung = new NguoiDung
        {
            TenTaiKhoan = "otpuser6", Email = "otpuser6@gmail.com", Salt = "salt", SoLanThuSai = 5,
        };
        kho.DanhSach.Add(nguoiDung);
        var matKhau = new DichVuMatKhau();
        nguoiDung.MaOtpBam = matKhau.BamMatKhau("123456", nguoiDung.Salt);
        nguoiDung.MaOtpHetHan = DateTime.UtcNow.AddMinutes(5);

        var ketQua = await dichVu.DatLaiMatKhauAsync("otpuser6@gmail.com", "123456", "MatKhauMoi123");

        Assert.False(ketQua.ThanhCong);
        Assert.Equal("Mã OTP đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại mã mới.", ketQua.ThongBao);
    }
```

- [ ] **Step 6: Thêm test tích hợp vào `NguoiDungControllerTests.cs`**

```csharp
    [Fact]
    public async Task QuenMatKhau_EmailTonTai_TraVe200VaGuiEmail()
    {
        var yeuCauDangKy = TaoYeuCauDangKyHopLe("quenmk1");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCauDangKy);

        var phanHoi = await _client.PostAsJsonAsync("/api/nguoidung/quen-mat-khau", new QuenMatKhauRequest(yeuCauDangKy.Email));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.Single(_factory.KhoEmailGiaLap.DaGui);
    }

    [Fact]
    public async Task QuenMatKhau_EmailKhongTonTai_VanTraVe200KhongGuiEmail()
    {
        var phanHoi = await _client.PostAsJsonAsync("/api/nguoidung/quen-mat-khau", new QuenMatKhauRequest("khong-ton-tai-thu@gmail.com"));

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.Empty(_factory.KhoEmailGiaLap.DaGui);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpDung_DangNhapDuocVoiMatKhauMoi()
    {
        var yeuCauDangKy = TaoYeuCauDangKyHopLe("quenmk2");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCauDangKy);
        await _client.PostAsJsonAsync("/api/nguoidung/quen-mat-khau", new QuenMatKhauRequest(yeuCauDangKy.Email));
        var maOtp = _factory.KhoEmailGiaLap.DaGui.Single().MaOtp;

        var phanHoiDatLai = await _client.PostAsJsonAsync(
            "/api/nguoidung/dat-lai-mat-khau",
            new DatLaiMatKhauRequest(yeuCauDangKy.Email, maOtp, "MatKhauMoi123"));
        Assert.Equal(HttpStatusCode.OK, phanHoiDatLai.StatusCode);

        var phanHoiDangNhap = await _client.PostAsJsonAsync(
            "/api/nguoidung/dang-nhap", new DangNhapRequest(yeuCauDangKy.TenTaiKhoan, "MatKhauMoi123"));
        Assert.Equal(HttpStatusCode.OK, phanHoiDangNhap.StatusCode);
    }

    [Fact]
    public async Task DatLaiMatKhau_OtpSai_TraVe400()
    {
        var yeuCauDangKy = TaoYeuCauDangKyHopLe("quenmk3");
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", yeuCauDangKy);
        await _client.PostAsJsonAsync("/api/nguoidung/quen-mat-khau", new QuenMatKhauRequest(yeuCauDangKy.Email));

        var phanHoi = await _client.PostAsJsonAsync(
            "/api/nguoidung/dat-lai-mat-khau",
            new DatLaiMatKhauRequest(yeuCauDangKy.Email, "000000", "MatKhauMoi123"));

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }
```

Thêm `_factory.KhoEmailGiaLap.DaGui.Clear();` vào constructor của `NguoiDungControllerTests` (cạnh `_factory.KhoGiaLap.DanhSach.Clear();` đã có) để mỗi test bắt đầu sạch.

- [ ] **Step 7: Build và test**

Run: `dotnet build backend/HaloChat.sln`
Expected: 0 lỗi.

Run: `dotnet test backend/HaloChat.sln`
Expected: toàn bộ pass, bao gồm 7 test mới ở `DichVuNguoiDungTests.cs` và 4 test mới ở `NguoiDungControllerTests.cs`. Đây là task backend cuối cùng — sau bước này API quên mật khẩu đã hoàn chỉnh.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Them 2 endpoint quen-mat-khau/dat-lai-mat-khau vao NguoiDungController"
```

---

## Task 4: Frontend — mở rộng `DichVuApi.ts`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/DichVuApi.test.ts`

**Interfaces:**
- Consumes: 2 endpoint REST từ Task 3.
- Produces: `KetQuaThongBao { thongBao: string }` (kiểu dùng chung mới), `GuiYeuCauQuenMatKhau(email: string): Promise<KetQuaThongBao>`, `DatLaiMatKhau(email: string, maOtp: string, matKhauMoi: string): Promise<KetQuaThongBao>` — Task 5 (trang frontend) gọi đúng 2 hàm này.

- [ ] **Step 1: Thêm kiểu `KetQuaThongBao` vào `KieuDuLieu.ts`**

Thêm vào cuối file:

```ts
export interface KetQuaThongBao {
  thongBao: string;
}
```

- [ ] **Step 2: Thêm 2 hàm vào `DichVuApi.ts`**

Thêm `KetQuaThongBao` vào khối `import type { ... } from './KieuDuLieu';` ở đầu file, rồi thêm 2 hàm vào cuối file:

```ts
export async function GuiYeuCauQuenMatKhau(email: string): Promise<KetQuaThongBao> {
  return goiApi<KetQuaThongBao>('/nguoidung/quen-mat-khau', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  });
}

export async function DatLaiMatKhau(email: string, maOtp: string, matKhauMoi: string): Promise<KetQuaThongBao> {
  return goiApi<KetQuaThongBao>('/nguoidung/dat-lai-mat-khau', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, maOtp, matKhauMoi }),
  });
}
```

- [ ] **Step 3: Thêm test vào `DichVuApi.test.ts`**

Thêm `GuiYeuCauQuenMatKhau, DatLaiMatKhau` vào `import { ... } from './DichVuApi';` ở đầu file, rồi thêm 2 test:

```ts
  it('GuiYeuCauQuenMatKhau gửi đúng POST với body email', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ thongBao: 'Nếu email tồn tại trong hệ thống, mã OTP đã được gửi.' }), { status: 200 }),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const ketQua = await GuiYeuCauQuenMatKhau('a@gmail.com');

    expect(ketQua.thongBao).toBe('Nếu email tồn tại trong hệ thống, mã OTP đã được gửi.');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung/quen-mat-khau'),
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ email: 'a@gmail.com' }) }),
    );
  });

  it('DatLaiMatKhau gửi đúng POST và ném LoiGoiApi khi OTP sai (400)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ thongBao: 'Mã OTP không đúng.' }), { status: 400 })),
    );

    await expect(DatLaiMatKhau('a@gmail.com', '000000', 'MatKhauMoi123')).rejects.toMatchObject({
      trangThai: 400,
      message: 'Mã OTP không đúng.',
    });
  });
```

- [ ] **Step 4: Build và test**

Run: `cd frontend && npm run test -- --run`
Expected: toàn bộ pass.

Run: `cd frontend && npm run build`
Expected: build thành công.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Frontend: mo rong DichVuApi cho quen mat khau (GuiYeuCauQuenMatKhau, DatLaiMatKhau)"
```

---

## Task 5: Frontend — `TrangQuenMatKhau.tsx` + route `/quen-mat-khau` + link từ đăng nhập

**Files:**
- Create: `frontend/src/Trang/TrangQuenMatKhau.tsx`
- Create: `frontend/src/Trang/TrangQuenMatKhau.test.tsx`
- Modify: `frontend/src/Trang/TrangDangNhap.tsx` (thêm link)
- Modify: `frontend/src/DinhTuyen.tsx` (thêm route)

**Interfaces:**
- Consumes: `GuiYeuCauQuenMatKhau`, `DatLaiMatKhau` (Task 4), `KhungXacThuc`/`TruongNhap`/`BieuTuongEmail`/`BieuTuongKhoa`/`BieuTuongMuiTen` (đã có, dùng lại nguyên trạng từ `TrangDangNhap.tsx`/`TrangDangKy.tsx`).
- Produces: không có (task cuối cùng của plan).

- [ ] **Step 1: Tạo `TrangQuenMatKhau.tsx`**

```tsx
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { GuiYeuCauQuenMatKhau, DatLaiMatKhau, LoiGoiApi } from '../DichVuApi';
import { KhungXacThuc } from '../ThanhPhan/KhungXacThuc';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongEmail, BieuTuongKhoa, BieuTuongMuiTen } from '../ThanhPhan/BieuTuong';

export function TrangQuenMatKhau() {
  const [email, setEmail] = useState('');
  const [daGuiOtp, setDaGuiOtp] = useState(false);
  const [maOtp, setMaOtp] = useState('');
  const [matKhauMoi, setMatKhauMoi] = useState('');
  const [thongBao, setThongBao] = useState<string | null>(null);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);
  const dieuHuong = useNavigate();

  async function xuLyGuiOtp(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setThongBao(null);
    setDangGui(true);
    try {
      const ketQua = await GuiYeuCauQuenMatKhau(email);
      setThongBao(ketQua.thongBao);
      setDaGuiOtp(true);
    } catch (loiBat) {
      setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  async function xuLyDatLaiMatKhau(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setDangGui(true);
    try {
      await DatLaiMatKhau(email, maOtp, matKhauMoi);
      dieuHuong('/dang-nhap', { state: { thongBaoDatLaiMatKhau: 'Đặt lại mật khẩu thành công. Vui lòng đăng nhập.' } });
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  return (
    <KhungXacThuc>
      <form onSubmit={daGuiOtp ? xuLyDatLaiMatKhau : xuLyGuiOtp}>
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        {thongBao && !loi && <p className="thong-bao-thanh-cong">{thongBao}</p>}

        <TruongNhap
          nhan="Email"
          bieuTuong={<BieuTuongEmail />}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Nhập email đã đăng ký..."
          required
          disabled={daGuiOtp}
        />

        {!daGuiOtp && (
          <button type="submit" className="nut-chinh" disabled={dangGui}>
            Gửi mã OTP <BieuTuongMuiTen />
          </button>
        )}

        {daGuiOtp && (
          <>
            <TruongNhap
              nhan="Mã OTP"
              bieuTuong={<BieuTuongKhoa />}
              value={maOtp}
              onChange={(e) => setMaOtp(e.target.value)}
              placeholder="Nhập mã 6 số..."
              required
            />
            <TruongNhap
              nhan="Mật khẩu mới"
              bieuTuong={<BieuTuongKhoa />}
              coTheAn
              value={matKhauMoi}
              onChange={(e) => setMatKhauMoi(e.target.value)}
              placeholder="Nhập mật khẩu mới..."
              required
            />
            <button type="submit" className="nut-chinh" disabled={dangGui}>
              Đặt lại mật khẩu <BieuTuongMuiTen />
            </button>
            <button
              type="button"
              className="nut-phu"
              onClick={() => {
                setDaGuiOtp(false);
                setThongBao(null);
              }}
              disabled={dangGui}
            >
              Sửa lại email / gửi lại mã
            </button>
          </>
        )}
      </form>
    </KhungXacThuc>
  );
}
```

Ghi chú implementer: nếu `className="nut-phu"` chưa có style sẵn trong `index.css`/`KhungXacThuc.css` (kiểm tra bằng `grep -rn "nut-phu" frontend/src`), không cần thêm CSS mới cho nó — để mặc định trình duyệt là đủ cho task này (không thuộc yêu cầu thiết kế nào của module này). Tương tự với `className="thong-bao-thanh-cong"` — nếu chưa có CSS, dùng luôn `className="thong-bao-loi"` nhưng đổi màu bằng style inline `style={{ color: 'var(--mau-chinh-dam)' }}` KHÔNG cần — đơn giản nhất là bỏ class riêng, dùng thẻ `<p>` trơn cho dòng thông báo thành công vì đây không phải trọng tâm task (chức năng đúng quan trọng hơn màu sắc).

- [ ] **Step 2: Thêm link vào `TrangDangNhap.tsx`**

Thêm ngay sau thẻ `</form>` đóng, bên trong `<KhungXacThuc>`:

```tsx
        <p className="lien-ket-phu">
          <Link to="/quen-mat-khau">Quên mật khẩu?</Link>
        </p>
```

Thêm `Link` vào import: đổi dòng `import { useNavigate } from 'react-router-dom';` thành `import { useNavigate, Link } from 'react-router-dom';`.

Ghi chú: nếu `className="lien-ket-phu"` chưa tồn tại trong CSS, bỏ class đó đi, chỉ giữ `<p><Link to="/quen-mat-khau">Quên mật khẩu?</Link></p>` — không thêm CSS mới cho task này (ngoài phạm vi, chỉ cần link hoạt động đúng).

- [ ] **Step 3: Thêm route vào `DinhTuyen.tsx`**

Thêm import: `import { TrangQuenMatKhau } from './Trang/TrangQuenMatKhau';`

Thêm route (route công khai, KHÔNG bọc `TuyenDuongRieng`/`KhungChinh`, đặt cạnh `/dang-nhap`):

```tsx
      <Route path="/quen-mat-khau" element={<TrangQuenMatKhau />} />
```

- [ ] **Step 4: Viết test cho `TrangQuenMatKhau`**

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangQuenMatKhau } from './TrangQuenMatKhau';
import * as DichVuApi from '../DichVuApi';

function renderVoiRouter() {
  return render(
    <MemoryRouter initialEntries={['/quen-mat-khau']}>
      <Routes>
        <Route path="/quen-mat-khau" element={<TrangQuenMatKhau />} />
        <Route path="/dang-nhap" element={<div>Trang đăng nhập</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('TrangQuenMatKhau', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('gửi email thành công hiện thêm ô nhập OTP và mật khẩu mới', async () => {
    vi.spyOn(DichVuApi, 'GuiYeuCauQuenMatKhau').mockResolvedValue({
      thongBao: 'Nếu email tồn tại trong hệ thống, mã OTP đã được gửi.',
    });

    renderVoiRouter();
    await userEvent.type(screen.getByLabelText('Email'), 'a@gmail.com');
    await userEvent.click(screen.getByRole('button', { name: /Gửi mã OTP/ }));

    expect(await screen.findByLabelText('Mã OTP')).toBeInTheDocument();
    expect(screen.getByLabelText('Mật khẩu mới')).toBeInTheDocument();
    expect(DichVuApi.GuiYeuCauQuenMatKhau).toHaveBeenCalledWith('a@gmail.com');
  });

  it('đặt lại mật khẩu thành công điều hướng sang /dang-nhap', async () => {
    vi.spyOn(DichVuApi, 'GuiYeuCauQuenMatKhau').mockResolvedValue({ thongBao: 'Đã gửi.' });
    vi.spyOn(DichVuApi, 'DatLaiMatKhau').mockResolvedValue({ thongBao: 'Đặt lại mật khẩu thành công.' });

    renderVoiRouter();
    await userEvent.type(screen.getByLabelText('Email'), 'a@gmail.com');
    await userEvent.click(screen.getByRole('button', { name: /Gửi mã OTP/ }));
    await screen.findByLabelText('Mã OTP');

    await userEvent.type(screen.getByLabelText('Mã OTP'), '123456');
    await userEvent.type(screen.getByLabelText('Mật khẩu mới'), 'MatKhauMoi123');
    await userEvent.click(screen.getByRole('button', { name: /Đặt lại mật khẩu/ }));

    expect(await screen.findByText('Trang đăng nhập')).toBeInTheDocument();
    expect(DichVuApi.DatLaiMatKhau).toHaveBeenCalledWith('a@gmail.com', '123456', 'MatKhauMoi123');
  });

  it('OTP sai hiển thị lỗi, không điều hướng', async () => {
    vi.spyOn(DichVuApi, 'GuiYeuCauQuenMatKhau').mockResolvedValue({ thongBao: 'Đã gửi.' });
    vi.spyOn(DichVuApi, 'DatLaiMatKhau').mockRejectedValue(new Error('Mã OTP không đúng.'));

    renderVoiRouter();
    await userEvent.type(screen.getByLabelText('Email'), 'a@gmail.com');
    await userEvent.click(screen.getByRole('button', { name: /Gửi mã OTP/ }));
    await screen.findByLabelText('Mã OTP');

    await userEvent.type(screen.getByLabelText('Mã OTP'), '000000');
    await userEvent.type(screen.getByLabelText('Mật khẩu mới'), 'MatKhauMoi123');
    await userEvent.click(screen.getByRole('button', { name: /Đặt lại mật khẩu/ }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Mã OTP không đúng.');
  });
});
```

- [ ] **Step 5: Cập nhật `TrangDangNhap.test.tsx` nếu cần**

Kiểm tra file này trước khi sửa (`grep -n "getByRole\|queryAllByRole" frontend/src/Trang/TrangDangNhap.test.tsx`) — nếu có assertion nào đếm số lượng link/button cụ thể trên trang mà việc thêm link "Quên mật khẩu?" làm sai lệch (ví dụ `getAllByRole('link')` mong đợi đúng 0 link), sửa lại cho khớp. Theo nội dung đã đọc ở trên, file hiện tại KHÔNG có assertion như vậy — nhiều khả năng không cần sửa gì, chỉ cần chạy lại test để xác nhận.

- [ ] **Step 6: Build và test**

Run: `cd frontend && npm run test -- --run`
Expected: toàn bộ pass, bao gồm 3 test mới ở `TrangQuenMatKhau.test.tsx`.

Run: `cd frontend && npm run build`
Expected: build thành công. Đây là task cuối cùng của plan — sau bước này module Quên mật khẩu hoàn chỉnh cả backend lẫn frontend.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Frontend: them TrangQuenMatKhau + route /quen-mat-khau + link tu trang dang nhap"
```

---

## Sau khi hoàn thành cả 5 task

1. Dùng **superpowers:finishing-a-development-branch** để quyết định push lên `origin/main` (dự án làm việc trực tiếp trên `main`, không dùng worktree/branch riêng, theo thói quen đã thiết lập).
2. **Cấu hình secret thật** (KHÔNG có trong bất kỳ commit nào, làm thủ công sau khi code đã lên `main`):
   - **Local (để `dotnet test`/chạy thử không cần Gmail thật vẫn qua được nhờ fake — nhưng nếu muốn chạy thật ở máy dev):**
     ```bash
     cd backend/HaloChat.Api
     dotnet user-secrets set "SmtpEmail:TenDangNhap" "halochatwebsite@gmail.com"
     dotnet user-secrets set "SmtpEmail:MatKhauUngDung" "<App Password 16 ký tự, không có khoảng trắng>"
     ```
   - **Render (production):** vào Render Dashboard → service backend → **Environment** → thêm 2 biến:
     - `SmtpEmail__TenDangNhap` = `halochatwebsite@gmail.com`
     - `SmtpEmail__MatKhauUngDung` = App Password (đúng App Password đã tạo, bỏ khoảng trắng)
   - Sau khi set xong trên Render, bấm **Manual Deploy → Deploy latest commit** để áp dụng.
3. Test thật trên `https://halochat.website`: bấm "Quên mật khẩu?" ở trang đăng nhập → nhập email đã đăng ký → kiểm tra hộp thư nhận được email chứa mã OTP → nhập OTP + mật khẩu mới → xác nhận đăng nhập lại được bằng mật khẩu mới.
