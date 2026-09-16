# GĐ7a — Kho Media & Tệp — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thêm icon folder trên header `KhungTinNhan` mở modal "Kho lưu
trữ Media & Tệp" liệt kê Ảnh/Tài liệu của đúng cuộc trò chuyện đang mở
(1-1 và nhóm), chia 2 tab, bấm ảnh mở lightbox, bấm tệp tải xuống.

**Architecture:** Backend thêm 2 hàm repo/service liệt kê tin
Anh/File của 1 cuộc trò chuyện (lọc bỏ tin thu hồi/ẩn cục bộ, tái dùng
`ITinNhanAnRepository` đã có từ GĐ6b) + 2 endpoint GET. Frontend thêm
component modal độc lập `PanelKhoMedia.tsx`, `KhungTinNhan` chỉ có prop
mở modal, `TrangChat.tsx`/`TrangNhom.tsx` quản lý state/gọi API.

**Tech Stack:** ASP.NET Core .NET 9 (backend/HaloChat.Api) + MongoDB
Driver, xUnit; React 19 + TypeScript + Vite (frontend/), Vitest.

**Spec:** `docs/superpowers/specs/2026-09-17-halochat-kho-media-va-tep.md`

## Global Constraints

- Đặt tên định danh không dấu tiếng Việt.
- Áp dụng cho CẢ chat 1-1 lẫn chat nhóm.
- Loại trừ tin đã thu hồi (`DaThuHoi == true`) và tin đã bị người đang
  xem ẩn cục bộ (`ITinNhanAnRepository`) khỏi kết quả — không ngoại lệ.
- Tải 1 lần toàn bộ danh sách khi mở modal, không phân trang.
- Ảnh → lightbox phóng to; Tệp → tải xuống ngay (dùng URL file có sẵn).
- `frontend/tsconfig.json` là solution-style — LUÔN dùng
  `npx tsc -b --noEmit` để kiểm tra type, KHÔNG dùng `tsc --noEmit`
  thường (false negative hoàn toàn, đã xác nhận ở GĐ6b).

---

## Task 1: Backend — repository, service, controller

**Files:**
- Modify: `backend/HaloChat.Api/Repositories/ITinNhanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/TinNhanRepository.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Controllers/TinNhanController.cs`
- Test: `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapMediaTests.cs`
- Test: `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`
- Test: `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs`

**Interfaces:**
- Consumes: `ITinNhanAnRepository.LayDanhSachIdDaAnAsync` (đã có từ GĐ6b).
- Produces: `IDichVuTinNhan.LayMediaTheoNguoiDungAsync(idHienTai, doiTacId): Task<List<TinNhanDto>>`,
  `LayMediaTheoNhomAsync(idHienTai, nhomId): Task<List<TinNhanDto>>`;
  endpoint `GET /api/tinnhan/nguoi-dung/{id}/media`, `GET /api/tinnhan/nhom/{id}/media`.

- [ ] **Step 1: Viết test repo mới (FAIL trước)**

Tạo `backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapMediaTests.cs`:

```csharp
using HaloChat.Api.Models;
using HaloChat.Api.Tests.Fakes;
using Xunit;

namespace HaloChat.Api.Tests.Repositories;

public class TinNhanGiaLapMediaTests
{
    [Fact]
    public async Task LayMediaTheoNguoiDungAsync_ChiTraVeAnhVaFile_LoaiTruText()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Anh });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.File });
        kho.DanhSach.Add(new TinNhan { NguoiGuiId = "a", NguoiNhanId = "b", LoaiTinNhan = LoaiTinNhan.Text });

        var ketQua = await kho.LayMediaTheoNguoiDungAsync("a", "b");

        Assert.Equal(2, ketQua.Count);
    }

    [Fact]
    public async Task LayMediaTheoNhomAsync_ChiTraVeAnhVaFileCuaDungNhom()
    {
        var kho = new TinNhanGiaLap();
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", LoaiTinNhan = LoaiTinNhan.Anh });
        kho.DanhSach.Add(new TinNhan { NhomId = "n1", LoaiTinNhan = LoaiTinNhan.Text });
        kho.DanhSach.Add(new TinNhan { NhomId = "n2", LoaiTinNhan = LoaiTinNhan.File });

        var ketQua = await kho.LayMediaTheoNhomAsync("n1");

        Assert.Single(ketQua);
    }
}
```

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter TinNhanGiaLapMediaTests`
Expected: FAIL biên dịch (method chưa tồn tại).

- [ ] **Step 3: Thêm method vào `ITinNhanRepository`/`TinNhanRepository`/fake**

`ITinNhanRepository.cs` — thêm vào cuối interface:

```csharp
    /// <summary>Toàn bộ tin Anh/File (2 chiều) giữa 2 người dùng, mới nhất trước.</summary>
    Task<List<TinNhan>> LayMediaTheoNguoiDungAsync(string nguoiA, string nguoiB);

    /// <summary>Toàn bộ tin Anh/File của 1 nhóm, mới nhất trước.</summary>
    Task<List<TinNhan>> LayMediaTheoNhomAsync(string nhomId);
