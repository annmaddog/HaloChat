# HaloChat Backend Nền Tảng — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng ASP.NET Core Web API (`backend/HaloChat.Api`) chạy được, kết nối MongoDB Atlas thật, và hoàn thiện GĐ3 của tài liệu (đăng ký, đăng nhập bằng JWT, danh sách người dùng) — kiểm chứng bằng Swagger và trực tiếp trên MongoDB Atlas.

**Architecture:** Controller-based ASP.NET Core Web API (.NET 9) dùng MongoDB.Driver chính thức, JWT Bearer cho xác thực. Logic nghiệp vụ nằm trong các Service (`DichVuMatKhau`, `DichVuJwt`, `DichVuNguoiDung`) phía sau interface, phụ thuộc vào `INguoiDungRepository` (không phụ thuộc trực tiếp MongoDB.Driver trong service) — nhờ vậy unit test dùng repository giả lập trong bộ nhớ, không cần Mongo thật khi chạy `dotnet test`. `HaloChat.Security` (thư viện mã hóa GĐ6) được scaffold sẵn với các stub `[BẢO MẬT - GĐ6]` nhưng chưa được gọi tới ở giai đoạn này.

**Tech Stack:** .NET 9 (net9.0), ASP.NET Core Web API (controllers), MongoDB.Driver, Swashbuckle.AspNetCore (Swagger UI), Microsoft.AspNetCore.Authentication.JwtBearer, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md`

## Global Constraints

- Toàn bộ code .NET nằm trong `backend/` (spec §3) — không tạo project ở gốc repo.
- Target framework: `net9.0` cho mọi project .NET (khớp SDK đã cài: 9.0.315).
- Không commit bí mật thật (chuỗi kết nối MongoDB, khóa ký JWT) vào bất kỳ file nào trong repo — dùng `dotnet user-secrets` (spec §3 "Cấu hình kết nối & bí mật").
- `KhoaCongKhai`/`KhoaBiMat` (RSA) của `NguoiDung` để trống ở giai đoạn này — sinh khóa và dùng khóa thuộc phạm vi GĐ6, không tự ý làm trước (spec §9).
- Mật khẩu: `SHA-256(MatKhau + Salt)` — băm thật ngay từ giai đoạn này, không phải chỗ stub (spec §9, đã xác nhận với người dùng).
- Đăng nhập chấp nhận cả `TenTaiKhoan` lẫn `Email` (spec §6).
- Quy ước đặt tên: biến camelCase / hàm & class PascalCase, tiếng Việt không dấu; giữ nguyên thuật ngữ kỹ thuật quen thuộc (JWT, MongoDB, SHA256...) (spec §12).

---

## Task 1: Scaffold solution & 3 project backend

**Files:**
- Create: `backend/HaloChat.sln`
- Create: `backend/HaloChat.Api/` (project ASP.NET Core Web API, template mặc định + dọn file mẫu)
- Create: `backend/HaloChat.Security/DichVuMaHoa.cs`
- Create: `backend/HaloChat.Api.Tests/` (project xUnit, template mặc định + dọn file mẫu)
- Modify: `.gitignore` (gốc repo)
- Delete: `HaloChat.sln` (file cũ ở gốc repo, không còn khớp cấu trúc mới)

**Interfaces:**
- Consumes: không có (task đầu tiên).
- Produces: solution `backend/HaloChat.sln` chạy build được; `HaloChat.Security.DichVuMaHoa` (namespace `HaloChat.Security`, 4 method stub `MaHoaTinNhan`, `GiaiMaTinNhan`, `MaHoaKhoaPhien`, `GiaiMaKhoaPhien` — chưa được gọi ở plan này). Task 2 xây dựng tiếp trên `HaloChat.Api`.

- [ ] **Bước 1: Xóa solution cũ ở gốc repo**

```bash
rm -f HaloChat.sln
```

- [ ] **Bước 2: Khởi tạo solution + 3 project trong `backend/`**

```bash
dotnet new sln -n HaloChat -o backend
dotnet new webapi -controllers -f net9.0 -n HaloChat.Api -o backend/HaloChat.Api
dotnet new classlib -f net9.0 -n HaloChat.Security -o backend/HaloChat.Security
dotnet new xunit -f net9.0 -n HaloChat.Api.Tests -o backend/HaloChat.Api.Tests
dotnet sln backend/HaloChat.sln add backend/HaloChat.Api/HaloChat.Api.csproj backend/HaloChat.Security/HaloChat.Security.csproj backend/HaloChat.Api.Tests/HaloChat.Api.Tests.csproj
dotnet add backend/HaloChat.Api.Tests/HaloChat.Api.Tests.csproj reference backend/HaloChat.Api/HaloChat.Api.csproj
dotnet add backend/HaloChat.Api/HaloChat.Api.csproj reference backend/HaloChat.Security/HaloChat.Security.csproj
```

- [ ] **Bước 3: Xóa file mẫu do template sinh ra**

```bash
rm backend/HaloChat.Api/WeatherForecast.cs
rm backend/HaloChat.Api/Controllers/WeatherForecastController.cs
rm backend/HaloChat.Security/Class1.cs
rm backend/HaloChat.Api.Tests/UnitTest1.cs
```

- [ ] **Bước 4: Tạo stub bảo mật GĐ6 trong `backend/HaloChat.Security/DichVuMaHoa.cs`**

```csharp
namespace HaloChat.Security;

public class DichVuMaHoa
{
    public string MaHoaTinNhan(string noiDungTinNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Mã hóa nội dung bằng AES-256-GCM: sinh Nonce ngẫu nhiên, mã hóa
        // noiDungTinNhan bằng AES Session Key, trả về Ciphertext kèm Nonce +
        // AuthTag (định dạng lưu trữ do nhóm quyết định).
        return "";
    }

    public string GiaiMaTinNhan(string tinNhanDaMaHoa)
    {
        // [BẢO MẬT - GĐ6]
        // Giải mã nội dung bằng AES-256-GCM, dùng lại Nonce + AuthTag đã lưu
        // kèm tin nhắn.
        return "";
    }

    public string MaHoaKhoaPhien(string khoaPhienAes, string khoaCongKhaiNguoiNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Mã hóa AES Session Key bằng RSA-OAEP, dùng Public Key của người
        // nhận, trước khi gửi lên Server (Server không cần đọc được
        // khoaPhienAes gốc).
        return "";
    }

