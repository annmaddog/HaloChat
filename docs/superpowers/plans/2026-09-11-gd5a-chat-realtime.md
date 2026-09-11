# HaloChat GĐ5a — Chat Realtime Lõi — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng SignalR `ChatHub` cho nhắn tin 1-1 realtime, lưu lịch sử vào MongoDB (`TinNhan`), thêm gửi ảnh/file qua endpoint REST, và giao diện chat 2 cột (danh sách người dùng + khung hội thoại) trên nền frontend đã có — kiểm chứng bằng cách gửi thử tin nhắn + ảnh/file giữa 2 tài khoản và xác nhận dữ liệu được lưu thật trên MongoDB.

**Architecture:** Backend thêm 1 Hub SignalR (`ChatHub`) xác thực bằng JWT truyền qua query string, cộng thêm tầng Service/Repository (`DichVuTinNhan`/`ITinNhanRepository`) theo đúng pattern đã có ở GĐ3 (interface + fake trong bộ nhớ cho unit test, không cần Mongo thật khi `dotnet test`). Upload ảnh/file đi qua REST riêng (không qua Hub), lưu đĩa + trả về đường dẫn tĩnh phục vụ qua `UseStaticFiles`. Frontend thêm 1 context mới (`NguCanhChat`) quản lý vòng đời kết nối SignalR (tách khỏi `NguCanhXacThuc`), và 1 trang `TrangChat` 2 cột thay cho trang danh sách người dùng đơn giản hiện tại.

**Tech Stack:** .NET 9 (SignalR có sẵn trong shared framework ASP.NET Core, không cần NuGet riêng cho server), `@microsoft/signalr` 9.0.19 (client), MongoDB.Driver, xUnit + `Microsoft.AspNetCore.SignalR.Client` (test), React 19 + TypeScript + Vite, Vitest + React Testing Library.

**Spec:** `docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md` (đặc biệt §4, §7, §8, §10).

## Global Constraints

- Toàn bộ code .NET nằm trong `backend/`, toàn bộ code frontend nằm trong `frontend/` (spec §3) — không tạo file ở gốc repo.
- **Ruling phạm vi GĐ5a** (spec §10.1 đầy đủ chỉ áp dụng từ GĐ5b trở đi): GĐ5b chưa tồn tại (`LoiMoiKetBan`, `ChoPhepTinNhanTuNguoiLa` chưa có), nên GĐ5a **cho phép nhắn tin tự do giữa bất kỳ 2 người dùng đã đăng nhập nào** — giống hệt cách `LayDanhSachNguoiDung` của GĐ4 đã hiển thị toàn bộ người dùng. Khi GĐ5b thêm bảng kết bạn, `DichVuTinNhan.GuiTinNhanAsync` sẽ được sửa để áp policy bạn bè/người lạ — không cần đổi chữ ký hàm hay schema.
- Không đụng tới `HaloChat.Security.DichVuMaHoa` — các method mã hóa vẫn là stub `[BẢO MẬT - GĐ6]`, không được gọi (spec §9).
- Giới hạn file: ảnh tối đa **5MB**, file khác tối đa **20MB**, kiểm tra ở **cả client và server** (spec §7, §8).
- Loại file cho phép (allow-list, đạt luôn hiệu quả chặn file thực thi): ảnh `jpg/jpeg/png/gif/webp`; tài liệu `pdf/docx/xlsx/zip` (spec §7).
- Upload phải **stream thẳng ra đĩa** — dùng `IFormFile.CopyToAsync` trực tiếp vào `FileStream`, không đọc vào `byte[]` trung gian (spec §8).
- File lưu tại `backend/HaloChat.Api/uploads/` (đã có trong `.gitignore`), MongoDB chỉ lưu metadata (spec §7).
- Quy ước đặt tên: biến camelCase, hàm & class PascalCase, tiếng Việt không dấu; giữ nguyên thuật ngữ kỹ thuật quen thuộc (JWT, SignalR, MongoDB...) (spec §13).
- Không đổi claim JWT hiện có (`sub`, `tenTaiKhoan`, `email`, `MapInboundClaims = false`) — mọi chỗ đọc user hiện tại tiếp tục dùng `JwtRegisteredClaimNames.Sub`.

---

## Task 1: Model + Repository cho TinNhan

**Files:**
- Create: `backend/HaloChat.Api/Models/TinNhan.cs`
- Create: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Create: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Create: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Modify: `backend/HaloChat.Api/Repositories/INguoiDungRepository.cs` (thêm `TimTheoIdAsync`)
- Modify: `backend/HaloChat.Api/Repositories/NguoiDungRepository.cs` (cài đặt `TimTheoIdAsync`)
- Modify: `backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs` (cài đặt `TimTheoIdAsync`)
- Modify: `backend/HaloChat.Api/Program.cs` (đăng ký `ITinNhanRepository`)

**Interfaces:**
- Consumes: `IMongoDatabase` (đã đăng ký ở GĐ3), `NguoiDung` model.
- Produces: `TinNhan` model (`enum LoaiTinNhan { Text, Anh, File }`), `ITinNhanRepository` với 3 method (`ThemMoiAsync`, `LayLichSuTheoNguoiDungAsync`, `DanhDauDaDocAsync`) — Task 2 (`DichVuTinNhan`) tiêu thụ trực tiếp interface này. `INguoiDungRepository.TimTheoIdAsync(string id)` — Task 2 dùng để kiểm tra người nhận tồn tại.

- [ ] **Bước 1: Thêm `TimTheoIdAsync` vào `INguoiDungRepository`**

Thêm 1 dòng vào interface (giữ nguyên các dòng khác):

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface INguoiDungRepository
{
    Task<bool> TonTaiDinhDanhAsync(string dinhDanh);
    Task ThemMoiAsync(NguoiDung nguoiDung);
    Task<NguoiDung?> TimTheoTenTaiKhoanHoacEmailAsync(string tenDangNhap);
    Task<List<NguoiDung>> LayTatCaAsync();
    Task<NguoiDung?> TimTheoIdAsync(string id);
}
```

- [ ] **Bước 2: Cài đặt `TimTheoIdAsync` trong `NguoiDungRepository` và trong fake**

Thêm method vào cuối `NguoiDungRepository` (`backend/HaloChat.Api/Repositories/NguoiDungRepository.cs`):

```csharp
    public async Task<NguoiDung?> TimTheoIdAsync(string id)
    {
        return await _collection.Find(nd => nd.Id == id).FirstOrDefaultAsync();
    }
```

Thêm method vào cuối `NguoiDungGiaLap` (`backend/HaloChat.Api.Tests/Fakes/NguoiDungGiaLap.cs`):

```csharp
    public Task<NguoiDung?> TimTheoIdAsync(string id) =>
        Task.FromResult(DanhSach.FirstOrDefault(nd => nd.Id == id));
```

- [ ] **Bước 3: Tạo model `TinNhan`**

Tạo `backend/HaloChat.Api/Models/TinNhan.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public enum LoaiTinNhan
{
    Text,
    Anh,
    File,
}

public class TinNhan
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiGuiId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? NguoiNhanId { get; set; }

    // [GĐ5b] Tin nhắn nhóm — để trống ở GĐ5a. Đúng một trong hai field
    // NguoiNhanId/NhomId có giá trị (spec §4, §10.2).
    [BsonRepresentation(BsonType.ObjectId)]
    public string? NhomId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public LoaiTinNhan LoaiTinNhan { get; set; } = LoaiTinNhan.Text;

    public string NoiDungTinNhan { get; set; } = string.Empty;

    public string? DuongDanFile { get; set; }
    public string? TenFileGoc { get; set; }
    public long? KichThuocFile { get; set; }
    public string? LoaiFile { get; set; }

    public bool DaDoc { get; set; } = false;

    public DateTime ThoiGianTao { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Bước 4: Tạo `ITinNhanRepository` + `TinNhanRepository`**

Tạo `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`:

```csharp
using HaloChat.Api.Models;

namespace HaloChat.Api.Repositories;

public interface ITinNhanRepository
{
    Task ThemMoiAsync(TinNhan tinNhan);

    /// <summary>
    /// Lấy lịch sử tin nhắn 1-1 giữa nguoiA và nguoiB, mới nhất trước.
    /// truocId (nếu có) chỉ lấy tin nhắn cũ hơn tin nhắn đó (phân trang lùi).
    /// </summary>
    Task<List<TinNhan>> LayLichSuTheoNguoiDungAsync(string nguoiA, string nguoiB, string? truocId, int soLuong);

    /// <summary>Đánh dấu đã đọc mọi tin nhắn nguoiGuiId đã gửi cho nguoiNhanId.</summary>
    Task DanhDauDaDocAsync(string nguoiGuiId, string nguoiNhanId);
}
```

Tạo `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`:

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class TinNhanRepository : ITinNhanRepository
{
    private readonly IMongoCollection<TinNhan> _collection;

    public TinNhanRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<TinNhan>("TinNhan");
    }

    public Task ThemMoiAsync(TinNhan tinNhan) => _collection.InsertOneAsync(tinNhan);

    public async Task<List<TinNhan>> LayLichSuTheoNguoiDungAsync(string nguoiA, string nguoiB, string? truocId, int soLuong)
    {
        var boLocCapDoi = Builders<TinNhan>.Filter.Or(
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiA),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiB)),
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiB),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiA)));

        var boLoc = string.IsNullOrEmpty(truocId)
            ? boLocCapDoi
            : Builders<TinNhan>.Filter.And(boLocCapDoi, Builders<TinNhan>.Filter.Lt(t => t.Id, truocId));

        return await _collection.Find(boLoc)
            .SortByDescending(t => t.Id)
            .Limit(soLuong)
            .ToListAsync();
    }

    public async Task DanhDauDaDocAsync(string nguoiGuiId, string nguoiNhanId)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiGuiId),
            Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiNhanId),
            Builders<TinNhan>.Filter.Eq(t => t.DaDoc, false));
        var capNhat = Builders<TinNhan>.Update.Set(t => t.DaDoc, true);

        await _collection.UpdateManyAsync(boLoc, capNhat);
    }
}
```

> Ghi chú: `Id` là `string` nhưng lưu dưới dạng `BsonType.ObjectId` — khi so sánh (`Lt`) hay sắp xếp (`SortByDescending`), MongoDB driver so sánh đúng theo giá trị ObjectId thật (12 byte, có timestamp ở đầu), nên thứ tự khớp đúng thứ tự tạo — không cần field `ThoiGianTao` riêng cho phân trang.

- [ ] **Bước 5: Tạo fake `TinNhanGiaLap` cho test**

Tạo `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class TinNhanGiaLap : ITinNhanRepository
{
    public List<TinNhan> DanhSach { get; } = new();

    public Task ThemMoiAsync(TinNhan tinNhan)
    {
        DanhSach.Add(tinNhan);
        return Task.CompletedTask;
    }

    public Task<List<TinNhan>> LayLichSuTheoNguoiDungAsync(string nguoiA, string nguoiB, string? truocId, int soLuong)
    {
        var ketQua = DanhSach
            .Where(t => (t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) ||
                        (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA))
            .Where(t => truocId is null || string.CompareOrdinal(t.Id, truocId) < 0)
            .OrderByDescending(t => t.Id)
            .Take(soLuong)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task DanhDauDaDocAsync(string nguoiGuiId, string nguoiNhanId)
    {
        foreach (var t in DanhSach.Where(t => t.NguoiGuiId == nguoiGuiId && t.NguoiNhanId == nguoiNhanId))
        {
            t.DaDoc = true;
        }
        return Task.CompletedTask;
    }
}
```

- [ ] **Bước 6: Đăng ký `ITinNhanRepository` trong `Program.cs`**

Trong `backend/HaloChat.Api/Program.cs`, thêm ngay sau dòng `builder.Services.AddScoped<IDichVuNguoiDung, DichVuNguoiDung>();`:

```csharp
builder.Services.AddScoped<ITinNhanRepository, TinNhanRepository>();
```

- [ ] **Bước 7: Build để xác nhận không có lỗi biên dịch**

Run: `dotnet build backend/HaloChat.sln`
Expected: Build succeeded, 0 Error.

- [ ] **Bước 8: Commit**

```bash
git add backend/
git commit -m "Them model va repository TinNhan (GD5a)"
```

---

## Task 2: DichVuTinNhan (tầng nghiệp vụ + validate)

**Files:**
- Create: `backend/HaloChat.Api/Dto/TinNhanDto.cs`
- Create: `backend/HaloChat.Api/Services/NguoiNhanKhongTonTaiException.cs`
- Create: `backend/HaloChat.Api/Services/TinNhanKhongHopLeException.cs`
- Create: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Create: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Create: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (đăng ký `IDichVuTinNhan`)

**Interfaces:**
- Consumes: `ITinNhanRepository`, `INguoiDungRepository.TimTheoIdAsync` (Task 1).
- Produces: `IDichVuTinNhan` với `GuiTinNhanAsync(string nguoiGuiId, string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan, string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile) : Task<TinNhanDto>`, `LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong) : Task<List<TinNhanDto>>`, `DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId) : Task`. `TinNhanDto` — Task 3 (Hub) và Task 4 (Controller) đều trả kiểu này ra ngoài. Ném `NguoiNhanKhongTonTaiException`/`TinNhanKhongHopLeException` khi dữ liệu không hợp lệ — Task 3 bắt 2 exception này để chuyển thành `HubException`.

- [ ] **Bước 1: Viết test cho `DichVuTinNhan` (sẽ fail vì chưa có class)**

Tạo `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Services;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Services;

