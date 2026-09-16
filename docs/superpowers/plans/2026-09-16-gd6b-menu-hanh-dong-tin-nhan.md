# GĐ6b — Menu hành động tin nhắn: Lưu / Ghim / Thu hồi / Xóa — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm icon "..." (cạnh icon Trả lời của GĐ6a) mở menu 4 hành
động trên mỗi tin nhắn: Lưu về thiết bị, Ghim/Bỏ ghim, Thu hồi (chỉ
người gửi), Xóa (ẩn cục bộ, chỉ phía người bấm).

**Architecture:** Backend lưu 2 field mới thẳng trên `TinNhan`
(`DaThuHoi`, `DaGhim`) và 1 collection nhỏ `TinNhanAn` ghi nhận tin đã
bị 1 người dùng ẩn cục bộ. 3 hành động Thu hồi/Ghim/Bỏ ghim broadcast
qua SignalR (hub method mới), Xóa cục bộ chỉ cần REST (không broadcast).

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api) + MongoDB
Driver, xUnit; React 19 + TypeScript + Vite (frontend/), Vitest.

**Spec:** `docs/superpowers/specs/2026-09-16-halochat-menu-hanh-dong-tin-nhan.md`

**Phụ thuộc:** GĐ6a phải đã hoàn thành và merge trước khi bắt đầu plan
này (dùng `.khung-tin-nhan__hang`/`.khung-tin-nhan__icon-noi`,
`layTenNguoiGui` prop đã có).

## Global Constraints

- Đặt tên định danh không dấu tiếng Việt.
- `TinNhanDto` chỉ **thêm** tham số cuối (sau `TraLoi` của GĐ6a):
  `DaThuHoi, DaGhim, ThoiGianGhim`.
- Ghim/Bỏ ghim: ai trong hội thoại 1-1 hoặc thành viên nhóm cũng làm
  được — không giới hạn quyền admin (đã chốt).
- Thu hồi: chỉ người gửi, không giới hạn thời gian (đã chốt).
- Khi `DaThuHoi = true`, tầng DTO (`AnhXaDto`) LUÔN trả nội dung/file
  bằng placeholder "Tin nhắn đã được thu hồi." — không phụ thuộc dữ
  liệu gốc còn lưu trong Mongo hay không.
- Xóa cục bộ lưu server-side theo từng người dùng (đã chốt) — không
  broadcast cho ai khác.

---

## Task 1: Backend — model, exception, repository

**Files:**
- Modify: `backend/HaloChat.Api/Models/TinNhan.cs`
- Create: `backend/HaloChat.Api/Models/TinNhanAn.cs`
- Create: `backend/HaloChat.Api/Services/NgoaiLeTinNhan.cs`
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Create: `backend/HaloChat.Api/Repositories/ITinNhanAnRepository.cs`
- Create: `backend/HaloChat.Api/Repositories/TinNhanAnRepository.cs`
- Modify: `backend/HaloChat.Api/Program.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Create: `backend/HaloChat.Api.Tests/Fakes/TinNhanAnGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`
- Test: `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapThuHoiGhimTests.cs`

**Interfaces:**
- Consumes: không có (task nền tảng, chỉ phụ thuộc GĐ6a's
  `ITinNhanRepository.TimTheoIdAsync`, đã có sẵn).
- Produces: `ITinNhanRepository.DanhDauThuHoiAsync/DatGhimAsync/LayTinDaGhimTheoNguoiDungAsync/LayTinDaGhimTheoNhomAsync`,
  `ITinNhanAnRepository.AnAsync/LayDanhSachIdDaAnAsync`, 3 exception mới
  (`TinNhanKhongTonTaiException`, `KhongPhaiNguoiGuiException`,
  `KhongCoQuyenTrenTinNhanException`) — Task 2 (service) dùng tất cả.

- [ ] **Step 1: Thêm field vào `TinNhan.cs`**

Thêm vào cuối class (sau field `TraLoi` của GĐ6a):

```csharp
    // [GĐ6b] Thu hồi: chỉ người gửi, không giới hạn thời gian. Khi true,
    // tầng DTO (AnhXaDto) LUÔN trả nội dung/file bằng placeholder.
    public bool DaThuHoi { get; set; } = false;

    // [GĐ6b] Ghim: ai trong hội thoại/nhóm cũng ghim/bỏ ghim được.
    public bool DaGhim { get; set; } = false;
    public DateTime? ThoiGianGhim { get; set; }
```

- [ ] **Step 2: Tạo model `TinNhanAn`**

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public class TinNhanAn
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string TinNhanId { get; set; } = string.Empty;

    public DateTime ThoiGianAn { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Tạo 3 exception mới**

```csharp
namespace HaloChat.Api.Services;

public class TinNhanKhongTonTaiException : Exception
{
    public TinNhanKhongTonTaiException() : base("Tin nhắn không tồn tại.")
    {
    }
}

public class KhongPhaiNguoiGuiException : Exception
{
    public KhongPhaiNguoiGuiException() : base("Bạn không phải người gửi tin nhắn này.")
    {
    }
}

public class KhongCoQuyenTrenTinNhanException : Exception
{
    public KhongCoQuyenTrenTinNhanException() : base("Bạn không có quyền thao tác trên tin nhắn này.")
    {
    }
}
```

- [ ] **Step 4: Viết test cho repo thật + fake (FAIL trước)**

Tạo `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapThuHoiGhimTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapThuHoiGhimTests
{
    [Fact]
    public async Task DanhDauThuHoiAsync_DatDaThuHoiTrue()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        kho.DanhSach.Add(tinNhan);

        await kho.DanhDauThuHoiAsync(tinNhan.Id);

        Assert.True(kho.DanhSach.Single().DaThuHoi);
    }

    [Fact]
    public async Task DatGhimAsync_Ghim_DatDungCaHaiField()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        kho.DanhSach.Add(tinNhan);
        var thoiGian = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await kho.DatGhimAsync(tinNhan.Id, true, thoiGian);

        Assert.True(kho.DanhSach.Single().DaGhim);
        Assert.Equal(thoiGian, kho.DanhSach.Single().ThoiGianGhim);
    }

    [Fact]
    public async Task DatGhimAsync_BoGhim_DatThoiGianGhimNull()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan { DaGhim = true, ThoiGianGhim = DateTime.UtcNow };
        kho.DanhSach.Add(tinNhan);

        await kho.DatGhimAsync(tinNhan.Id, false, null);

        Assert.False(kho.DanhSach.Single().DaGhim);
        Assert.Null(kho.DanhSach.Single().ThoiGianGhim);
    }

    [Fact]
    public async Task LayTinDaGhimTheoNguoiDungAsync_ChiTraVeTinDaGhimGiuaHaiNguoi()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", DaGhim = false });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "c", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });

        var ketQua = await kho.LayTinDaGhimTheoNguoiDungAsync("a", "b");

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task LayTinDaGhimTheoNhomAsync_ChiTraVeTinDaGhimCuaNhomDo()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", DaGhim = false });
        kho.DanhSach.Add(new TinNhan { NhomId = "n2", DaGhim = true, ThoiGianGhim = DateTime.UtcNow });

        var ketQua = await kho.LayTinDaGhimTheoNhomAsync("n1");

        Assert.Single(ketQua);
    }
}
```

Tạo `backend/HaloChat.Api.Tests/Fakes/TinNhanAnGiaLap.cs`:

```csharp
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Tests.Fakes;

public class TinNhanAnGiaLap : ITinNhanAnRepository
{
    // Danh sách (nguoiDungId, tinNhanId) đã ẩn.
    public List<(string NguoiDungId, string TinNhanId)> DanhSach { get; } = new();

    public Task AnAsync(string nguoiDungId, string tinNhanId)
    {
        if (!DanhSach.Contains((nguoiDungId, tinNhanId)))
        {
            DanhSach.Add((nguoiDungId, tinNhanId));
        }
        return Task.CompletedTask;
    }

    public Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds)
    {
        var idQuanTam = tinNhanIds.ToHashSet();
        var ketQua = DanhSach
            .Where(x => x.NguoiDungId == nguoiDungId && idQuanTam.Contains(x.TinNhanId))
            .Select(x => x.TinNhanId)
            .ToHashSet();
        return Task.FromResult(ketQua);
    }
}
```

- [ ] **Step 5: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter TinNhanGiaLapThuHoiGhimTests`
Expected: FAIL biên dịch (method chưa tồn tại trên
`TinNhanGiaLap`/`ITinNhanRepository`, `ITinNhanAnRepository` chưa tồn
tại).