```

`TinNhanRepository.cs` — thêm vào cuối class (biến collection là `_collection`):

```csharp
    public async Task<List<TinNhan>> LayMediaTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var boLocLoai = Builders<TinNhan>.Filter.Or(
            Builders<TinNhan>.Filter.Eq(t => t.LoaiTinNhan, LoaiTinNhan.Anh),
            Builders<TinNhan>.Filter.Eq(t => t.LoaiTinNhan, LoaiTinNhan.File));
        var boLocCapDoi = Builders<TinNhan>.Filter.Or(
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiA),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiB)),
            Builders<TinNhan>.Filter.And(
                Builders<TinNhan>.Filter.Eq(t => t.NguoiGuiId, nguoiB),
                Builders<TinNhan>.Filter.Eq(t => t.NguoiNhanId, nguoiA)));
        var boLoc = Builders<TinNhan>.Filter.And(boLocLoai, boLocCapDoi);
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianTao).ToListAsync();
    }

    public async Task<List<TinNhan>> LayMediaTheoNhomAsync(string nhomId)
    {
        var boLocLoai = Builders<TinNhan>.Filter.Or(
            Builders<TinNhan>.Filter.Eq(t => t.LoaiTinNhan, LoaiTinNhan.Anh),
            Builders<TinNhan>.Filter.Eq(t => t.LoaiTinNhan, LoaiTinNhan.File));
        var boLoc = Builders<TinNhan>.Filter.And(
            Builders<TinNhan>.Filter.Eq(t => t.NhomId, nhomId), boLocLoai);
        return await _collection.Find(boLoc).SortByDescending(t => t.ThoiGianTao).ToListAsync();
    }
```

`backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs` — thêm vào cuối class:

```csharp
    public Task<List<TinNhan>> LayMediaTheoNguoiDungAsync(string nguoiA, string nguoiB)
    {
        var ketQua = DanhSach
            .Where(t => t.LoaiTinNhan == LoaiTinNhan.Anh || t.LoaiTinNhan == LoaiTinNhan.File)
            .Where(t => (t.NguoiGuiId == nguoiA && t.NguoiNhanId == nguoiB) || (t.NguoiGuiId == nguoiB && t.NguoiNhanId == nguoiA))
            .OrderByDescending(t => t.ThoiGianTao)
            .ToList();
        return Task.FromResult(ketQua);
    }

    public Task<List<TinNhan>> LayMediaTheoNhomAsync(string nhomId)
    {
        var ketQua = DanhSach
            .Where(t => t.NhomId == nhomId && (t.LoaiTinNhan == LoaiTinNhan.Anh || t.LoaiTinNhan == LoaiTinNhan.File))
            .OrderByDescending(t => t.ThoiGianTao)
            .ToList();
        return Task.FromResult(ketQua);
    }
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter TinNhanGiaLapMediaTests`
Expected: PASS (2/2).

- [ ] **Step 5: Viết test service (FAIL trước)**

Thêm vào cuối `backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs`
(trước dấu `}` cuối class — nhớ dùng đúng chữ ký `TaoDichVu()` hiện tại
trả về 6 giá trị `(dichVu, khoTinNhan, khoNguoiDung, khoLoiMoiKetBan, khoNhom, khoTinNhanAn)`,
xem file để lấy đúng tên các biến destructure):

```csharp
    // --- Kho Media & Tệp (GĐ7a) ---

    [Fact]
    public async Task LayMediaTheoNguoiDungAsync_LocTinDaThuHoiVaDaAn()
    {
        var (dichVu, khoTinNhan, khoNguoiDung, _, _, khoTinNhanAn) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        var tinAnh1 = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439001", "a.png", 1024, "image/png", null);
        var tinAnh2 = await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439002", "b.png", 1024, "image/png", null);
        await dichVu.ThuHoiAsync(IdNguoiGui, tinAnh1.Id);
        await khoTinNhanAn.AnAsync(IdNguoiNhan, tinAnh2.Id);

        var ketQua = await dichVu.LayMediaTheoNguoiDungAsync(IdNguoiNhan, IdNguoiGui);

        Assert.Empty(ketQua);
    }

    [Fact]
    public async Task LayMediaTheoNguoiDungAsync_TinHopLe_TraVeDung()
    {
        var (dichVu, _, khoNguoiDung, _, _, _) = TaoDichVu();
        khoNguoiDung.DanhSach.Add(new NguoiDung { Id = IdNguoiGui, TenTaiKhoan = "NguoiGui", ChoPhepTinNhanTuNguoiLa = true });
        khoNguoiDung.DanhSach.Add(TaoNguoiNhanChoPhepNguoiLa());
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Anh", "", "/api/tinnhan/file/507f1f77bcf86cd799439003", "a.png", 1024, "image/png", null);
        await dichVu.GuiTinNhanAsync(IdNguoiGui, IdNguoiNhan, null, "Text", "Xin chào", null, null, null, null, null);

        var ketQua = await dichVu.LayMediaTheoNguoiDungAsync(IdNguoiGui, IdNguoiNhan);

        Assert.Single(ketQua);
    }

    [Fact]
    public async Task LayMediaTheoNhomAsync_KhongPhaiThanhVien_NemNgoaiLe()
    {
        var (dichVu, _, khoNguoiDung, _, khoNhom, _) = TaoDichVu();
        khoNhom.DanhSach.Add(new Nhom { Id = "n1", ThanhVienIds = new List<string> { "thanh-vien-khac" } });

        await Assert.ThrowsAsync<KhongPhaiThanhVienNhomException>(() => dichVu.LayMediaTheoNhomAsync(IdNguoiGui, "n1"));
    }