    public string GiaiMaKhoaPhien(string khoaPhienDaMaHoa, string khoaBiMatNguoiNhan)
    {
        // [BẢO MẬT - GĐ6]
        // Giải mã AES Session Key bằng RSA Private Key của người nhận
        // (KhoaBiMat).
        return "";
    }
}
```

Không viết test cho file này ở plan này — đây là stub cố ý để trống theo spec §9, hành vi thật và test tương ứng do nhóm tự viết ở GĐ6.

- [ ] **Bước 5: Build toàn bộ solution**

Run: `dotnet build backend/HaloChat.sln`
Expected: `Build succeeded.` (0 Error)

- [ ] **Bước 6: Chạy thử API mặc định**

Run: `dotnet run --project backend/HaloChat.Api` (dùng timeout ngắn, đây là tiến trình chạy mãi — dừng sau khi xác nhận)
Expected: log có dòng `Now listening on:` — không có exception.

- [ ] **Bước 7: Tạo `.gitignore` ở gốc repo**

```gitignore
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user

# Node / frontend (thêm khi làm plan Frontend)
frontend/node_modules/
frontend/dist/

# Bí mật — không commit giá trị thật
**/appsettings.*.local.json
.env
.env.*

# Dữ liệu người dùng tải lên (thêm khi làm GĐ5)
backend/HaloChat.Api/uploads/

# Editor / OS
.vs/
.vscode/
*.DS_Store
```

- [ ] **Bước 8: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Scaffold backend: HaloChat.Api, HaloChat.Security, HaloChat.Api.Tests

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Kết nối MongoDB Atlas + Swagger + CORS + endpoint kiểm tra sức khỏe

**Files:**
- Create: `backend/HaloChat.Api/Options/TuyChonMongoDb.cs`
- Modify: `backend/HaloChat.Api/HaloChat.Api.csproj` (qua `dotnet add package`)
- Modify: `backend/HaloChat.Api/appsettings.json`
- Modify: `backend/HaloChat.Api/Program.cs`

**Interfaces:**
- Consumes: project `HaloChat.Api` từ Task 1.
- Produces: `HaloChat.Api.Options.TuyChonMongoDb { const string TenMuc = "MongoDb"; string ChuoiKetNoi; string TenCoSoDuLieu; }`; DI singleton `IMongoDatabase`; chính sách CORS tên `"ChoPhepFrontend"`; endpoint `GET /api/kiem-tra-suc-khoe`. Task 3 tiêu thụ `IMongoDatabase` qua constructor injection.

- [ ] **Bước 1: Thêm gói NuGet**

```bash
dotnet add backend/HaloChat.Api package MongoDB.Driver
dotnet add backend/HaloChat.Api package Swashbuckle.AspNetCore
```

- [ ] **Bước 2: Tạo `backend/HaloChat.Api/Options/TuyChonMongoDb.cs`**

```csharp
namespace HaloChat.Api.Options;

public class TuyChonMongoDb
{
    public const string TenMuc = "MongoDb";

    public string ChuoiKetNoi { get; set; } = string.Empty;
    public string TenCoSoDuLieu { get; set; } = string.Empty;
}
```

- [ ] **Bước 3: Cập nhật `backend/HaloChat.Api/appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDb": {
    "TenCoSoDuLieu": "HaloChat"
  },
  "Cors": {
    "NguonChoPhep": "http://localhost:5173"
  }
}
```

- [ ] **Bước 4: Khởi tạo user-secrets và đặt chuỗi kết nối MongoDB thật**

```bash
dotnet user-secrets init --project backend/HaloChat.Api
dotnet user-secrets set "MongoDb:ChuoiKetNoi" "<CHUOI_KET_NOI_MONGODB_ATLAS_THAT_CUA_BAN>" --project backend/HaloChat.Api
```

Thay `<CHUOI_KET_NOI_MONGODB_ATLAS_THAT_CUA_BAN>` bằng chuỗi dạng `mongodb+srv://<user>:<password>@<cluster>/?appName=...` lấy từ Atlas — dán trực tiếp vào lệnh CLI, **không** dán vào bất kỳ file nào trong repo.

- [ ] **Bước 5: Viết lại toàn bộ `backend/HaloChat.Api/Program.cs`**

```csharp
using HaloChat.Api.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<TuyChonMongoDb>(builder.Configuration.GetSection(TuyChonMongoDb.TenMuc));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var tuyChon = sp.GetRequiredService<IOptions<TuyChonMongoDb>>().Value;
    var client = new MongoClient(tuyChon.ChuoiKetNoi);
    return client.GetDatabase(tuyChon.TenCoSoDuLieu);
});

const string TenChinhSachCors = "ChoPhepFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(TenChinhSachCors, policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:NguonChoPhep"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(TenChinhSachCors);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/kiem-tra-suc-khoe", async (IMongoDatabase csdl) =>
{
    try
    {
        await csdl.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        return Results.Ok(new { ketNoiMongoDb = true });
    }
    catch (Exception loi)
    {
        return Results.Problem(detail: loi.Message, statusCode: 500);
    }
});

app.Run();
```

- [ ] **Bước 6: Build, chạy, kiểm tra kết nối Mongo thật**

Run: `dotnet build backend/HaloChat.sln` → Expected: Build succeeded.
Run: `dotnet run --project backend/HaloChat.Api`, sau đó gọi:
`curl -k https://localhost:<port>/api/kiem-tra-suc-khoe` (cổng lấy từ log `Now listening on:`)
Expected: `{"ketNoiMongoDb":true}` — xác nhận kết nối MongoDB Atlas thật thành công. Nếu lỗi, kiểm tra lại chuỗi kết nối trong user-secrets và whitelist IP trên Atlas.

- [ ] **Bước 7: Xác nhận Swagger UI hoạt động**

Mở `https://localhost:<port>/swagger` (hoặc `curl -k` cùng URL) → Expected: 200, trang Swagger UI hiển thị.

- [ ] **Bước 8: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Kết nối MongoDB Atlas + Swagger + CORS + endpoint kiểm tra sức khỏe

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

Lưu ý: `dotnet user-secrets` lưu ngoài repo — `git status` sẽ không thấy giá trị bí mật vừa đặt.

---

## Task 3: Đăng ký tài khoản (DangKyTaiKhoan)

**Files:**
- Create: `backend/HaloChat.Api/Models/NguoiDung.cs`
- Create: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Create: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Create: `backend/HaloChat.Api/Services/IDichVuMatKhau.cs`
- Create: `backend/HaloChat.Api/Services/DichVuMatKhau.cs`
- Create: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Create: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Create: `backend/HaloChat.Api/Dto/KetQuaDangKyDto.cs`
- Create: `backend/HaloChat.Api/Dto/DangKyTaiKhoanRequest.cs`
- Create: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Modify: `backend/HaloChat.Api/Program.cs`
- Create: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Create: `backend/HaloChat.Api.Tests/Services/DichVuMatKhauTests.cs`
- Create: `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`