- [ ] **Step 6: Thêm method vào `ITinNhanRepository`/`TinNhanRepository`**

`ITinNhanRepository.cs` — thêm vào cuối interface:

```csharp
    /// <summary>[GĐ6b] Đánh dấu tin nhắn đã thu hồi.</summary>
    Task DanhDauThuHoiAsync(string id);

    /// <summary>[GĐ6b] Đặt/bỏ trạng thái ghim. thoiGianGhim = null khi bỏ ghim.</summary>
    Task DatGhimAsync(string id, bool daGhim, DateTime? thoiGianGhim);

    /// <summary>[GĐ6b] Toàn bộ tin đã ghim giữa 2 người dùng (2 chiều), mới ghim nhất trước.</summary>
    Task<List<TinNhan>> LayTinDaGhimTheoNguoiDungAsync(string nguoiA, string nguoiB);

    /// <summary>[GĐ6b] Toàn bộ tin đã ghim của 1 nhóm, mới ghim nhất trước.</summary>
    Task<List<TinNhan>> LayTinDaGhimTheoNhomAsync(string nhomId);
```

`TinNhanRepository.cs` — thêm vào cuối class (biến collection là
`_collection`, đã xác nhận ở GĐ6a):

```csharp
    public async Task DanhDauThuHoiAsync(string id)
    {
        var boLoc = Builders<TinNhan>.Filter.Eq(t => t.Id, id);
        var capNhat = Builders<TinNhan>.Update.Set(t => t.DaThuHoi, true);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task DatGhimAsync(string id, bool daGhim, DateTime? thoiGianGhim)
    {
        var boLoc = Builders<TinNhan>.Filter.Eq(t => t.Id, id);
        var capNhat = Builders<TinNhan>.Update
            .Set(t => t.DaGhim, daGhim)
            .Set(t => t.ThoiGianGhim, thoiGianGhim);
        await _collection.UpdateOneAsync(boLoc, capNhat);
    }

    public async Task<List<TinNhan>> LayTinDaGhimTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.DaGhim, true),
            Builders<TinNhan>.Filter.Or(
                Builders<TinNhan>.Filter.And(
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiA),
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiB)),
                Builders<TinNhan>.Filter.And(
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiB),
                    Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiA))));
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianGhim).ToListAsync();
    }

    public async Task<List<TinNhan>> LayTinDaGhimTheoNhomAsync(string nhomId)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId),
            Builders<TinNhan>.Filter.Eq(t => t.DaGhim, true));
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianGhim).ToListAsync();
    }
```

- [ ] **Step 7: Thêm method vào fake `TinNhanGiaLap`**

Thêm vào cuối class `TinNhanGiaLap`:

```csharp
    public Task DanhDauThuHoiAsync(string id)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        if (tinNhan is not null) tinNhan.DaThuHoi = true;
        return Task.CompletedTask;
    }

    public Task DatGhimAsync(string id, bool daGhim, DateTime? thoiGianGhim)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        if (tinNhan is not null)
        {
            tinNhan.DaGhim = daGhim;
            tinNhan.ThoiGianGhim = thoiGianGhim;
        }
        return Task.CompletedTask;
    }

    public Task<List<TinNhan>> LayTinDaGhimTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var ketQua = DanhSach
            .Where(t => t.DaGhim && ((t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) || (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA)))
            .OrderByDescending(t => t.ThoiGianGhim)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> LayTinDaGhimTheoNhomAsync(string nhomId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId && t.DaGhim)
            .OrderByDescending(t => t.ThoiGianGhim)
            .ToList();
        return Task.FromResult(ketQua);
    }
```

- [ ] **Step 8: Tạo `ITinNhanAnRepository` + `TinNhanAnRepository`**

```csharp
namespace HaloChat.Api.Repositories;

public interface ITinNhanAnRepository
{
    Task AnAsync(string nguoiDungId, string tinNhanId);

    /// <summary>Trả về tập id trong tinNhanIds mà nguoiDungId đã ẩn.</summary>
    Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds);
}
```

```csharp
using HaloChat.Api.Models;
using MongoDB.Driver;

namespace HaloChat.Api.Repositories;

public class TinNhanAnRepository : ITinNhanAnRepository
{
    private readonly IMongoCollection<TinNhanAn> _collection;

    public TinNhanAnRepository(IMongoDatabase csdl)
    {
        _collection = csdl.GetCollection<TinNhanAn>("TinNhanAn");
    }

    public async Task AnAsync(string nguoiDungId, string tinNhanId)
    {
        var boLoc = Builders<TinNhanAn>.Filter.And(
            Builders<TinNhanAn>.Filter.Eq(x => x.NguoiDungId, nguoiDungId),
            Builders<TinNhanAn>.Filter.Eq(x => x.TinNhanId, tinNhanId));
        var thayThe = new TinNhanAn { NguoiDungId = nguoiDungId, TinNhanId = tinNhanId };
        await _collection.ReplaceOneAsync(boLoc, thayThe, new ReplaceOptions { IsUpsert = true });
    }

    public async Task<HashSet<string>> LayDanhSachIdDaAnAsync(string nguoiDungId, IEnumerable<string> tinNhanIds)
    {
        var idQuanTam = tinNhanIds.ToList();
        if (idQuanTam.Count == 0) return new HashSet<string>();

        var boLoc = Builders<TinNhanAn>.Filter.And(
            Builders<TinNhanAn>.Filter.Eq(x => x.NguoiDungId, nguoiDungId),
            Builders<TinNhanAn>.Filter.In(x => x.TinNhanId, idQuanTam));
        var ketQua = await _collection.Find(boLoc).ToListAsync();
        return ketQua.Select(x => x.TinNhanId).ToHashSet();
    }
}
```

- [ ] **Step 9: Đăng ký DI trong `Program.cs`**

Thêm dòng sau (ngay sau dòng
`builder.Services.AddScoped<ITinNhanRepository, TinNhanRepository>();`):

```csharp
builder.Services.AddScoped<ITinNhanAnRepository, TinNhanAnRepository>();
```

- [ ] **Step 10: Đăng ký fake trong `ThietLapKiemThuTichHop.cs`**

Mở `backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs`. Thêm property
mới (cạnh `public TinNhanGiaLap KhoTinNhanGiaLap { get; } = new();`):

```csharp
    public TinNhanAnGiaLap KhoTinNhanAnGiaLap { get; } = new();
```

Thêm vào khối đăng ký DI (cạnh `dichVu.RemoveAll<ITinNhanRepository>(); dichVu.AddSingleton<ITinNhanRepository>(KhoTinNhanGiaLap);`):

```csharp
            dichVu.RemoveAll<ITinNhanAnRepository>();
            dichVu.AddSingleton<ITinNhanAnRepository>(KhoTinNhanAnGiaLap);
```

- [ ] **Step 11: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter TinNhanGiaLapThuHoiGhimTests`
Expected: PASS (5/5).

- [ ] **Step 12: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: build 0 lỗi (constructor `DichVuTinNhan` CHƯA đổi ở task
này nên không có lỗi thiếu tham số); toàn bộ test PASS.

- [ ] **Step 13: Commit**

```bash
git add backend/HaloChat.Api/Models/TinNhan.cs backend/HaloChat.Api/Models/TinNhanAn.cs backend/HaloChat.Api/Services/NgoaiLeTinNhan.cs backend/HaloChat.Api/Repositories/ITinNhanRepository.cs backend/HaloChat.Api/Repositories/TinNhanRepository.cs backend/HaloChat.Api/Repositories/ITinNhanAnRepository.cs backend/HaloChat.Api/Repositories/TinNhanAnRepository.cs backend/HaloChat.Api/Program.cs backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs backend/HaloChat.Api.Tests/Fakes/TinNhanAnGiaLap.cs backend/HaloChat.Api.Tests/ThietLapKiemThuTichHop.cs backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapThuHoiGhimTests.cs
git commit -m "feat(backend): model/repo cho thu hoi, ghim, an cuc bo tin nhan (GD6b)"
```

---

## Task 2: Backend — service `DichVuTinNhan` (thu hồi/ghim/bỏ ghim/ẩn + lọc lịch sử)

**Files:**
- Modify: `backend/HaloChat.Api/Dto/TinNhanDto.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Test: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`

**Interfaces:**
- Consumes: mọi thứ ở Task 1.
- Produces: `IDichVuTinNhan.ThuHoiAsync/GhimAsync/BoGhimAsync/AnAsync/LayTinDaGhimTheoNguoiDungAsync/LayTinDaGhimTheoNhomAsync`
  — Task 3 (hub/controller) gọi trực tiếp các method này.
  `TinNhanDto` có thêm 3 field cuối `DaThuHoi, DaGhim, ThoiGianGhim`.

- [ ] **Step 1: Cập nhật `TinNhanDto`**

```csharp
namespace HaloChat.Api.Dto;