```

- [ ] **Step 6: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "LayMediaTheo"`
Expected: FAIL biên dịch (method chưa tồn tại trên `IDichVuTinNhan`).

- [ ] **Step 7: Thêm method vào `IDichVuTinNhan`/`DichVuTinNhan`**

`IDichVuTinNhan.cs` — thêm vào cuối interface:

```csharp
    Task<List<TinNhanDto>> LayMediaTheoNguoiDungAsync(string idHienTai, string doiTacId);
    Task<List<TinNhanDto>> LayMediaTheoNhomAsync(string idHienTai, string nhomId);
```

`DichVuTinNhan.cs` — thêm vào cuối class (trước dấu `}` cuối cùng,
NGAY TRƯỚC method `private static TinNhanDto AnhXaDto`):

```csharp
    public async Task<List<TinNhanDto>> LayMediaTheoNguoiDungAsync(string idHienTai, string doiTacId)
    {
        var media = await _khoTinNhan.LayMediaTheoNguoiDungAsync(idHienTai, doiTacId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, media.Select(t => t.Id));
        return media.Where(t => !t.DaThuHoi && !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> LayMediaTheoNhomAsync(string idHienTai, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var media = await _khoTinNhan.LayMediaTheoNhomAsync(nhomId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, media.Select(t => t.Id));
        return media.Where(t => !t.DaThuHoi && !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }
```

- [ ] **Step 8: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "LayMediaTheo"`
Expected: PASS (3/3).

- [ ] **Step 9: Viết test controller (FAIL trước)**

Đọc phần đầu `backend/HaloChat.Api.Tests/TinNhanControllerTests.cs` để
dùng đúng pattern đăng ký + đăng nhập + set header đã có, thêm test mới
vào cuối class:

```csharp
    [Fact]
    public async Task LayMediaTheoNguoiDung_IdKhongPhaiObjectId_TraVe400()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "mediaA", email = "mediaA@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "mediaA", matKhau = "MatKhau123!" });
        var token = (await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nguoi-dung/khong-hop-le/media");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, phanHoi.StatusCode);
    }

    [Fact]
    public async Task LayMediaTheoNhom_KhongTonTai_TraVe404()
    {
        await _client.PostAsJsonAsync("/api/nguoidung/dang-ky", new { tenTaiKhoan = "mediaB", email = "mediaB@vi.du", matKhau = "MatKhau123!" });
        var dangNhap = await _client.PostAsJsonAsync("/api/nguoidung/dang-nhap", new { tenDangNhap = "mediaB", matKhau = "MatKhau123!" });
        var token = (await dangNhap.Content.ReadFromJsonAsync<DangNhapResponse>())!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var phanHoi = await _client.GetAsync("/api/tinnhan/nhom/507f1f77bcf86cd799439099/media");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, phanHoi.StatusCode);
    }
```

- [ ] **Step 10: Chạy test để thấy FAIL**

Run: `cd backend && dotnet test --filter "LayMediaTheo"`
Expected: FAIL với 404 "route not found" (endpoint chưa tồn tại).

- [ ] **Step 11: Thêm 2 endpoint vào `TinNhanController.cs`**

Thêm vào cuối class (trước dấu `}` cuối file):

```csharp
    [HttpGet("nguoi-dung/{id}/media")]
    public async Task<IActionResult> LayMediaTheoNguoiDung(string id)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest(new { thongBao = "Id người dùng không hợp lệ." });
        }

        return Ok(await _dichVuTinNhan.LayMediaTheoNguoiDungAsync(IdHienTai, id));
    }

    [HttpGet("nhom/{id}/media")]
    public async Task<IActionResult> LayMediaTheoNhom(string id)
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
            return Ok(await _dichVuTinNhan.LayMediaTheoNhomAsync(IdHienTai, id));
        }
        catch (NhomKhongTonTaiException loi) { return NotFound(new { thongBao = loi.Message }); }
        catch (KhongPhaiThanhVienNhomException loi) { return StatusCode(403, new { thongBao = loi.Message }); }
    }
