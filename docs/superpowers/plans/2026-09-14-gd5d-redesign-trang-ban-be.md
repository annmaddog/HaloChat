# GĐ5d — Redesign trang Bạn bè Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Viết lại trang Bạn bè theo mockup mới (2 tab, card gọn, tìm kiếm
chỉ hiện khi gõ, hiển thị đúng nút Nhắn tin/Kết bạn theo quan hệ + quyền
riêng tư người kia) và thêm tính năng Xóa bạn.

**Architecture:** Backend mở rộng `NguoiDungTomTatDto` thêm 1 field không
nhạy cảm (`ChoPhepTinNhanTuNguoiLa`) để frontend biết hiển thị nút nào ở
kết quả tìm kiếm, và thêm 1 endpoint xóa bạn mới (xóa hẳn bản ghi
`LoiMoiKetBan` đã chấp nhận, không đổi trạng thái). Frontend viết lại
`TrangBanBe.tsx` thành 2 tab + card gọn, tái sử dụng toàn bộ logic API đã
có (gửi/chấp nhận/từ chối lời mời, khung hồ sơ từ GĐ5c).

**Tech Stack:** Backend ASP.NET Core (.NET 9) + MongoDB Driver + xUnit.
Frontend React 19 + TypeScript + Vite + Vitest.

**Spec:** `docs/superpowers/specs/2026-09-14-halochat-redesign-trang-ban-be.md`

## Global Constraints

- Tên biến/hàm/route bằng tiếng Việt không dấu, đúng quy ước hiện có.
- Backend là nơi enforce chính cho việc chặn nhắn tin với người lạ không cho phép — logic này ĐÃ ĐÚNG trong `DichVuTinNhan.GuiTinNhanAsync`, KHÔNG được sửa gì ở đó trong plan này.
- Không thêm sự kiện SignalR real-time cho việc xóa bạn.
- Không đổi trang Cài đặt.
- Test backend: `dotnet test backend/HaloChat.sln`. Test frontend: `npm test` trong `frontend/`.

---

### Task 1: Backend — Mở rộng NguoiDungTomTatDto + Xóa bạn

**Files:**
- Modify: `backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNhom.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuTinNhan.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`
- Modify: `backend/HaloChat.Api/Services/DichVuKetBan.cs`
- Modify: `backend/HaloChat.Api/Services/IDichVuKetBan.cs`
- Modify: `backend/HaloChat.Api/Services/NgoaiLeKetBan.cs`
- Modify: `backend/HaloChat.Api/Repositories/ILoiMoiKetBanRepository.cs`
- Modify: `backend/HaloChat.Api/Repositories/LoiMoiKetBanRepository.cs`
- Modify: `backend/HaloChat.Api/Controllers/KetBanController.cs`
- Modify: `backend/HaloChat.Api.Tests/Fakes/LoiMoiKetBanGiaLap.cs`
- Modify: `backend/HaloChat.Api.Tests/Services/DichVuKetBanTests.cs`
- Modify: `backend/HaloChat.Api.Tests/KetBanControllerTests.cs`

**Interfaces:**
- Consumes: `NguoiDung.ChoPhepTinNhanTuNguoiLa` (đã có), `ILoiMoiKetBanRepository`'s `BoLocCapDoi` pattern (đã có, dùng làm mẫu cho `XoaAsync`).
- Produces:
  - `NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa)` — chữ ký mới, Task 2/3 (frontend) dùng.
  - `IDichVuKetBan.HuyKetBanAsync(string nguoiHienTaiId, string idBanBe) : Task`.
  - Endpoint `DELETE /api/ketban/ban-be/{idBanBe}`.
  - `KhongPhaiBanBeException` (ngoại lệ mới).

- [ ] **Step 1: Đổi `NguoiDungTomTatDto` và sửa 4 nơi khởi tạo**

Thay `backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs`:

```csharp
namespace HaloChat.Api.Dto;

public record NguoiDungTomTatDto(string Id, string TenTaiKhoan, string Email, bool ChoPhepTinNhanTuNguoiLa);
```

Trong `backend/HaloChat.Api/Services/DichVuNhom.cs`, method `AnhXaDtoAsync`, sửa dòng khởi tạo:
```csharp
thanhVien.Add(new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa));
```

Trong `backend/HaloChat.Api/Services/DichVuTinNhan.cs`, tìm dòng khởi tạo `NguoiDungTomTatDto` (trong `LayDanhSachHoiThoaiAsync`), sửa:
```csharp
new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa),
```

Trong `backend/HaloChat.Api/Services/DichVuNguoiDung.cs`, method `LayDanhSachNguoiDung`, sửa:
```csharp
.Select(nd => new NguoiDungTomTatDto(nd.Id, nd.TenTaiKhoan, nd.Email, nd.ChoPhepTinNhanTuNguoiLa))
```

Trong `backend/HaloChat.Api/Services/DichVuKetBan.cs`:
- Method `AnhXaDto` (static, nhận `nguoiGui`/`nguoiNhan` kiểu `NguoiDung`), sửa cả 2 dòng khởi tạo `NguoiDungTomTatDto`:
```csharp
private static LoiMoiKetBanDto AnhXaDto(LoiMoiKetBan l, NguoiDung nguoiGui, NguoiDung nguoiNhan) => new(
    l.Id,
    new NguoiDungTomTatDto(nguoiGui.Id, nguoiGui.TenTaiKhoan, nguoiGui.Email, nguoiGui.ChoPhepTinNhanTuNguoiLa),
    new NguoiDungTomTatDto(nguoiNhan.Id, nguoiNhan.TenTaiKhoan, nguoiNhan.Email, nguoiNhan.ChoPhepTinNhanTuNguoiLa),
    l.TrangThai.ToString(),
    l.ThoiGianTao);
```
- Method `LayBanBeAsync`, sửa dòng khởi tạo:
```csharp
ketQua.Add(new NguoiDungTomTatDto(ban.Id, ban.TenTaiKhoan, ban.Email, ban.ChoPhepTinNhanTuNguoiLa));
```