public record TinNhanDto(
    string Id, string NguoiGuiId, string? NguoiNhanId, string? NhomId, string LoaiTinNhan,
    string NoiDungTinNhan, string? DuongDanFile, string? TenFileGoc, long? KichThuocFile, string? LoaiFile,
    bool DaDoc, bool DaNhan, DateTime ThoiGianTao, TraLoiThongTinDto? TraLoi,
    bool DaThuHoi, bool DaGhim, DateTime? ThoiGianGhim);
```

- [ ] **Step 2: Viết test service mới (FAIL trước)**

Mở `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`. Sửa
`TaoDichVu()` để thêm dependency mới:

```csharp
    private static (DichVuTinNhan DichVu, TinNhanGiaLap KhoTinNhan, NguoiDungGiaLap KhoNguoiDung, LoiMoiKetBanGiaLap KhoLoiMoiKetBan, NhomGiaLap KhoNhom, TinNhanAnGiaLap KhoTinNhanAn) TaoDichVu()
    {
        var khoTinNhan = new TinNhanGiaLap();
        var khoNguoiDung = new NguoiDungGiaLap();
        var khoLoiMoiKetBan = new LoiMoiKetBanGiaLap();
        var khoNhom = new NhomGiaLap();
        var khoDocNhom = new DocNhomGiaLap();
        var khoTinNhanAn = new TinNhanAnGiaLap();
        var quanLyKetNoi = new QuanLyKetNoiChat();
        var dichVu = new DichVuTinNhan(khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoDocNhom, quanLyKetNoi, khoTinNhanAn);
        return (dichVu, khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoTinNhanAn);
    }
```

Vì chữ ký `TaoDichVu()` đổi (thêm 2 giá trị trả về `KhoNhom`,
`KhoTinNhanAn`), MỌI lời gọi hiện có dạng
`var (dichVu, _, khoNguoiDung, _) = TaoDichVu();` trong file sẽ lỗi biên
dịch — sửa TẤT CẢ các dòng destructure đó, thêm `_` cho đủ 6 vị trí theo
đúng thứ tự mới (ví dụ `var (dichVu, _, khoNguoiDung, _) = TaoDichVu();`
→ `var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();`), giữ nguyên
tên biến đã dùng ở đúng vị trí cũ, chỉ thêm `_` cho 2 vị trí mới ở cuối.

Thêm test mới vào cuối file (trước dấu `}` cuối class):

```csharp
    // --- Thu hồi / Ghim / Bỏ ghim / Ẩn cục bộ (GĐ6b) ---

    [Fact]
    public async Task ThuHoiAsync_LaNguoiGui_DatDaThuHoiVaAnNoiDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Bí mật", null, null, null, null, null);

        var ketQua = await dichVu.ThuHoiAsync(IdNguoiGui, tin.Id);

        Assert.True(ketQua.DaThuHoi);
        Assert.Equal("Tin nhắn đã được thu hồi.", ketQua.NoiDungTinNhan);
    }

    [Fact]
    public async Task ThuHoiAsync_KhongPhaiNguoiGui_NemKhongPhaiNguoiGui()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Bí mật", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongPhaiNguoiGuiException>(() => dichVu.ThuHoiAsync(IdNguoiNhan, tin.Id));
    }

    [Fact]
    public async Task GhimAsync_ThanhVienHopLe_DatDaGhim()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Ghi nhớ việc này", null, null, null, null, null);

        var ketQua = await dichVu.GhimAsync(IdNguoiNhan, tin.Id);

        Assert.True(ketQua.DaGhim);
        Assert.NotNull(ketQua.ThoiGianGhim);
    }

    [Fact]
    public async Task GhimAsync_KhongThuocHoiThoai_NemKhongCoQuyen()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Riêng tư", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongCoQuyenTrenTinNhanException>(() => dichVu.GhimAsync(IdNguoiThuBa, tin.Id));
    }

    [Fact]
    public async Task BoGhimAsync_DatLaiDaGhimFalse()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tạm ghim", null, null, null, null, null);
        await dichVu.GhimAsync(IdNguoiGui, tin.Id);

        var ketQua = await dichVu.BoGhimAsync(IdNguoiNhan, tin.Id);

        Assert.False(ketQua.DaGhim);
        Assert.Null(ketQua.ThoiGianGhim);
    }

    [Fact]
    public async Task AnAsync_SauKhiAn_KhongConXuatHienTrongLichSuNguoiDoAn()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin ẩn được", null, null, null, null, null);

        await dichVu.AnAsync(IdNguoiNhan, tin.Id);
        var lichSuCuaNguoiAn = await dichVu.LayLichSuAsync(IdNguoiNhan, IdNguoiGui, null, 30);

        Assert.Empty(lichSuCuaNguoiAn);
    }

    [Fact]
    public async Task AnAsync_KhongAnhHuongLichSuCuaNguoiKhac()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Tin ẩn được", null, null, null, null, null);

        await dichVu.AnAsync(IdNguoiNhan, tin.Id);
        var lichSuCuaNguoiGui = await dichVu.LayLichSuAsync(IdNguoiGui, IdNguoiNhan, null, 30);

        Assert.Single(lichSuCuaNguoiGui);
    }

    [Fact]
    public async Task LayTinDaGhimTheoNguoiDungAsync_LocDungTinDaAn()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Ghim rồi ẩn", null, null, null, null, null);
        await dichVu.GhimAsync(IdNguoiGui, tin.Id);
        await dichVu.AnAsync(IdNguoiNhan, tin.Id);

        var ghimTheoNguoiAn = await dichVu.LayTinDaGhimTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui);
        var ghimTheoNguoiKia = await dichVu.LayTinDaGhimTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan);

        Assert.Empty(ghimTheoNguoiAn);
        Assert.Single(ghimTheoNguoiKia);
    }
```

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter DichVuTinNhanTests`
Expected: FAIL biên dịch (constructor `DichVuTinNhan` chưa nhận
`ITinNhanAnRepository`, các method mới chưa tồn tại).

- [ ] **Step 4: Cập nhật `IDichVuTinNhan.cs`**

Thêm vào cuối interface:

```csharp
    Task<TinNhanDto> ThuHoiAsync(string idHienTai, string tinNhanId);
    Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId);
    Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId);
    Task AnAsync(string idHienTai, string tinNhanId);
    Task<List<TinNhanDto>> LayTinDaGhimTheoNguoiDungAsync(string idHienTai, string doiTacId);
    Task<List<TinNhanDto>> LayTinDaGhimTheoNhomAsync(string idHienTai, string nhomId);
```

- [ ] **Step 5: Cập nhật `DichVuTinNhan.cs`**

Đổi field/constructor (thêm dependency mới):

```csharp
    private readonly ITinNhanAnRepository _khoTinNhanAn;

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IDocNhomRepository khoDocNhom, IQuanLyKetNoiChat quanLyKetNoi,
        ITinNhanAnRepository khoTinNhanAn)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _khoDocNhom = khoDocNhom;
        _quanLyKetNoi = quanLyKetNoi;
        _khoTinNhanAn = khoTinNhanAn;
    }
```

Thêm các method mới (đặt sau `GuiTinNhanAsync`, trước `LayLichSuAsync`):

```csharp
    private async Task KiemTraQuyenTrenTinNhanAsync(string idHienTai, TinNhan tinNhan)
    {
        if (tinNhan.NhomId is not null)
        {
            var nhom = await _khoNhom.TimTheoIdAsync(tinNhan.NhomId) ?? throw new NhomKhongTonTaiException();
            if (!nhom.ThanhVienIds.Contains(idHienTai))
            {
                throw new KhongPhaiThanhVienNhomException();
            }
        }
        else if (tinNhan.NguoiGuiId != idHienTai && tinNhan.NguoiNhanId != idHienTai)
        {
            throw new KhongCoQuyenTrenTinNhanException();
        }
    }

    public async Task<TinNhanDto> ThuHoiAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        if (tinNhan.NguoiGuiId != idHienTai)
        {
            throw new KhongPhaiNguoiGuiException();
        }

        await _khoTinNhan.DanhDauThuHoiAsync(tinNhanId);
        tinNhan.DaThuHoi = true;
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        var thoiGian = DateTime.UtcNow;
        await _khoTinNhan.DatGhimAsync(tinNhanId, true, thoiGian);
        tinNhan.DaGhim = true;
        tinNhan.ThoiGianGhim = thoiGian;
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.DatGhimAsync(tinNhanId, false, null);
        tinNhan.DaGhim = false;
        tinNhan.ThoiGianGhim = null;
        return AnhXaDto(tinNhan);
    }

    public async Task AnAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);
        await _khoTinNhanAn.AnAsync(idHienTai, tinNhanId);
    }

    public async Task<List<TinNhanDto>> LayTinDaGhimTheoNguoiDungAsync(string idHienTai, string doiTacId)
    {
        var ghim = await _khoTinNhan.LayTinDaGhimTheoNguoiDungAsync(idHienTai, doiTacId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ghim.Select(t => t.Id));
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> LayTinDaGhimTheoNhomAsync(string idHienTai, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var ghim = await _khoTinNhan.LayTinDaGhimTheoNhomAsync(nhomId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ghim.Select(t => t.Id));
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }
```