**Interfaces:**
- Consumes: `IMongoDatabase` (Task 2).
- Produces: `NguoiDung` (model); `INguoiDungRepository { Task<bool> TonTaiTenTaiKhoanAsync(string), Task<bool> TonTaiEmailAsync(string), Task ThemMoiAsync(NguoiDung) }`; `IDichVuMatKhau { string TaoSalt(), string BamMatKhau(string,string), bool KiemTraMatKhau(string,string,string) }`; `IDichVuNguoiDung { Task<KetQuaDangKyDto> DangKyTaiKhoan(string,string,string) }`; `KetQuaDangKyDto(bool ThanhCong, string ThongBao)`. Task 4 mở rộng `INguoiDungRepository`, `IDichVuNguoiDung`, và `NguoiDungGiaLap`.

- [ ] **Bước 1: Viết test cho `DichVuMatKhau` trước**

Create `backend/HaloChat.Api.Tests/Services/DichVuMatKhauTests.cs`:

```csharp
using HaloChat.Api.Services;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuMatKhauTests
{
    private readonly DichVuMatKhau _dichVu = new();

    [Fact]
    public void TaoSalt_GoiHaiLan_TraVeHaiGiaTriKhacNhau()
    {
        var salt1 = _dichVu.TaoSalt();
        var salt2 = _dichVu.TaoSalt();

        Assert.NotEmpty(salt1);
        Assert.NotEqual(salt1, salt2);
    }

    [Fact]
    public void BamMatKhau_CungMatKhauVaSalt_TraVeCungKetQua()
    {
        var bam1 = _dichVu.BamMatKhau("MatKhau123", "salt-co-dinh");
        var bam2 = _dichVu.BamMatKhau("MatKhau123", "salt-co-dinh");

        Assert.Equal(bam1, bam2);
    }

    [Fact]
    public void BamMatKhau_SaltKhacNhau_TraVeKetQuaKhacNhau()
    {
        var bam1 = _dichVu.BamMatKhau("MatKhau123", "salt-1");
        var bam2 = _dichVu.BamMatKhau("MatKhau123", "salt-2");

        Assert.NotEqual(bam1, bam2);
    }

    [Fact]
    public void KiemTraMatKhau_DungMatKhau_TraVeTrue()
    {
        var salt = _dichVu.TaoSalt();
        var bam = _dichVu.BamMatKhau("MatKhau123", salt);

        Assert.True(_dichVu.KiemTraMatKhau("MatKhau123", salt, bam));
    }

    [Fact]
    public void KiemTraMatKhau_SaiMatKhau_TraVeFalse()
    {
        var salt = _dichVu.TaoSalt();
        var bam = _dichVu.BamMatKhau("MatKhau123", salt);

        Assert.False(_dichVu.KiemTraMatKhau("SaiRoi", salt, bam));
    }
}
```

- [ ] **Bước 2: Chạy test, xác nhận thất bại**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuMatKhauTests`
Expected: lỗi biên dịch "The type or namespace name 'DichVuMatKhau' could not be found".

- [ ] **Bước 3: Tạo `backend/HaloChat.Api/Services/IDichVuMatKhau.cs`**

```csharp
namespace HaloChat.Api.Services;

public interface IDichVuMatKhau
{
    string TaoSalt();
    string BamMatKhau(string matKhau, string salt);
    bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam);
}
```

- [ ] **Bước 4: Tạo `backend/HaloChat.Api/Services/DichVuMatKhau.cs`**

```csharp
using System.Security.Cryptography;
using System.Text;

namespace HaloChat.Api.Services;

public class DichVuMatKhau : IDichVuMatKhau
{
    public string TaoSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    public string BamMatKhau(string matKhau, string salt)
    {
        var duLieu = Encoding.UTF8.GetBytes(matKhau + salt);
        var bamBytes = SHA256.HashData(duLieu);
        return Convert.ToBase64String(bamBytes);
    }

    public bool KiemTraMatKhau(string matKhau, string salt, string matKhauBam)
    {
        var bamMoi = Convert.FromBase64String(BamMatKhau(matKhau, salt));
        var bamCu = Convert.FromBase64String(matKhauBam);

        if (bamMoi.Length != bamCu.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(bamMoi, bamCu);
    }
}
```

- [ ] **Bước 5: Chạy lại test, xác nhận pass**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuMatKhauTests`
Expected: 5 passed.

- [ ] **Bước 6: Tạo model `backend/HaloChat.Api/Models/NguoiDung.cs`**

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class NguoiDung
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string TenTaiKhoan { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MatKhauBam { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    // [BẢO MẬT - GĐ6] Cặp khóa RSA (KhoaCongKhai/KhoaBiMat) sẽ được sinh và
    // gán vào đây khi nhóm triển khai mã hóa lai RSA-AES. Để trống ở giai
    // đoạn này theo đúng chính sách stub trong spec (§9).
    public string KhoaCongKhai { get; set; } = string.Empty;
    public string KhoaBiMat { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Bước 7: Tạo `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`**

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan);
    Task<bool> TonTaiEmailAsync(string email);
    Task ThemMoiAsync(NguoiDung nguoiDung);
}
```

- [ ] **Bước 8: Tạo `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`**

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class NguoiDungRepository : INguoiDungRepository
{
    private readonly IMongoCollection<NguoiDung> _collection;

    public NguoiDungRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<NguoiDung>("NguoiDung");
    }

    public async Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan)
    {
        return await _collection.Find(nd => nd.TenTaiKhoan == tenTaiKhoan).AnyAsync();
    }

    public async Task<bool> TonTaiEmailAsync(string email)
    {
        return await _collection.Find(nd => nd.Email == email).AnyAsync();
    }

    public async Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        await _collection.InsertOneAsync(nguoiDung);
    }
}
```

- [ ] **Bước 9: Tạo repository giả lập cho test — `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`**

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NguoiDungGiaLap : INguoiDungRepository
{
    public List<NguoiDung> DanhSach { get; } = new();

    public Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan) =>
        Task.FromResult(DanhSach.Any(nd => nd.TenTaiKhoan == tenTaiKhoan));

    public Task<bool> TonTaiEmailAsync(string email) =>
        Task.FromResult(DanhSach.Any(nd => nd.Email == email));

    public Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        DanhSach.Add(nguoiDung);
        return Task.CompletedTask;
    }
}
```

- [ ] **Bước 10: Viết test cho `DichVuNguoiDung.DangKyTaiKhoan` trước**

Create `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNguoiDungTests
{
    private static (DichVuNguoiDung DichVu, NguoiDungGiaLap Kho) TaoDichVu()
    {
        var kho = new NguoiDungGiaLap();
        var dichVu = new DichVuNguoiDung(kho, new DichVuMatKhau());
        return (dichVu, kho);
    }

    [Fact]
    public async Task DangKyTaiKhoan_TenTaiKhoanDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "NguyenAn", Email = "khac@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_EmailDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "Khac", Email = "nguyenan@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_HopLe_LuuMatKhauDaBamKhongLuuBanRo()
    {
        var (dichVu, kho) = TaoDichVu();

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.True(ketQua.ThanhCong);
        var daLuu = Assert.Single(kho.DanhSach);
        Assert.Equal("NguyenAn", daLuu.TenTaiKhoan);
        Assert.NotEqual("MatKhau123", daLuu.MatKhauBam);
        Assert.NotEmpty(daLuu.Salt);
    }
}
```

- [ ] **Bước 11: Chạy test, xác nhận thất bại**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuNguoiDungTests`
Expected: lỗi biên dịch do thiếu `IDichVuNguoiDung`/`DichVuNguoiDung`/`KetQuaDangKyDto`.