```

- [ ] **Step 12: Chạy test để thấy PASS**

Run: `cd backend && dotnet test --filter "LayMediaTheo"`
Expected: PASS (2/2).

- [ ] **Step 13: Build + chạy toàn bộ test backend**

Run: `cd backend && dotnet build && dotnet test`
Expected: 0 lỗi build; toàn bộ test PASS.

- [ ] **Step 14: Commit**

```bash
git add backend/HaloChat.Api/Repositories/ITinNhanRepository.cs backend/HaloChat.Api/Repositories/TinNhanRepository.cs backend/HaloChat.Api.Tests/Fakes/TinNhanGiaLap.cs backend/HaloChat.Api/Services/IDichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Controllers/TinNhanController.cs backend/HaloChat.Api.Tests/Repositories/TinNhanGiaLapMediaTests.cs backend/HaloChat.Api.Tests/Services/DichVuTinNhanTests.cs backend/HaloChat.Api.Tests/TinNhanControllerTests.cs
git commit -m "feat(backend): endpoint lay media (anh/tep) theo cuoc tro chuyen (GD7a)"
```

---

## Task 2: Frontend — component `PanelKhoMedia.tsx` + icon mới

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts` (không cần đổi — `TinNhan` đã đủ field)
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/ThanhPhan/BieuTuong.tsx`
- Create: `frontend/src/Trang/PanelKhoMedia.tsx`
- Create: `frontend/src/Trang/PanelKhoMedia.css`
- Test: `frontend/src/DichVuApi.test.ts`
- Test: `frontend/src/Trang/PanelKhoMedia.test.tsx`

**Interfaces:**
- Consumes: JSON `TinNhan[]` từ Task 1 (field `loaiTinNhan`, `duongDanFile`, `tenFileGoc`, `kichThuocFile`).
- Produces: `PanelKhoMedia` props `{ danhSachMedia: TinNhan[]; tenCuocTroChuyen: string; onDong: () => void }` — Task 3 (TrangChat/TrangNhom) dựng và render component này.

- [ ] **Step 1: Viết test cho 2 hàm API mới (FAIL trước)**

Thêm vào `frontend/src/DichVuApi.test.ts` (dùng pattern
`new Response(JSON.stringify(...))` đã xác nhận đúng ở GĐ5f/GĐ6a):

```typescript
it('LayMediaTheoNguoiDung goi dung endpoint GET', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

  await LayMediaTheoNguoiDung('token-gia-lap', 'doi-tac-1');

  expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/tinnhan/nguoi-dung/doi-tac-1/media'), expect.anything());
});

it('LayMediaTheoNhom goi dung endpoint GET', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

  await LayMediaTheoNhom('token-gia-lap', 'nhom-1');

  expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/tinnhan/nhom/nhom-1/media'), expect.anything());
});
```

- [ ] **Step 2: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: FAIL (hàm chưa tồn tại).

- [ ] **Step 3: Thêm 2 hàm vào `DichVuApi.ts`**

Thêm vào cuối file:

```typescript
export async function LayMediaTheoNguoiDung(token: string, doiTacId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${doiTacId}/media`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayMediaTheoNhom(token: string, nhomId: string): Promise<TinNhan[]> {
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}/media`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

- [ ] **Step 4: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run DichVuApi`
Expected: PASS.

- [ ] **Step 5: Thêm 3 icon mới vào `BieuTuong.tsx`**

Thêm vào cuối file:

```tsx
export function BieuTuongKhoLuuTru() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z" />
    </svg>
  );
}

export function BieuTuongAnhMo() {
  return (
    <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
      <rect x="3" y="3" width="18" height="18" rx="2" />
      <circle cx="8.5" cy="8.5" r="1.5" />
      <path d="m21 15-5-5L5 21" />
    </svg>
  );
}

export function BieuTuongDong() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  );
}
```

- [ ] **Step 6: Viết test cho `PanelKhoMedia.tsx` (FAIL trước)**

Tạo `frontend/src/Trang/PanelKhoMedia.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { PanelKhoMedia } from './PanelKhoMedia';
import type { TinNhan } from '../KieuDuLieu';

const TIN_ANH: TinNhan = {
  id: 'm1', nguoiGuiId: '1', nguoiNhanId: '2', nhomId: null, loaiTinNhan: 'Anh',
  noiDungTinNhan: '', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439001', tenFileGoc: 'a.png',
  kichThuocFile: 1024, loaiFile: 'image/png', daDoc: true, daNhan: true, thoiGianTao: '2026-01-01T00:00:00Z',
  traLoi: null, daThuHoi: false, daGhim: false, thoiGianGhim: null,
};

const TIN_FILE: TinNhan = {
  ...TIN_ANH, id: 'm2', loaiTinNhan: 'File', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439002',
  tenFileGoc: 'bao-cao.docx', loaiFile: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
};

