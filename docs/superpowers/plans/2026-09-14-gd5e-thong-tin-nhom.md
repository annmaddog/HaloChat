# GĐ5e — Header nhóm bấm được + Thông tin nhóm + Quản lý nhóm Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bấm avatar/tên/số thành viên ở header chat nhóm mở trang "Thông
tin nhóm" (avatar lớn, thành viên, Rời/Giải tán nhóm), thay hẳn panel
"Thành viên" luôn hiện hiện tại; thao tác quản trị (đổi tên/ảnh, thêm/xóa
thành viên) chuyển vào màn "Quản lý nhóm" riêng chỉ admin vào được.

**Architecture:** `KhungTinNhan` (dùng chung cho chat 1-1 và nhóm) thêm 1
prop tùy chọn `onBamTieuDe` — chỉ `TrangNhom` truyền, `TrangChat` không
đổi gì. `TrangNhom` quản lý 1 state điều hướng panel
(`'khong'|'thong-tin'|'quan-ly'`) thay cho aside cố định hiện có, render
2 component panel mới tái sử dụng toàn bộ logic thêm/xóa thành viên/rời
nhóm đã có sẵn.

**Tech Stack:** React 19 + TypeScript + Vite + Vitest.

**Spec:** `docs/superpowers/specs/2026-09-14-halochat-thong-tin-nhom.md`

## Global Constraints

- Tên biến/hàm/route bằng tiếng Việt không dấu, đúng quy ước hiện có.
- Không đổi backend — `CapNhatNhom`/`TaiLenTep`/thêm-xóa-thành viên/rời nhóm đã đủ dùng.
- `TrangChat.tsx` (chat 1-1) KHÔNG được đổi hành vi — không truyền `onBamTieuDe`, header vẫn tĩnh như cũ.
- Không hardcode màu (`#fff`/hex) trong CSS mới — dùng token `var(--mau-*)` (bài học từ 2 lần lỗi dark mode ở GĐ5c/GĐ5d).
- Không làm link tham gia nhóm, không làm khu vực Ảnh/Video, không thêm vai trò admin phụ.
- Test frontend: `npm test` trong `frontend/`.

---

### Task 1: `KhungTinNhan.tsx` — header bấm được (tùy chọn)

**Files:**
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.tsx`
- Modify: `frontend/src/ThanhPhan/KhungTinNhan.css`
- Create: `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`

**Interfaces:**
- Produces: `PropsKhungTinNhan.onBamTieuDe?: () => void` — Task 4 (`TrangNhom.tsx`) dùng.

- [ ] **Step 1: Viết test cho `KhungTinNhan`** (chưa có file test nào cho component này — tạo mới)

Tạo `frontend/src/ThanhPhan/KhungTinNhan.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { KhungTinNhan } from './KhungTinNhan';

const PROPS_MAC_DINH = {
  loaiHoiThoai: 'nhom' as const,
  tenHienThi: 'Nhóm CNTT',
  phuDe: '3 thành viên',
  danhSachTinNhan: [],
  idHienTai: '1',
  dangKetNoi: true,
  dangTaiLichSu: false,
  coTheTaiThem: false,
  onTaiThemLichSuCu: () => {},
  onGuiVanBan: () => {},
  onGuiTep: () => {},
  dangTaiTep: false,
  loi: null,
};

describe('KhungTinNhan', () => {
  it('khong co onBamTieuDe: tieu de la span tinh, khong phai nut bam', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} />);

    expect(screen.getByText('Nhóm CNTT')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Nhóm CNTT/ })).not.toBeInTheDocument();
  });

  it('co onBamTieuDe: tieu de la nut bam duoc, bam goi dung ham', async () => {
    const onBamTieuDe = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onBamTieuDe={onBamTieuDe} />);

    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));

    expect(onBamTieuDe).toHaveBeenCalledTimes(1);
  });
});
```

- [ ] **Step 2: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- KhungTinNhan`
Expected: FAIL — prop `onBamTieuDe` chưa tồn tại / không có nút bấm nào trong header.

- [ ] **Step 3: Thêm prop `onBamTieuDe` vào `PropsKhungTinNhan`**

Trong `frontend/src/ThanhPhan/KhungTinNhan.tsx`, thêm vào cuối interface `PropsKhungTinNhan` (dòng 21-36):

```typescript
  onBamTieuDe?: () => void;
```

Thêm `onBamTieuDe` vào danh sách tham số destructure của hàm `KhungTinNhan` (dòng 38-41):

```typescript
export function KhungTinNhan({
  loaiHoiThoai, tenHienThi, phuDe, danhSachTinNhan, idHienTai, dangKetNoi, dangTaiLichSu,
  coTheTaiThem, onTaiThemLichSuCu, onGuiVanBan, onGuiTep, dangTaiTep, loi, onQuayLai, onBamTieuDe,
}: PropsKhungTinNhan) {
```

- [ ] **Step 4: Render có điều kiện tiêu đề bấm được**

Thay khối trong `<header className="khung-tin-nhan__tieu-de">` (dòng 75-87), thay 2 dòng:

