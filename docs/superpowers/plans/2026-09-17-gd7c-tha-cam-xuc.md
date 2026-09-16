# GĐ7c — Thả cảm xúc tin nhắn — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm icon 👍 nổi cạnh mỗi tin nhắn (cùng cụm với Trả lời/...
đã có). Hover mở popup 6 cảm xúc kiểu Messenger; bấm thẳng icon = toggle
"Thích" nhanh. Bong bóng hiện badge tổng hợp cảm xúc + số lượng.

**Architecture:** Backend lưu `List<CamXucTinNhan>` nhúng trực tiếp
trên `TinNhan` (1 phần tử/người dùng, thay thế khi đổi loại). Hub 2
method mới (`ThaCamXucTinNhan`/`BoCamXucTinNhan`) broadcast dùng lại
helper `GuiBroadcastCapNhatAsync` đã có từ GĐ6b. Frontend thêm UI hoàn
toàn trong `KhungTinNhan.tsx` (icon + popup + badge), `TrangChat.tsx`/
`TrangNhom.tsx` chỉ thêm 2 handler + 1 listener.

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api) + MongoDB
Driver, xUnit; React 19 + TypeScript + Vite (frontend/), Vitest.

**Spec:** `docs/superpowers/specs/2026-09-17-halochat-tha-cam-xuc.md`

**Phụ thuộc:** Không phụ thuộc GĐ7a/7b — có thể chạy độc lập, không đụng
file chung ngoài `KhungTinNhan.tsx`/`.css`/`.test.tsx` (JSX chèn thêm
tại vị trí riêng biệt, không xung đột nội dung với 2 plan kia nếu chạy
sau — nếu chạy TRƯỚC 7a/7b thì không có gì phải lo).

## Global Constraints

- Đặt tên định danh không dấu tiếng Việt.
- Đúng 6 loại cảm xúc cố định: `Thich` 👍, `YeuThich` ❤️, `Haha` 😂,
  `Wow` 😮, `Buon` 😢, `PhanNo` 😠 — không thêm/bớt.
- Mỗi người dùng chỉ có TỐI ĐA 1 cảm xúc/tin nhắn — thả cảm xúc khác
  THAY THẾ, không cộng dồn.
- Ai trong hội thoại 1-1 hoặc thành viên nhóm cũng thả được — dùng lại
  `KiemTraQuyenTrenTinNhanAsync` đã có từ GĐ6b.
- Tin đã THU HỒI: `AnhXaDto` LUÔN trả `DanhSachCamXuc` rỗng (badge ẩn
  hoàn toàn), bất kể dữ liệu gốc còn gì trong Mongo.
- `TinNhanDto` chỉ THÊM tham số cuối, không xóa/sắp xếp lại.
- `frontend/tsconfig.json` là solution-style — LUÔN dùng
  `npx tsc -b --noEmit`, KHÔNG dùng `tsc --noEmit` thường.

---

## Task 1: Backend — model, repository, service, hub

**Files:**
- Create: `backend/HaloChat.Api/Models/CamXucTinNhan.cs`
- Modify: `backend/HaloChat.Api/Models/TinNhan.cs`
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Create: `backend/HaloChat.Api/Dto/CamXucDto.cs`
- Modify: `backend/HaloChat.Api/Dto/TinNhanDto.cs`
- Modify: `backend/HaloChat.Api/Hubs/ChatHub.cs`
- Modify: `backend/HaloChat.Api.Tests/ChatHubTests.cs`
- Test: `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapCamXucTests.cs`
- Test: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`

**Interfaces:**
- Consumes: `KiemTraQuyenTrenTinNhanAsync` (đã có, GĐ6b).
- Produces: `IDichVuTinNhan.ThaCamXucAsync(idHienTai, tinNhanId, loaiCamXuc): Task<TinNhanDto>`,
  `BoCamXucAsync(idHienTai, tinNhanId): Task<TinNhanDto>`; hub
  `ThaCamXucTinNhan(tinNhanId, loaiCamXuc): Task<TinNhanDto>`,
  `BoCamXucTinNhan(tinNhanId): Task<TinNhanDto>` broadcast sự kiện
  `"TinNhanDaCamXuc"`; `TinNhanDto.DanhSachCamXuc: List<CamXucDto>`
  (mỗi phần tử `{NguoiDungId, LoaiCamXuc}` dạng string).

- [ ] **Step 1: Tạo model `CamXucTinNhan` + thêm field vào `TinNhan.cs`**

Tạo `backend/HaloChat.Api/Models/CamXucTinNhan.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HaloChat.Api.Models;