describe('PanelKhoMedia', () => {
  it('hien dung so luong tung tab va noi dung tab Hinh anh mac dinh', () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_ANH, TIN_FILE]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    expect(screen.getByText('Hình ảnh (1)')).toBeInTheDocument();
    expect(screen.getByText('Tài liệu & Tệp (1)')).toBeInTheDocument();
    expect(screen.getByAltText('a.png')).toBeInTheDocument();
  });

  it('doi sang tab Tai lieu hien dung tep, khong hien anh', async () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_ANH, TIN_FILE]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    await userEvent.click(screen.getByText('Tài liệu & Tệp (1)'));

    expect(screen.getByText('bao-cao.docx')).toBeInTheDocument();
    expect(screen.queryByAltText('a.png')).not.toBeInTheDocument();
  });

  it('tab rong hien dung thong bao rieng cho tung tab', async () => {
    render(<PanelKhoMedia danhSachMedia={[]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    expect(screen.getByText('Chưa có hình ảnh nào được chia sẻ trong đoạn chat này')).toBeInTheDocument();

    await userEvent.click(screen.getByText('Tài liệu & Tệp (0)'));

    expect(screen.getByText('Chưa có tài liệu nào được chia sẻ trong đoạn chat này')).toBeInTheDocument();
  });

  it('bam anh mo lightbox, bam dong lightbox tra ve panel', async () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_ANH]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    await userEvent.click(screen.getByAltText('a.png'));

    expect(screen.getByRole('img', { name: 'Xem ảnh lớn a.png' })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Đóng ảnh lớn' }));

    expect(screen.queryByRole('img', { name: 'Xem ảnh lớn a.png' })).not.toBeInTheDocument();
  });

  it('bam nut dong goi onDong', async () => {
    const onDong = vi.fn();
    render(<PanelKhoMedia danhSachMedia={[]} tenCuocTroChuyen="TranBinh" onDong={onDong} />);

    await userEvent.click(screen.getByRole('button', { name: 'Đóng' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });

  it('tep hien duoi dang link tai xuong dung duong dan', async () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_FILE]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);
    await userEvent.click(screen.getByText('Tài liệu & Tệp (1)'));

    const lienKet = screen.getByRole('link', { name: /bao-cao.docx/ });

    expect(lienKet).toHaveAttribute('href', expect.stringContaining('/api/tinnhan/file/507f1f77bcf86cd799439002'));
  });
});
```

- [ ] **Step 7: Chạy test để thấy FAIL**

Run: `cd frontend && npm test -- --run PanelKhoMedia`
Expected: FAIL (file `PanelKhoMedia.tsx` chưa tồn tại).

- [ ] **Step 8: Viết `PanelKhoMedia.tsx`**

Cần biết `DIA_CHI_GOC` (base URL backend) — export sẵn từ `DichVuApi.ts`
(đã dùng ở `KhungTinNhan.tsx`). Tạo file:

```tsx
import { useState } from 'react';
import { BieuTuongKhoLuuTru, BieuTuongAnhMo, BieuTuongDong, BieuTuongTaiLieu, BieuTuongTai } from '../ThanhPhan/BieuTuong';
import { DIA_CHI_GOC } from '../DichVuApi';
import type { TinNhan } from '../KieuDuLieu';
import './PanelKhoMedia.css';

interface PropsPanelKhoMedia {
  danhSachMedia: TinNhan[];
  tenCuocTroChuyen: string;
  onDong: () => void;
}

function dinhDangKichThuoc(bytes: number): string {
  const mb = bytes / (1024 * 1024);
  return mb >= 1 ? `${mb.toFixed(1)}MB` : `${Math.ceil(bytes / 1024)}KB`;
}