```tsx
        <span className="khung-tin-nhan__avatar">{tenHienThi.charAt(0).toUpperCase()}</span>
        <div className="khung-tin-nhan__ten-cum">
          <span className="khung-tin-nhan__ten">{tenHienThi}</span>
          {phuDe && <span className="khung-tin-nhan__phu-de">{phuDe}</span>}
        </div>
```

thành:

```tsx
        {onBamTieuDe ? (
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
        )}
```

- [ ] **Step 5: Thêm CSS cho tiêu đề bấm được**

Thêm vào cuối `frontend/src/ThanhPhan/KhungTinNhan.css`:

```css

.khung-tin-nhan__tieu-de-bam {
  display: flex;
  align-items: center;
  gap: 12px;
  border: none;
  background: none;
  padding: 0;
  font-family: inherit;
  text-align: left;
  cursor: pointer;
}
```

- [ ] **Step 6: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- KhungTinNhan`
Expected: PASS toàn bộ 2 test.

- [ ] **Step 7: Chạy toàn bộ test + build frontend (xác nhận `TrangChat.tsx` không bị ảnh hưởng)**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/ThanhPhan/KhungTinNhan.tsx frontend/src/ThanhPhan/KhungTinNhan.css frontend/src/ThanhPhan/KhungTinNhan.test.tsx
git commit -m "Frontend: KhungTinNhan them prop onBamTieuDe (header bam duoc, tuy chon)"
```

---

### Task 2: `PanelThongTinNhom.tsx` (component mới)

**Files:**
- Create: `frontend/src/Trang/PanelThongTinNhom.tsx`
- Create: `frontend/src/Trang/PanelThongTinNhom.css`
- Create: `frontend/src/Trang/PanelThongTinNhom.test.tsx`

**Interfaces:**
- Consumes: `Avatar` (đã có, `frontend/src/ThanhPhan/Avatar.tsx`), `Nhom` (đã có, `frontend/src/KieuDuLieu.ts`).
- Produces: `PanelThongTinNhom({ nhom, laAdmin, onDong, onMoQuanLy, onRoiNhom }: PropsPanelThongTinNhom)` — Task 4 dùng.

- [ ] **Step 1: Viết test**

Tạo `frontend/src/Trang/PanelThongTinNhom.test.tsx`:

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { PanelThongTinNhom } from './PanelThongTinNhom';
import type { Nhom } from '../KieuDuLieu';

function taoNhomGiaLap(soThanhVien: number): Nhom {
  return {
    id: 'n1',
    tenNhom: 'Nhóm CNTT',
    moTa: null,
    duongDanAnhDaiDien: null,
    nguoiTaoId: '1',
    thanhVien: Array.from({ length: soThanhVien }, (_, i) => ({
      id: `${i + 1}`, tenTaiKhoan: `NguoiDung${i + 1}`, email: `nd${i + 1}@gmail.com`, choPhepTinNhanTuNguoiLa: true,
    })),
    thoiGianTao: '2026-01-01T00:00:00Z',
  };
}