public class DichVuTinNhanTests
{
    private static (DichVuTinNhan DichVu, TinNhanGiaLap KhoTinNhan, NguoiDungGiaLap KhoNguoiDung) TaoDichVu()
    {
        var khoTinNhan = new TinNhanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung);
        return (dichVu, khoTinNhan, khoNguoiDung);
    }

    [Fact]
    public async Task GuiTinNhanAsync_NguoiNhanKhongTonTai_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<NguoiNhanKhongTonTaiException>(() =>
            dichVu.GuiTinNhanAsync("1", "khong-ton-tai", "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_NoiDungTextRong_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync("1", "2", "Text", "   ", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiKhongHopLe_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync("1", "2", "KhongTonTai", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_LoaiAnhThieuDuongDanFile_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() =>
            dichVu.GuiTinNhanAsync("1", "2", "Anh", "", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhanAsync_HopLe_LuuVaTraVeTinNhan()
    {
        var (dichVu, khoTinNhan, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });

        var ketQua = await dichVu.GuiTinNhanAsync("1", "2", "Text", "Xin chào", null, null, null, null);

        Assert.Equal("Xin chào", ketQua.NoiDungTinNhan);
        Assert.Equal("1", ketQua.NguoiGuiId);
        Assert.Equal("2", ketQua.NguoiNhanId);
        Assert.False(ketQua.DaDoc);
        var daLuu = Assert.Single(khoTinNhan.DanhSach);
        Assert.Equal(LoaiTinNhan.Text, daLuu.LoaiTinNhan);
    }

    [Fact]
    public async Task LayLichSuAsync_TraVeCaHaiChieuGuiVaNhan()
    {
        var (dichVu, _, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });
        await dichVu.GuiTinNhanAsync("1", "2", "Text", "Chào A gửi", null, null, null, null);
        await dichVu.GuiTinNhanAsync("2", "1", "Text", "Chào B gửi", null, null, null, null);

        var lichSu = await dichVu.LayLichSuAsync("1", "2", null, 30);

        Assert.Equal(2, lichSu.Count);
    }

    [Fact]
    public async Task DanhDauDaDocAsync_DanhDauTinNhanCuaNguoiGuiDaDoc()
    {
        var (dichVu, khoTinNhan, khoNguoiDung) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = "2", TenTaiKhoan = "NguoiNhan" });
        await dichVu.GuiTinNhanAsync("2", "1", "Text", "Chào", null, null, null, null);

        await dichVu.DanhDauDaDocAsync("1", "2");

        Assert.True(khoTinNhan.DanhSach.Single().DaDoc);
    }
}
```

- [ ] **Bước 2: Chạy test để xác nhận fail**

Run: `dotnet test backend/HaloChat.sln --filter DichVuTinNhanTests`
Expected: FAIL (biên dịch lỗi — chưa có `DichVuTinNhan`, `NguoiNhanKhongTonTaiException`, `TinNhanKhongHopLeException`).

- [ ] **Bước 3: Tạo DTO + 2 exception**

Tạo `backend/HaloChat.Api/Dto/TinNhanDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record TinNhanDto(
    string Id,
    string NguoiGuiId,
    string? NguoiNhanId,
    string LoaiTinNhan,
    string NoiDungTinNhan,
    string? DuongDanFile,
    string? TenFileGoc,
    long? KichThuocFile,
    string? LoaiFile,
    bool DaDoc,
    DateTime ThoiGianTao);
```

Tạo `backend/HaloChat.Api/Services/NguoiNhanKhongTonTaiException.cs`:

```csharp
namespace HaloChat.Api.Services;

/// <summary>Ném ra khi gửi tin nhắn cho một NguoiDungId không tồn tại trong hệ thống.</summary>
public class NguoiNhanKhongTonTaiException : Exception
{
    public NguoiNhanKhongTonTaiException(string nguoiNhanId)
        : base($"Người nhận không tồn tại: {nguoiNhanId}.")
    {
    }
}
```

Tạo `backend/HaloChat.Api/Services/TinNhanKhongHopLeException.cs`:

```csharp
namespace HaloChat.Api.Services;

/// <summary>Ném ra khi dữ liệu gửi tin nhắn không hợp lệ (loại sai, thiếu nội dung/file).</summary>
public class TinNhanKhongHopLeException : Exception
{
    public TinNhanKhongHopLeException(string thongBao) : base(thongBao)
    {
    }
}
```

- [ ] **Bước 4: Tạo `IDichVuTinNhan` + `DichVuTinNhan`**

Tạo `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`:

```csharp
using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuTinNhan
{
    Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile);

    Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong);

    Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId);
}
```

Tạo `backend/HaloChat.Api/Services/DichVuTinNhan.cs`:

```csharp
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuTinNhan : IDichVuTinNhan
{
    private readonly ITinNhanRepository _khoTinNhan;
    private readonly INguoiDungRepository _khoNguoiDung;

    public DichVuTinNhan(ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
    }

    public async Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        if (!Enum.TryParse<LoaiTinNhan>(loaiTinNhan, ignoreCase: true, out var loai))
        {
            throw new TinNhanKhongHopLeException($"Loại tin nhắn không hợp lệ: {loaiTinNhan}.");
        }

        if (loai == LoaiTinNhan.Text && string.IsNullOrWhiteSpace(noiDungTinNhan))
        {
            throw new TinNhanKhongHopLeException("Nội dung tin nhắn không được để trống.");
        }

        if (loai != LoaiTinNhan.Text && string.IsNullOrWhiteSpace(duongDanFile))
        {
            throw new TinNhanKhongHopLeException("Thiếu đường dẫn file đính kèm.");
        }

        var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(nguoiNhanId);
        if (nguoiNhan is null)
        {
            throw new NguoiNhanKhongTonTaiException(nguoiNhanId);
        }

        var tinNhan = new TinNhan
        {
            NguoiGuiId = nguoiGuiId,
            NguoiNhanId = nguoiNhanId,
            LoaiTinNhan = loai,
            NoiDungTinNhan = noiDungTinNhan ?? string.Empty,
            DuongDanFile = duongDanFile,
            TenFileGoc = tenFileGoc,
            KichThuocFile = kichThuocFile,
            LoaiFile = loaiFile,
        };

        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return AnhXaDto(tinNhan);
    }

    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        return lichSu.Select(AnhXaDto).ToList();
    }

    public Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId) =>
        _khoTinNhan.DanhDauDaDocAsync(nguoiGuiId, nguoiHienTaiId);

    private static TinNhanDto AnhXaDto(TinNhan t) => new(
        t.Id, t.NguoiGuiId, t.NguoiNhanId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
        t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.ThoiGianTao);
}
```

- [ ] **Bước 5: Chạy lại test để xác nhận pass**

Run: `dotnet test backend/HaloChat.sln --filter DichVuTinNhanTests`
Expected: PASS (7/7).

- [ ] **Bước 6: Đăng ký `IDichVuTinNhan` trong `Program.cs`**

Thêm ngay sau dòng vừa thêm ở Task 1 (`builder.Services.AddScoped<ITinNhanRepository, TinNhanRepository>();`):

```csharp
builder.Services.AddScoped<IDichVuTinNhan, DichVuTinNhan>();
```

- [ ] **Bước 7: Build toàn bộ solution rồi commit**

Run: `dotnet build backend/HaloChat.sln`
Expected: Build succeeded.

```bash
git add backend/
git commit -m "Them DichVuTinNhan: validate + luu tin nhan 1-1 (GD5a)"
```

---

## Task 3: ChatHub (SignalR) + xác thực JWT qua query string

**Files:**
- Create: `backend/HaloChat.Api/Services/NguoiDungIdProvider.cs`
- Create: `backend/HaloChat.Api/Hubs/ChatHub.cs`
- Create: `backend/HaloChat.Api.Tests/ChatHubTests.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (SignalR, `IUserIdProvider`, sự kiện đọc `access_token`, map hub)
- Modify: `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs` (swap `ITinNhanRepository` bằng fake)
- Modify: `backend/HaloChat.Api.Tests/HaloChat.Api.Tests.csproj` (thêm package `Microsoft.AspNetCore.SignalR.Client`)

**Interfaces:**
- Consumes: `IDichVuTinNhan` (Task 2).
- Produces: `ChatHub` với hub method `GuiTinNhan(string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan, string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile) : Task<TinNhanDto>` và `DanhDauDaDoc(string nguoiGuiId) : Task`, map tại đường dẫn `/hub/chat`. Sự kiện client nhận: `"NhanTinNhan"` kèm `TinNhanDto`. Task 5-7 (frontend) build trên các tên method/event này — **giữ nguyên chính xác chuỗi `"GuiTinNhan"`, `"DanhDauDaDoc"`, `"NhanTinNhan"`** vì SignalR JS client gọi bằng string, không có kiểm tra kiểu lúc biên dịch.