- [ ] **Step 2: Build để xác nhận đã sửa hết chỗ dùng constructor cũ**

Run: `dotnet build backend/HaloChat.sln`
Expected: nếu còn sót chỗ nào gọi `new NguoiDungTomTatDto(...)` với 3 tham số, biên dịch báo lỗi rõ ràng tên file/dòng — sửa hết cho tới khi build sạch. (Chỉ có đúng 4 chỗ liệt kê ở Step 1 — nếu build báo thêm chỗ khác thì đó là chỗ plan này bỏ sót, sửa theo đúng mẫu trên.)

- [ ] **Step 3: Commit Step 1-2**

```bash
git add backend/HaloChat.Api/Dto/NguoiDungTomTatDto.cs backend/HaloChat.Api/Services/DichVuNhom.cs backend/HaloChat.Api/Services/DichVuTinNhan.cs backend/HaloChat.Api/Services/DichVuNguoiDung.cs backend/HaloChat.Api/Services/DichVuKetBan.cs
git commit -m "Backend: mo rong NguoiDungTomTatDto them ChoPhepTinNhanTuNguoiLa"
```

- [ ] **Step 4: Viết test cho `HuyKetBanAsync` (TDD)**

Thêm vào cuối `backend/HaloChat.Api.Tests/Services/DichVuKetBanTests.cs` (trước dấu `}` đóng class):

```csharp
    [Fact]
    public async Task HuyKetBanAsync_DangLaBanBe_XoaBanGhiKetBan()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB, TrangThai = TrangThaiLoiMoiKetBan.DaChapNhan });

        await dichVu.HuyKetBanAsync(IdA, IdB);

        Assert.Empty(khoLoiMoi.DanhSach);
    }

    [Fact]
    public async Task HuyKetBanAsync_KhongPhaiBanBe_NemNgoaiLe()
    {
        var (dichVu, _, _) = TaoDichVu();

        await Assert.ThrowsAsync<KhongPhaiBanBeException>(() => dichVu.HuyKetBanAsync(IdA, IdB));
    }

    [Fact]
    public async Task HuyKetBanAsync_SauKhiXoa_CoTheGuiLaiLoiMoiMoi()
    {
        var (dichVu, khoLoiMoi, _) = TaoDichVu();
        khoLoiMoi.DanhSach.Add(new LoiMoiKetBan { Id = IdLoiMoi1, NguoiGuiId = IdA, NguoiNhanId = IdB, TrangThai = TrangThaiLoiMoiKetBan.DaChapNhan });

        await dichVu.HuyKetBanAsync(IdA, IdB);
        var ketQua = await dichVu.GuiLoiMoiAsync(IdA, IdB);

        Assert.Equal("ChoDuyet", ketQua.TrangThai);
    }
```

- [ ] **Step 5: Chạy test để xác nhận thất bại**

Run: `dotnet test backend/HaloChat.sln --filter DichVuKetBanTests`
Expected: FAIL biên dịch — `IDichVuKetBan`/`DichVuKetBan` chưa có `HuyKetBanAsync`, chưa có `KhongPhaiBanBeException`.

- [ ] **Step 6: Thêm ngoại lệ `KhongPhaiBanBeException`**

Thêm vào cuối `backend/HaloChat.Api/Services/NgoaiLeKetBan.cs`:

```csharp

/// <summary>Ném ra khi cố xóa bạn với 1 người hiện chưa (hoặc không còn) là bạn bè.</summary>
public class KhongPhaiBanBeException : Exception
{
    public KhongPhaiBanBeException() : base("Người này không phải bạn bè của bạn.")
    {
    }
}
```

- [ ] **Step 7: Thêm `XoaAsync` vào `ILoiMoiKetBanRepository`/`LoiMoiKetBanRepository`**

Trong `backend/HaloChat.Api/Repositories/ILoiMoiKetBanRepository.cs`, thêm cuối interface:

```csharp
    /// <summary>Xóa hẳn bản ghi lời mời đã chấp nhận giữa 2 người (dùng cho tính năng xóa bạn).</summary>
    Task XoaAsync(string nguoiA, string nguoiB);
```

Trong `backend/HaloChat.Api/Repositories/LoiMoiKetBanRepository.cs`, thêm cuối class (trước dấu `}` đóng):

```csharp

    public async Task XoaAsync(string nguoiA, string nguoiB)
    {
        var boLoc = Builders<LoiMoiKetBan>.Filter.And(
            BoLocCapDoi(nguoiA, nguoiB),
            Builders<LoiMoiKetBan>.Filter.Eq(l => l.TrangThai, TrangThaiLoiMoiKetBan.DaChapNhan));
        await _collection.DeleteOneAsync(boLoc);
    }
```

- [ ] **Step 8: Thêm fake `XoaAsync` vào `LoiMoiKetBanGiaLap`**

Trong `backend/HaloChat.Api.Tests/Fakes/LoiMoiKetBanGiaLap.cs`, thêm cuối class:

```csharp

    public Task XoaAsync(string nguoiA, string nguoiB)
    {
        DanhSach.RemoveAll(l => KhopCapDoi(l, nguoiA, nguoiB) && l.TrangThai == TrangThaiLoiMoiKetBan.DaChapNhan);
        return Task.CompletedTask;
    }
```

- [ ] **Step 9: Thêm `HuyKetBanAsync` vào `IDichVuKetBan`/`DichVuKetBan`**