describe('PanelThongTinNhom', () => {
  it('hien dung avatar, ten, so thanh vien', () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(3)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    expect(screen.getByText('Nhóm CNTT')).toBeInTheDocument();
    expect(screen.getByText('3 thành viên')).toBeInTheDocument();
  });

  it('khong phai admin: khong hien nut Chinh sua va Quan ly nhom', () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    expect(screen.queryByRole('button', { name: 'Chỉnh sửa' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Quản lý nhóm' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rời nhóm' })).toBeInTheDocument();
  });

  it('la admin: hien nut Chinh sua va Quan ly nhom, nut roi doi thanh Giai tan nhom', async () => {
    const onMoQuanLy = vi.fn();
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin onDong={() => {}} onMoQuanLy={onMoQuanLy} onRoiNhom={() => {}} />);

    expect(screen.getByRole('button', { name: 'Giải tán nhóm' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Chỉnh sửa' }));
    expect(onMoQuanLy).toHaveBeenCalledTimes(1);
  });

  it('hien toi da 8 avatar thanh vien mac dinh, con lai an sau nut', () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(10)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    expect(screen.getAllByText(/^[A-Z]$/).length).toBeLessThanOrEqual(8);
    expect(screen.getByRole('button', { name: 'Xem tất cả thành viên' })).toBeInTheDocument();
  });

  it('bam Xem tat ca thanh vien hien het avatar, an nut di', async () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(10)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    await userEvent.click(screen.getByRole('button', { name: 'Xem tất cả thành viên' }));

    expect(screen.queryByRole('button', { name: 'Xem tất cả thành viên' })).not.toBeInTheDocument();
  });

  it('bam Roi nhom goi onRoiNhom', async () => {
    const onRoiNhom = vi.fn();
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={onRoiNhom} />);

    await userEvent.click(screen.getByRole('button', { name: 'Rời nhóm' }));

    expect(onRoiNhom).toHaveBeenCalledTimes(1);
  });

  it('bam Nhan tin hoac nut dong goi onDong', async () => {
    const onDong = vi.fn();
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={onDong} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    await userEvent.click(screen.getByRole('button', { name: 'Nhắn tin' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });
});
```

- [ ] **Step 2: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- PanelThongTinNhom`
Expected: FAIL — không tìm thấy module `./PanelThongTinNhom`.

- [ ] **Step 3: Cài đặt `PanelThongTinNhom.css`**

Tạo `frontend/src/Trang/PanelThongTinNhom.css`:

```css
.panel-thong-tin-nhom {
  flex-shrink: 0;
  width: 300px;
  min-height: 100vh;
  background: var(--mau-nen-the);
  border-left: 1px solid var(--mau-vien);
  padding: 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  text-align: center;
  overflow-y: auto;
}

.panel-thong-tin-nhom__dong {
  align-self: flex-end;
  border: none;
  background: none;
  font-size: 20px;
  cursor: pointer;
  color: var(--mau-chu-phu);
}

.panel-thong-tin-nhom__ten {
  margin: 8px 0 0;
}

.panel-thong-tin-nhom__so-thanh-vien {
  color: var(--mau-chu-phu);
  font-size: 13px;
  margin: 0 0 8px;
}

.panel-thong-tin-nhom__hang-nut {
  display: flex;
  gap: 8px;
  width: 100%;
}

.panel-thong-tin-nhom__hang-nut .nut-chinh,
.panel-thong-tin-nhom__hang-nut .nut-phu {
  flex: 1;
  width: auto;
}

.panel-thong-tin-nhom__phan {
  width: 100%;
  text-align: left;
  margin-top: 16px;
}

.panel-thong-tin-nhom__phan h4 {
  font-size: 13px;
  color: var(--mau-chu-phu);
  margin: 0 0 8px;
}

.panel-thong-tin-nhom__avatar-hang {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.panel-thong-tin-nhom__nut-them {
  width: 32px;
  height: 32px;
  border-radius: 999px;
  border: 1px solid var(--mau-vien);
  background: var(--mau-nen-tren);
  color: var(--mau-chu-phu);
  cursor: pointer;
  font-family: inherit;
}

.panel-thong-tin-nhom__hang-lien-ket {
  width: 100%;
  text-align: left;
  margin-top: 16px;
  padding: 10px 0;
  border: none;
  border-top: 1px solid var(--mau-vien);
  background: none;
  color: var(--mau-chinh-dam);
  font-family: inherit;
  font-weight: 600;
  cursor: pointer;
}
```

- [ ] **Step 4: Cài đặt `PanelThongTinNhom.tsx`**

Tạo `frontend/src/Trang/PanelThongTinNhom.tsx`:

```tsx
import { useState } from 'react';
import { Avatar } from '../ThanhPhan/Avatar';
import type { Nhom } from '../KieuDuLieu';
import './PanelThongTinNhom.css';

const SO_AVATAR_HIEN_MAC_DINH = 8;

interface PropsPanelThongTinNhom {
  nhom: Nhom;
  laAdmin: boolean;
  onDong: () => void;
  onMoQuanLy: () => void;
  onRoiNhom: () => void;
}

export function PanelThongTinNhom({ nhom, laAdmin, onDong, onMoQuanLy, onRoiNhom }: PropsPanelThongTinNhom) {
  const [hienHetThanhVien, setHienHetThanhVien] = useState(false);
  const thanhVienHien = hienHetThanhVien ? nhom.thanhVien : nhom.thanhVien.slice(0, SO_AVATAR_HIEN_MAC_DINH);
  const conAnDi = nhom.thanhVien.length - SO_AVATAR_HIEN_MAC_DINH;

  return (
    <aside className="panel-thong-tin-nhom">
      <button className="panel-thong-tin-nhom__dong" onClick={onDong} aria-label="Đóng">×</button>

      <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" />
      <h3 className="panel-thong-tin-nhom__ten">{nhom.tenNhom}</h3>
      <p className="panel-thong-tin-nhom__so-thanh-vien">{nhom.thanhVien.length} thành viên</p>

      <div className="panel-thong-tin-nhom__hang-nut">
        {laAdmin && (
          <button className="nut-phu" onClick={onMoQuanLy}>Chỉnh sửa</button>
        )}
        <button className="nut-chinh" onClick={onDong}>Nhắn tin</button>
      </div>

      <div className="panel-thong-tin-nhom__phan">
        <h4>Thành viên</h4>
        <div className="panel-thong-tin-nhom__avatar-hang">
          {thanhVienHien.map((tv) => (
            <Avatar key={tv.id} id={tv.id} ten={tv.tenTaiKhoan} kichThuoc="nho" />
          ))}
          {!hienHetThanhVien && conAnDi > 0 && (
            <button className="panel-thong-tin-nhom__nut-them" onClick={() => setHienHetThanhVien(true)} aria-label="Xem tất cả thành viên">
              •••
            </button>
          )}
        </div>
      </div>

      {laAdmin && (
        <button className="panel-thong-tin-nhom__hang-lien-ket" onClick={onMoQuanLy}>
          Quản lý nhóm
        </button>
      )}

      <button className="trang-nhom__nut-roi" onClick={onRoiNhom}>
        {laAdmin ? 'Giải tán nhóm' : 'Rời nhóm'}
      </button>
    </aside>
  );
}
```

Class `trang-nhom__nut-roi` cố ý dùng lại nguyên style đã có trong
`TrangNhom.css` (không tạo class mới) — Task 4 sẽ sửa dark mode cho class
này 1 lần, áp dụng cho cả 2 nơi dùng nó (panel này + `PanelQuanLyNhom`
không dùng nút này nên không liên quan).

- [ ] **Step 5: Chạy `nut-phu`/`nut-chinh` — xác nhận các class dùng chung đã tồn tại**

Run: `grep -n "^\.nut-chinh\|^\.nut-phu" frontend/src/index.css frontend/src/Trang/TrangBanBe.css`
Expected: `.nut-chinh` có trong `index.css`; `.trang-ban-be__nut-phu` có trong `TrangBanBe.css` nhưng KHÔNG có `.nut-phu` global — nghĩa là class `.nut-phu` dùng ở Step 4 hiện CHƯA tồn tại global. Thêm vào cuối `frontend/src/index.css`:

```css

.nut-phu {
  border: 1px solid var(--mau-vien);
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
  border-radius: var(--ban-kinh-o);
  padding: 10px 16px;
  font-family: inherit;
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
}
```

(Đây là class dùng chung toàn app, đặt ở `index.css` cạnh `.nut-chinh` đã
có — không đặt trong `PanelThongTinNhom.css` để tránh lặp lại nếu
`PanelQuanLyNhom.tsx` ở Task 3 cũng cần dùng.)

- [ ] **Step 6: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- PanelThongTinNhom`
Expected: PASS toàn bộ 7 test.

- [ ] **Step 7: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 8: Commit**

```bash
git add frontend/src/Trang/PanelThongTinNhom.tsx frontend/src/Trang/PanelThongTinNhom.css frontend/src/Trang/PanelThongTinNhom.test.tsx frontend/src/index.css
git commit -m "Frontend: them component PanelThongTinNhom"
```

---

### Task 3: `PanelQuanLyNhom.tsx` (component mới, chỉ admin)

**Files:**
- Create: `frontend/src/Trang/PanelQuanLyNhom.tsx`
- Create: `frontend/src/Trang/PanelQuanLyNhom.css`
- Create: `frontend/src/Trang/PanelQuanLyNhom.test.tsx`

**Interfaces:**
- Consumes: `CapNhatNhom`, `TaiLenTep`, `LoiGoiApi` (đã có, `frontend/src/DichVuApi.ts`), `useXacThuc` (đã có), `Avatar` (đã có), `.nut-phu`/`.nut-chinh` (Task 2 đã thêm `.nut-phu` vào `index.css`).
- Produces: `PanelQuanLyNhom({ nhom, tatCaNguoiDung, onDong, onThemThanhVien, onXoaThanhVien, onCapNhatNhom }: PropsPanelQuanLyNhom)` — Task 4 dùng.

- [ ] **Step 1: Viết test**

Tạo `frontend/src/Trang/PanelQuanLyNhom.test.tsx`:

```tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PanelQuanLyNhom } from './PanelQuanLyNhom';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';
import type { Nhom } from '../KieuDuLieu';