public enum LoaiCamXuc
{
    Thich,
    YeuThich,
    Haha,
    Wow,
    Buon,
    PhanNo,
}

public class CamXucTinNhan
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string NguoiDungId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public LoaiCamXuc LoaiCamXuc { get; set; }
}
```

`TinNhan.cs` — thêm vào cuối class:

```csharp
    // [GĐ7c] Mỗi người dùng chỉ có tối đa 1 phần tử (thả cảm xúc khác =
    // thay thế, không cộng dồn).
    public List<CamXucTinNhan> DanhSachCamXuc { get; set; } = new();
```

- [ ] **Step 2: Viết test repo mới (FAIL trước)**

Tạo `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapCamXucTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapCamXucTests
{
    [Fact]
    public async Task ThaCamXucAsync_ChuaTung_ThemMoi()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        kho.DanhSach.Add(tinNhan);

        await kho.ThaCamXucAsync(tinNhan.Id, "nguoi-a", LoaiCamXuc.Thich);

        var camXuc = Assert.Single(kho.DanhSach.Single().DanhSachCamXuc);
        Assert.Equal("nguoi-a", camXuc.NguoiDungId);
        Assert.Equal(LoaiCamXuc.Thich, camXuc.LoaiCamXuc);
    }

    [Fact]
    public async Task ThaCamXucAsync_DaCoCamXucKhac_ThayTheKhongCongDon()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = "nguoi-a", LoaiCamXuc = LoaiCamXuc.Thich });
        kho.DanhSach.Add(tinNhan);

        await kho.ThaCamXucAsync(tinNhan.Id, "nguoi-a", LoaiCamXuc.Haha);

        var camXuc = Assert.Single(kho.DanhSach.Single().DanhSachCamXuc);
        Assert.Equal(LoaiCamXuc.Haha, camXuc.LoaiCamXuc);
    }

    [Fact]
    public async Task BoCamXucAsync_DaCo_XoaDung()
    {
        var kho = new TinNhanGiaLap();
        var tinNhan = new TinNhan();
        tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = "nguoi-a", LoaiCamXuc = LoaiCamXuc.Thich });
        kho.DanhSach.Add(tinNhan);

        await kho.BoCamXucAsync(tinNhan.Id, "nguoi-a");

        Assert.Empty(kho.DanhSach.Single().DanhSachCamXuc);
    }
}
```

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter TinNhanGiaLapCamXucTests`
Expected: FAIL biên dịch.

- [ ] **Step 4: Thêm method vào `ITinNhanRepository`/`TinNhanRepository`/fake**

`ITinNhanRepository.cs` — thêm vào cuối:

```csharp
    /// <summary>Thả/thay thế cảm xúc của nguoiDungId trên tin nhắn id (xóa cảm xúc cũ của cùng người nếu có, trước khi thêm mới).</summary>
    Task ThaCamXucAsync(string id, string nguoiDungId, LoaiCamXuc loaiCamXuc);

    /// <summary>Xóa cảm xúc của nguoiDungId trên tin nhắn id (nếu có).</summary>
    Task BoCamXucAsync(string id, string nguoiDungId);
```

`TinNhanRepository.cs` — thêm vào cuối class:

```csharp
    public async Task ThaCamXucAsync(string id, string nguoiDungId, LoaiCamXuc loaiCamXuc)
    {
        var boLoc = Builders<TinNhan>.Filter.Eq(t => t.Id, id);
        var xoaCu = Builders<TinNhan>.Update.PullFilter(
            t => t.DanhSachCamXuc, cx => cx.NguoiDungId == nguoiDungId);
        await _collection.UpdateOneAsync(boLoc, xoaCu);

        var themMoi = Builders<TinNhan>.Update.Push(
            t => t.DanhSachCamXuc, new CamXucTinNhan { NguoiDungId = nguoiDungId, LoaiCamXuc = loaiCamXuc });
        await _collection.UpdateOneAsync(boLoc, themMoi);
    }

    public async Task BoCamXucAsync(string id, string nguoiDungId)
    {
        var boLoc = Builders<TinNhan>.Filter.Eq(t => t.Id, id);
        var xoa = Builders<TinNhan>.Update.PullFilter(
            t => t.DanhSachCamXuc, cx => cx.NguoiDungId == nguoiDungId);
        await _collection.UpdateOneAsync(boLoc, xoa);
    }
```

`backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs` — thêm vào cuối class:

```csharp
    public Task ThaCamXucAsync(string id, string nguoiDungId, LoaiCamXuc loaiCamXuc)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        if (tinNhan is not null)
        {
            tinNhan.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == nguoiDungId);
            tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = nguoiDungId, LoaiCamXuc = loaiCamXuc });
        }
        return Task.CompletedTask;
    }

    public Task BoCamXucAsync(string id, string nguoiDungId)
    {
        var tinNhan = DanhSach.FirstOrDefault(t => t.Id == id);
        tinNhan?.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == nguoiDungId);
        return Task.CompletedTask;
    }
```

- [ ] **Step 5: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter TinNhanGiaLapCamXucTests`
Expected: PASS (3/3).

- [ ] **Step 6: Tạo `CamXucDto`, cập nhật `TinNhanDto`**

Tạo `backend/HaloChat.Api/Dto/CamXucDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record CamXucDto(string NguoiDungId, string LoaiCamXuc);
```

`TinNhanDto.cs` — thêm tham số cuối:

```csharp
public record TinNhanDto(
    string Id, string NguoiGuiId, string? NguoiNhanId, string? NhomId, string LoaiTinNhan,
    string NoiDungTinNhan, string? DuongDanFile, string? TenFileGoc, long? KichThuocFile, string? LoaiFile,
    bool DaDoc, bool DaNhan, DateTime ThoiGianTao, TraLoiThongTinDto? TraLoi,
    bool DaThuHoi, bool DaGhim, DateTime? ThoiGianGhim,
    List<CamXucDto> DanhSachCamXuc);
```

- [ ] **Step 7: Viết test service (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`:

```csharp
    // --- Thả cảm xúc (GĐ7c) ---

    [Fact]
    public async Task ThaCamXucAsync_ChuaTung_ThemMoi()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        var ketQua = await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Haha");

        var camXuc = Assert.Single(ketQua.DanhSachCamXuc);
        Assert.Equal(IdNguoiNhan, camXuc.NguoiDungId);
        Assert.Equal("Haha", camXuc.LoaiCamXuc);
    }

    [Fact]
    public async Task ThaCamXucAsync_DaCoCamXucKhac_ThayTheKhongCongDon()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);
        await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Thich");

        var ketQua = await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Wow");

        Assert.Single(ketQua.DanhSachCamXuc);
        Assert.Equal("Wow", ketQua.DanhSachCamXuc[0].LoaiCamXuc);
    }

    [Fact]
    public async Task ThaCamXucAsync_LoaiCamXucKhongHopLe_NemTinNhanKhongHopLe()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        await Assert.ThrowsAsync<TinNhanKhongHopLeException>(() => dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "KhongTonTai"));
    }

    [Fact]
    public async Task ThaCamXucAsync_KhongThuocHoiThoai_NemKhongCoQuyen()
    {
        const string IdNguoiThuBa = "507f1f77bcf86cd799439013";
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);

        await Assert.ThrowsAsync<KhongCoQuyenTrenTinNhanException>(() => dichVu.ThaCamXucAsync(IdNguoiThuBa, tin.Id, "Thich"));
    }

    [Fact]
    public async Task BoCamXucAsync_DaCo_XoaDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);
        await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Thich");

        var ketQua = await dichVu.BoCamXucAsync(IdNguoiNhan, tin.Id);

        Assert.Empty(ketQua.DanhSachCamXuc);
    }

    [Fact]
    public async Task LayLichSuAsync_TinDaThuHoiCoCamXucTruoc_AnDanhSachCamXuc()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Vui qua", null, null, null, null, null);
        await dichVu.ThaCamXucAsync(IdNguoiNhan, tin.Id, "Thich");
        await dichVu.ThuHoiAsync(IdNguoiGui, tin.Id);

        var lichSu = await dichVu.LayLichSuAsync(IdNguoiNhan, IdNguoiGui, null, 30);

        Assert.Empty(Assert.Single(lichSu).DanhSachCamXuc);
    }
```

- [ ] **Step 8: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "CamXuc"`
Expected: FAIL biên dịch.

- [ ] **Step 9: Thêm method vào `IDichVuTinNhan`/`DichVuTinNhan`, cập nhật `AnhXaDto`**

`IDichVuTinNhan.cs` — thêm vào cuối:

```csharp
    Task<TinNhanDto> ThaCamXucAsync(string idHienTai, string tinNhanId, string loaiCamXuc);
    Task<TinNhanDto> BoCamXucAsync(string idHienTai, string tinNhanId);
