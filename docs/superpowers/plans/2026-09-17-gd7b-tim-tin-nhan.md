# GĐ7b — Tìm tin nhắn trong cuộc trò chuyện — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Icon kính lúp trên header `KhungTinNhan` mở ô tìm kiếm pill,
tìm tin nhắn Text trong đúng cuộc trò chuyện đang mở (1-1 và nhóm) qua
backend, bấm 1 kết quả tự tải thêm lịch sử nếu cần rồi cuộn + nổi bật.

**Architecture:** Backend thêm 2 hàm repo/service tìm text trong đúng
cuộc trò chuyện (loại trừ tin thu hồi/ẩn) + 2 endpoint GET. Frontend:
`KhungTinNhan` tự quản lý UI ô tìm kiếm/dropdown/cuộn+nổi bật qua 2 prop
mới (`onTimKiem`, `onNhayToiTinNhan`), logic tải-thêm-lịch-sử-tới-khi-
thấy đặt ở `TrangChat.tsx`/`TrangNhom.tsx` (nơi đã có state phân trang).

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api) + MongoDB
Driver, xUnit; React 19 + TypeScript + Vite (frontend/), Vitest.

**Spec:** `docs/superpowers/specs/2026-09-17-halochat-tim-tin-nhan.md`

**Phụ thuộc:** Không phụ thuộc GĐ7a — có thể chạy song song hoặc trước/
sau tùy ý, không đụng file chung.

## Global Constraints

- Đặt tên định danh không dấu tiếng Việt.
- Chỉ tìm trong `NoiDungTinNhan` của tin loại Text — không tìm tên file.
- Loại trừ tin đã thu hồi (`DaThuHoi`) NGAY TRONG CÂU QUERY MONGO (không
  chỉ dựa vào `AnhXaDto`, vì thu hồi không xóa `NoiDungTinNhan` gốc —
  nếu chỉ lọc ở tầng DTO thì tin đã thu hồi vẫn "khớp" từ khóa ở tầng
  query rồi mới bị ẩn nội dung, nhưng VẪN xuất hiện trong danh sách kết
  quả với nội dung placeholder — sai yêu cầu). Và loại trừ tin đã bị
  người tìm kiếm ẩn cục bộ.
- Giới hạn tối đa 50 kết quả mỗi lần tìm.
- `tuKhoa` rỗng/toàn khoảng trắng → trả danh sách rỗng, không query.
- `frontend/tsconfig.json` là solution-style — LUÔN dùng
  `npx tsc -b --noEmit`, KHÔNG dùng `tsc --noEmit` thường.

---

## Task 1: Backend — repository, service, controller

**Files:**
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Controllers/TinNhanController.cs`
- Test: `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapTimKiemTests.cs`
- Test: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`
- Test: `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`

**Interfaces:**
- Consumes: `ITinNhanAnRepository.LayDanhSachIdDaAnAsync` (đã có).
- Produces: `IDichVuTinNhan.TimKiemTheoNguoiDungAsync(idHienTai, doiTacId, tuKhoa): Task<List<TinNhanDto>>`,
  `TimKiemTheoNhomAsync(idHienTai, nhomId, tuKhoa): Task<List<TinNhanDto>>`;
  endpoint `GET /api/tinnhan/nguoi-dung/{id}/tim-kiem?tuKhoa=...`,
  `GET /api/tinnhan/nhom/{id}/tim-kiem?tuKhoa=...`.

- [ ] **Step 1: Viết test repo mới (FAIL trước)**

Tạo `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapTimKiemTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapTimKiemTests
{
    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_KhopKhongPhanBietHoaThuong()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "Hẹn gặp lúc 5 giờ" });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "Không liên quan" });

        var ketQua = await kho.TimKiemTheoNguoiDungAsync("a", "b", "HẸN GẶP");

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_LoaiTruTinDaThuHoi()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "Bí mật quan trọng", DaThuHoi = true });

        var ketQua = await kho.TimKiemTheoNguoiDungAsync("a", "b", "bí mật");

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNhomAsync_ChiTraVeTinCuaDungNhom()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "họp nhóm 5h" });
        kho.DanhSach.Add(new TinNhan { NhomId = "n2", LoaiTinNhan = LoaiTinNhan.Text, NoiDungTinNhan = "họp nhóm 5h" });

        var ketQua = await kho.TimKiemTheoNhomAsync("n1", "họp");

        Assert.Single(ketQua);
    }
}
```

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter TinNhanGiaLapTimKiemTests`
Expected: FAIL biên dịch.

- [ ] **Step 3: Thêm method vào `ITinNhanRepository`/`TinNhanRepository`/fake**

`ITinNhanRepository.cs` — thêm vào cuối:

```csharp
    /// <summary>Tin Text (2 chiều) giữa 2 người dùng có NoiDungTinNhan chứa tuKhoa (không phân biệt hoa/thường), chưa thu hồi, tối đa 50 kết quả, mới nhất trước.</summary>
    Task<List<TinNhan>> TimKiemTheoNguoiDungAsync(string nguoiA, string nguoiB, string tuKhoa);

    /// <summary>Tin Text của 1 nhóm chứa tuKhoa, chưa thu hồi, tối đa 50 kết quả, mới nhất trước.</summary>
    Task<List<TinNhan>> TimKiemTheoNhomAsync(string nhomId, string tuKhoa);