Sửa `LayLichSuAsync`/`LayLichSuNhomAsync` để lọc tin đã ẩn cục bộ:

```csharp
    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var lichSu = await _khoTinNhan.LayLichSuNhomAsync(nhomId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }
```

Sửa `AnhXaDto` (thêm nhánh `DaThuHoi` + 3 tham số cuối mới):

```csharp
    private static TinNhanDto AnhXaDto(TinNhan t)
    {
        var traLoi = t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString());

        if (t.DaThuHoi)
        {
            return new(
                t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(),
                "Tin nhắn đã được thu hồi.", null, null, null, null,
                t.DaDoc, t.DaNhan, t.ThoiGianTao, traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim);
        }

        return new(
            t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
            t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
            traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim);
    }
```

Cũng cần sửa lời gọi `AnhXaDto` bên trong `LayDanhSachHoiThoaiAsync` NẾU
hàm đó gọi trực tiếp constructor `TinNhanDto` ở đâu đó khác — kiểm tra
bằng build ở Step 6, method này KHÔNG dùng `AnhXaDto` (dùng
`HoiThoaiTomTatDto` riêng) nên không bị ảnh hưởng.

- [ ] **Step 6: Build để lộ lỗi còn sót**

Run: `cd backend && dotnet build`
Expected: FAIL — liệt kê lỗi tại MỌI nơi khởi tạo `DichVuTinNhan` thiếu
tham số thứ 7 (`ITinNhanAnRepository`) — gồm `Program.cs` (DI tự động
resolve, KHÔNG lỗi vì `AddScoped` không gọi constructor trực tiếp — chỉ
lỗi nếu code THỦ CÔNG gọi `new DichVuTinNhan(...)` ở đâu đó, kiểm tra
bằng `grep -rn "new DichVuTinNhan(" backend/`).

- [ ] **Step 7: Sửa hết lỗi build còn lại**

Chạy `grep -rn "new DichVuTinNhan(" backend/` — nếu có kết quả ngoài
`DichVuTinNhanTests.cs` (đã sửa ở Step 2), thêm tham số
`khoTinNhanAn`/instance `ITinNhanAnRepository` tương ứng vào đúng vị trí
cuối cùng của lời gọi đó.

- [ ] **Step 8: Chạy toàn bộ test backend**

Run: `cd backend && dotnet test`
Expected: PASS toàn bộ, bao gồm 8 test mới ở Step 2.

- [ ] **Step 9: Commit**

```bash
git add backend/HaloChat.Api/Dto/TinNhanDto.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs
git commit -m "feat(backend): DichVuTinNhan ho tro thu hoi/ghim/bo ghim/an cuc bo (GD6b)"
```

---

## Task 3: Backend — hub + controller

**Files:**
- Modify: `backend/HaloChat.Api/Hubs/ChatHub.cs`
- Modify: `backend/HaloChat.Api/Controllers/TinNhanController.cs`
- Modify: `backend/HaloChat.Api.Tests/ChatHubTests.cs`
- Test: `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`

**Interfaces:**
- Consumes: `IDichVuTinNhan.ThuHoiAsync/GhimAsync/BoGhimAsync/AnAsync/LayTinDaGhimTheoNguoiDungAsync/LayTinDaGhimTheoNhomAsync` (Task 2).
- Produces: Hub method `ThuHoiTinNhan/GhimTinNhan/BoGhimTinNhan(string tinNhanId): Task<TinNhanDto>`
  phát sự kiện `TinNhanDaThuHoi`/`TinNhanDaGhim`/`TinNhanBoGhim`; REST
  `POST /api/tinnhan/{id}/an`, `GET /api/tinnhan/nguoi-dung/{id}/ghim`,
  `GET /api/tinnhan/nhom/{id}/ghim` — Task 5 (frontend) gọi các endpoint
  này qua `DichVuApi.ts` (Task 4).

- [ ] **Step 1: Viết test tích hợp hub cho Thu hồi (FAIL trước)**

Mở `backend/HaloChat.Api.Tests/ChatHubTests.cs`, thêm test mới vào cuối
class (trước dấu `}` cuối file):

```csharp
    [Fact]
    public async Task ThuHoiTinNhan_LaNguoiGui_PhiaKiaNhanDuocSuKienRealtime()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubthuhoia");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubthuhoib");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubthuhoib").Id;
        _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubthuhoib").ChoPhepTinNhanTuNguoiLa = true;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await using var ketNoiB = TaoKetNoiHub(tokenB);
        TinNhanDto? nhanDuoc = null;
        var daNhan = new TaskCompletionSource();
        ketNoiB.On<TinNhanDto>("TinNhanDaThuHoi", tn => { nhanDuoc = tn; daNhan.SetResult(); });

        await ketNoiA.StartAsync();
        await ketNoiB.StartAsync();
        var tinGui = await ketNoiA.InvokeAsync<TinNhanDto>("GuiTinNhan", idB, null, "Text", "Bí mật", null, null, null, null, null);

        await ketNoiA.InvokeAsync<TinNhanDto>("ThuHoiTinNhan", tinGui.Id);

        await daNhan.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(nhanDuoc);
        Assert.True(nhanDuoc!.DaThuHoi);
        Assert.Equal("Tin nhắn đã được thu hồi.", nhanDuoc.NoiDungTinNhan);
    }

    [Fact]
    public async Task ThuHoiTinNhan_KhongPhaiNguoiGui_NemHubException()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubthuhoic");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubthuhoid");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubthuhoid").Id;
        _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubthuhoid").ChoPhepTinNhanTuNguoiLa = true;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await using var ketNoiB = TaoKetNoiHub(tokenB);
        await ketNoiA.StartAsync();
        await ketNoiB.StartAsync();
        var tinGui = await ketNoiA.InvokeAsync<TinNhanDto>("GuiTinNhan", idB, null, "Text", "Bí mật", null, null, null, null, null);

        await Assert.ThrowsAsync<HubException>(() => ketNoiB.InvokeAsync<TinNhanDto>("ThuHoiTinNhan", tinGui.Id));
    }

    [Fact]
    public async Task GhimTinNhan_ThanhVienHopLe_PhiaKiaNhanDuocSuKien()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubghima");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubghimb");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubghimb").Id;
        _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubghimb").ChoPhepTinNhanTuNguoiLa = true;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await using var ketNoiB = TaoKetNoiHub(tokenB);
        TinNhanDto? nhanDuoc = null;
        var daNhan = new TaskCompletionSource();
        ketNoiB.On<TinNhanDto>("TinNhanDaGhim", tn => { nhanDuoc = tn; daNhan.SetResult(); });

        await ketNoiA.StartAsync();
        await ketNoiB.StartAsync();
        var tinGui = await ketNoiA.InvokeAsync<TinNhanDto>("GuiTinNhan", idB, null, "Text", "Ghi nhớ", null, null, null, null, null);

        await ketNoiA.InvokeAsync<TinNhanDto>("GhimTinNhan", tinGui.Id);

        await daNhan.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(nhanDuoc);
        Assert.True(nhanDuoc!.DaGhim);
    }
```

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "ThuHoiTinNhan|GhimTinNhan"`
Expected: FAIL (hub method `ThuHoiTinNhan`/`GhimTinNhan` chưa tồn tại).

- [ ] **Step 3: Thêm 3 hub method + helper broadcast vào `ChatHub.cs`**

Thêm vào cuối class `ChatHub` (sau method `GuiTinNhan`, trước
`DanhDauDaDoc`):

```csharp
    public async Task<TinNhanDto> ThuHoiTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.ThuHoiAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaThuHoi");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiNguoiGuiException loi) { throw new HubException(loi.Message); }
    }

    public async Task<TinNhanDto> GhimTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.GhimAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaGhim");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
        catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
    }

    public async Task<TinNhanDto> BoGhimTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.BoGhimAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanBoGhim");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
        catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
    }

    private async Task GuiBroadcastCapNhatAsync(TinNhanDto tinNhan, string tenSuKien)
    {
        if (tinNhan.NhomId is not null)
        {
            await Clients.OthersInGroup("nhom-" + tinNhan.NhomId).SendAsync(tenSuKien, tinNhan);
        }
        else
        {
            var idKia = tinNhan.NguoiGuiId == NguoiDungHienTaiId ? tinNhan.NguoiNhanId : tinNhan.NguoiGuiId;
            await Clients.User(idKia!).SendAsync(tenSuKien, tinNhan);
        }
    }
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "ThuHoiTinNhan|GhimTinNhan"`
Expected: PASS (3/3).

- [ ] **Step 5: Viết test controller cho 3 endpoint REST mới (FAIL trước)**

Mở `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`, đọc phần đầu
file để dùng đúng pattern đăng ký + đăng nhập + set header
`Authorization` mà các test khác trong file này dùng, thêm test mới vào
cuối class:

```csharp
    [Fact]
    public async Task An_IdKhongPhaiTinNhanTonTai_TraVe404()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "anA", email = "anA@vi.du", matKhau = "MatKhau123!" });
        var dangNhapA = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "anA", matKhau = "MatKhau123!" });
        var tokenA = (await dangNhapA.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var idA = (await (await _client.GetAsync("/api/nguoidung/toi")).Content.ReadFromJsonAsync<HoSoCaNhanDto>())!.Id;

        var phanHoiAn = await _client.PostAsync($"/api/tinnhan/{idA}/an", null);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, phanHoiAn.StatusCode);
    }