- [ ] **Bước 1: Thêm package `Microsoft.AspNetCore.SignalR.Client` vào project test**

```bash
dotnet add backend/HaloChat.Api.Tests package Microsoft.AspNetCore.SignalR.Client --version 9.0.9
```

- [ ] **Bước 2: Viết test cho ChatHub (sẽ fail vì chưa có Hub)**

Tạo `backend/HaloChat.Api.Tests/ChatHubTests.cs`:

```csharp
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace HaloChat.Api.Tests;

public class ChatHubTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;

    public ChatHubTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoTinNhanGiaLap.DanhSach.Clear();
    }

    private async Task<string> TaoTaiKhoanVaDangNhapAsync(string tenTaiKhoan)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/nguoidung/dang-ky", new
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = $"{tenTaiKhoan}@gmail.com",
            MatKhau = "MatKhau123",
        });
        var phanHoi = await client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new
        {
            TenDangNhap = tenTaiKhoan,
            MatKhau = "MatKhau123",
        });
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponseGiaLap>();
        return ketQua!.Token;
    }

    private record DangNhapResponseGiaLap(string Token);

    private HubConnection TaoKetNoiHub(string token)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hub/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
    }

    [Fact]
    public async Task GuiTinNhan_NguoiNhanDangKetNoi_NhanDuocTinNhanRealtime()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubnguoia");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubnguoib");
        var idNguoiB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubnguoib").Id;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await using var ketNoiB = TaoKetNoiHub(tokenB);

        TinNhanDto? tinNhanNhanDuoc = null;
        var daNhan = new TaskCompletionSource();
        ketNoiB.On<TinNhanDto>("NhanTinNhan", tinNhan =>
        {
            tinNhanNhanDuoc = tinNhan;
            daNhan.SetResult();
        });

        await ketNoiA.StartAsync();
        await ketNoiB.StartAsync();

        var tinNhanGui = await ketNoiA.InvokeAsync<TinNhanDto>(
            "GuiTinNhan", idNguoiB, "Text", "Chào bạn", null, null, null, null);

        await daNhan.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("Chào bạn", tinNhanGui.NoiDungTinNhan);
        Assert.NotNull(tinNhanNhanDuoc);
        Assert.Equal("Chào bạn", tinNhanNhanDuoc!.NoiDungTinNhan);
    }

    [Fact]
    public async Task GuiTinNhan_NguoiNhanKhongTonTai_NemHubException()
    {
        var token = await TaoTaiKhoanVaDangNhapAsync("hubnguoic");
        await using var ketNoi = TaoKetNoiHub(token);
        await ketNoi.StartAsync();

        await Assert.ThrowsAsync<HubException>(() =>
            ketNoi.InvokeAsync<TinNhanDto>(
                "GuiTinNhan", "000000000000000000000000", "Text", "Xin chào", null, null, null, null));
    }

    [Fact]
    public async Task GuiTinNhan_ChuaXacThuc_TuChoiKetNoi()
    {
        await using var ketNoi = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hub/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await Assert.ThrowsAnyAsync<Exception>(() => ketNoi.StartAsync());
    }
}
```

- [ ] **Bước 3: Chạy test để xác nhận fail**

Run: `dotnet test backend/HaloChat.sln --filter ChatHubTests`
Expected: FAIL (biên dịch lỗi — chưa có `ChatHub`, `_factory.KhoTinNhanGiaLap` chưa tồn tại).

- [ ] **Bước 4: Tạo `IUserIdProvider` tùy chỉnh**

Tạo `backend/HaloChat.Api/Services/NguoiDungIdProvider.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Services;

/// <summary>
/// SignalR mặc định lấy user id từ claim ClaimTypes.NameIdentifier để phục vụ
/// Clients.User(id). Token của HaloChat phát hành claim "sub" (JwtRegisteredClaimNames.Sub,
/// vì options.MapInboundClaims = false nên claim giữ nguyên tên gốc) — provider này đọc
/// đúng claim đó thay vì dựa vào mặc định.
/// </summary>
public class NguoiDungIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
```

- [ ] **Bước 5: Tạo `ChatHub`**

Tạo `backend/HaloChat.Api/Hubs/ChatHub.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IDichVuTinNhan _dichVuTinNhan;

    public ChatHub(IDichVuTinNhan dichVuTinNhan)
    {
        _dichVuTinNhan = dichVuTinNhan;
    }

    private string NguoiDungHienTaiId =>
        Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? throw new HubException("Không xác định được người dùng hiện tại.");

    public async Task<TinNhanDto> GuiTinNhan(
        string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GuiTinNhanAsync(
                NguoiDungHienTaiId, nguoiNhanId, loaiTinNhan, noiDungTinNhan,
                duongDanFile, tenFileGoc, kichThuocFile, loaiFile);

            await Clients.User(nguoiNhanId).SendAsync("NhanTinNhan", tinNhan);
            return tinNhan;
        }
        catch (NguoiNhanKhongTonTaiException loi)
        {
            throw new HubException(loi.Message);
        }
        catch (TinNhanKhongHopLeException loi)
        {
            throw new HubException(loi.Message);
        }
    }

    public Task DanhDauDaDoc(string nguoiGuiId) =>
        _dichVuTinNhan.DanhDauDaDocAsync(NguoiDungHienTaiId, nguoiGuiId);
}
```

> Lưu ý thiết kế: người gửi **không** nhận lại tin nhắn của chính mình qua sự kiện `"NhanTinNhan"` — họ nhận nó qua **giá trị trả về** của `InvokeAsync`/`invoke`. Tránh được tin nhắn bị hiển thị trùng lặp phía người gửi.

- [ ] **Bước 6: Cấu hình SignalR + JWT qua query string trong `Program.cs`**

Sửa khối `.AddJwtBearer(options => { ... });` trong `backend/HaloChat.Api/Program.cs` — thêm `options.Events` sau `options.TokenValidationParameters`:

```csharp
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
        // SignalR qua WebSocket/LongPolling không set được header Authorization —
        // client truyền JWT qua query string ?access_token=, chỉ chấp nhận cho
        // đúng đường dẫn Hub (spec §10.3).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hub/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });
```

Thêm ngay sau dòng `builder.Services.AddScoped<IDichVuTinNhan, DichVuTinNhan>();`:

```csharp
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, HaloChat.Api.Services.NguoiDungIdProvider>();
```

Thêm ngay sau `app.MapControllers();`:

```csharp
app.MapHub<HaloChat.Api.Hubs.ChatHub>("/hub/chat");
```

- [ ] **Bước 7: Swap `ITinNhanRepository` bằng fake trong fixture test tích hợp**

Sửa `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`:

```csharp
using HaloChat.Api.Repositories;
using HaloChat.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HaloChat.Api.Tests;

public class ThietLapKiemThuTichHop : WebApplicationFactory<Program>
{
    public NguoiDungGiaLap KhoGiaLap { get; } = new();
    public TinNhanGiaLap KhoTinNhanGiaLap { get; } = new();

    // ... (giữ nguyên static constructor hiện có) ...

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(dichVu =>
        {
            dichVu.RemoveAll<INguoiDungRepository>();
            dichVu.AddSingleton<INguoiDungRepository>(KhoGiaLap);
            dichVu.RemoveAll<ITinNhanRepository>();
            dichVu.AddSingleton<ITinNhanRepository>(KhoTinNhanGiaLap);
        });
    }
}
```

(Chỉ thêm dòng `KhoTinNhanGiaLap` + 2 dòng `RemoveAll`/`AddSingleton` cho `ITinNhanRepository` — không đổi phần static constructor và comment giải thích biến môi trường JWT đã có.)

- [ ] **Bước 8: Chạy lại test để xác nhận pass**

Run: `dotnet test backend/HaloChat.sln --filter ChatHubTests`
Expected: PASS (3/3).

- [ ] **Bước 9: Chạy toàn bộ test suite để đảm bảo không phá vỡ gì**

Run: `dotnet test backend/HaloChat.sln`
Expected: PASS toàn bộ.

- [ ] **Bước 10: Commit**

```bash
git add backend/
git commit -m "Them ChatHub SignalR: gui/nhan tin nhan realtime qua JWT (GD5a)"
```

---

## Task 4: TinNhanController — lịch sử phân trang + upload ảnh/file

**Files:**
- Create: `backend/HaloChat.Api/Dto/TepTinDaTaiLenDto.cs`
- Create: `backend/HaloChat.Api/Controllers/TinNhanController.cs`
- Create: `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`
- Modify: `backend/HaloChat.Api/Program.cs` (phục vụ file tĩnh từ `uploads/`)

**Interfaces:**
- Consumes: `IDichVuTinNhan` (Task 2).
- Produces: `GET /api/tinnhan/nguoi-dung/{id}?truoc=&soLuong=` → `TinNhanDto[]`; `POST /api/tinnhan/upload` (multipart, field tên `tep`) → `TepTinDaTaiLenDto(string DuongDanFile, string TenFileGoc, long KichThuocFile, string LoaiFile)`. File phục vụ tĩnh tại `{DuongDanFile}` (vd `/uploads/xxx.png`) không cần JWT (ảnh hiển thị qua thẻ `<img>` không gửi kèm header Authorization được) — Task 8 (frontend, gửi ảnh/file) build trực tiếp trên DTO và đường dẫn này.

- [ ] **Bước 1: Viết test cho `TinNhanController` (sẽ fail vì chưa có controller)**

Tạo `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using Xunit;

namespace HaloChat.Api.Tests;

public class TinNhanControllerTests : IClassFixture<ThietLapKiemThuTichHop>
{
    private readonly ThietLapKiemThuTichHop _factory;
    private readonly HttpClient _client;

    public TinNhanControllerTests(ThietLapKiemThuTichHop factory)
    {
        _factory = factory;
        _factory.KhoGiaLap.DanhSach.Clear();
        _factory.KhoTinNhanGiaLap.DanhSach.Clear();
        _client = factory.CreateClient();
    }

    private record DangNhapResponseGiaLap(string Token);

    private async Task<string> DangKyVaDangNhapAsync(string tenTaiKhoan)
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new
        {
            TenTaiKhoan = tenTaiKhoan,
            Email = $"{tenTaiKhoan}@gmail.com",
            MatKhau = "MatKhau123",
        });
        var phanHoi = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new
        {
            TenDangNhap = tenTaiKhoan,
            MatKhau = "MatKhau123",
        });
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<DangNhapResponseGiaLap>();
        return ketQua!.Token;
    }

    [Fact]
    public async Task LayLichSu_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.GetAsync("/api/tinnhan/nguoi-dung/000000000000000000000000");
        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayLichSu_DaDangNhap_TraVeDanhSach()
    {
        var tokenA = await DangKyVaDangNhapAsync("tinnhannguoia");
        var idA = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "tinnhannguoia").Id;
        await DangKyVaDangNhapAsync("tinnhannguoib");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "tinnhannguoib").Id;

        _factory.KhoTinNhanGiaLap.DanhSach.Add(new TinNhan
        {
            NguoiGuiId = idA,
            NguoiNhanId = idB,
            NoiDungTinNhan = "Xin chào",
        });

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.GetAsync($"/api/tinnhan/nguoi-dung/{idB}");

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var danhSach = await phanHoi.Content.ReadFromJsonAsync<List<TinNhanDto>>();
        Assert.Single(danhSach!);
    }

    [Fact]
    public async Task TaiLen_DinhDangKhongDuocHoTro_TraVe400()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannguoic");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var noiDung = new MultipartFormDataContent();
        noiDung.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "tep", "vi-du.exe");

        var phanHoi = await _client.PostAsync("/api/tinnhan/upload", noiDung);

        Assert.Equal(HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task TaiLen_AnhHopLe_TraVe200VaDuongDanFile()
    {
        var token = await DangKyVaDangNhapAsync("tinnhannguoid");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var noiDung = new MultipartFormDataContent();
        noiDung.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "tep", "anh-mau.png");

        var phanHoi = await _client.PostAsync("/api/tinnhan/upload", noiDung);

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<TepTinDaTaiLenDto>();
        Assert.StartsWith("/uploads/", ketQua!.DuongDanFile);
        Assert.EndsWith(".png", ketQua.DuongDanFile);

        // Dọn file test tạo ra trên đĩa thật (thư mục uploads/ đã gitignore,
        // nhưng dọn để không tích tụ rác qua nhiều lần chạy test cục bộ).
        var duongDanThat = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "HaloChat.Api",
            "uploads", Path.GetFileName(ketQua.DuongDanFile));
        if (File.Exists(duongDanThat)) File.Delete(duongDanThat);
    }
}
```