```

`TinNhanRepository.cs` — thêm vào cuối class (nhớ thêm
`using System.Text.RegularExpressions;` ở đầu file nếu chưa có —
kiểm tra file trước, `DichVuTinNhan.cs` đã dùng `System.Text.RegularExpressions.Regex`
đầy đủ tên nên `TinNhanRepository.cs` có thể CHƯA có using ngắn gọn):

```csharp
    public async Task<List<TinNhan>> TimKiemTheoNguoiDungAsync(string nguoiA, string nguoiB, string tuKhoa)
    {
        var boLocCapDoi = Builders<TinNhan>.Filter.Or(
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiA),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiB)),
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiB),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiA)));
        var boLoc = Builders<TinNhan>.Filter.And(
            boLocCapDoi,
            Builders<TinNhan>.Filter.Eq(t => t.LoaiTinNhan, LoaiTinNhan.Text),
            Builders<TinNhan>.Filter.Eq(t => t.DaThuHoi, false),
            Builders<TinNhan>.Filter.Regex(t => t.NoiDungTinNhan, new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(tuKhoa), "i")));
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianTao).Limit(50).ToListAsync();
    }

    public async Task<List<TinNhan>> TimKiemTheoNhomAsync(string nhomId, string tuKhoa)
    {
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId),
            Builders<TinNhan>.Filter.Eq(t => t.LoaiTinNhan, LoaiTinNhan.Text),
            Builders<TinNhan>.Filter.Eq(t => t.DaThuHoi, false),
            Builders<TinNhan>.Filter.Regex(t => t.NoiDungTinNhan, new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(tuKhoa), "i")));
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianTao).Limit(50).ToListAsync();
    }
```

`backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs` — thêm vào cuối class:

```csharp
    public Task<List<TinNhan>> TimKiemTheoNguoiDungAsync(string nguoiA, string nguoiB, string tuKhoa)
    {
        var ketQua = DanhSach
            .Where(t => t.LoaiTinNhan == LoaiTinNhan.Text && !t.DaThuHoi)
            .Where(t => (t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) || (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA))
            .Where(t => t.NoiDungTinNhan.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.ThoiGianTao)
            .Take(50)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> TimKiemTheoNhomAsync(string nhomId, string tuKhoa)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId && t.LoaiTinNhan == LoaiTinNhan.Text && !t.DaThuHoi)
            .Where(t => t.NoiDungTinNhan.Contains(tuKhoa, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.ThoiGianTao)
            .Take(50)
            .ToList();
        return Task.FromResult(ketQua);
    }
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter TinNhanGiaLapTimKiemTests`
Expected: PASS (3/3).

- [ ] **Step 5: Viết test service (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`:

```csharp
    // --- Tìm tin nhắn (GĐ7b) ---

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_TuKhoaRong_TraVeRong()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan, "   ");

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_LoaiTruTinDaAn()
    {
        var (dichVu, _, khoNguoiDung, _, _, khoTinNhanAn) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tin = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Hẹn 5 giờ chiều", null, null, null, null, null);
        await khoTinNhanAn.AnAsync(IdNguoiNhan, tin.Id);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui, "hẹn");

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDungAsync_TinHopLe_TraVeDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Hẹn 5 giờ chiều", null, null, null, null, null);

        var ketQua = await dichVu.TimKiemTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan, "hẹn");

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task TimKiemTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe()
    {
        var (dichVu, _, _, _, khoNhom, _) = TaoDichVu();
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", ThanhVienIds = new List<string> { "thanh-vien-khac" } });

        await Assert.ThrowsAsync<KhongPhaiThanhVienNhomException>(() => dichVu.TimKiemTheoNhomAsync(IdNguoiGui, "n1", "hẹn"));
    }
```

- [ ] **Step 6: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "TimKiemTheo"`
Expected: FAIL biên dịch.

- [ ] **Step 7: Thêm method vào `IDichVuTinNhan`/`DichVuTinNhan`**

`IDichVuTinNhan.cs` — thêm vào cuối:

```csharp
    Task<List<TinNhanDto>> TimKiemTheoNguoiDungAsync(string idHienTai, string doiTacId, string tuKhoa);
    Task<List<TinNhanDto>> TimKiemTheoNhomAsync(string idHienTai, string nhomId, string tuKhoa);