Trong `backend/HaloChat.Api/Services/IDichVuKetBan.cs`, thêm cuối interface:

```csharp
    Task HuyKetBanAsync(string nguoiHienTaiId, string idBanBe);
```

(Nếu file `IDichVuKetBan.cs` chưa tồn tại theo tên này — kiểm tra tên thật của interface đang được `DichVuKetBan : IDichVuKetBan` implement, dùng đúng tên file/interface đó.)

Trong `backend/HaloChat.Api/Services/DichVuKetBan.cs`, thêm cuối class (trước dấu `}` đóng):

```csharp

    public async Task HuyKetBanAsync(string nguoiHienTaiId, string idBanBe)
    {
        if (!ObjectId.TryParse(idBanBe, out _) || !await _khoLoiMoi.LaBanBeAsync(nguoiHienTaiId, idBanBe))
        {
            throw new KhongPhaiBanBeException();
        }

        await _khoLoiMoi.XoaAsync(nguoiHienTaiId, idBanBe);
    }
```

- [ ] **Step 10: Chạy test để xác nhận PASS**

Run: `dotnet test backend/HaloChat.sln --filter DichVuKetBanTests`
Expected: PASS toàn bộ (bao gồm 3 test mới).

- [ ] **Step 11: Thêm endpoint controller**

Trong `backend/HaloChat.Api/Controllers/KetBanController.cs`, thêm cuối class (trước dấu `}` đóng, sau method `LayLoiMoiGui`):

```csharp

    [HttpDelete("ban-be/{idBanBe}")]
    public async Task<IActionResult> XoaBanBe(string idBanBe)
    {
        if (IdHienTai is null)
        {
            return Unauthorized();
        }

        try
        {
            await _dichVu.HuyKetBanAsync(IdHienTai, idBanBe);
            return Ok(new { thongBao = "Đã xóa bạn." });
        }
        catch (KhongPhaiBanBeException loi)
        {
            return NotFound(new { thongBao = loi.Message });
        }
    }
```

- [ ] **Step 12: Viết test tích hợp cho endpoint**

Thêm vào cuối `backend/HaloChat.Api.Tests/KetBanControllerTests.cs` (trước dấu `}` đóng class):

```csharp

    [Fact]
    public async Task XoaBanBe_ChuaDangNhap_TraVe401()
    {
        var phanHoi = await _client.DeleteAsync("/api/ketban/ban-be/000000000000000000000000");
        Assert.Equal(HttpStatusCode.Unauthorized, phanHoi.StatusCode);
    }

    [Fact]
    public async Task XoaBanBe_DangLaBanBe_TraVe200VaXoaKhoiDanhSach()
    {
        var (tokenA, idA) = await DangKyVaDangNhapAsync("xoabannguoia");
        var (_, idB) = await DangKyVaDangNhapAsync("xoabannguoib");
        _factory.KhoLoiMoiKetBanGiaLap.DanhSach.Add(new HaloChat.Api.Models.LoiMoiKetBan
        {
            NguoiGuiId = idA, NguoiNhanId = idB, TrangThai = HaloChat.Api.Models.TrangThaiLoiMoiKetBan.DaChapNhan,
        });

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.DeleteAsync($"/api/ketban/ban-be/{idB}");

        Assert.Equal(HttpStatusCode.OK, phanHoi.StatusCode);
        Assert.Empty(_factory.KhoLoiMoiKetBanGiaLap.DanhSach);
    }

    [Fact]
    public async Task XoaBanBe_KhongPhaiBanBe_TraVe404()
    {
        var (tokenA, _) = await DangKyVaDangNhapAsync("xoabankhac1");
        var (_, idB) = await DangKyVaDangNhapAsync("xoabankhac2");

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);
        var phanHoi = await _client.DeleteAsync($"/api/ketban/ban-be/{idB}");

        Assert.Equal(HttpStatusCode.NotFound, phanHoi.StatusCode);
    }
```

- [ ] **Step 13: Build + chạy toàn bộ test backend**

Run: `dotnet build backend/HaloChat.sln`
Expected: `Build succeeded. 0 Error(s)`.

Run: `dotnet test backend/HaloChat.sln`
Expected: PASS toàn bộ.

- [ ] **Step 14: Commit**

```bash
git add backend/HaloChat.Api/Services/IDichVuKetBan.cs backend/HaloChat.Api/Services/DichVuKetBan.cs backend/HaloChat.Api/Services/NgoaiLeKetBan.cs backend/HaloChat.Api/Repositories/ILoiMoiKetBanRepository.cs backend/HaloChat.Api/Repositories/LoiMoiKetBanRepository.cs backend/HaloChat.Api/Controllers/KetBanController.cs backend/HaloChat.Api.Tests/Fakes/LoiMoiKetBanGiaLap.cs backend/HaloChat.Api.Tests/Services/DichVuKetBanTests.cs backend/HaloChat.Api.Tests/KetBanControllerTests.cs
git commit -m "Backend: them tinh nang xoa ban (DELETE /api/ketban/ban-be/id)"
```

---

### Task 2: Frontend — Mở rộng KieuDuLieu.ts + DichVuApi.ts

**Files:**
- Modify: `frontend/src/KieuDuLieu.ts`
- Modify: `frontend/src/DichVuApi.ts`
- Modify: `frontend/src/DichVuApi.test.ts`
- Modify: `frontend/src/Trang/TrangBanBe.test.tsx`
- Modify: `frontend/src/Trang/TrangChat.test.tsx`
- Modify: `frontend/src/Trang/TrangNhom.test.tsx`
- Modify: `frontend/src/ThanhPhan/KhungChinh.test.tsx`