```

`DichVuTinNhan.cs` — thêm vào cuối class (trước `private static TinNhanDto AnhXaDto`):

```csharp
    public async Task<TinNhanDto> ThaCamXucAsync(string idHienTai, string tinNhanId, string loaiCamXuc)
    {
        if (!Enum.TryParse<LoaiCamXuc>(loaiCamXuc, ignoreCase: true, out var loai))
        {
            throw new TinNhanKhongHopLeException($"Loại cảm xúc không hợp lệ: {loaiCamXuc}.");
        }

        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.ThaCamXucAsync(tinNhanId, idHienTai, loai);
        tinNhan.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == idHienTai);
        tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = idHienTai, LoaiCamXuc = loai });
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> BoCamXucAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.BoCamXucAsync(tinNhanId, idHienTai);
        tinNhan.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == idHienTai);
        return AnhXaDto(tinNhan);
    }
```

Sửa `AnhXaDto` — thêm ánh xạ `DanhSachCamXuc` (rỗng nếu `DaThuHoi`):

```csharp
    private static TinNhanDto AnhXaDto(TinNhan t)
    {
        var traLoi = t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString());
        var danhSachCamXuc = t.DaThuHoi
            ? new List<CamXucDto>()
            : t.DanhSachCamXuc.Select(cx => new CamXucDto(cx.NguoiDungId, cx.LoaiCamXuc.ToString())).ToList();

        if (t.DaThuHoi)
        {
            return new(
                t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(),
                "Tin nhắn đã được thu hồi.", null, null, null, null,
                t.DaDoc, t.DaNhan, t.ThoiGianTao, traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim, danhSachCamXuc);
        }

        return new(
            t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
            t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
            traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim, danhSachCamXuc);
    }
```

- [ ] **Step 10: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "CamXuc"`
Expected: PASS (6/6).

- [ ] **Step 11: Viết test hub (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/ChatHubTests.cs`:

```csharp
    [Fact]
    public async Task ThaCamXucTinNhan_HopLe_PhiaKiaNhanDuocSuKienRealtime()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubcamxuca");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubcamxucb");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubcamxucb").Id;
        _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubcamxucb").ChoPhepTinNhanTuNguoiLa = true;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await using var ketNoiB = TaoKetNoiHub(tokenB);
        TinNhanDto? nhanDuoc = null;
        var daNhan = new TaskCompletionSource();
        ketNoiB.On<TinNhanDto>("TinNhanDaCamXuc", tn => { nhanDuoc = tn; daNhan.SetResult(); });

        await ketNoiA.StartAsync();
        await ketNoiB.StartAsync();
        var tinGui = await ketNoiA.InvokeAsync<TinNhanDto>("GuiTinNhan", idB, null, "Text", "Vui qua", null, null, null, null, null);

        await ketNoiA.InvokeAsync<TinNhanDto>("ThaCamXucTinNhan", tinGui.Id, "Haha");

        await daNhan.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(nhanDuoc);
        Assert.Single(nhanDuoc!.DanhSachCamXuc);
        Assert.Equal("Haha", nhanDuoc.DanhSachCamXuc[0].LoaiCamXuc);
    }

    [Fact]
    public async Task BoCamXucTinNhan_HopLe_XoaVaBroadcast()
    {
        var tokenA = await TaoTaiKhoanVaDangNhapAsync("hubcamxucc");
        var tokenB = await TaoTaiKhoanVaDangNhapAsync("hubcamxucd");
        var idB = _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubcamxucd").Id;
        _factory.KhoGiaLap.DanhSach.Single(nd => nd.TenTaiKhoan == "hubcamxucd").ChoPhepTinNhanTuNguoiLa = true;

        await using var ketNoiA = TaoKetNoiHub(tokenA);
        await ketNoiA.StartAsync();
        var tinGui = await ketNoiA.InvokeAsync<TinNhanDto>("GuiTinNhan", idB, null, "Text", "Vui qua", null, null, null, null, null);
        await ketNoiA.InvokeAsync<TinNhanDto>("ThaCamXucTinNhan", tinGui.Id, "Thich");

        var ketQua = await ketNoiA.InvokeAsync<TinNhanDto>("BoCamXucTinNhan", tinGui.Id);

        Assert.Empty(ketQua.DanhSachCamXuc);
    }
```

- [ ] **Step 12: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "CamXuc"`
Expected: FAIL (hub method chưa tồn tại).

- [ ] **Step 13: Thêm 2 hub method vào `ChatHub.cs`**