```

`DichVuTinNhan.cs` — thêm vào cuối class (trước `private static TinNhanDto AnhXaDto`):

```csharp
    public async Task<List<TinNhanDto>> TimKiemTheoNguoiDungAsync(string idHienTai, string doiTacId, string tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return new List<TinNhanDto>();
        }

        var ketQua = await _khoTinNhan.TimKiemTheoNguoiDungAsync(idHienTai, doiTacId, tuKhoa.Trim());
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ketQua.Select(t => t.Id));
        return ketQua.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> TimKiemTheoNhomAsync(string idHienTai, string nhomId, string tuKhoa)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return new List<TinNhanDto>();
        }

        var ketQua = await _khoTinNhan.TimKiemTheoNhomAsync(nhomId, tuKhoa.Trim());
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ketQua.Select(t => t.Id));
        return ketQua.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }
```
Lưu ý: kiểm tra thành viên nhóm PHẢI chạy TRƯỚC khi kiểm tra `tuKhoa`
rỗng (test `TimKiemTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe` gửi
`tuKhoa = "hẹn"` không rỗng nên thứ tự không ảnh hưởng test này, nhưng
đặt kiểm tra quyền trước là đúng nguyên tắc bảo mật — không tiết lộ
hành vi khác nhau giữa "nhóm không tồn tại/không phải thành viên" và
"tuKhoa rỗng" cho người không có quyền).

- [ ] **Step 8: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "TimKiemTheo"`
Expected: PASS (4/4).

- [ ] **Step 9: Viết test controller (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`:

```csharp
    [Fact]
    public async Task TimKiemTheoNguoiDung_TraVeDungKetQua()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "timA", email = "timA@vi.du", matKhau = "MatKhau123!" });
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "timB", email = "timB@vi.du", matKhau = "MatKhau123!" });
        var dangNhapA = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "timA", matKhau = "MatKhau123!" });
        var tokenA = (await dangNhapA.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var dsNguoiDung = await (await _client.GetAsync("/api/nguoidung")).Content.ReadFromJsonAsync<List<NguoiDungTomTatDto>>();
        var idB = dsNguoiDung!.Single(nd => nd.TenTaiKhoan == "timB").Id;

        var phanHoi = await _client.GetAsync($"/api/tinnhan/nguoi-dung/{idB}/tim-kiem?tuKhoa=xyz");

        Assert.Equal(System.Net.HttpStatusCode.OK, phanHoi.StatusCode);
        var ketQua = await phanHoi.Content.ReadFromJsonAsync<List<TinNhanDto>>();
        Assert.Empty(ketQua!);
    }

    [Fact]
    public async Task TimKiemTheoNguoiDung_IdKhongPhaiObjectId_TraVe400()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "timC", email = "timC@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "timC", matKhau = "MatKhau123!" });
        var token = (await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nguoi-dung/khong-hop-le/tim-kiem?tuKhoa=xyz");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task TimKiemTheoNhom_KhongTonTai_TraVe404()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "timD", email = "timD@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "timD", matKhau = "MatKhau123!" });
        var token = (await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nhom/507f1f77bcf86cd799439099/tim-kiem?tuKhoa=xyz");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, phanHoi.StatusCode);
    }
```
Kiểm tra đầu file đã có `using HaloChat.Api.Dto;`/`using System.Linq;`
(cần cho `.Single(...)`) — nếu thiếu, thêm vào.

- [ ] **Step 10: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "TimKiemTheo"`
Expected: FAIL với 404 "route not found".

- [ ] **Step 11: Thêm 2 endpoint vào `TinNhanController.cs`**

Thêm vào cuối class:

```csharp
    [HttpGet("nguoi-dung/{id}/tim-kiem")]
    public async Task<IActionResult> TimKiemTheoNguoiDung(string id, [FromQuery] string tuKhoa)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id người dùng không hợp lệ." });
        }

        return Ok(await _dichVuTinNhan.TimKiemTheoNguoiDungAsync(IdHienTai, id, tuKhoa ?? string.Empty));
    }

    [HttpGet("nhom/{id}/tim-kiem")]
    public async Task<IActionResult> TimKiemTheoNhom(string id, [FromQuery] string tuKhoa)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id nhóm không hợp lệ." });
        }

        try
        {
            return Ok(await _dichVuTinNhan.TimKiemTheoNhomAsync(IdHienTai, id, tuKhoa ?? string.Empty));
        }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }
```

- [ ] **Step 12: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "TimKiemTheo"`
Expected: PASS (3/3).

- [ ] **Step 13: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: 0 lỗi build; toàn bộ test PASS.

- [ ] **Step 14: Commit**

```bash
git add backend/HaloChat.Api/Repositories/ITinNhanRepository.cs backend/HaloChat.Api/Repositories/TinNhanRepository.cs backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Controllers/TinNhanController.cs backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapTimKiemTests.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs backend/HaloChat.Api.Tests/TinNhanControllerTests.cs
git commit -m "feat(backend): endpoint tim tin nhan text trong cuoc tro chuyen (GD7b)"
```

---

## Task 2: Frontend — API + UI tìm kiếm/cuộn/nổi bật trong `KhungTinNhan.tsx`

**Files:**
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`
- Test: `frontend/src/DichVuApi.test.ts`