**Interfaces:**
- Consumes: `DELETE /api/ketban/ban-be/{idBanBe}` (Task 1).
- Produces:
  - `NguoiDungTomTat { id, tenTaiKhoan, email, choPhepTinNhanTuNguoiLa: boolean }` — Task 3 dùng.
  - `XoaBanBe(token: string, idBanBe: string): Promise<KetQuaThongBao>` — Task 3 dùng.

- [ ] **Step 1: Thêm field vào `NguoiDungTomTat`**

Trong `frontend/src/KieuDuLieu.ts`, thay `NguoiDungTomTat` (đầu file):

```typescript
export interface NguoiDungTomTat {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
}
```

- [ ] **Step 2: Thêm `XoaBanBe` vào `DichVuApi.ts`**

Thêm vào cuối `frontend/src/DichVuApi.ts`:

```typescript

export async function XoaBanBe(token: string, idBanBe: string): Promise<KetQuaThongBao> {
  return goiApi<KetQuaThongBao>(`/ketban/ban-be/${idBanBe}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

- [ ] **Step 3: Cập nhật MỌI object giả lập `NguoiDungTomTat` trong toàn bộ test frontend cho đủ field mới**

TypeScript sẽ báo lỗi biên dịch ở mọi nơi trong test tạo object `{ id, tenTaiKhoan, email }` (thiếu `choPhepTinNhanTuNguoiLa`) vì field này bắt buộc. Tìm và sửa TẤT CẢ (dùng `grep -rn "tenTaiKhoan:" frontend/src` để định vị, không chỉ các file liệt kê dưới đây — đây là danh sách các file ĐÃ BIẾT có object dạng này, có thể còn sót):

- `frontend/src/DichVuApi.test.ts`
- `frontend/src/Trang/TrangBanBe.test.tsx`
- `frontend/src/Trang/TrangChat.test.tsx`
- `frontend/src/Trang/TrangNhom.test.tsx`
- `frontend/src/ThanhPhan/KhungChinh.test.tsx`

Với mỗi object literal có dạng `{ id: '...', tenTaiKhoan: '...', email: '...' }`, thêm `choPhepTinNhanTuNguoiLa: true` vào (mặc định cho phép, trừ khi 1 test cụ thể cần kiểm tra trường hợp `false` — Task 3 sẽ tự thêm test riêng cho trường hợp đó, task này chỉ cần làm cho các test hiện có biên dịch được và giữ nguyên hành vi cũ).

- [ ] **Step 4: Build frontend để xác nhận hết lỗi biên dịch**

Run: `cd frontend && npx tsc --noEmit`
Expected: 0 lỗi. (Nếu còn lỗi `Property 'choPhepTinNhanTuNguoiLa' is missing`, đó là 1 chỗ Step 3 bỏ sót — sửa tiếp cho tới khi sạch.)

- [ ] **Step 5: Chạy toàn bộ test frontend**

Run: `cd frontend && npm test`
Expected: PASS toàn bộ, không có test nào bị hỏng.

- [ ] **Step 6: Chạy build production**

Run: `cd frontend && npm run build`
Expected: build thành công.

- [ ] **Step 7: Commit**

```bash
git add -A frontend/src/KieuDuLieu.ts frontend/src/DichVuApi.ts frontend/src/DichVuApi.test.ts frontend/src/Trang/TrangBanBe.test.tsx frontend/src/Trang/TrangChat.test.tsx frontend/src/Trang/TrangNhom.test.tsx frontend/src/ThanhPhan/KhungChinh.test.tsx
git commit -m "Frontend: them choPhepTinNhanTuNguoiLa vao NguoiDungTomTat, them XoaBanBe"
```

(Nếu Step 3 sửa thêm file ngoài danh sách trên, thêm chúng vào `git add` — dùng `git add -A` để không bỏ sót.)

---

### Task 3: Frontend — Viết lại TrangBanBe.tsx/.css hoàn chỉnh

**Files:**
- Modify: `frontend/src/Trang/TrangBanBe.tsx`
- Modify: `frontend/src/Trang/TrangBanBe.css`
- Modify: `frontend/src/Trang/TrangBanBe.test.tsx`

**Interfaces:**
- Consumes: `LayTrangThaiHoatDong(token, ids): Promise<Record<string, boolean>>` (đã có, dùng lại mẫu từ `TrangChat.tsx`), `XoaBanBe` (Task 2), `NguoiDungTomTat.choPhepTinNhanTuNguoiLa` (Task 2).

- [ ] **Step 1: Đọc `frontend/src/Trang/TrangBanBe.test.tsx` hiện tại (sau khi Task 2 đã sửa) để biết pattern mock/render đang dùng — giữ nguyên các mock helper đã có (`renderTrangBanBe()`, cách mock `DichVuApi.*`), chỉ thêm/sửa test cho khớp UI mới ở Step 5.**

- [ ] **Step 2: Viết lại toàn bộ `TrangBanBe.tsx`**

Thay toàn bộ nội dung `frontend/src/Trang/TrangBanBe.tsx`:

```tsx
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  LayBanBe,
  LayLoiMoiDen,
  LayLoiMoiGui,
  LayDanhSachNguoiDung,
  LayTrangThaiHoatDong,
  GuiLoiMoiKetBan,
  ChapNhanLoiMoiKetBan,
  TuChoiLoiMoiKetBan,
  XoaBanBe,
  LoiGoiApi,
} from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import type { NguoiDungTomTat, LoiMoiKetBan } from '../KieuDuLieu';
import { Avatar } from '../ThanhPhan/Avatar';
import './TrangBanBe.css';

type Tab = 'ban-be' | 'loi-moi';

export function TrangBanBe() {
  const { token } = useXacThuc();
  const navigate = useNavigate();

  const [banBe, setBanBe] = useState<NguoiDungTomTat[]>([]);
  const [loiMoiDen, setLoiMoiDen] = useState<LoiMoiKetBan[]>([]);
  const [loiMoiGui, setLoiMoiGui] = useState<LoiMoiKetBan[]>([]);
  const [tatCaNguoiDung, setTatCaNguoiDung] = useState<NguoiDungTomTat[]>([]);
  const [trangThaiOnline, setTrangThaiOnline] = useState<Record<string, boolean>>({});
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTai, setDangTai] = useState(true);
  const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');
  const [tabDangChon, setTabDangChon] = useState<Tab>('ban-be');
  const [menuMoChoId, setMenuMoChoId] = useState<string | null>(null);
  const [hoSoDangXem, setHoSoDangXem] = useState<NguoiDungTomTat | null>(null);
  const inputTimKiemRef = useRef<HTMLInputElement | null>(null);

  async function taiLaiTatCa(tokenHienTai: string) {
    const [dsBanBe, dsLoiMoiDen, dsLoiMoiGui, dsTatCa] = await Promise.all([
      LayBanBe(tokenHienTai),
      LayLoiMoiDen(tokenHienTai),
      LayLoiMoiGui(tokenHienTai),
      LayDanhSachNguoiDung(tokenHienTai),
    ]);
    setBanBe(dsBanBe);
    setLoiMoiDen(dsLoiMoiDen);
    setLoiMoiGui(dsLoiMoiGui);
    setTatCaNguoiDung(dsTatCa);
    if (dsBanBe.length > 0) {
      LayTrangThaiHoatDong(tokenHienTai, dsBanBe.map((b) => b.id))
        .then(setTrangThaiOnline)
        .catch(() => {});
    }
  }

  useEffect(() => {
    if (!token) return;
    taiLaiTatCa(token)
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function guiLoiMoi(nguoiNhanId: string) {
    if (!token) return;
    try {
      await GuiLoiMoiKetBan(token, nguoiNhanId);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Gửi lời mời thất bại.');
    }
  }

  async function chapNhan(id: string) {
    if (!token) return;
    try {
      await ChapNhanLoiMoiKetBan(token, id);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Chấp nhận lời mời thất bại.');
    }
  }

  async function tuChoi(id: string) {
    if (!token) return;
    try {
      await TuChoiLoiMoiKetBan(token, id);
      await taiLaiTatCa(token);
    } catch (loiBat) {
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Từ chối lời mời thất bại.');
    }
  }

  function xoaBan(b: NguoiDungTomTat) {
    if (!token) return;
    setMenuMoChoId(null);
    if (!window.confirm(`Xóa ${b.tenTaiKhoan} khỏi danh sách bạn bè?`)) return;
    XoaBanBe(token, b.id)
      .then(() => setBanBe((truoc) => truoc.filter((x) => x.id !== b.id)))
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Xóa bạn thất bại.'));
  }

  const idDaLaBanBeHoacDangCho = new Set<string>([
    ...banBe.map((b) => b.id),
    ...loiMoiDen.map((l) => l.nguoiGui.id),
    ...loiMoiGui.map((l) => l.nguoiNhan.id),
  ]);

  const dangTimKiem = tuKhoaTimKiem.trim().length > 0;
  const idDaGuiLoiMoi = new Set(loiMoiGui.map((l) => l.nguoiNhan.id));
  const ketQuaTimKiem = tatCaNguoiDung
    .filter((nd) => !idDaLaBanBeHoacDangCho.has(nd.id))
    .filter((nd) => nd.tenTaiKhoan.toLowerCase().includes(tuKhoaTimKiem.trim().toLowerCase()));

  return (
    <div className="trang-ban-be-bo-cuc">
      <div className="trang-ban-be">
        <input
          ref={inputTimKiemRef}
          type="text"
          className="trang-ban-be__tim-kiem"
          placeholder="🔎 Tìm bạn bè hoặc tên tài khoản..."
          value={tuKhoaTimKiem}
          onChange={(su) => setTuKhoaTimKiem(su.target.value)}
        />

        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}

        {!dangTimKiem && (
          <div className="trang-ban-be__tab-cum">
            <button
              className={`trang-ban-be__tab${tabDangChon === 'ban-be' ? ' trang-ban-be__tab--chon' : ''}`}
              onClick={() => setTabDangChon('ban-be')}
            >
              Bạn bè
            </button>
            <button
              className={`trang-ban-be__tab${tabDangChon === 'loi-moi' ? ' trang-ban-be__tab--chon' : ''}`}
              onClick={() => setTabDangChon('loi-moi')}
            >
              Lời mời ({loiMoiDen.length})
            </button>
          </div>
        )}

        {dangTai && <p>Đang tải...</p>}

        {dangTimKiem && (
          <section className="trang-ban-be__phan">
            <h2>Kết quả tìm kiếm</h2>
            {ketQuaTimKiem.length === 0 && <p className="trang-ban-be__trong">Không tìm thấy người dùng nào.</p>}
            <ul className="trang-ban-be__danh-sach">
              {ketQuaTimKiem.map((nd) => (
                <li key={nd.id} className="trang-ban-be__card trang-ban-be__card--tim-kiem">
                  <Avatar id={nd.id} ten={nd.tenTaiKhoan} />
                  <div className="trang-ban-be__card-thong-tin">
                    <span className="trang-ban-be__card-ten">{nd.tenTaiKhoan}</span>
                    <span className="trang-ban-be__card-email">{nd.email}</span>
                    {!nd.choPhepTinNhanTuNguoiLa && (
                      <span className="trang-ban-be__card-khoa">🔒 Chỉ nhận tin nhắn từ bạn bè</span>
                    )}
                  </div>
                  <div className="trang-ban-be__card-hanh-dong">
                    {nd.choPhepTinNhanTuNguoiLa && (
                      <button
                        className="nut-phu"
                        onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: nd } })}
                      >
                        Nhắn tin
                      </button>
                    )}
                    {idDaGuiLoiMoi.has(nd.id) ? (
                      <button className="nut-chinh" disabled>Đã gửi lời mời</button>
                    ) : (
                      <button className="nut-chinh" onClick={() => guiLoiMoi(nd.id)}>Kết bạn</button>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          </section>
        )}

        {!dangTimKiem && tabDangChon === 'ban-be' && (
          <section className="trang-ban-be__phan">
            <h2>Bạn bè của tôi ({banBe.length})</h2>
            {banBe.length === 0 ? (
              <div className="trang-ban-be__trong-toan-trang">
                <p className="trang-ban-be__trong-tieu-de">Bạn chưa có người bạn nào</p>
                <p>Hãy tìm kiếm và kết bạn với những người bạn biết.</p>
                <button className="nut-chinh" onClick={() => inputTimKiemRef.current?.focus()}>
                  Tìm bạn bè
                </button>
              </div>
            ) : (
              <ul className="trang-ban-be__danh-sach">
                {banBe.map((b) => (
                  <li key={b.id} className="trang-ban-be__card">
                    <Avatar id={b.id} ten={b.tenTaiKhoan} />
                    <div className="trang-ban-be__card-thong-tin">
                      <span className="trang-ban-be__card-ten">{b.tenTaiKhoan}</span>
                      {trangThaiOnline[b.id] && <span className="trang-ban-be__card-trang-thai">Đang hoạt động</span>}
                    </div>
                    <button
                      className="nut-chinh trang-ban-be__nut-nhan-tin"
                      onClick={() => navigate('/nguoi-dung', { state: { moNguoiDung: b } })}
                    >
                      Nhắn tin
                    </button>
                    <div className="trang-ban-be__menu-cum">
                      <button
                        className="trang-ban-be__nut-menu"
                        aria-label={`Thêm thao tác cho ${b.tenTaiKhoan}`}
                        onClick={() => setMenuMoChoId((truoc) => (truoc === b.id ? null : b.id))}
                      >
                        ⋯
                      </button>
                      {menuMoChoId === b.id && (
                        <div className="trang-ban-be__menu">
                          <button onClick={() => { setHoSoDangXem(b); setMenuMoChoId(null); }}>Xem thông tin</button>
                          <button className="trang-ban-be__menu-nguy-hiem" onClick={() => xoaBan(b)}>Xóa bạn</button>
                        </div>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}

        {!dangTimKiem && tabDangChon === 'loi-moi' && (
          <section className="trang-ban-be__phan">
            <h2>Lời mời kết bạn ({loiMoiDen.length})</h2>
            {loiMoiDen.length === 0 ? (
              <div className="trang-ban-be__trong-toan-trang">
                <p className="trang-ban-be__trong-tieu-de">Không có lời mời kết bạn</p>
                <p>Bạn chưa có lời mời kết bạn nào.</p>
              </div>
            ) : (
              <ul className="trang-ban-be__danh-sach">
                {loiMoiDen.map((l) => (
                  <li key={l.id} className="trang-ban-be__card">
                    <Avatar id={l.nguoiGui.id} ten={l.nguoiGui.tenTaiKhoan} />
                    <div className="trang-ban-be__card-thong-tin">
                      <span className="trang-ban-be__card-ten">{l.nguoiGui.tenTaiKhoan}</span>
                      <span className="trang-ban-be__card-phu">Muốn kết bạn với bạn</span>
                    </div>
                    <div className="trang-ban-be__card-hanh-dong">
                      <button className="nut-chinh" onClick={() => chapNhan(l.id)}>Chấp nhận</button>
                      <button className="nut-phu" onClick={() => tuChoi(l.id)}>Từ chối</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}
      </div>

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
    </div>
  );
}
```