```

Test trên chủ ý dùng `idA` (id NGƯỜI DÙNG, không phải id TIN NHẮN) làm
`tinNhanId` để kiểm chứng nhánh `TinNhanKhongTonTaiException` → 404,
tránh phải dựng hẳn 1 tin nhắn thật qua HTTP (không có endpoint REST gửi
tin nhắn — chỉ có qua Hub) chỉ để test riêng lẻ controller. Đây LÀ hành
vi đúng cần test (endpoint phải trả 404 khi id không phải tin nhắn tồn
tại), không phải test giả.

- [ ] **Step 6: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "TinNhanControllerTests"`
Expected: FAIL với 404 route not found (endpoint `POST /api/tinnhan/{id}/an`
chưa tồn tại) — SignalR/HTTP đều trả về không tìm thấy route nên response
thực tế có thể là 404 "no route" thay vì lỗi biên dịch, kiểm tra log để
phân biệt.

- [ ] **Step 7: Thêm 3 endpoint vào `TinNhanController.cs`**

Thêm vào cuối class (trước dấu `}` cuối file):

```csharp
    [HttpPost("{id}/an")]
    public async Task<IActionResult> An(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            await _dichVuTinNhan.AnAsync(IdHienTai, id);
            return Ok(new { thongBao = "Đã ẩn tin nhắn." });
        }
        catch (TinNhanKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongCoQuyenTrenTinNhanException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }

    [HttpGet("nguoi-dung/{id}/ghim")]
    public async Task<IActionResult> LayTinDaGhimTheoNguoiDung(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        return Ok(await _dichVuTinNhan.LayTinDaGhimTheoNguoiDungAsync(IdHienTai, id));
    }

    [HttpGet("nhom/{id}/ghim")]
    public async Task<IActionResult> LayTinDaGhimTheoNhom(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _dichVuTinNhan.LayTinDaGhimTheoNhomAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }
```

Kiểm tra đầu file `TinNhanController.cs` đã có
`using HaloChat.Api.Services;` (namespace của các exception mới) —
nếu chưa, thêm vào.

- [ ] **Step 8: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "TinNhanControllerTests"`
Expected: PASS toàn bộ (bao gồm test mới ở Step 5).

- [ ] **Step 9: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: 0 lỗi build; toàn bộ test PASS.

- [ ] **Step 10: Commit**

```bash
git add backend/HaloChat.Api/Hubs/ChatHub.cs backend/HaloChat.Api/Controllers/TinNhanController.cs backend/HaloChat.Api.Tests/ChatHubTests.cs backend/HaloChat.Api.Tests/TinNhanControllerTests.cs
git commit -m "feat(backend): hub thu hoi/ghim/bo ghim + REST an/ghim tin nhan (GD6b)"
```

---

## Task 4: Frontend — kiểu dữ liệu, API, và UI menu trong `KhungTinNhan.tsx`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Test: `frontend/src/DichVuApi.test.ts`
- Test: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`

**Interfaces:**
- Consumes: backend từ Task 1-3 (JSON field `daThuHoi`/`daGhim`/`thoiGianGhim`,
  endpoint `an`/`ghim`).
- Produces: `PropsKhungTinNhan.onThuHoi/onGhim/onBoGhim/onAn: (id: string) => void`,
  `PropsKhungTinNhan.danhSachTinNhanGhim: TinNhan[]` — Task 5
  (TrangChat.tsx/TrangNhom.tsx) truyền các prop này.

- [ ] **Step 1: Thêm field vào `TinNhan`**

`frontend/src/KieuDuLieu.ts`:

```typescript
export interface TinNhan {
  // ... field hiện có (bao gồm traLoi của GĐ6a) ...
  daThuHoi: boolean;
  daGhim: boolean;
  thoiGianGhim: string | null;
}
```

- [ ] **Step 2: Viết test cho 3 hàm API mới (FAIL trước)**

Mở `frontend/src/DichVuApi.test.ts`, đọc pattern test `DoiTenHienThi`
(dùng `new Response(JSON.stringify(...))`, KHÔNG dùng `{ok, json}`) đã
xác nhận đúng ở GĐ5f, thêm test mới:

```typescript
it('AnTinNhan goi dung endpoint POST', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ thongBao: 'Đã ẩn tin nhắn.' }), { status: 200 })));

  await AnTinNhan('token-gia-lap', 'm1');

  expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/tinnhan/m1/an'),
    expect.objectContaining({ method: 'POST' }),
  );
});

it('LayTinDaGhimTheoNguoiDung goi dung endpoint GET', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

  await LayTinDaGhimTheoNguoiDung('token-gia-lap', 'doi-tac-1');

  expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/tinnhan/nguoi-dung/doi-tac-1/ghim'),
    expect.anything(),
  );
});

it('LayTinDaGhimTheoNhom goi dung endpoint GET', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

  await LayTinDaGhimTheoNhom('token-gia-lap', 'nhom-1');

  expect(fetch).toHaveBeenCalledWith(
    expect.stringContaining('/tinnhan/nhom/nhom-1/ghim'),
    expect.anything(),
  );
});
```

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: FAIL (3 hàm chưa tồn tại).

- [ ] **Step 4: Thêm 3 hàm vào `DichVuApi.ts`**

Thêm vào cuối file:

```typescript
export async function AnTinNhan(token: string, id: string): Promise<void> {
  await goiApi<{ thongBao: string }>(`/tinnhan/${id}/an`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayTinDaGhimTheoNguoiDung(token: string, doiTacId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${doiTacId}/ghim`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayTinDaGhimTheoNhom(token: string, nhomId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}/ghim`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

- [ ] **Step 5: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: PASS.

- [ ] **Step 6: Thêm icon "..." vào `BieuTuong.tsx`**

```tsx
export function BieuTuongBaCham() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" stroke="none">
      <circle cx="5" cy="12" r="2" />
      <circle cx="12" cy="12" r="2" />
      <circle cx="19" cy="12" r="2" />
    </svg>
  );
}
```

- [ ] **Step 7: Sửa `TIN_NHAN_MAU` trong `KhungTinNhan.test.tsx`, thêm 3 field mới**

```typescript
const TIN_NHAN_MAU = {
  // ... các field hiện có (đã có traLoi: null từ GĐ6a) ...
  daThuHoi: false,
  daGhim: false,
  thoiGianGhim: null,
};
```

Cũng thêm `daThuHoi: false, daGhim: false, thoiGianGhim: null,` vào MỌI
object literal `TinNhan` khác được viết trực tiếp trong file test này ở
GĐ6a (các test `tinGoc`/`tinTraLoi`/`tinFile` đã thêm ở GĐ6a Task 3) —
định vị bằng nội dung `traLoi:` (mọi object có field này đều là
`TinNhan` cần thêm 3 field mới).

- [ ] **Step 8: Viết test mới cho menu (FAIL trước)**

Thêm vào cuối `describe('KhungTinNhan', ...)`:

```typescript
it('bam ... hien menu voi dung cac muc theo trang thai tin nhan', async () => {
  const tinCuaMinh = { ...TIN_NHAN_MAU, id: 'm1', nguoiGuiId: '1', loaiTinNhan: 'Text' as const };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinCuaMinh]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);

  await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

  expect(screen.getByRole('button', { name: 'Ghim' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Thu hồi tin nhắn' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Xóa' })).toBeInTheDocument();
});