**Interfaces:**
- Consumes: JSON `TinNhan[]` từ Task 1.
- Produces: `PropsKhungTinNhan.onTimKiem: (tuKhoa: string) => Promise<TinNhan[]>`,
  `onNhayToiTinNhan: (id: string) => Promise<boolean>` — Task 3
  (TrangChat/TrangNhom) implement 2 hàm này.

- [ ] **Step 1: Viết test cho 2 hàm API mới (FAIL trước)**

Thêm vào `frontend/src/DichVuApi.test.ts`:

```typescript
it('TimKiemTinNhanTheoNguoiDung goi dung endpoint kem tuKhoa', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

  await TimKiemTinNhanTheoNguoiDung('token-gia-lap', 'doi-tac-1', 'xin chao');

  expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/tinnhan/nguoi-dung/doi-tac-1/tim-kiem?tuKhoa=xin+chao'), expect.anything());
});

it('TimKiemTinNhanTheoNhom goi dung endpoint kem tuKhoa', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

  await TimKiemTinNhanTheoNhom('token-gia-lap', 'nhom-1', 'xin chao');

  expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/tinnhan/nhom/nhom-1/tim-kiem?tuKhoa=xin+chao'), expect.anything());
});
```

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: FAIL.

- [ ] **Step 3: Thêm 2 hàm vào `DichVuApi.ts`**

```typescript
export async function TimKiemTinNhanTheoNguoiDung(token: string, doiTacId: string, tuKhoa: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${doiTacId}/tim-kiem?${new URLSearchParams({ tuKhoa })}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function TimKiemTinNhanTheoNhom(token: string, nhomId: string, tuKhoa: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}/tim-kiem?${new URLSearchParams({ tuKhoa })}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: PASS.

- [ ] **Step 5: Thêm icon kính lúp vào `BieuTuong.tsx`**

```tsx
export function BieuTuongTimKiem() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="11" cy="11" r="7" />
      <line x1="21" y1="21" x2="16.65" y2="16.65" />
    </svg>
  );
}
```

- [ ] **Step 6: Viết test cho UI tìm kiếm trong `KhungTinNhan.test.tsx` (FAIL trước)**

Thêm `onTimKiem: () => Promise.resolve([])` và `onNhayToiTinNhan: () => Promise.resolve(true)`
vào `PROPS_MAC_DINH`. Thêm test mới vào cuối `describe('KhungTinNhan', ...)`:

```tsx
it('bam icon kinh lup hien o tim kiem, go tu khoa goi onTimKiem', async () => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
  const onTimKiem = vi.fn().mockResolvedValue([]);
  render(<KhungTinNhan {...PROPS_MAC_DINH} onTimKiem={onTimKiem} />);

  await userEvent.setup({ delay: null }).click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
  await userEvent.setup({ delay: null }).type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin chao');
  vi.advanceTimersByTime(350);

  expect(onTimKiem).toHaveBeenCalledWith('xin chao');
  vi.useRealTimers();
});

it('bam 1 ket qua da co san trong danh sach thi cuon toi va noi bat, khong goi onNhayToiTinNhan', async () => {
  const tin = { ...TIN_NHAN_MAU, id: 'm1', noiDungTinNhan: 'Xin chao ban' };
  const onTimKiem = vi.fn().mockResolvedValue([tin]);
  const onNhayToiTinNhan = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} onTimKiem={onTimKiem} onNhayToiTinNhan={onNhayToiTinNhan} />);
  await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
  await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin');
  const ketQua = await screen.findByTestId('ket-qua-tim-m1');

  await userEvent.click(ketQua);

  expect(onNhayToiTinNhan).not.toHaveBeenCalled();
  expect(screen.queryByPlaceholderText('Tìm tin nhắn...')).not.toBeInTheDocument();
});

it('bam 1 ket qua CHUA co trong danh sach thi goi onNhayToiTinNhan', async () => {
  const tin = { ...TIN_NHAN_MAU, id: 'm-xa', noiDungTinNhan: 'Xin chao ban cu' };
  const onTimKiem = vi.fn().mockResolvedValue([tin]);
  const onNhayToiTinNhan = vi.fn().mockResolvedValue(true);
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[]} onTimKiem={onTimKiem} onNhayToiTinNhan={onNhayToiTinNhan} />);
  await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
  await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin');
  const ketQua = await screen.findByTestId('ket-qua-tim-m-xa');

  await userEvent.click(ketQua);

  expect(onNhayToiTinNhan).toHaveBeenCalledWith('m-xa');
});