- [ ] **Bước 12: Tạo `backend/HaloChat.Api/Dto/KetQuaDangKyDto.cs`**

```csharp
namespace HaloChat.Api.Dto;

public record KetQuaDangKyDto(bool ThanhCong, string ThongBao);
```

- [ ] **Bước 13: Tạo `backend/HaloChat.Api/Dto/DangKyTaiKhoanRequest.cs`**

```csharp
namespace HaloChat.Api.Dto;

public record DangKyTaiKhoanRequest(string TenTaiKhoan, string Email, string MatKhau);
```

- [ ] **Bước 14: Tạo `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`**

```csharp
using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNguoiDung
{
    Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau);
}
```

- [ ] **Bước 15: Tạo `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`**

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuNguoiDung : IDichVuNguoiDung
{
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;

    public DichVuNguoiDung(INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
    }

    public async Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau)
    {
        if (await _kho.TonTaiTenTaiKhoanAsync(tenTaiKhoan))
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản đã tồn tại.");
        }

        if (await _kho.TonTaiEmailAsync(email))
        {
            return new KetQuaDangKyDto(false, "Email đã được sử dụng.");
        }

        var salt = _dichVuMatKhau.TaoSalt();
        var nguoiDungMoi = new NguoiDung
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = email,
            Salt = salt,
            MatKhauBam = _dichVuMatKhau.BamMatKhau(matKhau, salt),
        };

        await _kho.ThemMoiAsync(nguoiDungMoi);
        return new KetQuaDangKyDto(true, "Đăng ký thành công.");
    }
}
```

- [ ] **Bước 16: Chạy lại test, xác nhận pass**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuNguoiDungTests`
Expected: 3 passed.

- [ ] **Bước 17: Tạo controller `backend/HaloChat.Api/Controllers/NguoiDungController.cs`**

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nguoidung")]
public class NguoiDungController : ControllerBase
{
    private readonly IDichVuNguoiDung _dichVu;

    public NguoiDungController(IDichVuNguoiDung dichVu)
    {
        _dichVu = dichVu;
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy([FromBody] DangKyTaiKhoanRequest yeuCau)
    {
        var ketQua = await _dichVu.DangKyTaiKhoan(yeuCau.TenTaiKhoan, yeuCau.Email, yeuCau.MatKhau);
        if (!ketQua.ThanhCong)
        {
            return Conflict(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }
}
```

- [ ] **Bước 18: Viết lại toàn bộ `backend/HaloChat.Api/Program.cs` (thêm đăng ký DI cho repository/service)**

```csharp
using HaloChat.Api.Options;
using HaloChat.Api.Repositories;
using HaloChat.Api.Services;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<TuyChonMongoDb>(builder.Configuration.GetSection(TuyChonMongoDb.TenMuc));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var tuyChon = sp.GetRequiredService<IOptions<TuyChonMongoDb>>().Value;
    var client = new MongoClient(tuyChon.ChuoiKetNoi);
    return client.GetDatabase(tuyChon.TenCoSoDuLieu);
});

builder.Services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
builder.Services.AddScoped<IDichVuMatKhau, DichVuMatKhau>();
builder.Services.AddScoped<IDichVuNguoiDung, DichVuNguoiDung>();

const string TenChinhSachCors = "ChoPhepFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(TenChinhSachCors, policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:NguonChoPhep"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(TenChinhSachCors);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/kiem-tra-suc-khoe", async (IMongoDatabase csdl) =>
{
    try
    {
        await csdl.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        return Results.Ok(new { ketNoiMongoDb = true });
    }
    catch (Exception loi)
    {
        return Results.Problem(detail: loi.Message, statusCode: 500);
    }
});

app.Run();
```

- [ ] **Bước 19: Build + chạy toàn bộ test + kiểm tra thật qua Swagger và MongoDB Atlas**

Run: `dotnet build backend/HaloChat.sln` → Expected: Build succeeded.
Run: `dotnet test backend/HaloChat.Api.Tests` → Expected: tất cả pass.
Run: `dotnet run --project backend/HaloChat.Api`, mở `/swagger`, gọi `POST /api/nguoidung/dang-ky` với body:
```json
{"tenTaiKhoan":"NguyenAn","email":"nguyenan@gmail.com","matKhau":"MatKhau123"}
```
Expected: 200 OK `{"thongBao":"Đăng ký thành công."}`. Mở MongoDB Atlas → Browse Collections → database `HaloChat` → collection `NguoiDung` → xác nhận có document mới, trường `matKhauBam` là chuỗi băm (không phải `"MatKhau123"`), `khoaCongKhai`/`khoaBiMat` rỗng.
Gọi lại cùng request → Expected: 409 Conflict.

- [ ] **Bước 20: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm đăng ký tài khoản (DangKyTaiKhoan) với băm mật khẩu SHA-256+Salt

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Đăng nhập (DangNhap) + phát hành JWT

**Files:**
- Create: `backend/HaloChat.Api/Options/TuyChonJwt.cs`
- Create: `backend/HaloChat.Api/Services/IDichVuJwt.cs`
- Create: `backend/HaloChat.Api/Services/DichVuJwt.cs`
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Create: `backend/HaloChat.Api/Dto/DangNhapRequest.cs`
- Create: `backend/HaloChat.Api/Dto/DangNhapResponse.cs`
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Modify: `backend/HaloChat.Api/appsettings.json`
- Modify: `backend/HaloChat.Api/Program.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Create: `backend/HaloChat.Api.Tests/Services/DichVuJwtTests.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`

**Interfaces:**
- Consumes: `INguoiDungRepository`, `IDichVuMatKhau`, `NguoiDung` (Task 3).
- Produces: `IDichVuJwt { string TaoJwt(NguoiDung) }`; `IDichVuNguoiDung` có thêm `Task<string?> DangNhap(string tenDangNhap, string matKhau)`; `INguoiDungRepository` có thêm `Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string)`. Task 5 mở rộng tiếp cả hai interface và `NguoiDungGiaLap`.

- [ ] **Bước 1: Thêm gói JWT**