- [ ] **Step 3: Viết lại `TrangBanBe.css`**

Thay toàn bộ nội dung `frontend/src/Trang/TrangBanBe.css`:

```css
.trang-ban-be-bo-cuc {
  display: flex;
  align-items: flex-start;
}

.trang-ban-be {
  flex: 1;
  min-width: 0;
  max-width: 900px;
  margin: 0 auto;
  padding: 32px 24px;
}

.trang-ban-be__tim-kiem {
  width: 100%;
  padding: 14px 18px;
  border: 1px solid var(--mau-vien);
  border-radius: 14px;
  font-family: inherit;
  font-size: 15px;
  margin-bottom: 20px;
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  box-sizing: border-box;
}

.trang-ban-be__tab-cum {
  display: flex;
  gap: 8px;
  margin-bottom: 24px;
}

.trang-ban-be__tab {
  border: none;
  background: none;
  padding: 10px 18px;
  border-radius: 12px;
  font-family: inherit;
  font-size: 14px;
  font-weight: 600;
  color: var(--mau-chu-phu);
  cursor: pointer;
}

.trang-ban-be__tab--chon {
  background: var(--mau-nen-tren);
  color: var(--mau-chinh-dam);
}

.trang-ban-be__phan {
  margin-bottom: 32px;
}

.trang-ban-be__phan h2 {
  font-size: 16px;
  margin-bottom: 12px;
}

.trang-ban-be__trong {
  color: var(--mau-chu-phu);
  font-size: 14px;
}

.trang-ban-be__trong-toan-trang {
  text-align: center;
  padding: 48px 16px;
  color: var(--mau-chu-phu);
}

.trang-ban-be__trong-tieu-de {
  font-size: 16px;
  font-weight: 700;
  color: var(--mau-chu-dam);
  margin-bottom: 6px;
}

.trang-ban-be__trong-toan-trang .nut-chinh {
  width: auto;
  margin: 16px auto 0;
  padding: 10px 24px;
}

.trang-ban-be__danh-sach {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.trang-ban-be__card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  background: var(--mau-nen-the);
  border-radius: 14px;
  border: 1px solid var(--mau-vien);
  box-shadow: 0 2px 8px -4px rgba(31, 66, 135, 0.15);
  min-width: 0;
}

.trang-ban-be__card-thong-tin {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.trang-ban-be__card-ten {
  font-weight: 600;
  color: var(--mau-chu-dam);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trang-ban-be__card-email,
.trang-ban-be__card-phu {
  font-size: 12px;
  color: var(--mau-chu-phu);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trang-ban-be__card-trang-thai {
  font-size: 12px;
  color: var(--mau-chinh);
}

.trang-ban-be__card-khoa {
  font-size: 12px;
  color: var(--mau-chu-phu);
}

.trang-ban-be__card-hanh-dong {
  display: flex;
  gap: 8px;
  flex-shrink: 0;
}

.trang-ban-be__nut-nhan-tin {
  width: auto;
  flex-shrink: 0;
  padding: 8px 18px;
  min-height: 36px;
}

.trang-ban-be__menu-cum {
  position: relative;
  flex-shrink: 0;
}

.trang-ban-be__nut-menu {
  border: none;
  background: none;
  font-size: 18px;
  line-height: 1;
  padding: 6px 8px;
  cursor: pointer;
  color: var(--mau-chu-phu);
  border-radius: 8px;
}

.trang-ban-be__nut-menu:hover {
  background: var(--mau-nen-tren);
}

.trang-ban-be__menu {
  position: absolute;
  top: 100%;
  right: 0;
  min-width: 160px;
  background: var(--mau-nen-the);
  border: 1px solid var(--mau-vien);
  border-radius: 10px;
  box-shadow: var(--bong-the);
  padding: 6px;
  z-index: 10;
  display: flex;
  flex-direction: column;
}

.trang-ban-be__menu button {
  border: none;
  background: none;
  text-align: left;
  padding: 8px 10px;
  border-radius: 8px;
  font-family: inherit;
  font-size: 13px;
  color: var(--mau-chu-dam);
  cursor: pointer;
}

.trang-ban-be__menu button:hover {
  background: var(--mau-nen-tren);
}

.trang-ban-be__menu-nguy-hiem {
  color: var(--mau-loi) !important;
}

.nut-phu {
  border: 1px solid var(--mau-vien);
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  border-radius: 999px;
  padding: 8px 16px;
  font-family: inherit;
  font-weight: 600;
  font-size: 13px;
  cursor: pointer;
}

.trang-ban-be__ho-so {
  flex-shrink: 0;
  width: 280px;
  min-height: 100vh;
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

@media (max-width: 640px) {
  .trang-ban-be {
    padding: 16px 12px;
  }

  .trang-ban-be__card {
    flex-wrap: wrap;
  }

  .trang-ban-be__nut-nhan-tin {
    min-height: 40px;
    flex: 1;
  }

  .trang-ban-be__menu {
    right: 0;
    left: auto;
    max-width: calc(100vw - 32px);
  }

  .trang-ban-be__ho-so {
    position: fixed;
    inset: 0;
    width: 100%;
    z-index: 20;
  }
}
```