it('khong tim thay tin sau khi tai het lich su thi hien thong bao', async () => {
  const tin = { ...TIN_NHAN_MAU, id: 'm-mat', noiDungTinNhan: 'Tin da mat' };
  const onTimKiem = vi.fn().mockResolvedValue([tin]);
  const onNhayToiTinNhan = vi.fn().mockResolvedValue(false);
  render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[]} onTimKiem={onTimKiem} onNhayToiTinNhan={onNhayToiTinNhan} />);
  await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
  await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin');
  const ketQua = await screen.findByTestId('ket-qua-tim-m-mat');

  await userEvent.click(ketQua);

  expect(await screen.findByText('Không tìm thấy tin nhắn này trong lịch sử.')).toBeInTheDocument();
});
```

- [ ] **Step 7: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: FAIL (UI tìm kiếm chưa tồn tại).

- [ ] **Step 8: Cập nhật `PropsKhungTinNhan` + import**

```tsx
import { BieuTuongGhim, BieuTuongTraLoi, BieuTuongTaiLieu, BieuTuongTai, BieuTuongBaCham, BieuTuongMatCuoi, BieuTuongKhoLuuTru, BieuTuongTimKiem } from './BieuTuong';
```
(Chỉ thêm `BieuTuongTimKiem` — `BieuTuongKhoLuuTru` chỉ có nếu GĐ7a đã
chạy trước; nếu GĐ7a CHƯA chạy, bỏ `BieuTuongKhoLuuTru` khỏi dòng import
này, không phải phạm vi task này.)

Thêm vào `PropsKhungTinNhan`:
```tsx
  onTimKiem: (tuKhoa: string) => Promise<TinNhan[]>;
  onNhayToiTinNhan: (id: string) => Promise<boolean>;
```

Thêm vào destructure props + state mới (cạnh `hienBangEmoji`):
```tsx
  const [hienOTimKiem, setHienOTimKiem] = useState(false);
  const [tuKhoaTim, setTuKhoaTim] = useState('');
  const [ketQuaTim, setKetQuaTim] = useState<TinNhan[]>([]);
  const [loiTim, setLoiTim] = useState<string | null>(null);
  const [idCanCuonToi, setIdCanCuonToi] = useState<string | null>(null);
  const [idDangNoiBat, setIdDangNoiBat] = useState<string | null>(null);
  const thamChieuBongBongRef = useRef<Map<string, HTMLDivElement>>(new Map());
  const bomTimKiemRef = useRef<ReturnType<typeof setTimeout> | null>(null);