```bash
dotnet add backend/HaloChat.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

- [ ] **Bước 2: Viết test cho `DichVuJwt` trước**

Create `backend/HaloChat.Api.Tests/Services/DichVuJwtTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using HaloChat.Api.Models;
using HaloChat.Api.Options;
using HaloChat.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuJwtTests
{
    private static DichVuJwt TaoDichVu() => new(Options.Create(new TuyChonJwt
    {
        ChuoiBiMat = "khoa-bi-mat-du-dai-danh-cho-kiem-thu-toi-thieu-32-ky-tu",
        NguoiPhatHanh = "HaloChat",
        DoiTuong = "HaloChatNguoiDung",
        SoPhutHetHan = 60,
    }));

    [Fact]
    public void TaoJwt_TraVeTokenChuaThongTinNguoiDung()
    {
        var dichVu = TaoDichVu();
        var nguoiDung = new NguoiDung { Id = "123", TenTaiKhoan = "NguyenAn", Email = "nguyenan@gmail.com" };

        var token = dichVu.TaoJwt(nguoiDung);
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("123", payload.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("NguyenAn", payload.Claims.First(c => c.Type == "tenTaiKhoan").Value);
        Assert.True(payload.ValidTo > DateTime.UtcNow);
    }
}
```

- [ ] **Bước 3: Chạy test, xác nhận thất bại**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuJwtTests`
Expected: lỗi biên dịch do thiếu `TuyChonJwt`/`DichVuJwt`.

- [ ] **Bước 4: Tạo `backend/HaloChat.Api/Options/TuyChonJwt.cs`**

```csharp
namespace HaloChat.Api.Options;

public class TuyChonJwt
{
    public const string TenMuc = "Jwt";

    public string ChuoiBiMat { get; set; } = string.Empty;
    public string NguoiPhatHanh { get; set; } = "HaloChat";
    public string DoiTuong { get; set; } = "HaloChatNguoiDung";
    public int SoPhutHetHan { get; set; } = 60;
}
```

- [ ] **Bước 5: Tạo `backend/HaloChat.Api/Services/IDichVuJwt.cs`**

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Services;

public interface IDichVuJwt
{
    string TaoJwt(NguoiDung nguoiDung);
}
```

- [ ] **Bước 6: Tạo `backend/HaloChat.Api/Services/DichVuJwt.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HaloChat.Api.Models;
using HaloChat.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HaloChat.Api.Services;

public class DichVuJwt : IDichVuJwt
{
    private readonly TuyChonJwt _tuyChon;

    public DichVuJwt(IOptions<TuyChonJwt> tuyChon)
    {
        _tuyChon = tuyChon.Value;
    }

    public string TaoJwt(NguoiDung nguoiDung)
    {
        var danhSachClaim = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, nguoiDung.Id),
            new Claim("tenTaiKhoan", nguoiDung.TenTaiKhoan),
            new Claim(JwtRegisteredClaimNames.Email, nguoiDung.Email),
        };

        var khoaKy = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tuyChon.ChuoiBiMat));
        var thongTinKy = new SigningCredentials(khoaKy, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _tuyChon.NguoiPhatHanh,
            audience: _tuyChon.DoiTuong,
            claims: danhSachClaim,
            expires: DateTime.UtcNow.AddMinutes(_tuyChon.SoPhutHetHan),
            signingCredentials: thongTinKy);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Bước 7: Chạy lại test, xác nhận pass**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuJwtTests`
Expected: 1 passed.

- [ ] **Bước 8: Viết lại toàn bộ `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`**

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan);
    Task<bool> TonTaiEmailAsync(string email);
    Task ThemMoiAsync(NguoiDung nguoiDung);
    Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap);
}
```

- [ ] **Bước 9: Viết lại toàn bộ `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`**

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class NguoiDungRepository : INguoiDungRepository
{
    private readonly IMongoCollection<NguoiDung> _collection;

    public NguoiDungRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<NguoiDung>("NguoiDung");
    }

    public async Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan)
    {
        return await _collection.Find(nd => nd.TenTaiKhoan == tenTaiKhoan).AnyAsync();
    }

    public async Task<bool> TonTaiEmailAsync(string email)
    {
        return await _collection.Find(nd => nd.Email == email).AnyAsync();
    }

    public async Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        await _collection.InsertOneAsync(nguoiDung);
    }

    public async Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var boLoc = Builders<NguoiDung>.Filter.Or(
            Builders<NguoiDung>.Filter.Eq(nd => nd.TenTaiKhoan, tenDangNhap),
            Builders<NguoiDung>.Filter.Eq(nd => nd.Email, tenDangNhap));

        return await _collection.Find(boLoc).FirstOrDefaultAsync();
    }
}
```

- [ ] **Bước 10: Viết lại toàn bộ `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`**

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NguoiDungGiaLap : INguoiDungRepository
{
    public List<NguoiDung> DanhSach { get; } = new();

    public Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan) =>
        Task.FromResult(DanhSach.Any(nd => nd.TenTaiKhoan == tenTaiKhoan));

    public Task<bool> TonTaiEmailAsync(string email) =>
        Task.FromResult(DanhSach.Any(nd => nd.Email == email));

    public Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        DanhSach.Add(nguoiDung);
        return Task.CompletedTask;
    }

    public Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var ketQua = DanhSach.FirstOrDefault(nd => nd.TenTaiKhoan == tenDangNhap || nd.Email == tenDangNhap);
        return Task.FromResult(ketQua);
    }
}
```