const NHOM_GIA_LAP: Nhom = {
  id: 'n1',
  tenNhom: 'Nhóm CNTT',
  moTa: null,
  duongDanAnhDaiDien: null,
  nguoiTaoId: '1',
  thanhVien: [
    { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
    { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
  ],
  thoiGianTao: '2026-01-01T00:00:00Z',
};

const TAT_CA_NGUOI_DUNG = [
  { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
  { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
  { id: '3', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
];

function renderPanel(propsGhiDe: Partial<Parameters<typeof PanelQuanLyNhom>[0]> = {}) {
  return render(
    <NhaCungCapXacThuc>
      <PanelQuanLyNhom
        nhom={NHOM_GIA_LAP}
        tatCaNguoiDung={TAT_CA_NGUOI_DUNG}
        onDong={() => {}}
        onThemThanhVien={() => {}}
        onXoaThanhVien={() => {}}
        onCapNhatNhom={() => {}}
        {...propsGhiDe}
      />
    </NhaCungCapXacThuc>,
  );
}

describe('PanelQuanLyNhom', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('hien dung danh sach thanh vien, danh dau Admin cho nguoi tao', () => {
    renderPanel();

    expect(screen.getByText('NguyenAn (Admin)')).toBeInTheDocument();
    expect(screen.getByText('TranBinh')).toBeInTheDocument();
  });

  it('khong hien nut xoa cho nguoi tao nhom', () => {
    renderPanel();

    expect(screen.queryByRole('button', { name: 'Xóa NguyenAn' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Xóa TranBinh' })).toBeInTheDocument();
  });

  it('bam xoa 1 thanh vien goi onXoaThanhVien voi dung id', async () => {
    const onXoaThanhVien = vi.fn();
    renderPanel({ onXoaThanhVien });

    await userEvent.click(screen.getByRole('button', { name: 'Xóa TranBinh' }));

    expect(onXoaThanhVien).toHaveBeenCalledWith('2');
  });

  it('chon 1 nguoi trong dropdown them thanh vien goi onThemThanhVien', async () => {
    const onThemThanhVien = vi.fn();
    renderPanel({ onThemThanhVien });

    await userEvent.selectOptions(screen.getByRole('combobox'), 'LeC');

    expect(onThemThanhVien).toHaveBeenCalledWith('3');
  });

  it('doi ten nhom goi CapNhatNhom voi ten moi', async () => {
    vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({ ...NHOM_GIA_LAP, tenNhom: 'Nhóm mới' });
    const onCapNhatNhom = vi.fn();
    renderPanel({ onCapNhatNhom });

    const oTen = screen.getByDisplayValue('Nhóm CNTT');
    await userEvent.clear(oTen);
    await userEvent.type(oTen, 'Nhóm mới');
    await userEvent.click(screen.getByRole('button', { name: 'Lưu' }));

    await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', 'n1', 'Nhóm mới', null, null));
    await waitFor(() => expect(onCapNhatNhom).toHaveBeenCalledWith({ ...NHOM_GIA_LAP, tenNhom: 'Nhóm mới' }));
  });

  it('nut Luu bi disabled khi ten khong doi', () => {
    renderPanel();

    expect(screen.getByRole('button', { name: 'Lưu' })).toBeDisabled();
  });

  it('chon file anh goi TaiLenTep roi CapNhatNhom voi duongDanFile tra ve', async () => {
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({
      duongDanFile: '/uploads/anh-moi.png', tenFileGoc: 'anh.png', kichThuocFile: 1000, loaiFile: 'image/png',
    });
    vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({ ...NHOM_GIA_LAP, duongDanAnhDaiDien: '/uploads/anh-moi.png' });
    const onCapNhatNhom = vi.fn();
    renderPanel({ onCapNhatNhom });

    const tep = new File(['noi-dung'], 'anh.png', { type: 'image/png' });
    const oChonTep = document.querySelector('input[type="file"]') as HTMLInputElement;
    await userEvent.upload(oChonTep, tep);

    await waitFor(() => expect(DichVuApi.TaiLenTep).toHaveBeenCalledWith('token-gia-lap', tep));
    await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', 'n1', 'Nhóm CNTT', null, '/uploads/anh-moi.png'));
    await waitFor(() => expect(onCapNhatNhom).toHaveBeenCalledWith({ ...NHOM_GIA_LAP, duongDanAnhDaiDien: '/uploads/anh-moi.png' }));
  });

  it('bam nut dong goi onDong', async () => {
    const onDong = vi.fn();
    renderPanel({ onDong });

    await userEvent.click(screen.getByRole('button', { name: 'Quay lại' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });
});
```

- [ ] **Step 2: Chạy test để xác nhận thất bại**

Run: `cd frontend && npm test -- PanelQuanLyNhom`
Expected: FAIL — không tìm thấy module `./PanelQuanLyNhom`.

- [ ] **Step 3: Cài đặt `PanelQuanLyNhom.css`**

Tạo `frontend/src/Trang/PanelQuanLyNhom.css`:

```css
.panel-quan-ly-nhom {
  flex-shrink: 0;
  width: 320px;
  min-height: 100vh;
  background: var(--mau-nen-the);
  border-left: 1px solid var(--mau-vien);
  padding: 24px;
  overflow-y: auto;
  box-sizing: border-box;
}

.panel-quan-ly-nhom__dong {
  border: none;
  background: none;
  font-size: 18px;
  cursor: pointer;
  color: var(--mau-chu-phu);
  padding: 0 0 12px;
}

.panel-quan-ly-nhom h3 {
  margin: 0 0 16px;
}

.panel-quan-ly-nhom__doi-anh {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  margin-bottom: 20px;
}

.panel-quan-ly-nhom__nut-doi-anh {
  display: inline-block;
  cursor: pointer;
  font-size: 13px;
}

.panel-quan-ly-nhom__nhan {
  display: block;
  font-size: 13px;
  color: var(--mau-chu-phu);
  margin-bottom: 16px;
}

.panel-quan-ly-nhom__hang-ten {
  display: flex;
  gap: 8px;
  margin-top: 6px;
}

.panel-quan-ly-nhom__hang-ten input {
  flex: 1;
  padding: 8px 12px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  font-size: 14px;
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
}

.panel-quan-ly-nhom__hang-ten .nut-chinh {
  width: auto;
  padding: 8px 16px;
}

.panel-quan-ly-nhom__phan {
  margin-top: 8px;
}

.panel-quan-ly-nhom__phan h4 {
  font-size: 13px;
  color: var(--mau-chu-phu);
  margin: 0 0 8px;
}

.panel-quan-ly-nhom__ds-thanh-vien {
  list-style: none;
  margin: 0 0 12px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.panel-quan-ly-nhom__ds-thanh-vien li {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
}

.panel-quan-ly-nhom__ds-thanh-vien li span {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.panel-quan-ly-nhom__ds-thanh-vien li button {
  border: none;
  background: none;
  color: var(--mau-chu-phu);
  cursor: pointer;
  font-size: 16px;
  flex-shrink: 0;
}

.panel-quan-ly-nhom__them-thanh-vien {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  font-family: inherit;
  background: var(--mau-nen-the);
  color: var(--mau-chu-dam);
}
```

- [ ] **Step 4: Cài đặt `PanelQuanLyNhom.tsx`**

Tạo `frontend/src/Trang/PanelQuanLyNhom.tsx`:

```tsx
import { useState } from 'react';
import { CapNhatNhom, TaiLenTep, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { Avatar } from '../ThanhPhan/Avatar';
import type { Nhom, NguoiDungTomTat } from '../KieuDuLieu';
import './PanelQuanLyNhom.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;

interface PropsPanelQuanLyNhom {
  nhom: Nhom;
  tatCaNguoiDung: NguoiDungTomTat[];
  onDong: () => void;
  onThemThanhVien: (userId: string) => void;
  onXoaThanhVien: (userId: string) => void;
  onCapNhatNhom: (nhomMoi: Nhom) => void;
}

export function PanelQuanLyNhom({
  nhom, tatCaNguoiDung, onDong, onThemThanhVien, onXoaThanhVien, onCapNhatNhom,
}: PropsPanelQuanLyNhom) {
  const { token } = useXacThuc();
  const [tenNhom, setTenNhom] = useState(nhom.tenNhom);
  const [dangLuu, setDangLuu] = useState(false);
  const [dangTaiAnh, setDangTaiAnh] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  function luuTen() {
    if (!token || !tenNhom.trim() || tenNhom === nhom.tenNhom) return;
    setDangLuu(true);
    CapNhatNhom(token, nhom.id, tenNhom.trim(), nhom.moTa, nhom.duongDanAnhDaiDien)
      .then(onCapNhatNhom)
      .catch((loiBat) => setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi tên nhóm thất bại.'))
      .finally(() => setDangLuu(false));
  }

  function doiAnh(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoi('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoi(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setDangTaiAnh(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => CapNhatNhom(token, nhom.id, nhom.tenNhom, nhom.moTa, daTaiLen.duongDanFile))
      .then(onCapNhatNhom)
      .catch(() => setLoi('Đổi ảnh đại diện thất bại.'))
      .finally(() => setDangTaiAnh(false));
  }

  return (
    <aside className="panel-quan-ly-nhom">
      <button className="panel-quan-ly-nhom__dong" onClick={onDong} aria-label="Quay lại">← Quay lại</button>
      <h3>Quản lý nhóm</h3>

      {loi && <p className="thong-bao-loi" role="alert">{loi}</p>}

      <div className="panel-quan-ly-nhom__doi-anh">
        <Avatar id={nhom.id} ten={nhom.tenNhom} kichThuoc="lon" />
        <label className="nut-phu panel-quan-ly-nhom__nut-doi-anh">
          {dangTaiAnh ? 'Đang tải...' : 'Đổi ảnh đại diện'}
          <input
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            hidden
            disabled={dangTaiAnh}
            onChange={(su) => {
              const tep = su.target.files?.[0];
              if (tep) doiAnh(tep);
              su.target.value = '';
            }}
          />
        </label>
      </div>

      <label className="panel-quan-ly-nhom__nhan">
        Tên nhóm
        <div className="panel-quan-ly-nhom__hang-ten">
          <input type="text" value={tenNhom} onChange={(su) => setTenNhom(su.target.value)} disabled={dangLuu} />
          <button className="nut-chinh" onClick={luuTen} disabled={dangLuu || !tenNhom.trim() || tenNhom === nhom.tenNhom}>
            Lưu
          </button>
        </div>
      </label>

      <div className="panel-quan-ly-nhom__phan">
        <h4>Thành viên ({nhom.thanhVien.length})</h4>
        <ul className="panel-quan-ly-nhom__ds-thanh-vien">
          {nhom.thanhVien.map((tv) => (
            <li key={tv.id}>
              <Avatar id={tv.id} ten={tv.tenTaiKhoan} kichThuoc="nho" />
              <span>{tv.tenTaiKhoan}{tv.id === nhom.nguoiTaoId ? ' (Admin)' : ''}</span>
              {tv.id !== nhom.nguoiTaoId && (
                <button onClick={() => onXoaThanhVien(tv.id)} aria-label={`Xóa ${tv.tenTaiKhoan}`}>×</button>
              )}
            </li>
          ))}
        </ul>
        <select
          className="panel-quan-ly-nhom__them-thanh-vien"
          onChange={(su) => { if (su.target.value) onThemThanhVien(su.target.value); su.target.value = ''; }}
        >
          <option value="">+ Thêm thành viên...</option>
          {tatCaNguoiDung
            .filter((nd) => !nhom.thanhVien.some((tv) => tv.id === nd.id))
            .map((nd) => (
              <option key={nd.id} value={nd.id}>{nd.tenTaiKhoan}</option>
            ))}
        </select>
      </div>
    </aside>
  );
}
```

- [ ] **Step 5: Chạy test để xác nhận PASS**

Run: `cd frontend && npm test -- PanelQuanLyNhom`
Expected: PASS toàn bộ 9 test.

- [ ] **Step 6: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/Trang/PanelQuanLyNhom.tsx frontend/src/Trang/PanelQuanLyNhom.css frontend/src/Trang/PanelQuanLyNhom.test.tsx
git commit -m "Frontend: them component PanelQuanLyNhom (doi ten/anh, them/xoa thanh vien)"
```

---

### Task 4: `TrangNhom.tsx` — nối các panel mới, xóa panel cũ, sửa dark mode

**Files:**
- Modify: `frontend/src/Trang/TrangNhom.tsx`
- Modify: `frontend/src/Trang/TrangNhom.css`
- Modify: `frontend/src/Trang/TrangNhom.test.tsx`

**Interfaces:**
- Consumes: `KhungTinNhan.onBamTieuDe` (Task 1), `PanelThongTinNhom` (Task 2), `PanelQuanLyNhom` (Task 3).

- [ ] **Step 1: Đọc `frontend/src/Trang/TrangNhom.test.tsx` hiện tại trước khi sửa** — file này dùng `localStorage.setItem('haloChatToken', 'token-gia-lap')` (chuỗi KHÔNG giải mã được thành JWT thật), khiến `nguoiDungHienTai` luôn là `null` và `idHienTai` luôn là `''` trong mọi test hiện có — none trong số đó test tính năng admin. Với các test MỚI ở Step 6 cần `laAdmin = true`, PHẢI đổi cách set token sang dạng giải mã được (xem mẫu `TrangChat.test.tsx` dòng 74-75: `btoa(JSON.stringify({ sub: '1', ... }))` ghép thành `header.<phan>.chuky`) — chỉ áp dụng cho các test mới cần quyền admin, giữ nguyên `beforeEach` hiện có cho các test cũ không cần đổi gì.

- [ ] **Step 2: Thêm state `panelDangMo`, xóa import/logic không còn dùng của aside cũ**

Trong `frontend/src/Trang/TrangNhom.tsx`, thêm import ở đầu file (sau import `Avatar`):

```typescript
import { PanelThongTinNhom } from './PanelThongTinNhom';
import { PanelQuanLyNhom } from './PanelQuanLyNhom';
```

Thêm state mới sau dòng `const [tuKhoaTimKiem, setTuKhoaTimKiem] = useState('');`:

```typescript
  const [panelDangMo, setPanelDangMo] = useState<'khong' | 'thong-tin' | 'quan-ly'>('khong');
```

- [ ] **Step 3: Đóng panel khi đổi nhóm đang chọn**

Sửa hàm chọn nhóm trong sidebar — tìm dòng `onClick={() => setNhomDangChonId(n.id)}` (trong `.map((n) => ...)` của `trang-nhom__danh-sach`), thay bằng:

```tsx
                onClick={() => {
                  setNhomDangChonId(n.id);
                  ketNoi?.invoke('DanhDauDaDoc', null, n.id).catch(() => {});
                  setPanelDangMo('khong');
                }}
```

(Giữ nguyên dòng `ketNoi?.invoke('DanhDauDaDoc', null, n.id).catch(() => {});` đã có từ GĐ5c Task 8 — chỉ thêm `setPanelDangMo('khong')` vào cùng handler.)

- [ ] **Step 4: Xóa aside "Thành viên" cũ, truyền `onBamTieuDe`, render 2 panel mới**

Thay toàn bộ khối JSX từ `<KhungTinNhan` tới hết `</aside>` (bên trong `{nhomDangChon && (<div className="trang-nhom__khung-phai">...)}`) — nguyên văn khối hiện tại:

```tsx
          <KhungTinNhan
            loaiHoiThoai="nhom"
            tenHienThi={nhomDangChon.tenNhom}
            phuDe={`${nhomDangChon.thanhVien.length} thành viên`}
            danhSachTinNhan={tinNhanTheoNhom[nhomDangChon.id] ?? []}
            idHienTai={idHienTai}
            dangKetNoi={dangKetNoi}
            dangTaiLichSu={dangTaiLichSu}
            coTheTaiThem
            onTaiThemLichSuCu={taiThemLichSuCu}
            onGuiVanBan={guiTinNhanVanBan}
            onGuiTep={guiTep}
            dangTaiTep={dangTaiTep}
            loi={loi}
            onQuayLai={() => setNhomDangChonId(null)}
          />
          <aside className="trang-nhom__thong-tin">
            <h3>Thành viên</h3>
            <ul className="trang-nhom__ds-thanh-vien">
              {nhomDangChon.thanhVien.map((tv) => (
                <li key={tv.id}>
                  <span>{tv.tenTaiKhoan}{tv.id === nhomDangChon.nguoiTaoId ? ' (Admin)' : ''}</span>
                  {laAdmin && tv.id !== idHienTai && (
                    <button onClick={() => xoaThanhVien(tv.id)} aria-label={`Xóa ${tv.tenTaiKhoan}`}>×</button>
                  )}
                </li>
              ))}
            </ul>
            {laAdmin && (
              <select onChange={(su) => { if (su.target.value) themThanhVien(su.target.value); su.target.value = ''; }}>
                <option value="">+ Thêm thành viên...</option>
                {tatCaNguoiDung
                  .filter((nd) => !nhomDangChon.thanhVien.some((tv) => tv.id === nd.id))
                  .map((nd) => (
                    <option key={nd.id} value={nd.id}>{nd.tenTaiKhoan}</option>
                  ))}
              </select>
            )}
            <button className="trang-nhom__nut-roi" onClick={roiNhom}>
              {laAdmin ? 'Giải tán nhóm' : 'Rời nhóm'}
            </button>
          </aside>
```

thành:

```tsx
          <KhungTinNhan
            loaiHoiThoai="nhom"
            tenHienThi={nhomDangChon.tenNhom}
            phuDe={`${nhomDangChon.thanhVien.length} thành viên`}
            danhSachTinNhan={tinNhanTheoNhom[nhomDangChon.id] ?? []}
            idHienTai={idHienTai}
            dangKetNoi={dangKetNoi}
            dangTaiLichSu={dangTaiLichSu}
            coTheTaiThem
            onTaiThemLichSuCu={taiThemLichSuCu}
            onGuiVanBan={guiTinNhanVanBan}
            onGuiTep={guiTep}
            dangTaiTep={dangTaiTep}
            loi={loi}
            onQuayLai={() => setNhomDangChonId(null)}
            onBamTieuDe={() => setPanelDangMo('thong-tin')}
          />
          {panelDangMo === 'thong-tin' && (
            <PanelThongTinNhom
              nhom={nhomDangChon}
              laAdmin={laAdmin}
              onDong={() => setPanelDangMo('khong')}
              onMoQuanLy={() => setPanelDangMo('quan-ly')}
              onRoiNhom={roiNhom}
            />
          )}
          {panelDangMo === 'quan-ly' && laAdmin && (
            <PanelQuanLyNhom
              nhom={nhomDangChon}
              tatCaNguoiDung={tatCaNguoiDung}
              onDong={() => setPanelDangMo('thong-tin')}
              onThemThanhVien={themThanhVien}
              onXoaThanhVien={xoaThanhVien}
              onCapNhatNhom={(nhomMoi) => setDanhSachNhom((truoc) => truoc.map((n) => (n.id === nhomMoi.id ? nhomMoi : n)))}
            />
          )}
```

- [ ] **Step 5: Sửa dark mode cho `.trang-nhom__nut-roi`**

Trong `frontend/src/Trang/TrangNhom.css`, tìm rule `.trang-nhom__nut-roi` (dòng 129-140), đổi dòng:
```css
  background: #fff;
```
thành:
```css
  background: var(--mau-nen-the);
```

- [ ] **Step 6: Thêm test mới cho `TrangNhom.test.tsx`**

Thêm vào cuối `frontend/src/Trang/TrangNhom.test.tsx` (trước dấu `}` đóng `describe`):

```tsx

  it('bam vao tieu de header mo PanelThongTinNhom', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [
        { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
      ], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));

    expect(screen.getByText('1 thành viên')).toBeInTheDocument();
  });

  it('la admin (nguoiTaoId trung idHienTai): thay Chinh sua va Quan ly nhom trong PanelThongTinNhom', async () => {
    const phanThanToken = btoa(JSON.stringify({ sub: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${phanThanToken}.chuky`);
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [
        { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
      ], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));

    expect(await screen.findByRole('button', { name: 'Chỉnh sửa' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Quản lý nhóm' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Giải tán nhóm' })).toBeInTheDocument();
  });

  it('bam Chinh sua chuyen sang PanelQuanLyNhom', async () => {
    const phanThanToken = btoa(JSON.stringify({ sub: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${phanThanToken}.chuky`);
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [
        { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
      ], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));
    await userEvent.click(await screen.findByRole('button', { name: 'Chỉnh sửa' }));

    expect(await screen.findByText('Quản lý nhóm')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Nhóm CNTT')).toBeInTheDocument();
  });

  it('doi nhom dang chon thi dong panel dang mo', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
      { id: 'n2', tenNhom: 'Nhóm Toán', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));
    expect(await screen.findByText('0 thành viên')).toBeInTheDocument();

    await userEvent.click(screen.getByText('Nhóm Toán'));

    expect(screen.queryByText('0 thành viên')).not.toBeInTheDocument();
  });
```

Thêm `btoa` không cần import (là hàm global của trình duyệt/jsdom, đã dùng
sẵn theo mẫu `TrangChat.test.tsx`).

- [ ] **Step 7: Chạy test riêng cho `TrangNhom`**

Run: `cd frontend && npm test -- TrangNhom`
Expected: PASS toàn bộ (9 test cũ + 4 test mới = 13 test).

- [ ] **Step 8: Chạy toàn bộ test + build frontend**

Run: `cd frontend && npm test && npm run build`
Expected: PASS toàn bộ, build thành công.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/Trang/TrangNhom.tsx frontend/src/Trang/TrangNhom.css frontend/src/Trang/TrangNhom.test.tsx
git commit -m "Frontend: TrangNhom dung PanelThongTinNhom/PanelQuanLyNhom thay panel Thanh vien cu, sua dark mode nut roi nhom"
```

---

## Ghi chú cho người thực thi plan

- Task 1-3 độc lập với nhau, có thể làm theo bất kỳ thứ tự nào trong 3 task
  đó, nhưng Task 4 PHẢI làm sau cùng vì nó dùng cả 3.
- Task 4 Step 1 nhắc kỹ về vấn đề token giả không giải mã được trong test
  hiện có — đọc kỹ trước khi viết test mới, đừng đoán.