```

- [ ] **Step 9: Thêm effect debounce tìm kiếm + effect cuộn/nổi bật**

Thêm sau effect `[tenHienThi]` reset trả lời hiện có:

```tsx
  useEffect(() => {
    if (!hienOTimKiem) return;
    if (bomTimKiemRef.current) clearTimeout(bomTimKiemRef.current);
    if (!tuKhoaTim.trim()) {
      setKetQuaTim([]);
      return;
    }
    bomTimKiemRef.current = setTimeout(() => {
      onTimKiem(tuKhoaTim.trim()).then(setKetQuaTim).catch(() => setKetQuaTim([]));
    }, 300);
    return () => {
      if (bomTimKiemRef.current) clearTimeout(bomTimKiemRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tuKhoaTim, hienOTimKiem]);

  useEffect(() => {
    if (!idCanCuonToi) return;
    const phanTu = thamChieuBongBongRef.current.get(idCanCuonToi);
    if (!phanTu) return;
    phanTu.scrollIntoView({ block: 'center' });
    setIdDangNoiBat(idCanCuonToi);
    setIdCanCuonToi(null);
    const bom = setTimeout(() => setIdDangNoiBat(null), 2000);
    return () => clearTimeout(bom);
  }, [idCanCuonToi, danhSachTinNhan]);
```

- [ ] **Step 10: Thêm hàm `moKetQuaTim`**

```tsx
  async function moKetQuaTim(tn: TinNhan) {
    const daCoSan = danhSachTinNhan.some((t) => t.id === tn.id);
    setHienOTimKiem(false);
    setTuKhoaTim('');
    setLoiTim(null);
    if (daCoSan) {
      setIdCanCuonToi(tn.id);
      return;
    }
    const timThay = await onNhayToiTinNhan(tn.id);
    if (timThay) {
      setIdCanCuonToi(tn.id);
    } else {
      setLoiTim('Không tìm thấy tin nhắn này trong lịch sử.');
    }
  }
```

- [ ] **Step 11: Thêm icon kính lúp + ô tìm kiếm/dropdown vào header**

Trong `<header className="khung-tin-nhan__tieu-de">`, thêm icon TRƯỚC
icon kho media (nếu có) hoặc trước `.mat-ket-noi` (nếu GĐ7a chưa chạy),
và thay toàn bộ phần hiện cụm tên bằng điều kiện `hienOTimKiem`:

```tsx
        {hienOTimKiem ? (
          <div className="khung-tin-nhan__o-tim-kiem-cum">
            <input
              autoFocus
              type="text"
              className="khung-tin-nhan__o-tim-kiem"
              placeholder="Tìm tin nhắn..."
              value={tuKhoaTim}
              onChange={(su) => setTuKhoaTim(su.target.value)}
            />
            {(ketQuaTim.length > 0 || loiTim) && (
              <div className="khung-tin-nhan__ket-qua-tim">
                {loiTim && <p className="khung-tin-nhan__loi-tim">{loiTim}</p>}
                {ketQuaTim.map((tn) => (
                  <button
                    key={tn.id}
                    data-testid={`ket-qua-tim-${tn.id}`}
                    className="khung-tin-nhan__dong-ket-qua-tim"
                    onClick={() => moKetQuaTim(tn)}
                  >
                    <span className="khung-tin-nhan__ket-qua-ten">{layTenNguoiGui ? layTenNguoiGui(tn.nguoiGuiId) : 'một người dùng'}</span>
                    <span className="khung-tin-nhan__ket-qua-noi-dung">{tn.noiDungTinNhan}</span>
                    <span className="khung-tin-nhan__ket-qua-gio">{dinhDangGio(tn.thoiGianTao)}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        ) : (
          onBamTieuDe ? (
            <button className="khung-tin-nhan__tieu-de-bam" onClick={onBamTieuDe}>
              <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
              <div className="khung-tin-nhan__ten-cum">
                <span className="khung-tin-nhan__ten">{tenHienThi}</span>
                {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
              </div>
            </button>
          ) : (
            <>
              <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
              <div className="khung-tin-nhan__ten-cum">
                <span className="khung-tin-nhan__ten">{tenHienThi}</span>
                {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
              </div>
            </>
          )
        )}
        <button
          className="khung-tin-nhan__nut-tim-kiem"
          onClick={() => setHienOTimKiem((truoc) => !truoc)}
          aria-label="Tìm tin nhắn"
        >
          <BieuTuongTimKiem />
        </button>
```

(Khối JSX trên THAY THẾ toàn bộ đoạn `{onBamTieuDe ? (...) : (...)}` hiện
có trong header — đọc kỹ file thật trước khi thay để giữ đúng nội dung
2 nhánh `onBamTieuDe`, chỉ bọc thêm điều kiện `hienOTimKiem` bên ngoài.)

- [ ] **Step 12: Gắn ref lưu bong bóng theo id + class nổi bật**

Trong JSX render mỗi tin nhắn, sửa dòng mở `.khung-tin-nhan__hang` (đã
có `className` động từ GĐ6a/6b) thêm `ref`:

```tsx
            <div
              key={tn.id}
              ref={(el) => {
                if (el) thamChieuBongBongRef.current.set(tn.id, el);
                else thamChieuBongBongRef.current.delete(tn.id);
              }}
              className={`khung-tin-nhan__hang${laCuaMinh ? ' khung-tin-nhan__hang--minh' : ''}${tinDangMoId === tn.id ? ' khung-tin-nhan__hang--mo' : ''}${idDangNoiBat === tn.id ? ' khung-tin-nhan__hang--noi-bat' : ''}`}
            >
```

- [ ] **Step 13: Thêm CSS**

Thêm vào cuối `KhungTinNhan.css`:

```css
.khung-tin-nhan__nut-tim-kiem {
  flex-shrink: 0;
  border: none;
  background: none;
  color: var(--mau-chu-phu);
  cursor: pointer;
  padding: 6px;
  border-radius: 8px;
  margin-left: auto;
}

.khung-tin-nhan__nut-tim-kiem:hover {
  background: var(--mau-nen-tren);
}

.khung-tin-nhan__o-tim-kiem-cum {
  position: relative;
  flex: 1;
  min-width: 0;
}

.khung-tin-nhan__o-tim-kiem {
  width: 100%;
  padding: 8px 16px;
  border: 1px solid var(--mau-vien);
  border-radius: 999px;
  font-family: inherit;
  font-size: 14px;
  background: var(--mau-nen-tren);
  color: var(--mau-chu-dam);
  box-sizing: border-box;
}

.khung-tin-nhan__ket-qua-tim {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  z-index: 6;
  max-height: 260px;
  overflow-y: auto;
  background: var(--mau-nen-the);
  border: 1px solid var(--mau-vien);
  border-radius: 12px;
  box-shadow: var(--bong-the);
  padding: 6px;
}

.khung-tin-nhan__loi-tim {
  margin: 4px 8px;
  font-size: 13px;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__dong-ket-qua-tim {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  border: none;
  background: none;
  text-align: left;
  padding: 8px;
  border-radius: 8px;
  cursor: pointer;
  font-family: inherit;
}

.khung-tin-nhan__dong-ket-qua-tim:hover {
  background: var(--mau-nen-tren);
}

.khung-tin-nhan__ket-qua-ten {
  flex-shrink: 0;
  font-weight: 700;
  font-size: 13px;
}

.khung-tin-nhan__ket-qua-noi-dung {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__ket-qua-gio {
  flex-shrink: 0;
  font-size: 11px;
  color: var(--mau-chu-phu);
}

.khung-tin-nhan__hang--noi-bat .khung-tin-nhan__bong {
  outline: 2px solid var(--mau-chinh-dam);
  outline-offset: 2px;
  transition: outline-color 2s ease;
}
```

- [ ] **Step 14: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: PASS toàn bộ.

- [ ] **Step 15: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: có thể còn lỗi ở `TrangChat.tsx`/`TrangNhom.tsx` (thiếu 2 prop
mới `onTimKiem`/`onNhayToiTinNhan` bắt buộc) — đây LÀ phạm vi Task 3,
không sửa ở đây. Xác nhận KHÔNG còn lỗi bên trong
`KhungTinNhan.tsx`/`.test.tsx`/`BieuTuong.tsx`/`DichVuApi.ts`/`.test.ts`.

- [ ] **Step 16: Commit**

```bash
git add frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts frontend/src/ThanhPhan/BieuTuong.tsx frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "feat(frontend): UI tim tin nhan + cuon-noi-bat ket qua trong KhungTinNhan (GD7b)"
```

---

## Task 3: Frontend — nối dây `TrangChat.tsx`/`TrangNhom.tsx`

**Files:**
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `TimKiemTinNhanTheoNguoiDung`/`TheoNhom` (Task 2 backend
  JSON shape), `PropsKhungTinNhan.onTimKiem`/`onNhayToiTinNhan` (Task 2).
- Produces: không có task nào phụ thuộc (task cuối GĐ7b).

- [ ] **Step 1: Thêm `timKiemTinNhan`/`nhayToiTinNhan` vào `TrangChat.tsx`**

Thêm import `TimKiemTinNhanTheoNguoiDung` vào dòng import từ
`'../DichVuApi'`. Thêm 2 hàm (cạnh `taiThemLichSuCu`):

```typescript
  function timKiemTinNhan(tuKhoa: string): Promise<TinNhan[]> {
    if (!token || !nguoiDangChon) return Promise.resolve([]);
    return TimKiemTinNhanTheoNguoiDung(token, nguoiDangChon.id, tuKhoa);
  }

  async function nhayToiTinNhan(id: string): Promise<boolean> {
    if (!token || !nguoiDangChon) return false;
    let dsHienTai = tinNhanTheoNguoiDung[nguoiDangChon.id] ?? [];
    if (dsHienTai.some((tn) => tn.id === id)) return true;

    for (let lan = 0; lan < 20; lan++) {
      const cuNhat = dsHienTai[0];
      if (!cuNhat) return false;

      setDangTaiLichSu(true);
      let cuHon: TinNhanHienThi[];
      try {
        cuHon = await LayLichSuTinNhan(token, nguoiDangChon.id, cuNhat.id, SO_LUONG_LICH_SU_THEM);
      } catch {
        setLoi('Không tải được tin nhắn cũ hơn.');
        return false;
      } finally {
        setDangTaiLichSu(false);
      }

      if (cuHon.length < SO_LUONG_LICH_SU_THEM) {
        setConThemLichSu((truoc) => ({ ...truoc, [nguoiDangChon.id]: false }));
      }
      const thuTuThoiGian = [...cuHon].reverse();
      dsHienTai = [...thuTuThoiGian, ...dsHienTai];
      setTinNhanTheoNguoiDung((truoc) => ({ ...truoc, [nguoiDangChon.id]: dsHienTai }));

      if (cuHon.some((tn) => tn.id === id)) return true;
      if (cuHon.length === 0) return false;
    }
    return false;
  }
```
(Lưu ý chấp nhận được: nếu có tin nhắn mới đến qua SignalR đúng lúc
vòng lặp này đang chạy, lần `setTinNhanTheoNguoiDung` cuối cùng của vòng
lặp có thể ghi đè tin mới đó — race hiếm, chấp nhận được cho tính năng
tìm kiếm, không cần xử lý thêm.)

Truyền `onTimKiem={timKiemTinNhan}` và `onNhayToiTinNhan={nhayToiTinNhan}`
cho `<KhungTinNhan>`.

- [ ] **Step 2: Thêm tương tự vào `TrangNhom.tsx`**

Import `TimKiemTinNhanTheoNhom`. Thêm 2 hàm:

```typescript
  function timKiemTinNhan(tuKhoa: string): Promise<TinNhan[]> {
    if (!token || !nhomDangChon) return Promise.resolve([]);
    return TimKiemTinNhanTheoNhom(token, nhomDangChon.id, tuKhoa);
  }

  async function nhayToiTinNhan(id: string): Promise<boolean> {
    if (!token || !nhomDangChon) return false;
    let dsHienTai = tinNhanTheoNhom[nhomDangChon.id] ?? [];
    if (dsHienTai.some((tn) => tn.id === id)) return true;

    for (let lan = 0; lan < 20; lan++) {
      const cuNhat = dsHienTai[0];
      if (!cuNhat) return false;

      setDangTaiLichSu(true);
      let cuHon: TinNhanHienThi[];
      try {
        cuHon = await LayLichSuNhom(token, nhomDangChon.id, cuNhat.id, SO_LUONG_LICH_SU_THEM);
      } catch {
        setLoi('Không tải được tin nhắn cũ hơn.');
        return false;
      } finally {
        setDangTaiLichSu(false);
      }

      if (cuHon.length < SO_LUONG_LICH_SU_THEM) {
        setConThemLichSu((truoc) => ({ ...truoc, [nhomDangChon.id]: false }));
      }
      const thuTu = [...cuHon].reverse();
      dsHienTai = [...thuTu, ...dsHienTai];
      setTinNhanTheoNhom((truoc) => ({ ...truoc, [nhomDangChon.id]: dsHienTai }));

      if (cuHon.some((tn) => tn.id === id)) return true;
      if (cuHon.length === 0) return false;
    }
    return false;
  }
```
Truyền `onTimKiem={timKiemTinNhan}`/`onNhayToiTinNhan={nhayToiTinNhan}`
cho `<KhungTinNhan>`.

- [ ] **Step 3: Viết test cho `TrangChat.test.tsx`**

Thêm vào cuối `describe('TrangChat', ...)`:

```typescript
it('go tim kiem tin nhan goi dung TimKiemTinNhanTheoNguoiDung', async () => {
  vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
  vi.spyOn(DichVuApi, 'TimKiemTinNhanTheoNguoiDung').mockResolvedValue([]);

  renderTrangChat();
  await userEvent.click(await screen.findByText('TranBinh'));
  await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
  await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin chao');

  await waitFor(() => expect(DichVuApi.TimKiemTinNhanTheoNguoiDung).toHaveBeenCalledWith('token-gia-lap', '2', 'xin chao'));
});

it('bam ket qua tim kiem chua tai ve thi tu dong tai them lich su toi khi thay', async () => {
  const tinCu = taoTinNhanGiaLap({ id: 'm-cu-nhat', noiDungTinNhan: 'Tin dau tien' });
  vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValueOnce([tinCu]);
  const tinXa = taoTinNhanGiaLap({ id: 'm-xa-nhat', noiDungTinNhan: 'Xin chao rat xa' });
  vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValueOnce([tinXa]);
  vi.spyOn(DichVuApi, 'TimKiemTinNhanTheoNguoiDung').mockResolvedValue([tinXa]);

  renderTrangChat();
  await userEvent.click(await screen.findByText('TranBinh'));
  await screen.findByText('Tin dau tien');
  await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
  await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin chao');
  const ketQua = await screen.findByTestId('ket-qua-tim-m-xa-nhat');

  await userEvent.click(ketQua);

  expect(await screen.findAllByText('Xin chao rat xa')).not.toHaveLength(0);
});
```
Kiểm tra `taoTinNhanGiaLap` (factory sẵn có trong file) chấp nhận
`id`/`noiDungTinNhan` qua tham số `Partial` — đã xác nhận đúng ở GĐ6a.

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run TrangChat`
Expected: PASS toàn bộ.

- [ ] **Step 5: Lặp lại Step 3-4 cho `TrangNhom.test.tsx`**

Áp dụng logic tương tự, thay `TimKiemTinNhanTheoNguoiDung` →
`TimKiemTinNhanTheoNhom`, `LayLichSuTinNhan` → `LayLichSuNhom`, `'TranBinh'`
→ tên nhóm mẫu trong file (`'Nhóm CNTT'`), theo đúng pattern render/mock
đã dùng trong các test khác của file này.

- [ ] **Step 6: `tsc -b --noEmit` + toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.test.tsx
git commit -m "feat(frontend): noi day tim tin nhan + tu dong tai lich su toi khi thay vao TrangChat/TrangNhom (GD7b)"
```

---

## Ghi chú cho reviewer / executor

- Task 1 độc lập. Task 2 phụ thuộc Task 1 chỉ qua JSON shape (test dùng
  mock). Task 3 phụ thuộc Task 2 (props `onTimKiem`/`onNhayToiTinNhan`).
- Nếu GĐ7a ĐÃ chạy trước plan này, `KhungTinNhan.tsx` sẽ đã có icon kho
  media và import `BieuTuongKhoLuuTru` — Task 2 Step 8/11 của plan này
  chỉ THÊM icon tìm kiếm cạnh icon đã có, không xóa/thay icon kho media.
  Nếu GĐ7a CHƯA chạy, bỏ qua mọi tham chiếu tới icon kho media trong
  Step 11, chỉ thêm đúng icon tìm kiếm.