- [ ] **Bước 11: Viết lại toàn bộ `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs` (thêm test đăng nhập)**

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Options;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuNguoiDungTests
{
    private static (DichVuNguoiDung DichVu, NguoiDungGiaLap Kho) TaoDichVu()
    {
        var kho = new NguoiDungGiaLap();
        var dichVuJwt = new DichVuJwt(Options.Create(new TuyChonJwt
        {
            ChuoiBiMat = "khoa-bi-mat-du-dai-danh-cho-kiem-thu-toi-thieu-32-ky-tu",
        }));
        var dichVu = new DichVuNguoiDung(kho, new DichVuMatKhau(), dichVuJwt);
        return (dichVu, kho);
    }

    [Fact]
    public async Task DangKyTaiKhoan_TenTaiKhoanDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "NguyenAn", Email = "khac@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_EmailDaTonTai_TraVeThatBai()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { TenTaiKhoan = "Khac", Email = "nguyenan@gmail.com" });

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.False(ketQua.ThanhCong);
    }

    [Fact]
    public async Task DangKyTaiKhoan_HopLe_LuuMatKhauDaBamKhongLuuBanRo()
    {
        var (dichVu, kho) = TaoDichVu();

        var ketQua = await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        Assert.True(ketQua.ThanhCong);
        var daLuu = Assert.Single(kho.DanhSach);
        Assert.Equal("NguyenAn", daLuu.TenTaiKhoan);
        Assert.NotEqual("MatKhau123", daLuu.MatKhauBam);
        Assert.NotEmpty(daLuu.Salt);
    }

    [Fact]
    public async Task DangNhap_SaiMatKhau_TraVeNull()
    {
        var (dichVu, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("NguyenAn", "SaiMatKhau");

        Assert.Null(token);
    }

    [Fact]
    public async Task DangNhap_TaiKhoanKhongTonTai_TraVeNull()
    {
        var (dichVu, _) = TaoDichVu();

        var token = await dichVu.DangNhap("KhongTonTai", "MatKhau123");

        Assert.Null(token);
    }

    [Fact]
    public async Task DangNhap_DungMatKhauBangTenTaiKhoan_TraVeToken()
    {
        var (dichVu, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("NguyenAn", "MatKhau123");

        Assert.NotNull(token);
    }

    [Fact]
    public async Task DangNhap_DungMatKhauBangEmail_TraVeToken()
    {
        var (dichVu, _) = TaoDichVu();
        await dichVu.DangKyTaiKhoan("NguyenAn", "nguyenan@gmail.com", "MatKhau123");

        var token = await dichVu.DangNhap("nguyenan@gmail.com", "MatKhau123");

        Assert.NotNull(token);
    }
}
```

- [ ] **Bước 12: Chạy test, xác nhận thất bại**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuNguoiDungTests`
Expected: lỗi biên dịch (thiếu `DangNhap`, sai số tham số constructor).

- [ ] **Bước 13: Viết lại toàn bộ `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`**

```csharp
using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNguoiDung
{
    Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau);
    Task<string?> DangNhap(string tenDangNhap, string matKhau);
}
```

- [ ] **Bước 14: Viết lại toàn bộ `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`**

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuNguoiDung : IDichVuNguoiDung
{
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;
    private readonly IDichVuJwt _dichVuJwt;

    public DichVuNguoiDung(INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau, IDichVuJwt dichVuJwt)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
        _dichVuJwt = dichVuJwt;
    }

    public async Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau)
    {
        if (await _kho.TonTaiTenTaiKhoanAsync(tenTaiKhoan))
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản đã tồn tại.");
        }

        if (await _kho.TonTaiEmailAsync(email))
        {
            return new KetQuaDangKyDto(false, "Email đã được sử dụng.");
        }

        var salt = _dichVuMatKhau.TaoSalt();
        var nguoiDungMoi = new NguoiDung
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = email,
            Salt = salt,
            MatKhauBam = _dichVuMatKhau.BamMatKhau(matKhau, salt),
        };

        await _kho.ThemMoiAsync(nguoiDungMoi);
        return new KetQuaDangKyDto(true, "Đăng ký thành công.");
    }

    public async Task<string?> DangNhap(string tenDangNhap, string matKhau)
    {
        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(tenDangNhap);
        if (nguoiDung is null)
        {
            return null;
        }

        if (!_dichVuMatKhau.KiemTraMatKhau(matKhau, nguoiDung.Salt, nguoiDung.MatKhauBam))
        {
            return null;
        }

        return _dichVuJwt.TaoJwt(nguoiDung);
    }
}
```

- [ ] **Bước 15: Chạy lại toàn bộ test, xác nhận pass**

Run: `dotnet test backend/HaloChat.Api.Tests`
Expected: tất cả pass.

- [ ] **Bước 16: Tạo `backend/HaloChat.Api/Dto/DangNhapRequest.cs`**

```csharp
namespace HaloChat.Api.Dto;

public record DangNhapRequest(string TenDangNhap, string MatKhau);
```

- [ ] **Bước 17: Tạo `backend/HaloChat.Api/Dto/DangNhapResponse.cs`**

```csharp
namespace HaloChat.Api.Dto;

public record DangNhapResponse(string Token);
```

- [ ] **Bước 18: Viết lại toàn bộ `backend/HaloChat.Api/Controllers/NguoiDungController.cs`**

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nguoidung")]
public class NguoiDungController : ControllerBase
{
    private readonly IDichVuNguoiDung _dichVu;

    public NguoiDungController(IDichVuNguoiDung dichVu)
    {
        _dichVu = dichVu;
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy([FromBody] DangKyTaiKhoanRequest yeuCau)
    {
        var ketQua = await _dichVu.DangKyTaiKhoan(yeuCau.TenTaiKhoan, yeuCau.Email, yeuCau.MatKhau);
        if (!ketQua.ThanhCong)
        {
            return Conflict(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> DangNhap([FromBody] DangNhapRequest yeuCau)
    {
        var token = await _dichVu.DangNhap(yeuCau.TenDangNhap, yeuCau.MatKhau);
        if (token is null)
        {
            return Unauthorized(new { thongBao = "Sai tên đăng nhập hoặc mật khẩu." });
        }

        return Ok(new DangNhapResponse(token));
    }
}
```

- [ ] **Bước 19: Cập nhật `backend/HaloChat.Api/appsettings.json` (thêm mục `Jwt` — không chứa bí mật)**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDb": {
    "TenCoSoDuLieu": "HaloChat"
  },
  "Jwt": {
    "NguoiPhatHanh": "HaloChat",
    "DoiTuong": "HaloChatNguoiDung",
    "SoPhutHetHan": 60
  },
  "Cors": {
    "NguonChoPhep": "http://localhost:5173"
  }
}
```

- [ ] **Bước 20: Đặt khóa ký JWT bằng user-secrets**

```bash
dotnet user-secrets set "Jwt:ChuoiBiMat" "<TU_SINH_CHUOI_NGAU_NHIEN_TOI_THIEU_32_KY_TU>" --project backend/HaloChat.Api
```

Tự sinh một chuỗi ngẫu nhiên đủ dài (ví dụ `openssl rand -base64 32`), không dùng giá trị ví dụ trong plan này.

- [ ] **Bước 21: Viết lại toàn bộ `backend/HaloChat.Api/Program.cs` (thêm xác thực JWT)**

```csharp
using System.Text;
using HaloChat.Api.Options;
using HaloChat.Api.Repositories;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<TuyChonMongoDb>(builder.Configuration.GetSection(TuyChonMongoDb.TenMuc));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var tuyChon = sp.GetRequiredService<IOptions<TuyChonMongoDb>>().Value;
    var client = new MongoClient(tuyChon.ChuoiKetNoi);
    return client.GetDatabase(tuyChon.TenCoSoDuLieu);
});

builder.Services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
builder.Services.AddScoped<IDichVuMatKhau, DichVuMatKhau>();
builder.Services.AddScoped<IDichVuJwt, DichVuJwt>();
builder.Services.AddScoped<IDichVuNguoiDung, DichVuNguoiDung>();

builder.Services.Configure<TuyChonJwt>(builder.Configuration.GetSection(TuyChonJwt.TenMuc));
var tuyChonJwt = builder.Configuration.GetSection(TuyChonJwt.TenMuc).Get<TuyChonJwt>()
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings/user-secrets.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = tuyChonJwt.NguoiPhatHanh,
            ValidateAudience = true,
            ValidAudience = tuyChonJwt.DoiTuong,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tuyChonJwt.ChuoiBiMat)),
        };
    });
builder.Services.AddAuthorization();