- [ ] **Step 4: Xóa CSS cũ không còn dùng**

Kiểm tra không còn rule nào tham chiếu các class đã bị xóa khỏi component:
`.trang-ban-be__muc`, `.trang-ban-be__hang`, `.trang-ban-be__hang--bam-duoc`,
`.trang-ban-be__hanh-dong`. File CSS Step 3 đã thay TOÀN BỘ nội dung nên các
rule này tự động không còn — bước này chỉ để xác nhận (không có hành động
thêm nếu Step 3 đã làm đúng "thay toàn bộ nội dung").

- [ ] **Step 5: Cập nhật `TrangBanBe.test.tsx`**

Thay toàn bộ nội dung `frontend/src/Trang/TrangBanBe.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangBanBe } from './TrangBanBe';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderTrangBanBe() {
  return render(
    <MemoryRouter>
      <NhaCungCapXacThuc>
        <TrangBanBe />
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TrangBanBe', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([
      { id: 'b', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      {
        id: 'l1',
        nguoiGui: { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
        nguoiNhan: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com', choPhepTinNhanTuNguoiLa: true },
        trangThai: 'ChoDuyet',
        thoiGianTao: new Date().toISOString(),
      },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiGui').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: 'b', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
      { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
      { id: 'd', tenTaiKhoan: 'PhamD', email: 'd@gmail.com', choPhepTinNhanTuNguoiLa: true },
      { id: 'e', tenTaiKhoan: 'HoangE', email: 'e@gmail.com', choPhepTinNhanTuNguoiLa: false },
    ]);
    vi.spyOn(DichVuApi, 'LayTrangThaiHoatDong').mockResolvedValue({});
  });

  it('mac dinh hien tab Ban be voi danh sach ban be', async () => {
    renderTrangBanBe();
    expect(await screen.findByText('Bạn bè của tôi (1)')).toBeInTheDocument();
    expect(screen.getByText('TranBinh')).toBeInTheDocument();
  });

  it('chuyen sang tab Loi moi hien dung danh sach loi moi den', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Lời mời \(1\)/ }));

    expect(await screen.findByText('Lời mời kết bạn (1)')).toBeInTheDocument();
    expect(screen.getByText('LeC')).toBeInTheDocument();
    expect(screen.getByText('Muốn kết bạn với bạn')).toBeInTheDocument();
  });

  it('tab Loi moi rong hien dung empty state', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Lời mời \(0\)/ }));

    expect(await screen.findByText('Không có lời mời kết bạn')).toBeInTheDocument();
  });

  it('go tu khoa tim kiem chi hien Ket qua tim kiem, an ca 2 tab', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'Pham');

    expect(await screen.findByText('Kết quả tìm kiếm')).toBeInTheDocument();
    expect(screen.getByText('PhamD')).toBeInTheDocument();
    expect(screen.queryByText('LeC')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Lời mời/ })).not.toBeInTheDocument();
  });

  it('nguoi khong cho phep nguoi la nhan tin: chi hien nut Ket ban, khong hien Nhan tin', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'HoangE');

    const the = (await screen.findByText('HoangE')).closest('li')!;
    expect(the.textContent).toContain('Chỉ nhận tin nhắn từ bạn bè');
    expect(within(the).queryByRole('button', { name: 'Nhắn tin' })).not.toBeInTheDocument();
    expect(within(the).getByRole('button', { name: 'Kết bạn' })).toBeInTheDocument();
  });

  it('nguoi cho phep nguoi la nhan tin: hien ca Nhan tin va Ket ban', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'PhamD');

    const the = (await screen.findByText('PhamD')).closest('li')!;
    expect(within(the).getByRole('button', { name: 'Nhắn tin' })).toBeInTheDocument();
    expect(within(the).getByRole('button', { name: 'Kết bạn' })).toBeInTheDocument();
  });

  it('sau khi gui loi moi, nut doi thanh Da gui loi moi', async () => {
    vi.spyOn(DichVuApi, 'GuiLoiMoiKetBan').mockResolvedValue({
      id: 'l2',
      nguoiGui: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com', choPhepTinNhanTuNguoiLa: true },
      nguoiNhan: { id: 'd', tenTaiKhoan: 'PhamD', email: 'd@gmail.com', choPhepTinNhanTuNguoiLa: true },
      trangThai: 'ChoDuyet',
      thoiGianTao: new Date().toISOString(),
    });
    renderTrangBanBe();
    await screen.findByText('TranBinh');
    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'PhamD');
    await screen.findByText('PhamD');

    await userEvent.click(screen.getByRole('button', { name: 'Kết bạn' }));

    expect(await screen.findByRole('button', { name: 'Đã gửi lời mời' })).toBeInTheDocument();
  });

  it('bam Chap nhan goi ChapNhanLoiMoiKetBan', async () => {
    const chapNhanSpy = vi.spyOn(DichVuApi, 'ChapNhanLoiMoiKetBan').mockResolvedValue({
      id: 'l1',
      nguoiGui: { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
      nguoiNhan: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com', choPhepTinNhanTuNguoiLa: true },
      trangThai: 'DaChapNhan',
      thoiGianTao: new Date().toISOString(),
    });
    renderTrangBanBe();
    await screen.findByText('TranBinh');
    await userEvent.click(screen.getByRole('button', { name: /Lời mời/ }));
    await screen.findByText('LeC');

    await userEvent.click(screen.getByRole('button', { name: 'Chấp nhận' }));

    await waitFor(() => expect(chapNhanSpy).toHaveBeenCalledWith('token-gia-lap', 'l1'));
  });

  it('bam nut menu roi Xem thong tin mo khung ho so', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Thêm thao tác cho TranBinh/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Xem thông tin' }));

    expect(screen.getByText('b@gmail.com')).toBeInTheDocument();
  });

  it('bam Xoa ban trong menu goi XoaBanBe sau khi xac nhan', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const xoaBanSpy = vi.spyOn(DichVuApi, 'XoaBanBe').mockResolvedValue({ thongBao: 'Đã xóa bạn.' });
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Thêm thao tác cho TranBinh/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Xóa bạn' }));

    await waitFor(() => expect(xoaBanSpy).toHaveBeenCalledWith('token-gia-lap', 'b'));
    await waitFor(() => expect(screen.queryByText('TranBinh')).not.toBeInTheDocument());
  });

  it('bam Xoa ban nhung huy xac nhan thi khong goi API', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const xoaBanSpy = vi.spyOn(DichVuApi, 'XoaBanBe');
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Thêm thao tác cho TranBinh/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Xóa bạn' }));

    expect(xoaBanSpy).not.toHaveBeenCalled();
  });

  it('chua co ban be nao hien empty state toan trang', async () => {
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([]);
    renderTrangBanBe();

    expect(await screen.findByText('Bạn chưa có người bạn nào')).toBeInTheDocument();
  });
});
```