Thêm vào cuối class (sau method `BoGhimTinNhan`, trước
`GuiBroadcastCapNhatAsync`):

```csharp
    public async Task<TinNhanDto> ThaCamXucTinNhan(string tinNhanId, string loaiCamXuc)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.ThaCamXucAsync(NguoiDungHienTaiId, tinNhanId, loaiCamXuc);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaCamXuc");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (TinNhanKhongHopLeException loi) { throw new HubException(loi.Message); }
        catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
        catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
    }

    public async Task<TinNhanDto> BoCamXucTinNhan(string tinNhanId)
    {
        try
        {
            var tinNhan = await _dichVuTinNhan.BoCamXucAsync(NguoiDungHienTaiId, tinNhanId);
            await GuiBroadcastCapNhatAsync(tinNhan, "TinNhanDaCamXuc");
            return tinNhan;
        }
        catch (TinNhanKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongCoQuyenTrenTinNhanException loi) { throw new HubException(loi.Message); }
        catch (NhomKhongTonTaiException loi) { throw new HubException(loi.Message); }
        catch (KhongPhaiThanhVienNhomException loi) { throw new HubException(loi.Message); }
    }
```

- [ ] **Step 14: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "CamXuc"`
Expected: PASS (2/2).

- [ ] **Step 15: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: 0 lỗi build; toàn bộ test PASS.

- [ ] **Step 16: Commit**

```bash
git add backend/HaloChat.Api/Models/CamXucTinNhan.cs backend/HaloChat.Api/Models/TinNhan.cs backend/HaloChat.Api/Repositories/ITinNhanRepository.cs backend/HaloChat.Api/Repositories/TinNhanRepository.cs backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Dto/CamXucDto.cs backend/HaloChat.Api/Dto/TinNhanDto.cs backend/HaloChat.Api/Hubs/ChatHub.cs backend/HaloChat.Api.Tests/ChatHubTests.cs backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapCamXucTests.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs
git commit -m "feat(backend): tha/bo cam xuc tin nhan, an khi da thu hoi (GD7c)"
```

---

## Task 2: Frontend — UI popup 6 cảm xúc + badge trong `KhungTinNhan.tsx`

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`

**Interfaces:**
- Consumes: JSON `TinNhan.danhSachCamXuc` từ Task 1.
- Produces: `PropsKhungTinNhan.onThaCamXuc: (id: string, loaiCamXuc: LoaiCamXuc) => void`,
  `onBoCamXuc: (id: string) => void` — Task 3 (TrangChat/TrangNhom) implement.

- [ ] **Step 1: Thêm kiểu dữ liệu vào `KieuDuLieu.ts`**

```typescript
export type LoaiCamXuc = 'Thich' | 'YeuThich' | 'Haha' | 'Wow' | 'Buon' | 'PhanNo';

export interface CamXuc {
  nguoiDungId: string;
  loaiCamXuc: LoaiCamXuc;
}
```
Thêm `danhSachCamXuc: CamXuc[];` vào cuối interface `TinNhan`.

- [ ] **Step 2: Viết test cho UI cảm xúc (FAIL trước)**

Thêm `onThaCamXuc: () => {}, onBoCamXuc: () => {}` vào `PROPS_MAC_DINH`.
Thêm `danhSachCamXuc: []` vào `TIN_NHAN_MAU`. Thêm vào cuối
`describe('KhungTinNhan', ...)`:

```tsx
it('bam nhanh icon like khi chua co cam xuc thi tha Thich', async () => {
  const tin = { ...TIN_NHAN_MAU, id: 'm1', danhSachCamXuc: [] };
  const onThaCamXuc = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThaCamXuc={onThaCamXuc} />);

  await userEvent.click(screen.getByRole('button', { name: 'Thích tin nhắn này' }));

  expect(onThaCamXuc).toHaveBeenCalledWith('m1', 'Thich');
});

it('bam nhanh icon like khi DA co cam xuc cua minh thi bo cam xuc', async () => {
  const tin = { ...TIN_NHAN_MAU, id: 'm1', danhSachCamXuc: [{ nguoiDungId: '1', loaiCamXuc: 'Haha' as const }] };
  const onBoCamXuc = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onBoCamXuc={onBoCamXuc} />);

  await userEvent.click(screen.getByRole('button', { name: 'Thích tin nhắn này' }));

  expect(onBoCamXuc).toHaveBeenCalledWith('m1');
});

it('hover icon like hien popup 6 cam xuc, bam 1 cai goi dung loai', async () => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
  const tin = { ...TIN_NHAN_MAU, id: 'm1', danhSachCamXuc: [] };
  const onThaCamXuc = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThaCamXuc={onThaCamXuc} />);

  await userEvent.setup({ delay: null }).hover(screen.getByRole('button', { name: 'Thích tin nhắn này' }));
  vi.advanceTimersByTime(450);

  const nutWow = await screen.findByRole('button', { name: 'Thả cảm xúc Wow' });
  await userEvent.setup({ delay: null }).click(nutWow);

  expect(onThaCamXuc).toHaveBeenCalledWith('m1', 'Wow');
  vi.useRealTimers();
});

it('tin co cam xuc hien badge tong hop dung so luong', () => {
  const tin = {
    ...TIN_NHAN_MAU, id: 'm1',
    danhSachCamXuc: [
      { nguoiDungId: '1', loaiCamXuc: 'Thich' as const },
      { nguoiDungId: '2', loaiCamXuc: 'YeuThich' as const },
    ],
  };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" />);

  expect(screen.getByText('👍❤️ 2')).toBeInTheDocument();
});

it('tin da thu hoi khong hien badge cam xuc du danhSachCamXuc khong rong', () => {
  const tin = {
    ...TIN_NHAN_MAU, id: 'm1', daThuHoi: true,
    danhSachCamXuc: [{ nguoiDungId: '2', loaiCamXuc: 'Thich' as const }],
  };
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" />);

  expect(screen.queryByText(/👍/)).not.toBeInTheDocument();
});
```

- [ ] **Step 3: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: FAIL (UI cảm xúc chưa tồn tại).

- [ ] **Step 4: Thêm hằng số ánh xạ emoji + import type**

Thêm import `type { TinNhan, LoaiCamXuc }` (sửa dòng import type hiện
có từ `'../KieuDuLieu'` thành có thêm `LoaiCamXuc`). Thêm hằng số sau
`DANH_SACH_EMOJI`:

```typescript
const EMOJI_CAM_XUC: Record<LoaiCamXuc, string> = {
  Thich: '👍', YeuThich: '❤️', Haha: '😂', Wow: '😮', Buon: '😢', PhanNo: '😠',
};
const THU_TU_CAM_XUC: LoaiCamXuc[] = ['Thich', 'YeuThich', 'Haha', 'Wow', 'Buon', 'PhanNo'];
```

- [ ] **Step 5: Thêm prop + state**

Thêm vào `PropsKhungTinNhan`:
```tsx
  onThaCamXuc: (id: string, loaiCamXuc: LoaiCamXuc) => void;
  onBoCamXuc: (id: string) => void;
```
Thêm vào destructure props. Thêm state (cạnh `menuMoChoTinNhanId`):
```tsx
  const [popupCamXucChoTinNhanId, setPopupCamXucChoTinNhanId] = useState<string | null>(null);
  const homGioHanCamXucRef = useRef<ReturnType<typeof setTimeout> | null>(null);
```

- [ ] **Step 6: Thêm icon 👍 + popup 6 cảm xúc vào `.khung-tin-nhan__icon-noi`**

Trong khối JSX render mỗi tin nhắn, thêm ngay TRƯỚC nút "Trả lời" hiện
có (bên trong `.khung-tin-nhan__icon-noi`):

```tsx
                <div className="khung-tin-nhan__cam-xuc-cum">
                  <button
                    type="button"
                    className="khung-tin-nhan__nut-cam-xuc"
                    aria-label="Thích tin nhắn này"
                    onClick={(su) => {
                      su.stopPropagation();
                      const daCoCuaMinh = tn.danhSachCamXuc.some((cx) => cx.nguoiDungId === idHienTai);
                      if (daCoCuaMinh) onBoCamXuc(tn.id);
                      else onThaCamXuc(tn.id, 'Thich');
                    }}
                    onMouseEnter={() => {
                      if (homGioHanCamXucRef.current) clearTimeout(homGioHanCamXucRef.current);
                      homGioHanCamXucRef.current = setTimeout(() => setPopupCamXucChoTinNhanId(tn.id), 400);
                    }}
                    onMouseLeave={() => {
                      if (homGioHanCamXucRef.current) clearTimeout(homGioHanCamXucRef.current);
                    }}
                  >
                    👍
                  </button>
                  {popupCamXucChoTinNhanId === tn.id && (
                    <div
                      className="khung-tin-nhan__popup-cam-xuc"
                      onMouseLeave={() => setPopupCamXucChoTinNhanId(null)}
                    >
                      {THU_TU_CAM_XUC.map((loai) => (
                        <button
                          key={loai}
                          type="button"
                          aria-label={`Thả cảm xúc ${loai}`}
                          onClick={(su) => {
                            su.stopPropagation();
                            onThaCamXuc(tn.id, loai);
                            setPopupCamXucChoTinNhanId(null);
                          }}
                        >
                          {EMOJI_CAM_XUC[loai]}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
```