const string TenChinhSachCors = "ChoPhepFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(TenChinhSachCors, policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:NguonChoPhep"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập: Bearer {token}",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(TenChinhSachCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/kiem-tra-suc-khoe", async (IMongoDatabase csdl) =>
{
    try
    {
        await csdl.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        return Results.Ok(new { ketNoiMongoDb = true });
    }
    catch (Exception loi)
    {
        return Results.Problem(detail: loi.Message, statusCode: 500);
    }
});

app.Run();
```

- [ ] **Bước 22: Build, chạy toàn bộ test, kiểm tra thật qua Swagger**

Run: `dotnet build backend/HaloChat.sln` → Expected: Build succeeded.
Run: `dotnet test backend/HaloChat.Api.Tests` → Expected: tất cả pass.
Run: `dotnet run --project backend/HaloChat.Api`, trên Swagger:
- `POST /api/nguoidung/dang-nhap` với mật khẩu sai → Expected: 401.
- `POST /api/nguoidung/dang-nhap` với tài khoản `NguyenAn` đã tạo ở Task 3, mật khẩu đúng → Expected: 200 kèm `token`. Giữ token này để dùng ở Task 5.

- [ ] **Bước 23: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm đăng nhập (DangNhap) và phát hành JWT

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Danh sách người dùng (LayDanhSachNguoiDung)

**Files:**
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Create: `backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs`
- Modify: `backend/HaloChat.Api/Controllers/NguoiDungController.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuNguoiDungTests.cs`

**Interfaces:**
- Consumes: mọi thứ từ Task 4.
- Produces: `NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email)`; `IDichVuNguoiDung.LayDanhSachNguoiDung(string idHienTai) -> Task<List<NguoiDungTomTatDto>>`; endpoint `GET /api/nguoidung` (`[Authorize]`).

- [ ] **Bước 1: Thêm test cho `LayDanhSachNguoiDung` — thêm vào cuối class `DichVuNguoiDungTests` (giữ nguyên các test đã có)**

```csharp
    [Fact]
    public async Task LayDanhSachNguoiDung_KhongBaoGomChinhMinh()
    {
        var (dichVu, kho) = TaoDichVu();
        kho.DanhSach.Add(new NguoiDung { Id = "1", TenTaiKhoan = "NguyenAn", Email = "a@gmail.com" });
        kho.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "TranBinh", Email = "b@gmail.com" });

        var danhSach = await dichVu.LayDanhSachNguoiDung("1");

        var duyNhat = Assert.Single(danhSach);
        Assert.Equal("TranBinh", duyNhat.TenTaiKhoan);
    }
```

- [ ] **Bước 2: Chạy test, xác nhận thất bại**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuNguoiDungTests`
Expected: lỗi biên dịch do thiếu `LayDanhSachNguoiDung`.

- [ ] **Bước 3: Viết lại toàn bộ `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs`**

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan);
    Task<bool> TonTaiEmailAsync(string email);
    Task ThemMoiAsync(NguoiDung nguoiDung);
    Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap);
    Task<List<NguoiDung>> LayTatCaAsync();
}
```

- [ ] **Bước 4: Viết lại toàn bộ `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`**

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class NguoiDungRepository : INguoiDungRepository
{
    private readonly IMongoCollection<NguoiDung> _collection;

    public NguoiDungRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<NguoiDung>("NguoiDung");
    }

    public async Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan)
    {
        return await _collection.Find(nd => nd.TenTaiKhoan == tenTaiKhoan).AnyAsync();
    }

    public async Task<bool> TonTaiEmailAsync(string email)
    {
        return await _collection.Find(nd => nd.Email == email).AnyAsync();
    }

    public async Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        await _collection.InsertOneAsync(nguoiDung);
    }

    public async Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var boLoc = Builders<NguoiDung>.Filter.Or(
            Builders<NguoiDung>.Filter.Eq(nd => nd.TenTaiKhoan, tenDangNhap),
            Builders<NguoiDung>.Filter.Eq(nd => nd.Email, tenDangNhap));

        return await _collection.Find(boLoc).FirstOrDefaultAsync();
    }

    public async Task<List<NguoiDung>> LayTatCaAsync()
    {
        return await _collection.Find(FilterDefinition<NguoiDung>.Empty).ToListAsync();
    }
}
```

- [ ] **Bước 5: Viết lại toàn bộ `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`**

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class NguoiDungGiaLap : INguoiDungRepository
{
    public List<NguoiDung> DanhSach { get; } = new();

    public Task<bool> TonTaiTenTaiKhoanAsync(string tenTaiKhoan) =>
        Task.FromResult(DanhSach.Any(nd => nd.TenTaiKhoan == tenTaiKhoan));

    public Task<bool> TonTaiEmailAsync(string email) =>
        Task.FromResult(DanhSach.Any(nd => nd.Email == email));

    public Task ThemMoiAsync(NguoiDung nguoiDung)
    {
        DanhSach.Add(nguoiDung);
        return Task.CompletedTask;
    }

    public Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap)
    {
        var ketQua = DanhSach.FirstOrDefault(nd => nd.TenTaiKhoan == tenDangNhap || nd.Email == tenDangNhap);
        return Task.FromResult(ketQua);
    }

    public Task<List<NguoiDung>> LayTatCaAsync() => Task.FromResult(DanhSach.ToList());
}
```

- [ ] **Bước 6: Chạy lại test, vẫn thất bại (đúng như dự kiến, chưa sửa service)**

Run: `dotnet test backend/HaloChat.Api.Tests --filter DichVuNguoiDungTests`

- [ ] **Bước 7: Tạo `backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs`**

```csharp
namespace HaloChat.Api.Dto;