- [ ] **Bước 2: Chạy test để xác nhận fail**

Run: `dotnet test backend/HaloChat.sln --filter TinNhanControllerTests`
Expected: FAIL (404 — chưa có route `/api/tinnhan/...`).

- [ ] **Bước 3: Tạo `TepTinDaTaiLenDto`**

Tạo `backend/HaloChat.Api/Dto/TepTinDaTaiLenDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record TepTinDaTaiLenDto(string DuongDanFile, string TenFileGoc, long KichThuocFile, string LoaiFile);
```

- [ ] **Bước 4: Tạo `TinNhanController`**

Tạo `backend/HaloChat.Api/Controllers/TinNhanController.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using HaloChat.Api.Dto;
using HaloChat.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HaloChat.Api.Controllers;

[ApiController]
[Route("api/tinnhan")]
[Authorize]
public class TinNhanController : ControllerBase
{
    // Allow-list theo spec §7: chấp nhận đúng các phần mở rộng này, mọi thứ
    // khác (kể cả file thực thi) bị từ chối — đơn giản và an toàn hơn một
    // block-list liệt kê phần mở rộng nguy hiểm.
    private static readonly Dictionary<string, string> LoaiAnhChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    private static readonly Dictionary<string, string> LoaiFileChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".zip"] = "application/zip",
    };

    private const long GioiHanAnhBytes = 5L * 1024 * 1024;
    private const long GioiHanFileBytes = 20L * 1024 * 1024;

    private readonly IDichVuTinNhan _dichVuTinNhan;
    private readonly IWebHostEnvironment _moiTruong;

    public TinNhanController(IDichVuTinNhan dichVuTinNhan, IWebHostEnvironment moiTruong)
    {
        _dichVuTinNhan = dichVuTinNhan;
        _moiTruong = moiTruong;
    }

    private string? IdHienTai => User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    [HttpGet("nguoi-dung/{id}")]
    public async Task<IActionResult> LayLichSu(string id, [FromQuery] string? truoc, [FromQuery] int soLuong = 30)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        var soLuongThucTe = Math.Clamp(soLuong, 1, 100);
        var lichSu = await _dichVuTinNhan.LayLichSuAsync(IdHienTai, id, truoc, soLuongThucTe);
        return Ok(lichSu);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(GioiHanFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = GioiHanFileBytes)]
    public async Task<IActionResult> TaiLen(IFormFile tep)
    {
        if (tep.Length == 0)
        {
            return BadRequest(new { thongBao = "File rỗng." });
        }

        var phanMoRong = Path.GetExtension(tep.FileName);
        var laAnh = LoaiAnhChoPhep.TryGetValue(phanMoRong, out var mimeAnh);
        var laFile = !laAnh && LoaiFileChoPhep.TryGetValue(phanMoRong, out var mimeFile);

        if (!laAnh && !laFile)
        {
            return BadRequest(new { thongBao = "Định dạng file không được hỗ trợ." });
        }

        var gioiHan = laAnh ? GioiHanAnhBytes : GioiHanFileBytes;
        if (tep.Length > gioiHan)
        {
            return BadRequest(new { thongBao = $"File vượt quá giới hạn {gioiHan / 1024 / 1024}MB." });
        }

        var thuMucTaiLen = Path.Combine(_moiTruong.ContentRootPath, "uploads");
        Directory.CreateDirectory(thuMucTaiLen);
        var tenFileLuu = $"{Guid.NewGuid()}{phanMoRong}";
        var duongDanDayDu = Path.Combine(thuMucTaiLen, tenFileLuu);

        // Stream thẳng ra đĩa (spec §8) — CopyToAsync không tạo byte[] trung
        // gian trong bộ nhớ ứng dụng.
        await using (var luongGhi = new FileStream(duongDanDayDu, FileMode.Create))
        {
            await tep.CopyToAsync(luongGhi);
        }

        return Ok(new TepTinDaTaiLenDto($"/uploads/{tenFileLuu}", tep.FileName, tep.Length, (laAnh ? mimeAnh : mimeFile)!));
    }
}
```

- [ ] **Bước 5: Phục vụ file tĩnh từ `uploads/` trong `Program.cs`**

Thêm `using Microsoft.Extensions.FileProviders;` vào đầu `backend/HaloChat.Api/Program.cs`, và thêm đoạn sau ngay sau `app.UseCors(TenChinhSachCors);` (trước `app.UseAuthentication();`):

```csharp
// File tải lên phục vụ công khai qua đường dẫn tĩnh, không cần JWT — thẻ
// <img>/<a> không tự đính kèm được header Authorization. Tên file là GUID
// ngẫu nhiên nên chỉ ai có đúng đường link (nhận qua tin nhắn) mới xem được.
var thuMucTaiLen = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(thuMucTaiLen);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(thuMucTaiLen),
    RequestPath = "/uploads",
});
```

- [ ] **Bước 6: Chạy lại test để xác nhận pass**

Run: `dotnet test backend/HaloChat.sln --filter TinNhanControllerTests`
Expected: PASS (4/4).

- [ ] **Bước 7: Chạy toàn bộ test suite backend**

Run: `dotnet test backend/HaloChat.sln`
Expected: PASS toàn bộ (không phá vỡ test cũ).

- [ ] **Bước 8: Commit**

```bash
git add backend/
git commit -m "Them TinNhanController: lich su phan trang + upload anh/file (GD5a)"
```

---

## Task 5: Frontend — Kiểu dữ liệu, DichVuApi, giải mã JWT

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts` (thêm `TinNhan`, `TepTinDaTaiLen`)
- Modify: `frontend/src/DichVuApi.ts` (export `DIA_CHI_GOC`, thêm `LayLichSuTinNhan`, `TaiLenTep`)
- Modify: `frontend/src/DichVuApi.test.ts` (test 2 hàm mới)
- Create: `frontend/src/TienIch/GiaiMaJwt.ts`
- Create: `frontend/src/TienIch/GiaiMaJwt.test.ts`
- Modify: `frontend/src/NguCanh/NguCanhXacThuc.tsx` (thêm `nguoiDungHienTai`)
- Modify: `frontend/src/NguCanh/NguCanhXacThuc.test.tsx` (test `nguoiDungHienTai`)

**Interfaces:**
- Consumes: không có (task độc lập, chỉ dựa trên DTO backend đã thống nhất ở Task 2-4).
- Produces: type `TinNhan`, `TepTinDaTaiLen`, `LoaiTinNhan` (KieuDuLieu.ts); `LayLichSuTinNhan(token, nguoiKiaId, truoc?, soLuong?) : Promise<TinNhan[]>`, `TaiLenTep(token, tep: File) : Promise<TepTinDaTaiLen>`, hằng số `DIA_CHI_GOC` (origin gốc, không có `/api`) — Task 6-8 dùng để build URL Hub và hiển thị ảnh/file. `useXacThuc().nguoiDungHienTai: { id, tenTaiKhoan, email } | null` — Task 7 dùng `id` để phân biệt tin nhắn của mình.

- [ ] **Bước 1: Viết test cho `GiaiMaJwt` (sẽ fail vì chưa có file)**

Tạo `frontend/src/TienIch/GiaiMaJwt.test.ts`:

```ts
import { describe, it, expect } from 'vitest';
import { GiaiMaJwt } from './GiaiMaJwt';

function taoJwtGiaLap(payload: object): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const than = btoa(JSON.stringify(payload));
  return `${header}.${than}.chu-ky-gia`;
}