export function PanelKhoMedia({ danhSachMedia, tenCuocTroChuyen, onDong }: PropsPanelKhoMedia) {
  const [tabDangChon, setTabDangChon] = useState<'anh' | 'tep'>('anh');
  const [anhDangXemToId, setAnhDangXemToId] = useState<string | null>(null);

  const danhSachAnh = danhSachMedia.filter((tn) => tn.loaiTinNhan === 'Anh');
  const danhSachTep = danhSachMedia.filter((tn) => tn.loaiTinNhan === 'File');
  const anhDangXemTo = danhSachAnh.find((tn) => tn.id === anhDangXemToId) ?? null;

  return (
    <div className="panel-kho-media-nen" onClick={onDong}>
      <div className="panel-kho-media" onClick={(su) => su.stopPropagation()}>
        <div className="panel-kho-media__dau">
          <span className="panel-kho-media__icon"><BieuTuongKhoLuuTru /></span>
          <div className="panel-kho-media__tieu-de-cum">
            <h3 className="panel-kho-media__tieu-de">Kho lưu trữ Media & Tệp</h3>
            <p className="panel-kho-media__phu-de">{tenCuocTroChuyen}</p>
          </div>
          <button className="panel-kho-media__dong" onClick={onDong} aria-label="Đóng"><BieuTuongDong /></button>
        </div>

        <div className="panel-kho-media__tab-cum">
          <button
            className={`panel-kho-media__tab${tabDangChon === 'anh' ? ' panel-kho-media__tab--chon' : ''}`}
            onClick={() => setTabDangChon('anh')}
          >
            Hình ảnh ({danhSachAnh.length})
          </button>
          <button
            className={`panel-kho-media__tab${tabDangChon === 'tep' ? ' panel-kho-media__tab--chon' : ''}`}
            onClick={() => setTabDangChon('tep')}
          >
            Tài liệu & Tệp ({danhSachTep.length})
          </button>
        </div>

        <div className="panel-kho-media__noi-dung">
          {tabDangChon === 'anh' && (
            danhSachAnh.length === 0 ? (
              <div className="panel-kho-media__trong">
                <BieuTuongAnhMo />
                <p>Chưa có hình ảnh nào được chia sẻ trong đoạn chat này</p>
              </div>
            ) : (
              <div className="panel-kho-media__luoi-anh">
                {danhSachAnh.map((tn) => (
                  <img
                    key={tn.id}
                    className="panel-kho-media__anh-nho"
                    src={`${DIA_CHI_GOC}${tn.duongDanFile}`}
                    alt={tn.tenFileGoc ?? 'ảnh'}
                    onClick={() => setAnhDangXemToId(tn.id)}
                  />
                ))}
              </div>
            )
          )}

          {tabDangChon === 'tep' && (
            danhSachTep.length === 0 ? (
              <div className="panel-kho-media__trong">
                <BieuTuongAnhMo />
                <p>Chưa có tài liệu nào được chia sẻ trong đoạn chat này</p>
              </div>
            ) : (
              <ul className="panel-kho-media__ds-tep">
                {danhSachTep.map((tn) => (
                  <li key={tn.id}>
                    <a href={`${DIA_CHI_GOC}${tn.duongDanFile}`} target="_blank" rel="noreferrer">
                      <span className="panel-kho-media__tep-icon"><BieuTuongTaiLieu /></span>
                      <div className="panel-kho-media__tep-thong-tin">
                        <span className="panel-kho-media__tep-ten">{tn.tenFileGoc}</span>
                        <span className="panel-kho-media__tep-size">{dinhDangKichThuoc(tn.kichThuocFile ?? 0)}</span>
                      </div>
                      <BieuTuongTai />
                    </a>
                  </li>
                ))}
              </ul>
            )
          )}
        </div>
      </div>

      {anhDangXemTo && (
        <div className="panel-kho-media__lightbox" onClick={() => setAnhDangXemToId(null)}>
          <img src={`${DIA_CHI_GOC}${anhDangXemTo.duongDanFile}`} alt={`Xem ảnh lớn ${anhDangXemTo.tenFileGoc}`} />
          <button className="panel-kho-media__lightbox-dong" onClick={() => setAnhDangXemToId(null)} aria-label="Đóng ảnh lớn">
            <BieuTuongDong />
          </button>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 9: Thêm CSS**

Tạo `frontend/src/Trang/PanelKhoMedia.css`:

```css
.panel-kho-media-nen {
  position: fixed;
  inset: 0;
  background: rgba(16, 27, 51, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 20;
}

.panel-kho-media {
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-the);
  width: 640px;
  max-width: calc(100vw - 32px);
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.panel-kho-media__dau {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 20px 20px 0;
}

.panel-kho-media__icon {
  flex-shrink: 0;
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: var(--mau-nen-tren);
  color: var(--mau-chinh-dam);
  display: flex;
  align-items: center;
  justify-content: center;
}

.panel-kho-media__tieu-de-cum {
  flex: 1;
  min-width: 0;
}

.panel-kho-media__tieu-de {
  margin: 0;
  font-size: 17px;
}

.panel-kho-media__phu-de {
  margin: 2px 0 0;
  font-size: 13px;
  color: var(--mau-chu-phu);
}

.panel-kho-media__dong {
  flex-shrink: 0;
  border: none;
  background: none;
  cursor: pointer;
  color: var(--mau-chu-phu);
}

.panel-kho-media__tab-cum {
  display: flex;
  gap: 4px;
  padding: 16px 20px 0;
  border-bottom: 1px solid var(--mau-vien);
}

.panel-kho-media__tab {
  border: none;
  background: none;
  padding: 10px 4px;
  margin-right: 20px;
  font-family: inherit;
  font-size: 14px;
  font-weight: 600;
  color: var(--mau-chu-phu);
  cursor: pointer;
  border-bottom: 2px solid transparent;
}

.panel-kho-media__tab--chon {
  color: var(--mau-chinh-dam);
  border-bottom-color: var(--mau-chinh-dam);
}

.panel-kho-media__noi-dung {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
}

.panel-kho-media__trong {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 48px 16px;
  color: var(--mau-chu-phu);
  text-align: center;
}

.panel-kho-media__luoi-anh {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
  gap: 8px;
}

.panel-kho-media__anh-nho {
  width: 100%;
  aspect-ratio: 1;
  object-fit: cover;
  border-radius: 8px;
  cursor: pointer;
}

.panel-kho-media__ds-tep {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.panel-kho-media__ds-tep a {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px;
  border-radius: 10px;
  border: 1px solid var(--mau-vien);
  color: var(--mau-chu-dam);
  text-decoration: none;
}

.panel-kho-media__ds-tep a:hover {
  background: var(--mau-nen-tren);
}

.panel-kho-media__tep-icon {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: var(--mau-chinh-dam);
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.panel-kho-media__tep-thong-tin {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.panel-kho-media__tep-ten {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.panel-kho-media__tep-size {
  font-size: 11px;
  color: var(--mau-chu-phu);
}

.panel-kho-media__lightbox {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 21;
}

.panel-kho-media__lightbox img {
  max-width: 90vw;
  max-height: 90vh;
  object-fit: contain;
}

.panel-kho-media__lightbox-dong {
  position: absolute;
  top: 16px;
  right: 16px;
  border: none;
  background: rgba(255, 255, 255, 0.2);
  color: #fff;
  border-radius: 50%;
  width: 36px;
  height: 36px;
  cursor: pointer;
}

@media (max-width: 640px) {
  .panel-kho-media {
    width: 100%;
    max-height: 100vh;
    border-radius: 0;
  }
}
```

- [ ] **Step 10: Chạy test để thấy PASS**

Run: `cd frontend && npm test -- --run PanelKhoMedia`
Expected: PASS toàn bộ.

- [ ] **Step 11: `tsc -b --noEmit` + chạy toàn bộ test frontend**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ test PASS.

- [ ] **Step 12: Commit**

```bash
git add frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts frontend/src/ThanhPhan/BieuTuong.tsx frontend/src/Trang/PanelKhoMedia.tsx frontend/src/Trang/PanelKhoMedia.css frontend/src/Trang/PanelKhoMedia.test.tsx
git commit -m "feat(frontend): component PanelKhoMedia hien anh/tep cuoc tro chuyen (GD7a)"
```

---

## Task 3: Frontend — nối dây icon folder vào `KhungTinNhan.tsx`/`TrangChat.tsx`/`TrangNhom.tsx`

**Files:**
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`
- Modify: `frontend/src/Trang/TrangChat.tsx`
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Test: `frontend/src/Trang/TrangChat.test.tsx`
- Test: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `PanelKhoMedia` (Task 2), `LayMediaTheoNguoiDung`/`LayMediaTheoNhom` (Task 2).
- Produces: không có task nào phụ thuộc (task cuối GĐ7a).

- [ ] **Step 1: Thêm prop `onMoKhoMedia` vào `KhungTinNhan.tsx`**

Trong `PropsKhungTinNhan`, thêm: `onMoKhoMedia: () => void;`. Trong
destructure component, thêm `onMoKhoMedia` vào danh sách tham số.

Trong `<header className="khung-tin-nhan__tieu-de">`, thêm nút icon
folder ngay TRƯỚC dòng `{!dangKetNoi && <span className="khung-tin-nhan__mat-ket-noi">...`:

```tsx
        <button className="khung-tin-nhan__nut-kho-media" onClick={onMoKhoMedia} aria-label="Kho lưu trữ Media & Tệp">
          <BieuTuongKhoLuuTru />
        </button>
```

Thêm `BieuTuongKhoLuuTru` vào dòng import từ `./BieuTuong`.

- [ ] **Step 2: Thêm CSS cho nút icon folder**

Thêm vào `KhungTinNhan.css`:

```css
.khung-tin-nhan__nut-kho-media {
  margin-left: auto;
  flex-shrink: 0;
  border: none;
  background: none;
  color: var(--mau-chu-phu);
  cursor: pointer;
  padding: 6px;
  border-radius: 8px;
}

.khung-tin-nhan__nut-kho-media:hover {
  background: var(--mau-nen-tren);
}
```
(Đã xác nhận: `.khung-tin-nhan__mat-ket-noi` hiện có sẵn `margin-left: auto`
riêng. Vì icon folder được chèn TRƯỚC `.khung-tin-nhan__mat-ket-noi`
trong DOM — xem Step 1 — `margin-left: auto` trên icon folder đẩy CẢ
NHÓM phần tử còn lại (icon folder + `.mat-ket-noi` nếu có) về cuối
header cùng lúc; `margin-left: auto` thứ 2 trên `.mat-ket-noi` (đứng
sau icon đã có auto) không cộng dồn thêm khoảng cách, hoàn toàn vô hại
— không cần sửa gì thêm ở `.khung-tin-nhan__mat-ket-noi`.)

- [ ] **Step 3: Viết test cho nút icon folder trong `KhungTinNhan.test.tsx`**

Thêm vào cuối `describe('KhungTinNhan', ...)`:

```tsx
it('bam icon kho media goi onMoKhoMedia', async () => {
  const onMoKhoMedia = vi.fn();
  render(<KhungTinNhan {...PROPS_MAC_DINH} onMoKhoMedia={onMoKhoMedia} />);

  await userEvent.click(screen.getByRole('button', { name: 'Kho lưu trữ Media & Tệp' }));

  expect(onMoKhoMedia).toHaveBeenCalledTimes(1);
});
```
Thêm `onMoKhoMedia: () => {}` vào `PROPS_MAC_DINH` (hằng số đầu file)
để các test hiện có khác không vỡ do thiếu prop bắt buộc mới.

- [ ] **Step 4: Chạy test `KhungTinNhan` để thấy PASS**

Run: `cd frontend && npm test -- --run KhungTinNhan`
Expected: PASS toàn bộ.

- [ ] **Step 5: Nối dây `TrangChat.tsx`**

Thêm import `LayMediaTheoNguoiDung` vào dòng import từ `'../DichVuApi'`
và `PanelKhoMedia` từ `'./PanelKhoMedia'`. Thêm state (cạnh
`tinNhanGhimTheoDoiTac`):

```typescript
  const [hienKhoMedia, setHienKhoMedia] = useState(false);
  const [danhSachMedia, setDanhSachMedia] = useState<TinNhan[]>([]);
```

Thêm hàm (cạnh các hàm xử lý khác):

```typescript
  function moKhoMedia() {
    if (!token || !nguoiDangChon) return;
    LayMediaTheoNguoiDung(token, nguoiDangChon.id)
      .then((media) => {
        setDanhSachMedia(media);
        setHienKhoMedia(true);
      })
      .catch(() => setLoi('Không tải được kho media.'));
  }
```

Truyền `onMoKhoMedia={moKhoMedia}` cho `<KhungTinNhan>`. Ngay SAU thẻ
đóng của `<KhungTinNhan ... />`, thêm:

```tsx
          {hienKhoMedia && (
            <PanelKhoMedia
              danhSachMedia={danhSachMedia}
              tenCuocTroChuyen={nguoiDangChon.tenHienThi}
              onDong={() => setHienKhoMedia(false)}
            />
          )}
```

- [ ] **Step 6: Nối dây `TrangNhom.tsx` — cùng logic Step 5 nhưng dùng `LayMediaTheoNhom`/`nhomDangChon`**

Thêm import `LayMediaTheoNhom` + `PanelKhoMedia`. State:
```typescript
  const [hienKhoMedia, setHienKhoMedia] = useState(false);
  const [danhSachMedia, setDanhSachMedia] = useState<TinNhan[]>([]);
```
Hàm:
```typescript
  function moKhoMedia() {
    if (!token || !nhomDangChon) return;
    LayMediaTheoNhom(token, nhomDangChon.id)
      .then((media) => {
        setDanhSachMedia(media);
        setHienKhoMedia(true);
      })
      .catch(() => setLoi('Không tải được kho media.'));
  }
```
Truyền `onMoKhoMedia={moKhoMedia}` cho `<KhungTinNhan>`, render
`<PanelKhoMedia danhSachMedia={danhSachMedia} tenCuocTroChuyen={nhomDangChon.tenNhom} onDong={() => setHienKhoMedia(false)} />`
khi `hienKhoMedia`.

- [ ] **Step 7: Cập nhật fixture props trong `TrangChat.test.tsx`/`TrangNhom.test.tsx`**

Vì `KhungTinNhan` giờ có prop bắt buộc `onMoKhoMedia` — TRONG 2 FILE
NÀY, `<KhungTinNhan>` được render THẬT qua `TrangChat`/`TrangNhom`
(không phải mock trực tiếp props), nên không cần sửa gì ở đây (prop đã
được truyền đúng trong JSX thật ở Step 5/6, không phải fixture test).
Chỉ cần đảm bảo `npx tsc -b --noEmit` sạch — nếu còn lỗi thiếu import
`TinNhan` type trong 2 file trang (đã có sẵn từ GĐ6b, kiểm tra lại).

- [ ] **Step 8: Viết test cho việc mở kho media trong `TrangChat.test.tsx`**

Thêm vào cuối `describe('TrangChat', ...)`:

```typescript
it('bam icon kho media goi LayMediaTheoNguoiDung va hien panel', async () => {
  vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
  vi.spyOn(DichVuApi, 'LayMediaTheoNguoiDung').mockResolvedValue([
    taoTinNhanGiaLap({ id: 'm-anh', loaiTinNhan: 'Anh', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439001', tenFileGoc: 'a.png' }),
  ]);

  renderTrangChat();
  await userEvent.click(await screen.findByText('TranBinh'));
  await userEvent.click(screen.getByRole('button', { name: 'Kho lưu trữ Media & Tệp' }));

  expect(await screen.findByText('Kho lưu trữ Media & Tệp')).toBeInTheDocument();
  expect(screen.getByText('Hình ảnh (1)')).toBeInTheDocument();
});
```

- [ ] **Step 9: Chạy test để thấy PASS, chạy toàn bộ test + tsc**

Run: `cd frontend && npx tsc -b --noEmit && npm test -- --run`
Expected: 0 lỗi; toàn bộ PASS.

- [ ] **Step 10: Commit**

```bash
git add frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx frontend/src/Trang/TrangChat.tsx frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.test.tsx
git commit -m "feat(frontend): noi day icon Kho Media vao KhungTinNhan/TrangChat/TrangNhom (GD7a)"
```

---

## Ghi chú cho reviewer / executor

- Task 1 độc lập (backend). Task 2 phụ thuộc Task 1 chỉ qua JSON shape
  (dùng mock trong test, không cần backend chạy thật). Task 3 phụ
  thuộc Task 2 (component `PanelKhoMedia` + hàm API).
- Không có 2 task nào cùng sửa 1 file trùng nhau ngoại trừ Task 3 sửa
  `KhungTinNhan.tsx`/`.css`/`.test.tsx` mà GĐ6a/6b cũng đã sửa trước đó
  — không xung đột vì đây là plan MỚI chạy SAU khi GĐ6a/6b đã merge,
  làm việc trên bản mới nhất.