public record NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email);
```

- [ ] **Bước 8: Viết lại toàn bộ `backend/HaloChat.Api/Services/IDichVuNguoiDung.cs`**

```csharp
using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNguoiDung
{
    Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau);
    Task<string?> DangNhap(string tenDangNhap, string matKhau);
    Task<List<NguoiDungTomTatDto>> LayDanhSachNguoiDung(string idHienTai);
}
```

- [ ] **Bước 9: Viết lại toàn bộ `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`**

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuNguoiDung : IDichVuNguoiDung
{
    private readonly INguoiDungRepository _kho;
    private readonly IDichVuMatKhau _dichVuMatKhau;
    private readonly IDichVuJwt _dichVuJwt;

    public DichVuNguoiDung(INguoiDungRepository kho, IDichVuMatKhau dichVuMatKhau, IDichVuJwt dichVuJwt)
    {
        _kho = kho;
        _dichVuMatKhau = dichVuMatKhau;
        _dichVuJwt = dichVuJwt;
    }

    public async Task<KetQuaDangKyDto> DangKyTaiKhoan(string tenTaiKhoan, string email, string matKhau)
    {
        if (await _kho.TonTaiTenTaiKhoanAsync(tenTaiKhoan))
        {
            return new KetQuaDangKyDto(false, "Tên tài khoản đã tồn tại.");
        }

        if (await _kho.TonTaiEmailAsync(email))
        {
            return new KetQuaDangKyDto(false, "Email đã được sử dụng.");
        }

        var salt = _dichVuMatKhau.TaoSalt();
        var nguoiDungMoi = new NguoiDung
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = email,
            Salt = salt,
            MatKhauBam = _dichVuMatKhau.BamMatKhau(matKhau, salt),
        };

        await _kho.ThemMoiAsync(nguoiDungMoi);
        return new KetQuaDangKyDto(true, "Đăng ký thành công.");
    }

    public async Task<string?> DangNhap(string tenDangNhap, string matKhau)
    {
        var nguoiDung = await _kho.TimTheoTenTaiKhoanHoacEmailAsync(tenDangNhap);
        if (nguoiDung is null)
        {
            return null;
        }

        if (!_dichVuMatKhau.KiemTraMatKhau(matKhau, nguoiDung.Salt, nguoiDung.MatKhauBam))
        {
            return null;
        }

        return _dichVuJwt.TaoJwt(nguoiDung);
    }

    public async Task<List<NguoiDungTomTatDto>> LayDanhSachNguoiDung(string idHienTai)
    {
        var tatCa = await _kho.LayTatCaAsync();
        return tatCa
            .Where(nd => nd.Id != idHienTai)
            .Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email))
            .ToList();
    }
}
```

- [ ] **Bước 10: Chạy lại toàn bộ test, xác nhận pass**

Run: `dotnet test backend/HaloChat.Api.Tests`
Expected: tất cả pass.

- [ ] **Bước 11: Viết lại toàn bộ `backend/HaloChat.Api/Controllers/NguoiDungController.cs`**

```csharp
using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/nguoidung")]
public class NguoiDungController : ControllerBase
{
    private readonly IDichVuNguoiDung _dichVu;

    public NguoiDungController(IDichVuNguoiDung dichVu)
    {
        _dichVu = dichVu;
    }

    [HttpPost("dang-ky")]
    public async Task<IActionResult> DangKy([FromBody] DangKyTaiKhoanRequest yeuCau)
    {
        var ketQua = await _dichVu.DangKyTaiKhoan(yeuCau.TenTaiKhoan, yeuCau.Email, yeuCau.MatKhau);
        if (!ketQua.ThanhCong)
        {
            return Conflict(new { thongBao = ketQua.ThongBao });
        }

        return Ok(new { thongBao = ketQua.ThongBao });
    }

    [HttpPost("dang-nhap")]
    public async Task<IActionResult> DangNhap([FromBody] DangNhapRequest yeuCau)
    {
        var token = await _dichVu.DangNhap(yeuCau.TenDangNhap, yeuCau.MatKhau);
        if (token is null)
        {
            return Unauthorized(new { thongBao = "Sai tên đăng nhập hoặc mật khẩu." });
        }

        return Ok(new DangNhapResponse(token));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> LayDanhSach()
    {
        var idHienTai = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (idHienTai is null)
        {
            return Unauthorized();
        }

        var danhSach = await _dichVu.LayDanhSachNguoiDung(idHienTai);
        return Ok(danhSach);
    }
}
```

- [ ] **Bước 12: Build, chạy toàn bộ test, kiểm tra thật qua Swagger**

Run: `dotnet build backend/HaloChat.sln` → Expected: Build succeeded.
Run: `dotnet test backend/HaloChat.Api.Tests` → Expected: tất cả pass.
Run: `dotnet run --project backend/HaloChat.Api`, trên Swagger:
- Gọi `GET /api/nguoidung` khi chưa Authorize → Expected: 401.
- Đăng ký thêm tài khoản thứ 2 (vd `TranBinh`/`tranbinh@gmail.com`), đăng nhập tài khoản `NguyenAn`, bấm "Authorize" với `Bearer <token>` nhận được, gọi `GET /api/nguoidung` → Expected: 200, danh sách chỉ chứa `TranBinh` (không chứa `NguyenAn`), JSON không có trường `matKhauBam`/`salt`/`khoaBiMat`.

- [ ] **Bước 13: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm danh sách người dùng (LayDanhSachNguoiDung) có xác thực JWT

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Kiểm tra đầu-cuối với MongoDB Atlas thật + tổng kết GĐ3

**Files:** không có file mới — đây là task xác nhận (verification) toàn bộ GĐ3 chạy đúng trên hạ tầng thật.

**Interfaces:**
- Consumes: toàn bộ API từ Task 1-5.
- Produces: xác nhận GĐ3 hoàn thành, sẵn sàng cho plan Frontend (GĐ4) tiếp theo.

- [ ] **Bước 1: Chạy toàn bộ test tự động**

Run: `dotnet test backend/HaloChat.sln`
Expected: tất cả pass, 0 failed.

- [ ] **Bước 2: Khởi động lại API từ đầu**

Run: `dotnet run --project backend/HaloChat.Api`

- [ ] **Bước 3: Kịch bản kiểm tra đầy đủ trên Swagger + MongoDB Atlas**

- Đăng ký 2 tài khoản mới (khác các tài khoản đã tạo ở Task 3-5): vd `LeCam`/`lecam@gmail.com` và `PhamDuc`/`phamduc@gmail.com`.
- Mở MongoDB Atlas → Browse Collections → `HaloChat.NguoiDung` → xác nhận cả 2 document tồn tại; `matKhauBam`/`salt` khác rỗng và khác mật khẩu gốc; `khoaCongKhai`/`khoaBiMat` đang rỗng (đúng thiết kế GĐ3, chưa đụng GĐ6).
- Đăng nhập `LeCam` bằng username, đăng nhập `PhamDuc` bằng email — cả hai đều nhận được token.
- Dùng token của `LeCam` gọi `GET /api/nguoidung` → thấy `PhamDuc` và các tài khoản khác đã tạo trước đó, không thấy chính `LeCam`.
- Đăng ký lại `LeCam` với email khác → Expected: 409 (trùng tên tài khoản). Đăng ký tài khoản mới với email `lecam@gmail.com` → Expected: 409 (trùng email).

- [ ] **Bước 4: Xác nhận không có bí mật nào bị commit**

Run: `git log -p -- backend/HaloChat.Api/appsettings.json`
Expected: không xuất hiện chuỗi kết nối MongoDB thật hay khóa ký JWT thật (chỉ có `TenCoSoDuLieu`, `NguoiPhatHanh`, `DoiTuong`, `SoPhutHetHan`).
Run: `git status`
Expected: sạch, không có file `.env` hay `appsettings.*.local.json` nào bị track.

- [ ] **Bước 5: Không cần commit thêm nếu Bước 1-4 không phát hiện vấn đề**

Đây là task xác nhận thuần túy. Nếu phát hiện lỗi ở bước nào, quay lại đúng Task tương ứng (1-5) để sửa và commit riêng ở đó.