describe('GiaiMaJwt', () => {
  it('giải mã đúng payload từ token hợp lệ', () => {
    const token = taoJwtGiaLap({ sub: '123', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' });

    const ketQua = GiaiMaJwt(token);

    expect(ketQua).toEqual({ sub: '123', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' });
  });

  it('giải mã đúng ký tự có dấu tiếng Việt', () => {
    const token = taoJwtGiaLap({ sub: '1', tenTaiKhoan: 'Nguyễn Ăn', email: 'a@gmail.com' });

    expect(GiaiMaJwt(token)?.tenTaiKhoan).toBe('Nguyễn Ăn');
  });

  it('trả về null với token không hợp lệ', () => {
    expect(GiaiMaJwt('khong-phai-jwt')).toBeNull();
  });
});
```

- [ ] **Bước 2: Chạy test để xác nhận fail**

Run: `npm run test --prefix frontend -- GiaiMaJwt`
Expected: FAIL (không tìm thấy module `./GiaiMaJwt`).

- [ ] **Bước 3: Tạo `GiaiMaJwt.ts`**

Tạo `frontend/src/TienIch/GiaiMaJwt.ts`:

```ts
export interface PayloadJwt {
  sub: string;
  tenTaiKhoan: string;
  email: string;
}

/** Giải mã phần payload của JWT — chỉ đọc, không xác minh chữ ký (server đã xác minh). */
export function GiaiMaJwt(token: string): PayloadJwt | null {
  try {
    const phanThan = token.split('.')[1];
    if (!phanThan) return null;

    const base64 = phanThan.replace(/-/g, '+').replace(/_/g, '/');
    const vanBan = decodeURIComponent(
      atob(base64)
        .split('')
        .map((ky) => '%' + ky.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    );
    return JSON.parse(vanBan) as PayloadJwt;
  } catch {
    return null;
  }
}
```

- [ ] **Bước 4: Chạy lại test để xác nhận pass**

Run: `npm run test --prefix frontend -- GiaiMaJwt`
Expected: PASS (3/3).

- [ ] **Bước 5: Thêm `nguoiDungHienTai` vào `NguCanhXacThuc`**

Sửa `frontend/src/NguCanh/NguCanhXacThuc.tsx` thành:

```tsx
import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { DangNhap as GoiDangNhap } from '../DichVuApi';
import { GiaiMaJwt } from '../TienIch/GiaiMaJwt';

interface NguoiDungHienTai {
  id: string;
  tenTaiKhoan: string;
  email: string;
}

interface TrangThaiXacThuc {
  token: string | null;
  daDangNhap: boolean;
  nguoiDungHienTai: NguoiDungHienTai | null;
  dangNhap: (tenDangNhap: string, matKhau: string) => Promise<void>;
  dangXuat: () => void;
}

const KHOA_LUU_TOKEN = 'haloChatToken';

const BoiCanhXacThuc = createContext<TrangThaiXacThuc | undefined>(undefined);

export function NhaCungCapXacThuc({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(KHOA_LUU_TOKEN));

  const nguoiDungHienTai = useMemo<NguoiDungHienTai | null>(() => {
    if (!token) return null;
    const payload = GiaiMaJwt(token);
    if (!payload) return null;
    return { id: payload.sub, tenTaiKhoan: payload.tenTaiKhoan, email: payload.email };
  }, [token]);

  async function dangNhap(tenDangNhap: string, matKhau: string) {
    const ketQua = await GoiDangNhap(tenDangNhap, matKhau);
    localStorage.setItem(KHOA_LUU_TOKEN, ketQua.token);
    setToken(ketQua.token);
  }

  function dangXuat() {
    localStorage.removeItem(KHOA_LUU_TOKEN);
    setToken(null);
  }

  return (
    <BoiCanhXacThuc.Provider value={{ token, daDangNhap: token !== null, nguoiDungHienTai, dangNhap, dangXuat }}>
      {children}
    </BoiCanhXacThuc.Provider>
  );
}

export function useXacThuc(): TrangThaiXacThuc {
  const giaTri = useContext(BoiCanhXacThuc);
  if (!giaTri) {
    throw new Error('useXacThuc phải được dùng bên trong NhaCungCapXacThuc');
  }
  return giaTri;
}
```

- [ ] **Bước 6: Thêm test cho `nguoiDungHienTai`**

Thêm vào cuối `describe('NguCanhXacThuc', ...)` trong `frontend/src/NguCanh/NguCanhXacThuc.test.tsx` (giữ nguyên các test hiện có, chỉ thêm 1 test mới và 1 component thử nghiệm mới):

```tsx
  it('nguoiDungHienTai giải mã đúng từ token trong localStorage', () => {
    const than = btoa(JSON.stringify({ sub: '42', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${than}.chuky`);

    function ThanhPhanNguoiDungHienTai() {
      const { nguoiDungHienTai } = useXacThuc();
      return <span>{nguoiDungHienTai?.tenTaiKhoan ?? 'khong-co'}</span>;
    }

    render(
      <NhaCungCapXacThuc>
        <ThanhPhanNguoiDungHienTai />
      </NhaCungCapXacThuc>,
    );

    expect(screen.getByText('NguyenAn')).toBeInTheDocument();
  });
```

- [ ] **Bước 7: Chạy test để xác nhận pass**

Run: `npm run test --prefix frontend -- NguCanhXacThuc`
Expected: PASS toàn bộ (4/4).

- [ ] **Bước 8: Thêm type + hàm API mới**

Thêm vào cuối `frontend/src/KieuDuLieu.ts`:

```ts
export type LoaiTinNhan = 'Text' | 'Anh' | 'File';

export interface TinNhan {
  id: string;
  nguoiGuiId: string;
  nguoiNhanId: string | null;
  loaiTinNhan: LoaiTinNhan;
  noiDungTinNhan: string;
  duongDanFile: string | null;
  tenFileGoc: string | null;
  kichThuocFile: number | null;
  loaiFile: string | null;
  daDoc: boolean;
  thoiGianTao: string;
}

export interface TepTinDaTaiLen {
  duongDanFile: string;
  tenFileGoc: string;
  kichThuocFile: number;
  loaiFile: string;
}
```

Sửa đầu `frontend/src/DichVuApi.ts` — đổi hằng số gốc thành 2 hằng (giữ nguyên phần còn lại của file, kể cả `goiApi`/`LoiGoiApi`):

```ts
import type { KetQuaDangKy, KetQuaDangNhap, NguoiDungTomTat, TinNhan, TepTinDaTaiLen } from './KieuDuLieu';

export const DIA_CHI_GOC = 'http://localhost:5231';
const DIA_CHI_GOC_API = `${DIA_CHI_GOC}/api`;
```

Thêm vào cuối `frontend/src/DichVuApi.ts`:

```ts
export async function LayLichSuTinNhan(
  token: string,
  nguoiKiaId: string,
  truoc?: string,
  soLuong = 30,
): Promise<TinNhan[]> {
  const thamSo = new URLSearchParams({ soLuong: String(soLuong) });
  if (truoc) thamSo.set('truoc', truoc);
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${nguoiKiaId}?${thamSo.toString()}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function TaiLenTep(token: string, tep: File): Promise<TepTinDaTaiLen> {
  const duLieu = new FormData();
  duLieu.append('tep', tep);

  let phanHoi: Response;
  try {
    phanHoi = await fetch(`${DIA_CHI_GOC_API}/tinnhan/upload`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: duLieu,
    });
  } catch {
    throw new LoiGoiApi(0, 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra backend đang chạy.');
  }

  const vanBan = await phanHoi.text();
  let ketQua: unknown = null;
  if (vanBan) {
    try {
      ketQua = JSON.parse(vanBan);
    } catch {
      ketQua = null;
    }
  }

  if (!phanHoi.ok) {
    const thongBao =
      (ketQua as { thongBao?: string } | null)?.thongBao ?? 'Tải file lên thất bại, vui lòng thử lại.';
    throw new LoiGoiApi(phanHoi.status, thongBao);
  }

  return ketQua as TepTinDaTaiLen;
}
```

> Lưu ý: `TaiLenTep` không dùng chung `goiApi` vì `FormData` **không được** kèm header `Content-Type` thủ công (trình duyệt tự thêm boundary multipart) — `goiApi` hiện có không nhận tham số bỏ qua việc set header, nên viết logic fetch riêng (lặp lại phần xử lý lỗi mạng/JSON của `goiApi` là chấp nhận được ở quy mô 1 hàm).

- [ ] **Bước 9: Thêm test cho 2 hàm mới**

Thêm vào cuối `frontend/src/DichVuApi.test.ts`:

```ts
  it('LayLichSuTinNhan gửi kèm Bearer token và query đúng', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 }));
    vi.stubGlobal('fetch', fetchGiaLap);

    await LayLichSuTinNhan('token-gia-lap', 'nguoi-kia-id', 'truoc-id', 10);

    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/tinnhan/nguoi-dung/nguoi-kia-id?soLuong=10&truoc=truoc-id'),
      expect.objectContaining({ headers: { Authorization: 'Bearer token-gia-lap' } }),
    );
  });

  it('TaiLenTep gửi FormData và trả về metadata khi thành công', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({ duongDanFile: '/uploads/x.png', tenFileGoc: 'x.png', kichThuocFile: 10, loaiFile: 'image/png' }),
        { status: 200 },
      ),
    );
    vi.stubGlobal('fetch', fetchGiaLap);
    const tep = new File(['abc'], 'x.png', { type: 'image/png' });

    const ketQua = await TaiLenTep('token-gia-lap', tep);

    expect(ketQua.duongDanFile).toBe('/uploads/x.png');
    const [, tuyChon] = fetchGiaLap.mock.calls[0];
    expect(tuyChon.body).toBeInstanceOf(FormData);
  });

  it('TaiLenTep ném LoiGoiApi khi file bị từ chối (400)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ thongBao: 'Định dạng file không được hỗ trợ.' }), { status: 400 })),
    );
    const tep = new File(['abc'], 'x.exe', { type: 'application/octet-stream' });

    await expect(TaiLenTep('token-gia-lap', tep)).rejects.toMatchObject({
      trangThai: 400,
      message: 'Định dạng file không được hỗ trợ.',
    });
  });
```

Thêm `LayLichSuTinNhan, TaiLenTep` vào dòng import ở đầu file test.

- [ ] **Bước 10: Chạy toàn bộ test frontend, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: PASS toàn bộ.

- [ ] **Bước 11: Commit**

```bash
git add frontend/
git commit -m "Them kieu du lieu TinNhan, LayLichSuTinNhan/TaiLenTep, giai ma JWT (GD5a)"
```

---

## Task 6: Frontend — DichVuSignalR + NguCanhChat

**Files:**
- Modify: `frontend/package.json` (thêm `@microsoft/signalr`)
- Create: `frontend/src/DichVuSignalR.ts`
- Create: `frontend/src/NguCanh/NguCanhChat.tsx`
- Create: `frontend/src/NguCanh/NguCanhChat.test.tsx`

**Interfaces:**
- Consumes: `useXacThuc().token` (Task 5), `DIA_CHI_GOC` (Task 5).
- Produces: `useChat() : { ketNoi: HubConnection | null; dangKetNoi: boolean }` — **context có giá trị mặc định (không throw khi không có Provider)**, để `TrangChat`/`DinhTuyen.test.tsx` không bắt buộc phải bọc `NhaCungCapChat` khi test không cần realtime. Task 7-8 dùng `ketNoi.invoke(...)`/`ketNoi.on(...)`/`ketNoi.off(...)` trực tiếp.

- [ ] **Bước 1: Cài đặt `@microsoft/signalr`**

```bash
npm install @microsoft/signalr@9.0.19 --prefix frontend
```

- [ ] **Bước 2: Viết test cho `NguCanhChat` (sẽ fail vì chưa có file)**

Tạo `frontend/src/NguCanh/NguCanhChat.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NhaCungCapChat, useChat } from './NguCanhChat';
import { NhaCungCapXacThuc } from './NguCanhXacThuc';

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
  HubConnectionBuilder: vi.fn().mockImplementation(() => ({
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn().mockReturnValue(ketNoiGiaLap),
  })),
  LogLevel: { Warning: 2 },
}));

function TrangThuNghiem() {
  const { dangKetNoi } = useChat();
  return <p>{dangKetNoi ? 'da-ket-noi' : 'chua-ket-noi'}</p>;
}

describe('NhaCungCapChat', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('kết nối ChatHub khi đã có token đăng nhập', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');

    render(
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangThuNghiem />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>,
    );

    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalledTimes(1));
    expect(await screen.findByText('da-ket-noi')).toBeInTheDocument();
  });

  it('không tạo kết nối khi chưa đăng nhập', () => {
    render(
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangThuNghiem />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>,
    );

    expect(ketNoiGiaLap.start).not.toHaveBeenCalled();
    expect(screen.getByText('chua-ket-noi')).toBeInTheDocument();
  });
});
```

- [ ] **Bước 3: Chạy test để xác nhận fail**

Run: `npm run test --prefix frontend -- NguCanhChat`
Expected: FAIL (không tìm thấy module `./NguCanhChat`).

- [ ] **Bước 4: Tạo `DichVuSignalR.ts`**

Tạo `frontend/src/DichVuSignalR.ts`:

```ts
import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr';
import { DIA_CHI_GOC } from './DichVuApi';

export function TaoKetNoiChat(token: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(`${DIA_CHI_GOC}/hub/chat`, { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}
```

- [ ] **Bước 5: Tạo `NguCanhChat.tsx`**

Tạo `frontend/src/NguCanh/NguCanhChat.tsx`:

```tsx
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { TaoKetNoiChat } from '../DichVuSignalR';
import { useXacThuc } from './NguCanhXacThuc';

interface TrangThaiChat {
  ketNoi: HubConnection | null;
  dangKetNoi: boolean;
}

// Giá trị mặc định KHÔNG throw (khác NguCanhXacThuc cố ý): nơi nào chưa bọc
// NhaCungCapChat (vd DinhTuyen.test.tsx hiện có) vẫn chạy được, chỉ là chưa
// có realtime — tránh phải sửa lại các test không liên quan tới chat.
const BoiCanhChat = createContext<TrangThaiChat>({ ketNoi: null, dangKetNoi: false });

export function NhaCungCapChat({ children }: { children: ReactNode }) {
  const { token } = useXacThuc();
  const [ketNoi, setKetNoi] = useState<HubConnection | null>(null);
  const [dangKetNoi, setDangKetNoi] = useState(false);

  useEffect(() => {
    if (!token) {
      setKetNoi(null);
      setDangKetNoi(false);
      return;
    }

    const ketNoiMoi = TaoKetNoiChat(token);
    ketNoiMoi.onreconnected(() => setDangKetNoi(true));
    ketNoiMoi.onreconnecting(() => setDangKetNoi(false));
    ketNoiMoi.onclose(() => setDangKetNoi(false));

    ketNoiMoi
      .start()
      .then(() => setDangKetNoi(true))
      .catch((loi) => {
        // Không throw ra ngoài: mất kết nối realtime không được phép làm sập
        // trang — người dùng vẫn dùng được REST (lịch sử, gửi ảnh/file).
        console.error('Không kết nối được ChatHub:', loi);
      });

    setKetNoi(ketNoiMoi);

    return () => {
      setDangKetNoi(false);
      void ketNoiMoi.stop();
    };
  }, [token]);

  return <BoiCanhChat.Provider value={{ ketNoi, dangKetNoi }}>{children}</BoiCanhChat.Provider>;
}

export function useChat(): TrangThaiChat {
  return useContext(BoiCanhChat);
}
```

- [ ] **Bước 6: Chạy lại test để xác nhận pass**

Run: `npm run test --prefix frontend -- NguCanhChat`
Expected: PASS (2/2).

- [ ] **Bước 7: Commit**

```bash
git add frontend/
git commit -m "Them DichVuSignalR + NguCanhChat: vong doi ket noi ChatHub (GD5a)"
```

---

## Task 7: Frontend — TrangChat (danh sách + hội thoại realtime, chỉ văn bản)

**Files:**
- Create: `frontend/src/Trang/TrangChat.tsx`
- Create: `frontend/src/Trang/TrangChat.css`
- Create: `frontend/src/Trang/TrangChat.test.tsx`

**Interfaces:**
- Consumes: `useXacThuc()` (token, `nguoiDungHienTai`, `dangXuat`), `useChat()` (Task 6), `LayDanhSachNguoiDung`, `LayLichSuTinNhan` (Task 5).
- Produces: component `TrangChat` (chưa export gì cho task khác dùng lại — Task 8 **mở rộng trực tiếp file này**, không tạo file mới).

- [ ] **Bước 1: Viết test cho `TrangChat` (sẽ fail vì chưa có component)**

Tạo `frontend/src/Trang/TrangChat.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangChat } from './TrangChat';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';

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
  HubConnectionBuilder: vi.fn().mockImplementation(() => ({
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn().mockReturnValue(ketNoiGiaLap),
  })),
  LogLevel: { Warning: 2 },
}));

function renderTrangChat() {
  return render(
    <NhaCungCapXacThuc>
      <NhaCungCapChat>
        <TrangChat />
      </NhaCungCapChat>
    </NhaCungCapXacThuc>,
  );
}

function taoTinNhanGiaLap(gan: Partial<Awaited<ReturnType<typeof DichVuApi.LayLichSuTinNhan>>[number]>) {
  return {
    id: 'm1',
    nguoiGuiId: '2',
    nguoiNhanId: '1',
    loaiTinNhan: 'Text' as const,
    noiDungTinNhan: 'Chào bạn',
    duongDanFile: null,
    tenFileGoc: null,
    kichThuocFile: null,
    loaiFile: null,
    daDoc: false,
    thoiGianTao: new Date().toISOString(),
    ...gan,
  };
}

describe('TrangChat', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' },
    ]);
  });

  it('hiển thị danh sách người dùng sau khi tải', async () => {
    renderTrangChat();

    expect(await screen.findByText('TranBinh')).toBeInTheDocument();
  });

  it('chọn 1 người thì tải và hiển thị lịch sử tin nhắn', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([taoTinNhanGiaLap({})]);

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));

    expect(await screen.findByText('Chào bạn')).toBeInTheDocument();
  });

  it('gửi tin nhắn văn bản gọi ketNoi.invoke và hiển thị tin nhắn vừa gửi', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    ketNoiGiaLap.invoke.mockResolvedValue(
      taoTinNhanGiaLap({ id: 'm2', nguoiGuiId: '1', nguoiNhanId: '2', noiDungTinNhan: 'Xin chào' }),
    );

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Xin chào');
    await userEvent.click(screen.getByRole('button', { name: 'Gửi' }));

    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('GuiTinNhan', '2', 'Text', 'Xin chào', null, null, null, null),
    );
    expect(await screen.findByText('Xin chào')).toBeInTheDocument();
  });

  it('nhận tin nhắn realtime qua sự kiện NhanTinNhan hiển thị ngay trong khung đang mở', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.on).toHaveBeenCalledWith('NhanTinNhan', expect.any(Function)));

    const handler = ketNoiGiaLap.on.mock.calls.find(([ten]: [string]) => ten === 'NhanTinNhan')![1];
    handler(taoTinNhanGiaLap({ id: 'm3', noiDungTinNhan: 'Tin nhắn realtime' }));

    expect(await screen.findByText('Tin nhắn realtime')).toBeInTheDocument();
  });
});
```

- [ ] **Bước 2: Chạy test để xác nhận fail**

Run: `npm run test --prefix frontend -- TrangChat`
Expected: FAIL (không tìm thấy module `./TrangChat`).

- [ ] **Bước 3: Tạo `TrangChat.tsx`**

Tạo `frontend/src/Trang/TrangChat.tsx`:

```tsx
import { useEffect, useMemo, useRef, useState } from 'react';
import { LayDanhSachNguoiDung, LayLichSuTinNhan, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import type { NguoiDungTomTat, TinNhan } from '../KieuDuLieu';
import './TrangChat.css';

function idNguoiKia(tinNhan: TinNhan, idHienTai: string): string {
  return tinNhan.nguoiGuiId === idHienTai ? (tinNhan.nguoiNhanId ?? '') : tinNhan.nguoiGuiId;
}

export function TrangChat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const { ketNoi, dangKetNoi } = useChat();

  const [danhSachNguoiDung, setDanhSachNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [nguoiDangChon, setNguoiDangChon] = useState<NguoiDungTomTat | null>(null);
  const [tinNhanTheoNguoiDung, setTinNhanTheoNguoiDung] = useState<Record<string, TinNhan[]>>({});
  const [dangTaiDanhSach, setDangTaiDanhSach] = useState(true);
  const [dangTaiLichSu, setDangTaiLichSu] = useState(false);
  const [noiDungDangGo, setNoiDungDangGo] = useState('');
  const [loi, setLoi] = useState<string | null>(null);
  const cuoiDanhSachRef = useRef<HTMLDivElement | null>(null);

  const idHienTai = nguoiDungHienTai?.id ?? '';

  useEffect(() => {
    if (!token) return;
    LayDanhSachNguoiDung(token)
      .then(setDanhSachNguoiDung)
      .catch((loiBat) => {
        if (loiBat instanceof LoiGoiApi && loiBat.trangThai === 401) {
          dangXuat();
          return;
        }
        setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
      })
      .finally(() => setDangTaiDanhSach(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  useEffect(() => {
    if (!token || !nguoiDangChon) return;
    if (tinNhanTheoNguoiDung[nguoiDangChon.id]) return;

    setDangTaiLichSu(true);
    LayLichSuTinNhan(token, nguoiDangChon.id)
      .then((moiNhatTruoc) => {
        const thuTuThoiGian = [...moiNhatTruoc].reverse();
        setTinNhanTheoNguoiDung((truoc) => ({ ...truoc, [nguoiDangChon.id]: thuTuThoiGian }));
      })
      .catch((loiBat) => {
        setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được lịch sử tin nhắn.');
      })
      .finally(() => setDangTaiLichSu(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, nguoiDangChon]);

  useEffect(() => {
    if (!ketNoi) return;

    function xuLyTinNhanMoi(tinNhan: TinNhan) {
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [idKia]: [...(truoc[idKia] ?? []), tinNhan],
      }));
    }

    ketNoi.on('NhanTinNhan', xuLyTinNhanMoi);
    return () => {
      ketNoi.off('NhanTinNhan', xuLyTinNhanMoi);
    };
  }, [ketNoi, idHienTai]);

  useEffect(() => {
    cuoiDanhSachRef.current?.scrollIntoView({ block: 'end' });
  }, [nguoiDangChon, tinNhanTheoNguoiDung]);

  const tinNhanDangHien = useMemo(
    () => (nguoiDangChon ? (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? []) : []),
    [nguoiDangChon, tinNhanTheoNguoiDung],
  );

  async function guiTinNhanVanBan() {
    if (!ketNoi || !nguoiDangChon || !noiDungDangGo.trim()) return;

    const noiDungGui = noiDungDangGo.trim();
    setNoiDungDangGo('');
    try {
      const tinNhanDaGui = await ketNoi.invoke<TinNhan>(
        'GuiTinNhan',
        nguoiDangChon.id,
        'Text',
        noiDungGui,
        null,
        null,
        null,
        null,
      );
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanDaGui],
      }));
    } catch {
      setLoi('Gửi tin nhắn thất bại. Vui lòng thử lại.');
    }
  }

  async function taiThemLichSuCu() {
    if (!token || !nguoiDangChon) return;
    const cuNhat = (tinNhanTheoNguoiDung[nguoiDangChon.id] ?? [])[0];
    if (!cuNhat) return;

    setDangTaiLichSu(true);
    try {
      const cuHon = await LayLichSuTinNhan(token, nguoiDangChon.id, cuNhat.id);
      const thuTuThoiGian = [...cuHon].reverse();
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [nguoiDangChon.id]: [...thuTuThoiGian, ...(truoc[nguoiDangChon.id] ?? [])],
      }));
    } catch {
      setLoi('Không tải được tin nhắn cũ hơn.');
    } finally {
      setDangTaiLichSu(false);
    }
  }

  return (
    <div className="trang-chat">
      <aside className="trang-chat__sidebar">
        <div className="trang-chat__sidebar-dau">
          <h1>HaloChat</h1>
          <button className="trang-chat__nut-dang-xuat" onClick={dangXuat}>
            Đăng xuất
          </button>
        </div>
        {dangTaiDanhSach && <p>Đang tải...</p>}
        <ul className="trang-chat__danh-sach">
          {danhSachNguoiDung.map((nd) => (
            <li key={nd.id}>
              <button
                className={`trang-chat__muc${nguoiDangChon?.id === nd.id ? ' trang-chat__muc--dang-chon' : ''}`}
                onClick={() => setNguoiDangChon(nd)}
              >
                <span className="trang-chat__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>
                <span className="trang-chat__ten">{nd.tenTaiKhoan}</span>
              </button>
            </li>
          ))}
        </ul>
      </aside>

      <main className="trang-chat__khung-chinh">
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        {!nguoiDangChon && <p className="trang-chat__trong">Chọn một người để bắt đầu trò chuyện.</p>}
        {nguoiDangChon && (
          <>
            <header className="trang-chat__tieu-de">
              <span>{nguoiDangChon.tenTaiKhoan}</span>
              {!dangKetNoi && <span className="trang-chat__mat-ket-noi">Mất kết nối realtime...</span>}
            </header>

            <div className="trang-chat__danh-sach-tin-nhan">
              <button className="trang-chat__nut-tai-them" onClick={taiThemLichSuCu} disabled={dangTaiLichSu}>
                {dangTaiLichSu ? 'Đang tải...' : 'Tải tin nhắn cũ hơn'}
              </button>
              {tinNhanDangHien.map((tn) => (
                <div
                  key={tn.id}
                  className={`trang-chat__bong-tin-nhan${tn.nguoiGuiId === idHienTai ? ' trang-chat__bong-tin-nhan--minh' : ''}`}
                >
                  {tn.noiDungTinNhan}
                </div>
              ))}
              <div ref={cuoiDanhSachRef} />
            </div>

            <form
              className="trang-chat__form-gui"
              onSubmit={(su) => {
                su.preventDefault();
                void guiTinNhanVanBan();
              }}
            >
              <input
                type="text"
                value={noiDungDangGo}
                onChange={(su) => setNoiDungDangGo(su.target.value)}
                placeholder="Nhập tin nhắn..."
                disabled={!dangKetNoi}
              />
              <button type="submit" disabled={!dangKetNoi || !noiDungDangGo.trim()}>
                Gửi
              </button>
            </form>
          </>
        )}
      </main>
    </div>
  );
}
```

- [ ] **Bước 4: Tạo `TrangChat.css`**

Tạo `frontend/src/Trang/TrangChat.css`:

```css
.trang-chat {
  height: 100vh;
  display: flex;
}

.trang-chat__sidebar {
  width: 280px;
  flex-shrink: 0;
  background: var(--mau-nen-the);
  border-right: 1px solid var(--mau-vien);
  display: flex;
  flex-direction: column;
}

.trang-chat__sidebar-dau {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px;
  border-bottom: 1px solid var(--mau-vien);
}

.trang-chat__sidebar-dau h1 {
  font-size: 18px;
  margin: 0;
  color: var(--mau-chinh-dam);
}

.trang-chat__nut-dang-xuat {
  border: 1px solid var(--mau-vien);
  background: #fff;
  color: var(--mau-chu-dam);
  border-radius: 999px;
  padding: 6px 14px;
  font-family: inherit;
  font-weight: 600;
  font-size: 12px;
  cursor: pointer;
}

.trang-chat__danh-sach {
  list-style: none;
  margin: 0;
  padding: 8px;
  overflow-y: auto;
}

.trang-chat__muc {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border: none;
  background: none;
  border-radius: var(--ban-kinh-o);
  cursor: pointer;
  text-align: left;
  font-family: inherit;
  font-size: 14px;
  color: var(--mau-chu-dam);
}

.trang-chat__muc:hover,
.trang-chat__muc--dang-chon {
  background: var(--mau-nen-tren);
}

.trang-chat__avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  flex-shrink: 0;
}

.trang-chat__khung-chinh {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.trang-chat__trong {
  margin: auto;
  color: var(--mau-chu-phu);
}

.trang-chat__tieu-de {
  padding: 16px 24px;
  border-bottom: 1px solid var(--mau-vien);
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 12px;
}

.trang-chat__mat-ket-noi {
  font-size: 12px;
  font-weight: 500;
  color: var(--mau-loi);
}

.trang-chat__danh-sach-tin-nhan {
  flex: 1;
  overflow-y: auto;
  padding: 16px 24px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.trang-chat__nut-tai-them {
  align-self: center;
  border: 1px solid var(--mau-vien);
  background: #fff;
  border-radius: 999px;
  padding: 6px 16px;
  font-size: 12px;
  font-family: inherit;
  cursor: pointer;
  margin-bottom: 8px;
}

.trang-chat__bong-tin-nhan {
  max-width: 60%;
  padding: 10px 14px;
  border-radius: var(--ban-kinh-o);
  background: var(--mau-nen-tren);
  align-self: flex-start;
}

.trang-chat__bong-tin-nhan--minh {
  align-self: flex-end;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
}

.trang-chat__form-gui {
  display: flex;
  gap: 12px;
  padding: 16px 24px;
  border-top: 1px solid var(--mau-vien);
}

.trang-chat__form-gui input[type='text'] {
  flex: 1;
  padding: 12px 16px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 14px;
}

.trang-chat__form-gui button[type='submit'] {
  border: none;
  border-radius: var(--ban-kinh-o);
  padding: 0 24px;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}

.trang-chat__form-gui button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 640px) {
  .trang-chat {
    flex-direction: column;
    height: auto;
    min-height: 100vh;
  }

  .trang-chat__sidebar {
    width: 100%;
    max-height: 40vh;
  }
}
```

- [ ] **Bước 5: Chạy lại test để xác nhận pass**

Run: `npm run test --prefix frontend -- TrangChat`
Expected: PASS (4/4).

- [ ] **Bước 6: Chạy toàn bộ test frontend**

Run: `npm run test --prefix frontend`
Expected: PASS toàn bộ.

- [ ] **Bước 7: Commit**

```bash
git add frontend/
git commit -m "Them TrangChat: danh sach + hoi thoai 1-1 realtime qua ChatHub (GD5a)"
```

---

## Task 8: Frontend — Gửi ảnh/file trong TrangChat

**Files:**
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx` (thêm `BieuTuongGhim`)
- Modify: `frontend/src/Trang/TrangChat.tsx` (thêm luồng upload)
- Modify: `frontend/src/Trang/TrangChat.css` (style ảnh/file/nút ghim)
- Modify: `frontend/src/Trang/TrangChat.test.tsx` (thêm test upload)

**Interfaces:**
- Consumes: `TaiLenTep`, `TepTinDaTaiLen` (Task 5), `ketNoi.invoke('GuiTinNhan', ...)` (Task 3/7).
- Produces: không có gì mới cho task khác — đây là task cuối chạm vào `TrangChat.tsx`.

- [ ] **Bước 1: Thêm biểu tượng ghim**

Thêm vào cuối `frontend/src/ThanhPhan/BieuTuong.tsx`:

```tsx
export function BieuTuongGhim() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M21.44 11.05 12.25 20.24a5 5 0 0 1-7.07-7.07l9.19-9.19a3.5 3.5 0 0 1 4.95 4.95L9.64 18.36a2 2 0 0 1-2.83-2.83l8.49-8.48" />
    </svg>
  );
}
```

- [ ] **Bước 2: Thêm 2 test upload (sẽ fail — chưa có input file trong TrangChat)**

Thêm vào cuối `describe('TrangChat', ...)` trong `frontend/src/Trang/TrangChat.test.tsx`:

```tsx
  it('chọn file ảnh gọi TaiLenTep rồi GuiTinNhan với loại Anh', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({
      duongDanFile: '/uploads/abc.png',
      tenFileGoc: 'anh.png',
      kichThuocFile: 1024,
      loaiFile: 'image/png',
    });
    ketNoiGiaLap.invoke.mockResolvedValue(
      taoTinNhanGiaLap({
        id: 'm4',
        nguoiGuiId: '1',
        nguoiNhanId: '2',
        loaiTinNhan: 'Anh',
        noiDungTinNhan: '',
        duongDanFile: '/uploads/abc.png',
        tenFileGoc: 'anh.png',
        kichThuocFile: 1024,
        loaiFile: 'image/png',
      }),
    );

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    const tep = new File(['noi-dung-gia-lap'], 'anh.png', { type: 'image/png' });
    const inputTep = document.querySelector('.trang-chat__input-tep') as HTMLInputElement;
    await userEvent.upload(inputTep, tep);

    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith(
        'GuiTinNhan', '2', 'Anh', '', '/uploads/abc.png', 'anh.png', 1024, 'image/png',
      ),
    );
    expect(await screen.findByRole('img')).toHaveAttribute('src', expect.stringContaining('/uploads/abc.png'));
  });

  it('file ảnh vượt quá 5MB bị chặn ở client, không gọi TaiLenTep', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    const taiLenSpy = vi.spyOn(DichVuApi, 'TaiLenTep');

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));

    const tepQuaKho = new File([new Uint8Array(6 * 1024 * 1024)], 'to.png', { type: 'image/png' });
    const inputTep = document.querySelector('.trang-chat__input-tep') as HTMLInputElement;
    await userEvent.upload(inputTep, tepQuaKho);

    expect(taiLenSpy).not.toHaveBeenCalled();
    expect(await screen.findByText(/vượt quá giới hạn/)).toBeInTheDocument();
  });
```

- [ ] **Bước 3: Chạy test để xác nhận fail**

Run: `npm run test --prefix frontend -- TrangChat`
Expected: FAIL (2 test mới — không tìm thấy `.trang-chat__input-tep`).

- [ ] **Bước 4: Sửa `TrangChat.tsx` — thêm luồng upload**

Sửa dòng import đầu file:

```tsx
import { useEffect, useMemo, useRef, useState } from 'react';
import { LayDanhSachNguoiDung, LayLichSuTinNhan, TaiLenTep, LoiGoiApi, DIA_CHI_GOC } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { useChat } from '../NguCanh/NguCanhChat';
import { BieuTuongGhim } from '../ThanhPhan/BieuTuong';
import type { NguoiDungTomTat, TinNhan } from '../KieuDuLieu';
import './TrangChat.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;
const GIOI_HAN_FILE_BYTES = 20 * 1024 * 1024;

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}
```

Thêm state + ref ngay sau khai báo `const [loi, setLoi] = useState<string | null>(null);`:

```tsx
  const [dangTaiTep, setDangTaiTep] = useState(false);
  const inputTepRef = useRef<HTMLInputElement | null>(null);
```

Thêm hàm `guiTep` ngay sau `guiTinNhanVanBan`:

```tsx
  async function guiTep(tep: File) {
    if (!ketNoi || !nguoiDangChon || !token) return;

    const laAnh = tep.type.startsWith('image/');
    const gioiHan = laAnh ? GIOI_HAN_ANH_BYTES : GIOI_HAN_FILE_BYTES;
    if (tep.size > gioiHan) {
      setLoi(`File vượt quá giới hạn ${gioiHan / 1024 / 1024}MB.`);
      return;
    }

    setDangTaiTep(true);
    try {
      const daTaiLen = await TaiLenTep(token, tep);
      const tinNhanDaGui = await ketNoi.invoke<TinNhan>(
        'GuiTinNhan',
        nguoiDangChon.id,
        laAnh ? 'Anh' : 'File',
        '',
        daTaiLen.duongDanFile,
        daTaiLen.tenFileGoc,
        daTaiLen.kichThuocFile,
        daTaiLen.loaiFile,
      );
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []), tinNhanDaGui],
      }));
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Gửi file thất bại. Vui lòng thử lại.');
    } finally {
      setDangTaiTep(false);
      if (inputTepRef.current) inputTepRef.current.value = '';
    }
  }