it('tin da thu hoi an placeholder thay vi noi dung goc', () => {
  const tinDaThuHoi = { ...TIN_NHAN_MAU, id: 'm2', daThuHoi: true, noiDungTinNhan: 'noi dung cu' };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinDaThuHoi]} idHienTai="1" danhSachTinNhanGhim={[]} onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} />);

  expect(screen.getByText('Tin nhắn đã được thu hồi.')).toBeInTheDocument();
  expect(screen.queryByText('noi dung cu')).not.toBeInTheDocument();
});

it('tin da thu hoi khong con muc Thu hoi/Ghim/Luu trong menu', async () => {
  const tinDaThuHoi = { ...TIN_NHAN_MAU, id: 'm2', nguoiGuiId: '1', daThuHoi: true };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinDaThuHoi]} idHienTai="1" danhSachTinNhanGhim={[]} onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} />);

  await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

  expect(screen.queryByRole('button', { name: 'Ghim' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Thu hồi tin nhắn' })).not.toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Xóa' })).toBeInTheDocument();
});

it('bam Thu hoi goi onThuHoi dung id', async () => {
  const tinCuaMinh = { ...TIN_NHAN_MAU, id: 'm1', nguoiGuiId: '1' };
  const onThuHoi = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinCuaMinh]} idHienTai="1" onThuHoi={onThuHoi} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
  await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

  await userEvent.click(screen.getByRole('button', { name: 'Thu hồi tin nhắn' }));

  expect(onThuHoi).toHaveBeenCalledWith('m1');
});

it('bam Ghim goi onGhim, bam lai (da ghim) goi onBoGhim', async () => {
  const tinChuaGhim = { ...TIN_NHAN_MAU, id: 'm1', daGhim: false };
  const onGhim = vi.fn();
  const { rerender } = render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinChuaGhim]} idHienTai="1" onThuHoi={() => {}} onGhim={onGhim} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
  await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));
  await userEvent.click(screen.getByRole('button', { name: 'Ghim' }));
  expect(onGhim).toHaveBeenCalledWith('m1');

  const onBoGhim = vi.fn();
  const tinDaGhim = { ...TIN_NHAN_MAU, id: 'm1', daGhim: true };
  rerender(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinDaGhim]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={onBoGhim} onAn={() => {}} danhSachTinNhanGhim={[]} />);
  await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));
  await userEvent.click(screen.getByRole('button', { name: 'Bỏ ghim' }));
  expect(onBoGhim).toHaveBeenCalledWith('m1');
});

it('bam Xoa goi onAn dung id', async () => {
  const tin = { ...TIN_NHAN_MAU, id: 'm1' };
  const onAn = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={onAn} danhSachTinNhanGhim={[]} />);
  await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

  await userEvent.click(screen.getByRole('button', { name: 'Xóa' }));

  expect(onAn).toHaveBeenCalledWith('m1');
});

it('banner ghim hien dung danh sach tin da ghim, an khi rong', () => {
  const { rerender } = render(<KhungTinNhan {...PROPS_MAC_DINH} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
  expect(screen.queryByText(/^\[File\]|^\[Ảnh\]/)).not.toBeInTheDocument();

  const tinGhim = { ...TIN_NHAN_MAU, id: 'm1', nguoiGuiId: 'nguoi-kia', noiDungTinNhan: 'Nhớ nộp báo cáo' };
  rerender(<KhungTinNhan {...PROPS_MAC_DINH} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[tinGhim]} />);

  expect(screen.getByText(/Nhớ nộp báo cáo/)).toBeInTheDocument();
});
```

- [ ] **Step 9: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: FAIL (props/UI mới chưa tồn tại).

- [ ] **Step 10: Cập nhật `PropsKhungTinNhan` + import**

```tsx
import { useEffect, useRef, useState, type FormEvent, type ChangeEvent } from 'react';
import { BieuTuongGhim, BieuTuongTraLoi, BieuTuongTaiLieu, BieuTuongTai, BieuTuongBaCham } from './BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
import './KhungTinNhan.css';
```

Thêm vào interface `PropsKhungTinNhan`:

```tsx
  onThuHoi: (id: string) => void;
  onGhim: (id: string) => void;
  onBoGhim: (id: string) => void;
  onAn: (id: string) => void;
  danhSachTinNhanGhim: TinNhan[];