Thêm import `within` từ `@testing-library/react` (sửa dòng import đầu file
từ `import { render, screen, waitFor } from '@testing-library/react';`
thành `import { render, screen, waitFor, within } from '@testing-library/react';`
— đã áp dụng đúng trong khối code test ở trên, chỉ nhắc lại để không bỏ sót
khi copy).

- [ ] **Step 6: Chạy test**

Run: `cd frontend && npm test -- TrangBanBe`
Expected: PASS toàn bộ 13 test.

- [ ] **Step 7: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/Trang/TrangBanBe.tsx frontend/src/Trang/TrangBanBe.css frontend/src/Trang/TrangBanBe.test.tsx
git commit -m "Frontend: viet lai TrangBanBe (2 tab, card gon, xoa ban, nhan tin nguoi la)"
```

---

## Ghi chú cho người thực thi plan

- Task 1 và Task 2 đổi chữ ký `NguoiDungTomTatDto`/`NguoiDungTomTat` dùng ở
  RẤT NHIỀU nơi — implementer PHẢI dùng `grep` để tìm hết chỗ dùng thay vì
  chỉ sửa danh sách file đã liệt kê, đây là việc bình thường khi đổi 1 kiểu
  dữ liệu dùng chung rộng.
- Task 3 phụ thuộc Task 1+2 đã xong.