- [ ] **Step 7: Thêm badge tổng hợp trong bong bóng**

Ngay SAU khối nội dung tin nhắn (`{tn.daThuHoi ? (...) : (...)}`) và
TRƯỚC khối `{tinDangMoId === tn.id && (...)}`, thêm:

```tsx
                {!tn.daThuHoi && tn.danhSachCamXuc.length > 0 && (
                  <span className="khung-tin-nhan__badge-cam-xuc">
                    {Array.from(new Set(tn.danhSachCamXuc.map((cx) => cx.loaiCamXuc)))
                      .slice(0, 3)
                      .map((loai) => EMOJI_CAM_XUC[loai])
                      .join('')}
                    {' '}
                    {tn.danhSachCamXuc.length}
                  </span>
                )}
```

- [ ] **Step 8: Thêm CSS**

Thêm vào cuối `KhungTinNhan.css`:

```css
.khung-tin-nhan__cam-xuc-cum {
  position: relative;
}

.khung-tin-nhan__nut-cam-xuc {
  width: 26px;
  height: 26px;
  border-radius: 50%;
  border: 1px solid var(--mau-vien);
  background: var(--mau-nen-the);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 14px;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.15);
}

.khung-tin-nhan__popup-cam-xuc {
  position: absolute;
  bottom: calc(100% + 6px);
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  gap: 2px;
  background: var(--mau-nen-the);
  border: 1px solid var(--mau-vien);
  border-radius: 999px;
  padding: 4px 6px;
  box-shadow: var(--bong-the);
  z-index: 7;
}

.khung-tin-nhan__popup-cam-xuc button {
  border: none;
  background: none;
  font-size: 20px;
  line-height: 1;
  padding: 4px;
  cursor: pointer;
  transition: transform 0.1s ease;
}

.khung-tin-nhan__popup-cam-xuc button:hover {
  transform: scale(1.3);
}

.khung-tin-nhan__badge-cam-xuc {
  align-self: flex-end;
  background: var(--mau-nen-the);
  border: 1px solid var(--mau-vien);
  border-radius: 999px;
  padding: 1px 6px;
  font-size: 11px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.15);
  margin-top: -8px;
  margin-right: -4px;
}
```

- [ ] **Step 9: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: PASS toàn bộ.

- [ ] **Step 10: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: có thể còn lỗi ở `TrangChat.tsx`/`TrangNhom.tsx` (thiếu 2 prop
mới bắt buộc `onThaCamXuc`/`onBoCamXuc`, và fixture `TinNhan` thiếu
`danhSachCamXuc`) — đây LÀ phạm vi Task 3. Xác nhận KHÔNG còn lỗi bên
trong `KhungTinNhan.tsx`/`.test.tsx`/`KieuDuLieu.ts`.

- [ ] **Step 11: Commit**

```bash
git add frontend/src/KieuDuLieu.ts frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "feat(frontend): popup 6 cam xuc + badge tong hop trong KhungTinNhan (GD7c)"
```

---

## Task 3: Frontend — nối dây `TrangChat.tsx`/`TrangNhom.tsx`

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `PropsKhungTinNhan.onThaCamXuc`/`onBoCamXuc` (Task 2).
- Produces: không có task nào phụ thuộc (task cuối GĐ7c).

- [ ] **Step 1: Thêm handler + listener vào `TrangChat.tsx`**

Thêm import `type { LoaiCamXuc }` vào dòng import type từ
`'../KieuDuLieu'`. Thêm 2 hàm (cạnh `boGhimTinNhan`):

```typescript
  function thaCamXuc(id: string, loaiCamXuc: LoaiCamXuc) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('ThaCamXucTinNhan', id, loaiCamXuc)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Thả cảm xúc thất bại.'));
  }

  function boCamXuc(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('BoCamXucTinNhan', id)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Bỏ cảm xúc thất bại.'));
  }
```