```

Thêm vào destructure props của component + state mới:

```tsx
export function KhungTinNhan({
  tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi, onQuayLai, onBamTieuDe, layTenNguoiGui,
  onThuHoi, onGhim, onBoGhim, onAn, danhSachTinNhanGhim,
}: PropsKhungTinNhan) {
  // ... state hiện có ...
  const [menuMoChoTinNhanId, setMenuMoChoTinNhanId] = useState<string | null>(null);
```

- [ ] **Step 11: Thêm banner tin ghim ngay sau `<header>`**

Ngay SAU thẻ đóng `</header>` (kết thúc `.khung-tin-nhan__tieu-de`) và
TRƯỚC `<div className="khung-tin-nhan__danh-sach-tin-nhan">`, thêm:

```tsx
      {danhSachTinNhanGhim.length > 0 && (
        <div className="khung-tin-nhan__banner-ghim">
          {danhSachTinNhanGhim.map((tn) => (
            <div key={tn.id} className="khung-tin-nhan__dong-ghim">
              <BieuTuongGhim />
              <span className="khung-tin-nhan__dong-ghim-noi-dung">
                {layTenNguoiGui ? layTenNguoiGui(tn.nguoiGuiId) : 'một người dùng'}: {trichNoiDungTinNhan(tn)}
              </span>
              <button onClick={() => onBoGhim(tn.id)} aria-label="Bỏ ghim">×</button>
            </div>
          ))}
        </div>
      )}
```

- [ ] **Step 12: Thêm icon "..." + menu + placeholder thu hồi trong danh sách tin nhắn**

Trong khối JSX của mỗi tin nhắn (đã dựng ở GĐ6a), thêm nút "..." vào
`.khung-tin-nhan__icon-noi` (cạnh nút Trả lời):

```tsx
              <div className="khung-tin-nhan__icon-noi">
                <button
                  type="button"
                  className="khung-tin-nhan__nut-tra-loi"
                  onClick={(su) => { su.stopPropagation(); setDangTraLoiId(tn.id); noiDungRef.current?.focus(); }}
                  aria-label="Trả lời tin nhắn này"
                >
                  <BieuTuongTraLoi />
                </button>
                <button
                  type="button"
                  className="khung-tin-nhan__nut-them"
                  onClick={(su) => { su.stopPropagation(); setMenuMoChoTinNhanId((truoc) => (truoc === tn.id ? null : tn.id)); }}
                  aria-label="Thêm tùy chọn"
                >
                  <BieuTuongBaCham />
                </button>
                {menuMoChoTinNhanId === tn.id && (
                  <div className="khung-tin-nhan__menu" onClick={(su) => su.stopPropagation()}>
                    {!tn.daThuHoi && tn.loaiTinNhan !== 'Text' && (
                      <a href={`${DIA_CHI_GOC}${tn.duongDanFile}`} download target="_blank" rel="noreferrer" onClick={() => setMenuMoChoTinNhanId(null)}>
                        Lưu về thiết bị
                      </a>
                    )}
                    {!tn.daThuHoi && !tn.daGhim && (
                      <button onClick={() => { onGhim(tn.id); setMenuMoChoTinNhanId(null); }}>Ghim</button>
                    )}
                    {!tn.daThuHoi && tn.daGhim && (
                      <button onClick={() => { onBoGhim(tn.id); setMenuMoChoTinNhanId(null); }}>Bỏ ghim</button>
                    )}
                    {laCuaMinh && !tn.daThuHoi && (
                      <button onClick={() => { onThuHoi(tn.id); setMenuMoChoTinNhanId(null); }}>Thu hồi tin nhắn</button>
                    )}
                    <button className="khung-tin-nhan__menu-nguy-hiem" onClick={() => { onAn(tn.id); setMenuMoChoTinNhanId(null); }}>Xóa</button>
                  </div>
                )}
              </div>
```

Trong bong bóng, thay nội dung tùy loại tin bằng điều kiện `daThuHoi`
trước (bọc toàn bộ khối `{tn.loaiTinNhan === 'Anh' && ...}` /
`{tn.loaiTinNhan === 'File' && ...}` / `{tn.loaiTinNhan === 'Text' && ...}`
hiện có trong 1 nhánh `else`):

```tsx
                {tn.daThuHoi ? (
                  <span className="khung-tin-nhan__da-thu-hoi">Tin nhắn đã được thu hồi.</span>
                ) : (
                  <>
                    {tn.loaiTinNhan === 'Anh' && (
                      <img className="khung-tin-nhan__anh" src={`${DIA_CHI_GOC}${tn.duongDanFile}`} alt={tn.tenFileGoc ?? 'ảnh'} />
                    )}
                    {tn.loaiTinNhan === 'File' && (
                      <div className="khung-tin-nhan__file">
                        <span className="khung-tin-nhan__file-icon"><BieuTuongTaiLieu /></span>
                        <div className="khung-tin-nhan__file-thong-tin">
                          <span className="khung-tin-nhan__file-ten">{tn.tenFileGoc}</span>
                          <span className="khung-tin-nhan__file-size">{dinhDangKichThuoc(tn.kichThuocFile ?? 0)}</span>
                        </div>
                        <a
                          className="khung-tin-nhan__file-nut-tai"
                          href={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                          target="_blank"
                          rel="noreferrer"
                          onClick={(su) => su.stopPropagation()}
                          aria-label={`Tải xuống ${tn.tenFileGoc}`}
                        >
                          <BieuTuongTai />
                        </a>
                      </div>
                    )}
                    {tn.loaiTinNhan === 'Text' && tn.noiDungTinNhan}
                  </>
                )}
```

- [ ] **Step 13: Thêm CSS mới**

Thêm vào cuối `KhungTinNhan.css`:

```css
.khung-tin-nhan__nut-them {
  width: 26px;
  height: 26px;
  border-radius: 50%;
  border: 1px solid var(--mau-vien);
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.15);
}

.khung-tin-nhan__menu {
  position: absolute;
  top: 16px;
  right: 8px;
  z-index: 5;
  min-width: 160px;
  background: var(--mau-nen-the);
  border: 1px solid var(--mau-vien);
  border-radius: 10px;
  box-shadow: var(--bong-the);
  padding: 6px;
  display: flex;
  flex-direction: column;
}

.khung-tin-nhan__menu button,
.khung-tin-nhan__menu a {
  border: none;
  background: none;
  text-align: left;
  padding: 8px 10px;
  border-radius: 8px;
  font-family: inherit;
  font-size: 13px;
  color: var(--mau-chu-dam);
  cursor: pointer;
  text-decoration: none;
}

.khung-tin-nhan__menu button:hover,
.khung-tin-nhan__menu a:hover {
  background: var(--mau-nen-tren);
}

.khung-tin-nhan__menu-nguy-hiem {
  color: var(--mau-loi) !important;
}

.khung-tin-nhan__da-thu-hoi {
  font-style: italic;
  opacity: 0.75;
}

.khung-tin-nhan__banner-ghim {
  padding: 6px 24px;
  border-bottom: 1px solid var(--mau-vien);
  background: var(--mau-nen-tren);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.khung-tin-nhan__dong-ghim {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__dong-ghim-noi-dung {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.khung-tin-nhan__dong-ghim button {
  flex-shrink: 0;
  border: none;
  background: none;
  cursor: pointer;
  color: var(--mau-chu-phu);
}
```

- [ ] **Step 14: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: PASS toàn bộ.

- [ ] **Step 15: `tsc --noEmit`**

Run: `cd frontend && npx tsc --noEmit`
Expected: có thể còn lỗi ở `TrangChat.tsx`/`TrangNhom.tsx` (thiếu 5 prop
mới bắt buộc) — đây LÀ phạm vi Task 5. Xác nhận KHÔNG còn lỗi bên trong
`KhungTinNhan.tsx`/`.test.tsx`/`BieuTuong.tsx`/`DichVuApi.ts`/`.test.ts`.

- [ ] **Step 16: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts frontend/src/ThanhPhan/BieuTuong.tsx frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "feat(frontend): menu hanh dong tin nhan + banner ghim trong KhungTinNhan (GD6b)"
```

---

## Task 5: Frontend — nối dây `TrangChat.tsx`/`TrangNhom.tsx`

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `PropsKhungTinNhan.onThuHoi/onGhim/onBoGhim/onAn/danhSachTinNhanGhim` (Task 4);
  `AnTinNhan`/`LayTinDaGhimTheoNguoiDung`/`LayTinDaGhimTheoNhom` (Task 4).
- Produces: không có task nào phụ thuộc (task cuối của GĐ6b).

- [ ] **Step 1: Thêm state + effect tải tin ghim trong `TrangChat.tsx`**

Thêm import: `LayTinDaGhimTheoNguoiDung, AnTinNhan` vào dòng import từ
`../DichVuApi` hiện có. Thêm state mới (cạnh `conThemLichSu`):

```typescript
  const [tinNhanGhimTheoDoiTac, setTinNhanGhimTheoDoiTac] = useState<Record<string, TinNhan[]>>({});
```

(Cần import thêm `TinNhan` từ `'../KieuDuLieu'` nếu chưa có trong dòng
import kiểu hiện tại.)

Thêm effect mới (đặt sau effect tải lịch sử hiện có):

```typescript
  useEffect(() => {
    if (!token || !nguoiDangChon) return;
    LayTinDaGhimTheoNguoiDung(token, nguoiDangChon.id)
      .then((ghim) => setTinNhanGhimTheoDoiTac((truoc) => ({ ...truoc, [nguoiDangChon.id]: ghim })))
      .catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token, nguoiDangChon]);
```

- [ ] **Step 2: Thêm 4 handler mới trong `TrangChat.tsx`**

Đặt sau hàm `guiTep`:

```typescript
  function capNhatTinNhanTrongState(tinCapNhat: TinNhanHienThi) {
    const idKia = idNguoiKia(tinCapNhat, idHienTai);
    setTinNhanTheoNguoiDung((truoc) => ({
      ...truoc,
      [idKia]: (truoc[idKia] ?? []).map((tn) => (tn.id === tinCapNhat.id ? tinCapNhat : tn)),
    }));
  }

  function thuHoiTinNhan(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('ThuHoiTinNhan', id)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Thu hồi tin nhắn thất bại.'));
  }

  function ghimTinNhan(id: string) {
    if (!ketNoi || !nguoiDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('GhimTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoDoiTac((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: [...(truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== tinCapNhat.id), tinCapNhat],
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Ghim tin nhắn thất bại.'));
  }

  function boGhimTinNhan(id: string) {
    if (!ketNoi || !nguoiDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('BoGhimTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoDoiTac((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Bỏ ghim thất bại.'));
  }

  function anTinNhanCucBo(id: string) {
    if (!token || !nguoiDangChon) return;
    AnTinNhan(token, id)
      .then(() => {
        setTinNhanTheoNguoiDung((truoc) => ({
          ...truoc,
          [nguoiDangChon.id]: (truoc[nguoiDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
      })
      .catch(() => setLoi('Xóa tin nhắn thất bại.'));
  }
```

- [ ] **Step 3: Đăng ký 3 SignalR listener mới trong `TrangChat.tsx`**

Trong effect đăng ký listener `NhanTinNhan`/`TrangThaiHoatDongThayDoi`
hiện có, thêm handler + đăng ký/hủy đăng ký:

```typescript
    function xuLyTinNhanCapNhat(tinNhan: TinNhanHienThi) {
      if (tinNhan.nhomId) return;
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanTheoNguoiDung((truoc) => ({
        ...truoc,
        [idKia]: (truoc[idKia] ?? []).map((tn) => (tn.id === tinNhan.id ? tinNhan : tn)),
      }));
    }

    function xuLyTinNhanGhim(tinNhan: TinNhanHienThi) {
      xuLyTinNhanCapNhat(tinNhan);
      if (tinNhan.nhomId) return;
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanGhimTheoDoiTac((truoc) => ({
        ...truoc,
        [idKia]: [...(truoc[idKia] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan],
      }));
    }

    function xuLyTinNhanBoGhim(tinNhan: TinNhanHienThi) {
      xuLyTinNhanCapNhat(tinNhan);
      if (tinNhan.nhomId) return;
      const idKia = idNguoiKia(tinNhan, idHienTai);
      setTinNhanGhimTheoDoiTac((truoc) => ({
        ...truoc,
        [idKia]: (truoc[idKia] ?? []).filter((tn) => tn.id !== tinNhan.id),
      }));
    }

    ketNoi.on('TinNhanDaThuHoi', xuLyTinNhanCapNhat);
    ketNoi.on('TinNhanDaGhim', xuLyTinNhanGhim);
    ketNoi.on('TinNhanBoGhim', xuLyTinNhanBoGhim);
```

và trong hàm cleanup (`return () => { ... }`) của CÙNG effect đó, thêm:

```typescript
      ketNoi.off('TinNhanDaThuHoi', xuLyTinNhanCapNhat);
      ketNoi.off('TinNhanDaGhim', xuLyTinNhanGhim);
      ketNoi.off('TinNhanBoGhim', xuLyTinNhanBoGhim);
```

- [ ] **Step 4: Truyền prop mới cho `<KhungTinNhan>` trong `TrangChat.tsx`**

```tsx
          onThuHoi={thuHoiTinNhan}
          onGhim={ghimTinNhan}
          onBoGhim={boGhimTinNhan}
          onAn={anTinNhanCucBo}
          danhSachTinNhanGhim={nguoiDangChon ? (tinNhanGhimTheoDoiTac[nguoiDangChon.id] ?? []) : []}
```

- [ ] **Step 5: Lặp lại Step 1-4 cho `TrangNhom.tsx`**

Áp dụng cùng logic, thay `nguoiDangChon`/`tinNhanTheoNguoiDung`/
`LayTinDaGhimTheoNguoiDung`/`idNguoiKia` bằng
`nhomDangChon`/`tinNhanTheoNhom`/`LayTinDaGhimTheoNhom`/(không cần
`idNguoiKia` vì tin nhóm không phân biệt 2 chiều — key state luôn là
`nhomDangChon.id`). Cụ thể:

State mới: `const [tinNhanGhimTheoNhom, setTinNhanGhimTheoNhom] = useState<Record<string, TinNhan[]>>({});`

Effect tải tin ghim:
```typescript
  useEffect(() => {
    if (!token || !nhomDangChonId) return;
    LayTinDaGhimTheoNhom(token, nhomDangChonId)
      .then((ghim) => setTinNhanGhimTheoNhom((truoc) => ({ ...truoc, [nhomDangChonId]: ghim })))
      .catch(() => {});
  }, [token, nhomDangChonId]);
```

4 handler:
```typescript
  function capNhatTinNhanTrongState(tinCapNhat: TinNhanHienThi) {
    if (!tinCapNhat.nhomId) return;
    setTinNhanTheoNhom((truoc) => ({
      ...truoc,
      [tinCapNhat.nhomId as string]: (truoc[tinCapNhat.nhomId as string] ?? []).map((tn) => (tn.id === tinCapNhat.id ? tinCapNhat : tn)),
    }));
  }

  function thuHoiTinNhan(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('ThuHoiTinNhan', id)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Thu hồi tin nhắn thất bại.'));
  }

  function ghimTinNhan(id: string) {
    if (!ketNoi || !nhomDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('GhimTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoNhom((truoc) => ({
          ...truoc,
          [nhomDangChon.id]: [...(truoc[nhomDangChon.id] ?? []).filter((tn) => tn.id !== tinCapNhat.id), tinCapNhat],
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Ghim tin nhắn thất bại.'));
  }

  function boGhimTinNhan(id: string) {
    if (!ketNoi || !nhomDangChon) return;
    ketNoi.invoke<TinNhanHienThi>('BoGhimTinNhan', id)
      .then((tinCapNhat) => {
        capNhatTinNhanTrongState(tinCapNhat);
        setTinNhanGhimTheoNhom((truoc) => ({
          ...truoc,
          [nhomDangChon.id]: (truoc[nhomDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Bỏ ghim thất bại.'));
  }

  function anTinNhanCucBo(id: string) {
    if (!token || !nhomDangChon) return;
    AnTinNhan(token, id)
      .then(() => {
        setTinNhanTheoNhom((truoc) => ({
          ...truoc,
          [nhomDangChon.id]: (truoc[nhomDangChon.id] ?? []).filter((tn) => tn.id !== id),
        }));
      })
      .catch(() => setLoi('Xóa tin nhắn thất bại.'));
  }
```

3 SignalR listener mới (trong effect đăng ký `NhanTinNhan`/`DuocThemVaoNhom`/...
hiện có):
```typescript
    function xuLyTinNhanGhim(tinNhan: TinNhanHienThi) {
      capNhatTinNhanTrongState(tinNhan);
      if (!tinNhan.nhomId) return;
      setTinNhanGhimTheoNhom((truoc) => ({
        ...truoc,
        [tinNhan.nhomId as string]: [...(truoc[tinNhan.nhomId as string] ?? []).filter((tn) => tn.id !== tinNhan.id), tinNhan],
      }));
    }

    function xuLyTinNhanBoGhim(tinNhan: TinNhanHienThi) {
      capNhatTinNhanTrongState(tinNhan);
      if (!tinNhan.nhomId) return;
      setTinNhanGhimTheoNhom((truoc) => ({
        ...truoc,
        [tinNhan.nhomId as string]: (truoc[tinNhan.nhomId as string] ?? []).filter((tn) => tn.id !== tinNhan.id),
      }));
    }

    ketNoi.on('TinNhanDaThuHoi', capNhatTinNhanTrongState);
    ketNoi.on('TinNhanDaGhim', xuLyTinNhanGhim);
    ketNoi.on('TinNhanBoGhim', xuLyTinNhanBoGhim);
```
và cleanup tương ứng `ketNoi.off(...)` cho cả 3.

Prop truyền vào `<KhungTinNhan>`:
```tsx
          onThuHoi={thuHoiTinNhan}
          onGhim={ghimTinNhan}
          onBoGhim={boGhimTinNhan}
          onAn={anTinNhanCucBo}
          danhSachTinNhanGhim={nhomDangChon ? (tinNhanGhimTheoNhom[nhomDangChon.id] ?? []) : []}
```

Import thêm `LayTinDaGhimTheoNhom, AnTinNhan` từ `'../DichVuApi'`.

- [ ] **Step 6: Cập nhật fixture `TinNhan` trong 2 file test**

Run: `cd frontend && npx tsc --noEmit`
Expected: liệt kê lỗi thiếu field `daThuHoi`/`daGhim`/`thoiGianGhim` (và
`traLoi` nếu còn sót từ GĐ6a) ở mọi fixture `TinNhan` literal trong
`TrangChat.test.tsx`/`TrangNhom.test.tsx`. Thêm `daThuHoi: false, daGhim: false, thoiGianGhim: null,`
vào từng fixture đó (định vị bằng nội dung `thoiGianTao:` cạnh
`daDoc:`/`daNhan:`).

- [ ] **Step 7: Chạy toàn bộ test frontend**

Run: `cd frontend && npm test -- --run`
Expected: PASS toàn bộ.

- [ ] **Step 8: `tsc --noEmit` toàn repo frontend**

Run: `cd frontend && npx tsc --noEmit`
Expected: 0 lỗi.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.test.tsx
git commit -m "feat(frontend): noi day thu hoi/ghim/bo ghim/an tin nhan vao TrangChat/TrangNhom (GD6b)"
```

---

## Ghi chú cho reviewer / executor

- Task 1 độc lập (nền tảng model/repo).
- Task 2 phụ thuộc Task 1.
- Task 3 phụ thuộc Task 2.
- Task 4 phụ thuộc Task 3 CHỈ về JSON shape (`daThuHoi`/`daGhim`/`thoiGianGhim`,
  endpoint `an`/`ghim`) — có thể code song song về mặt tĩnh, test đơn vị
  dùng mock nên không cần backend chạy thật.
- Task 5 phụ thuộc Task 4 (props mới của `KhungTinNhan`).
- QUAN TRỌNG (rút kinh nghiệm từ GĐ6a): `ChatHubTests.cs` gọi hub qua
  `InvokeAsync` — dynamic, KHÔNG được `dotnet build` kiểm tra kiểu tĩnh.
  GĐ6b KHÔNG đổi thêm tham số của `GuiTinNhan` (chỉ thêm hub method MỚI)
  nên KHÔNG có rủi ro tương tự — nhưng nếu review phát hiện bất kỳ thay
  đổi chữ ký hub method NÀO trong tương lai, luôn tự grep
  `InvokeAsync<...>("TênMethod"` để rà thủ công, không tin tưởng
  `dotnet build` sạch là đủ.