```

Sửa khối render 1 tin nhắn (bên trong `.map`) để phân biệt loại:

```tsx
              {tinNhanDangHien.map((tn) => (
                <div
                  key={tn.id}
                  className={`trang-chat__bong-tin-nhan${tn.nguoiGuiId === idHienTai ? ' trang-chat__bong-tin-nhan--minh' : ''}`}
                >
                  {tn.loaiTinNhan === 'Anh' && (
                    <img
                      className="trang-chat__anh-tin-nhan"
                      src={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                      alt={tn.tenFileGoc ?? 'ảnh'}
                    />
                  )}
                  {tn.loaiTinNhan === 'File' && (
                    <a
                      className="trang-chat__file-tin-nhan"
                      href={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      📎 {tn.tenFileGoc} ({dinhDangKichThuoc(tn.kichThuocFile ?? 0)})
                    </a>
                  )}
                  {tn.loaiTinNhan === 'Text' && tn.noiDungTinNhan}
                </div>
              ))}
```

Sửa `<form className="trang-chat__form-gui" ...>` để thêm nút ghim + input file, và thêm dòng "đang tải" ngay sau `</form>`:

```tsx
            <form
              className="trang-chat__form-gui"
              onSubmit={(su) => {
                su.preventDefault();
                void guiTinNhanVanBan();
              }}
            >
              <button
                type="button"
                className="trang-chat__nut-ghim"
                onClick={() => inputTepRef.current?.click()}
                disabled={!dangKetNoi || dangTaiTep}
                aria-label="Đính kèm file"
              >
                <BieuTuongGhim />
              </button>
              <input
                ref={inputTepRef}
                type="file"
                className="trang-chat__input-tep"
                accept="image/jpeg,image/png,image/gif,image/webp,application/pdf,.docx,.xlsx,.zip"
                onChange={(su) => {
                  const tep = su.target.files?.[0];
                  if (tep) void guiTep(tep);
                }}
              />
              <input
                type="text"
                value={noiDungDangGo}
                onChange={(su) => setNoiDungDangGo(su.target.value)}
                placeholder="Nhập tin nhắn..."
                disabled={!dangKetNoi}
              />
              <button type="submit" disabled={!dangKetNoi || !noiDungDangGo.trim()}>
                Gửi
              </button>
            </form>
            {dangTaiTep && <p className="trang-chat__dang-tai-tep">Đang tải file lên...</p>}
```

- [ ] **Bước 5: Thêm CSS cho ảnh/file/nút ghim**

Thêm vào cuối `frontend/src/Trang/TrangChat.css`:

```css
.trang-chat__input-tep {
  display: none;
}

.trang-chat__nut-ghim {
  border: 1px solid var(--mau-vien);
  background: #fff;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: var(--mau-chu-dam);
  flex-shrink: 0;
}

.trang-chat__anh-tin-nhan {
  max-width: 220px;
  border-radius: 12px;
  display: block;
}

.trang-chat__file-tin-nhan {
  color: inherit;
  text-decoration: underline;
}

.trang-chat__dang-tai-tep {
  margin: 0;
  padding: 0 24px 12px;
  font-size: 12px;
  color: var(--mau-chu-phu);
}
```

- [ ] **Bước 6: Chạy lại test để xác nhận pass**

Run: `npm run test --prefix frontend -- TrangChat`
Expected: PASS (6/6).

- [ ] **Bước 7: Chạy toàn bộ test frontend**

Run: `npm run test --prefix frontend`
Expected: PASS toàn bộ.

- [ ] **Bước 8: Commit**

```bash
git add frontend/
git commit -m "Them gui anh/file trong TrangChat: upload + hien thi preview (GD5a)"
```

---

## Task 9: Wiring cuối — thay trang chính, README, xác minh end-to-end

**Files:**
- Modify: `frontend/src/DinhTuyen.tsx` (dùng `TrangChat` thay `TrangDanhSachNguoiDung`)
- Modify: `frontend/src/App.tsx` (bọc `NhaCungCapChat`)
- Delete: `frontend/src/Trang/TrangDanhSachNguoiDung.tsx`
- Delete: `frontend/src/Trang/TrangDanhSachNguoiDung.css`
- Delete: `frontend/src/Trang/TrangDanhSachNguoiDung.test.tsx`
- Modify: `README.md` (cập nhật trạng thái GĐ5a + hướng dẫn xác minh)

**Interfaces:**
- Consumes: mọi thứ từ Task 1-8.
- Produces: ứng dụng chạy được end-to-end — task cuối của plan này.

- [ ] **Bước 1: Sửa `DinhTuyen.tsx`**

```tsx
import { Routes, Route, Navigate } from 'react-router-dom';
import { TuyenDuongRieng } from './ThanhPhan/TuyenDuongRieng';
import { TrangDangKy } from './Trang/TrangDangKy';
import { TrangDangNhap } from './Trang/TrangDangNhap';
import { TrangChat } from './Trang/TrangChat';

export function DinhTuyen() {
  return (
    <Routes>
      <Route path="/dang-ky" element={<TrangDangKy />} />
      <Route path="/dang-nhap" element={<TrangDangNhap />} />
      <Route
        path="/nguoi-dung"
        element={
          <TuyenDuongRieng>
            <TrangChat />
          </TuyenDuongRieng>
        }
      />
      <Route path="*" element={<Navigate to="/dang-nhap" replace />} />
    </Routes>
  );
}
```

> Giữ nguyên đường dẫn `/nguoi-dung` (không đổi tên route) để `DinhTuyen.test.tsx` hiện có **không cần sửa** — `useChat()` có giá trị mặc định an toàn (Task 6) nên `TrangChat` render bình thường kể cả không có `NhaCungCapChat` bao quanh trong test đó; nút "Đăng xuất" vẫn hiển thị y hệt trang cũ.

- [ ] **Bước 2: Sửa `App.tsx`**

```tsx
import { BrowserRouter } from 'react-router-dom';
import { NhaCungCapXacThuc } from './NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from './NguCanh/NguCanhChat';
import { DinhTuyen } from './DinhTuyen';

function App() {
  return (
    <BrowserRouter>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <DinhTuyen />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </BrowserRouter>
  );
}

export default App;
```

- [ ] **Bước 3: Xóa trang danh sách người dùng cũ (đã thay bằng TrangChat)**

```bash
git rm frontend/src/Trang/TrangDanhSachNguoiDung.tsx frontend/src/Trang/TrangDanhSachNguoiDung.css frontend/src/Trang/TrangDanhSachNguoiDung.test.tsx
```

- [ ] **Bước 4: Chạy toàn bộ test frontend — xác nhận `DinhTuyen.test.tsx` vẫn pass không sửa**

Run: `npm run test --prefix frontend`
Expected: PASS toàn bộ (không còn file test của `TrangDanhSachNguoiDung`, `DinhTuyen.test.tsx` không đổi mà vẫn xanh).

- [ ] **Bước 5: Cập nhật `README.md`**

Sửa mục "Trạng thái các giai đoạn" ở cuối `README.md`:

```markdown
## Trạng thái các giai đoạn

- GĐ3 (Backend nền tảng — đăng ký/đăng nhập/JWT/danh sách người dùng): hoàn thành.
- GĐ4 (Frontend nền tảng — trang đăng ký/đăng nhập/danh sách người dùng, giao diện HaloChat): hoàn thành.
- GĐ5a (Chat realtime lõi — SignalR ChatHub, nhắn tin 1-1, gửi ảnh/file, lịch sử phân trang):
  hoàn thành. Nhắn tin hiện mở tự do giữa mọi người dùng đã đăng nhập (chưa có kết bạn — xem spec §10.1).
- GĐ5b (Kết bạn + nhóm chat + thông báo realtime), GĐ6 (Bảo mật AES/RSA — nhóm tự viết), GĐ7
  (Quên mật khẩu): chưa bắt đầu.

### Xác minh GĐ5a thủ công

1. Chạy backend + frontend (xem hướng dẫn phía trên).
2. Mở 2 trình duyệt (hoặc 1 cửa sổ ẩn danh + 1 bình thường), đăng ký 2 tài khoản khác nhau, đăng nhập cả hai.
3. Ở tài khoản A, chọn tài khoản B trong danh sách, gửi 1 tin nhắn văn bản — xác nhận B nhận được ngay (không cần F5).
4. Gửi 1 ảnh (< 5MB) và 1 file `.pdf`/`.docx` (< 20MB) — xác nhận hiển thị ảnh/link file ở cả 2 phía.
5. Mở MongoDB Atlas, kiểm tra collection `TinNhan` có đủ các bản ghi vừa gửi (kể cả `DuongDanFile`/`TenFileGoc` cho ảnh/file).
6. Thử gửi file `.exe` — xác nhận bị từ chối với thông báo lỗi.
```

- [ ] **Bước 6: Commit**

```bash
git add -A
git commit -m "Noi TrangChat vao dinh tuyen chinh, xoa trang danh sach cu, cap nhat README (GD5a)"
```

- [ ] **Bước 7: Xác minh cuối — build + test toàn bộ 2 phía**

Run: `dotnet test backend/HaloChat.sln && npm run test --prefix frontend`
Expected: PASS toàn bộ cả 2 phía. Đây là cổng cuối trước khi plan này coi là hoàn thành — nếu bất kỳ test nào fail, dừng lại xử lý trước khi báo cáo hoàn thành.

---

## Ghi chú cho GĐ5b (không thuộc phạm vi plan này)

Khi viết plan GĐ5b (kết bạn + nhóm chat + thông báo, spec §10), các điểm nối cần nhớ:

- `DichVuTinNhan.GuiTinNhanAsync` sẽ cần thêm tham số kiểm tra bạn bè/`ChoPhepTinNhanTuNguoiLa` (spec §10.1) — hiện tại đang cho phép tự do (Global Constraints của plan này).
- `TinNhan.NhomId` đã có sẵn trong model (Task 1) nhưng chưa dùng — GĐ5b thêm `Nhom` + logic gửi tin nhắn nhóm sẽ tái dùng field này, không cần đổi schema.
- `ChatHub.OnConnectedAsync` (spec §10.3, add-to-group khi có `Nhom`) chưa được cài ở GĐ5a vì chưa có `Nhom` — sẽ thêm khi GĐ5b có collection đó.
- Dropdown thông báo (spec §10.5) sẽ cần thêm sự kiện Hub mới (`NhanLoiMoiKetBan`, `LoiMoiKetBanDuocChapNhan`, `DuocThemVaoNhom`) — `NguCanhChat` hiện tại đã export `ketNoi` trực tiếp nên các trang GĐ5b có thể tự `ketNoi.on(...)` thêm mà không cần sửa context này.