Trong effect đăng ký listener SignalR hiện có, thêm:
```typescript
    ketNoi.on('TinNhanDaCamXuc', xuLyTinNhanCapNhat);
```
và cleanup tương ứng:
```typescript
      ketNoi.off('TinNhanDaCamXuc', xuLyTinNhanCapNhat);
```
(Dùng lại ĐÚNG `xuLyTinNhanCapNhat` đã có — hàm này cập nhật cả
`tinNhanTheoNguoiDung` lẫn `tinNhanGhimTheoDoiTac`, đủ dùng cho cảm xúc
vì cảm xúc không cần đồng bộ thêm state nào khác ngoài object tin nhắn
chính nó.)

Truyền `onThaCamXuc={thaCamXuc}` và `onBoCamXuc={boCamXuc}` cho
`<KhungTinNhan>`.

- [ ] **Step 2: Thêm tương tự vào `TrangNhom.tsx`**

```typescript
  function thaCamXuc(id: string, loaiCamXuc: LoaiCamXuc) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('ThaCamXucTinNhan', id, loaiCamXuc)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Thả cảm xúc thất bại.'));
  }

  function boCamXuc(id: string) {
    if (!ketNoi) return;
    ketNoi.invoke<TinNhanHienThi>('BoCamXucTinNhan', id)
      .then((tinCapNhat) => capNhatTinNhanTrongState(tinCapNhat))
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Bỏ cảm xúc thất bại.'));
  }
```
Đăng ký `ketNoi.on('TinNhanDaCamXuc', capNhatTinNhanTrongState);` (ở
`TrangNhom.tsx`, `capNhatTinNhanTrongState` dùng trực tiếp làm handler
— đúng pattern GĐ6b đã dùng cho `TinNhanDaThuHoi` ở file này) + cleanup
tương ứng. Truyền `onThaCamXuc={thaCamXuc}`/`onBoCamXuc={boCamXuc}` cho
`<KhungTinNhan>`.

- [ ] **Step 3: Cập nhật fixture `TinNhan` trong 2 file test**

Run: `cd frontend && npx tsc -b --noEmit`
Expected: liệt kê lỗi thiếu field `danhSachCamXuc` ở mọi fixture
`TinNhan` trong `TrangChat.test.tsx`/`TrangNhom.test.tsx` (factory
`taoTinNhanGiaLap` và literal trong `TrangNhom.test.tsx`). Thêm
`danhSachCamXuc: [],` vào từng nơi đó (định vị bằng nội dung
`thoiGianGhim:` — mọi object có field này đều là `TinNhan` cần thêm).

- [ ] **Step 4: Viết test cho `TrangChat.test.tsx`**

Thêm vào cuối `describe('TrangChat', ...)`:

```typescript
it('bam nhanh nut like goi ThaCamXucTinNhan qua hub', async () => {
  vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([taoTinNhanGiaLap({ id: 'm1' })]);
  ketNoiGiaLap.invoke.mockResolvedValue(taoTinNhanGiaLap({ id: 'm1', danhSachCamXuc: [{ nguoiDungId: '1', loaiCamXuc: 'Thich' }] }));

  renderTrangChat();
  await userEvent.click(await screen.findByText('TranBinh'));
  await userEvent.click(await screen.findByRole('button', { name: 'Thích tin nhắn này' }));

  await waitFor(() => expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('ThaCamXucTinNhan', 'm1', 'Thich'));
});
```

- [ ] **Step 5: Chạy test để thấy PASS, lặp lại cho `TrangNhom.test.tsx`**

Run: `cd frontend && npm test -- --run TrangChat`
Expected: PASS. Viết 1 test tương tự trong `TrangNhom.test.tsx` (dùng
đúng pattern render/mock `LayLichSuNhom` đã có trong file).

- [ ] **Step 6: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.test.tsx
git commit -m "feat(frontend): noi day tha/bo cam xuc tin nhan vao TrangChat/TrangNhom (GD7c)"
```

---

## Ghi chú cho reviewer / executor

- Task 1 độc lập. Task 2 phụ thuộc Task 1 chỉ qua JSON shape. Task 3
  phụ thuộc Task 2 (props `onThaCamXuc`/`onBoCamXuc`).
- Nếu GĐ7b đã chạy trước plan này, `KhungTinNhan.tsx` đã có
  `.khung-tin-nhan__icon-noi` chứa thêm state/JSX của tìm kiếm — Task 2
  Step 6 CHỈ chèn thêm cụm cảm xúc vào ĐẦU `.khung-tin-nhan__icon-noi`,
  không xóa/đổi bất kỳ nút nào khác đã có trong đó (Trả lời/...).
