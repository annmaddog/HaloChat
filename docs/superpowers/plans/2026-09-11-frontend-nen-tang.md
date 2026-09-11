# HaloChat Frontend Nền Tảng — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng React + TypeScript (Vite) trong `frontend/`, hoàn thiện GĐ4 của tài liệu: trang đăng ký, trang đăng nhập, danh sách người dùng (route được bảo vệ) — gọi thật vào backend GĐ3 đã hoàn thành (`backend/HaloChat.Api`, đang chạy tại `http://localhost:5231`) — với giao diện mang thương hiệu HaloChat (logo, màu sắc, font) theo đúng ảnh thiết kế mẫu đã duyệt.

**Architecture:** Vite + React 19 + TypeScript. Một lớp `DichVuApi` (fetch thuần) bọc 3 endpoint đã có (`dang-ky`, `dang-nhap`, `nguoidung`). Một `NguCanhXacThuc` (React Context) giữ JWT trong `localStorage`. `react-router-dom` điều hướng giữa 3 trang, với `TuyenDuongRieng` chặn truy cập trang danh sách người dùng khi chưa đăng nhập. Một bộ thành phần giao diện dùng chung (`KhungXacThuc`, `TruongNhap`, `BieuTuong`) dựng khung thẻ trắng bo tròn + tab chuyển đổi đăng nhập/đăng ký đúng theo ảnh mẫu, dùng chung giữa 2 trang xác thực — tránh lặp code. Test bằng Vitest + React Testing Library (không cần chạy backend thật để chạy test).

**Tech Stack:** Vite, React 19 + TypeScript, react-router-dom, Vitest, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, jsdom, font Google Fonts "Be Vietnam Pro".

**Spec:** `docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md` (đặc biệt mục "Thương hiệu" vừa thêm)

## Global Constraints

- Toàn bộ code frontend nằm trong `frontend/` (spec §3), thư mục gốc độc lập với `backend/`.
- API backend đã có sẵn 3 endpoint (đã build, test, review xong ở plan trước):
  - `POST /api/nguoidung/dang-ky` — body `{tenTaiKhoan, email, matKhau}` → 200 `{thongBao}` hoặc 409 `{thongBao}`.
  - `POST /api/nguoidung/dang-nhap` — body `{tenDangNhap, matKhau}` → 200 `{token}` hoặc 401 `{thongBao}`.
  - `GET /api/nguoidung` (yêu cầu header `Authorization: Bearer <token>`) → 200 `[{id, tenTaiKhoan, email}, ...]` hoặc 401.
  - Base URL cố định cho môi trường dev: `http://localhost:5231/api` (đúng theo `backend/HaloChat.Api/Properties/launchSettings.json`, profile `http`).
  - CORS backend đã cho phép origin `http://localhost:5173` (mặc định của Vite dev server) — không cần sửa backend.
- Quy ước đặt tên: biến camelCase / hàm & component PascalCase, tiếng Việt không dấu; giữ nguyên thuật ngữ kỹ thuật quen thuộc (API, JWT...). Tên thư mục/file theo vai trò: `Trang/` (pages), `ThanhPhan/` (components dùng chung), `NguCanh/` (React Context).
- JWT lưu ở `localStorage` (đơn giản hóa hợp lý cho phạm vi đồ án — không phải sản phẩm thực tế nhiều thiết bị).
- Không dùng thư viện quản lý state ngoài (Redux/Zustand...) — React Context + `useState` là đủ cho phạm vi GĐ4 (YAGNI).
- Thương hiệu: tên hiển thị **"HaloChat"**, logo tại `assets/halochat-logo.png` (gốc repo, đã commit), màu chính xanh dương gradient `#2F7BF6` → `#1A56C4`, chữ đậm `#101B33`, font "Be Vietnam Pro" (Google Fonts). Bố cục trang đăng nhập/đăng ký: thẻ trắng bo tròn giữa màn hình, logo + tên + khẩu hiệu ở đầu thẻ, tab chuyển đổi Đăng nhập/Đăng ký, ô nhập có icon, nút submit gradient — đúng theo ảnh mẫu đã duyệt trong hội thoại brainstorming (không phải phạm vi kết bạn/nhóm — đó là GĐ5, ngoài phạm vi plan này).

---

## Task 1: Scaffold frontend (Vite + React + TypeScript + Vitest) + logo

**Files:**
- Create: `frontend/` (toàn bộ do `npm create vite` sinh ra)
- Modify: `frontend/vite.config.ts`
- Create: `frontend/src/thietLapKiemThu.ts`
- Modify: `frontend/package.json` (thêm script `test`)
- Modify: `frontend/index.html` (tiêu đề, favicon, `lang`)
- Create: `frontend/public/halochat-logo.png` (copy từ `assets/halochat-logo.png` ở gốc repo)
- Delete: `frontend/public/vite.svg` (favicon mặc định, không dùng)

**Interfaces:**
- Consumes: `assets/halochat-logo.png` (đã có sẵn ở gốc repo, xem spec mục "Thương hiệu").
- Produces: dev server chạy được tại `http://localhost:5173`; `npm run test --prefix frontend` chạy được (chưa có test nào — pass nhờ `passWithNoTests`); `npm run build --prefix frontend` biên dịch TypeScript sạch; file logo có sẵn tại `frontend/public/halochat-logo.png` (dùng bởi Task 4 qua đường dẫn `/halochat-logo.png`, không cần import JS). Các task sau tạo file trong `frontend/src/`.

- [ ] **Bước 1: Scaffold project bằng Vite**

```bash
npm create vite@latest frontend -- --template react-ts
```

- [ ] **Bước 2: Cài dependency**

```bash
npm install --prefix frontend
npm install --prefix frontend react-router-dom
npm install --prefix frontend -D vitest @testing-library/react @testing-library/jest-dom @testing-library/user-event jsdom
```

- [ ] **Bước 3: Viết lại toàn bộ `frontend/vite.config.ts`**

```ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/thietLapKiemThu.ts'],
    passWithNoTests: true,
  },
});
```

- [ ] **Bước 4: Tạo file thiết lập test `frontend/src/thietLapKiemThu.ts`**

```ts
import '@testing-library/jest-dom/vitest';
```

- [ ] **Bước 5: Thêm script `test` vào `frontend/package.json`**

Mở `frontend/package.json`, trong object `"scripts"`, thêm dòng sau (giữ nguyên các script `dev`/`build`/`lint`/`preview` đã có sẵn do Vite sinh ra):

```json
"test": "vitest run"
```

- [ ] **Bước 6: Copy logo vào `frontend/public/`, xóa favicon mặc định**

```bash
cp assets/halochat-logo.png frontend/public/halochat-logo.png
rm -f frontend/public/vite.svg
```

- [ ] **Bước 7: Viết lại toàn bộ `frontend/index.html`**

```html
<!doctype html>
<html lang="vi">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/png" href="/halochat-logo.png" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>HaloChat</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

- [ ] **Bước 8: Xác nhận build và test chạy được**

Run: `npm run build --prefix frontend`
Expected: biên dịch thành công, không lỗi TypeScript.

Run: `npm run test --prefix frontend`
Expected: pass (chưa có file test nào, `passWithNoTests: true` nên không báo lỗi).

- [ ] **Bước 9: Xác nhận dev server chạy được**

Run: `npm run dev --prefix frontend -- --port 5173` (dùng timeout ngắn, đây là tiến trình chạy mãi — dừng sau khi xác nhận)
Expected: log hiện `Local: http://localhost:5173/`. Gọi `curl http://localhost:5173` → HTML chứa `<div id="root">` và `<title>HaloChat</title>`.

- [ ] **Bước 10: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Scaffold frontend: Vite + React + TypeScript + Vitest + logo HaloChat

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Kiểu dữ liệu dùng chung + lớp gọi API (DichVuApi)

**Files:**
- Create: `frontend/src/KieuDuLieu.ts`
- Create: `frontend/src/DichVuApi.ts`
- Create: `frontend/src/DichVuApi.test.ts`

**Interfaces:**
- Consumes: backend API thật (không gọi trong test — test mock `global.fetch`).
- Produces: `NguoiDungTomTat { id, tenTaiKhoan, email }`; `KetQuaDangKy { thongBao }`; `KetQuaDangNhap { token }`; `class LoiGoiApi extends Error { trangThai: number }`; hàm `DangKy(tenTaiKhoan, email, matKhau): Promise<KetQuaDangKy>`, `DangNhap(tenDangNhap, matKhau): Promise<KetQuaDangNhap>`, `LayDanhSachNguoiDung(token): Promise<NguoiDungTomTat[]>`. Task 3, 5, 6, 7 import các hàm và kiểu này.

- [ ] **Bước 1: Tạo `frontend/src/KieuDuLieu.ts`**

```ts
export interface NguoiDungTomTat {
  id: string;
  tenTaiKhoan: string;
  email: string;
}

export interface KetQuaDangKy {
  thongBao: string;
}

export interface KetQuaDangNhap {
  token: string;
}
```

- [ ] **Bước 2: Viết test cho `DichVuApi` trước — tạo `frontend/src/DichVuApi.test.ts`**

```ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DangKy, DangNhap, LayDanhSachNguoiDung } from './DichVuApi';

describe('DichVuApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('DangKy gửi đúng request và trả về kết quả khi thành công', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ thongBao: 'Đăng ký thành công.' }), { status: 200 }),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const ketQua = await DangKy('NguyenAn', 'nguyenan@gmail.com', 'MatKhau123');

    expect(ketQua.thongBao).toBe('Đăng ký thành công.');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung/dang-ky'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ tenTaiKhoan: 'NguyenAn', email: 'nguyenan@gmail.com', matKhau: 'MatKhau123' }),
      }),
    );
  });

  it('DangKy ném LoiGoiApi kèm thongBao khi trùng tên tài khoản (409)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ thongBao: 'Tên tài khoản đã tồn tại.' }), { status: 409 }),
      ),
    );

    await expect(DangKy('NguyenAn', 'a@gmail.com', 'x')).rejects.toMatchObject({
      trangThai: 409,
      message: 'Tên tài khoản đã tồn tại.',
    });
  });

  it('DangNhap trả về token khi đăng nhập đúng', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ token: 'abc.def.ghi' }), { status: 200 })),
    );

    const ketQua = await DangNhap('NguyenAn', 'MatKhau123');

    expect(ketQua.token).toBe('abc.def.ghi');
  });

  it('DangNhap ném LoiGoiApi khi sai mật khẩu (401)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ thongBao: 'Sai tên đăng nhập hoặc mật khẩu.' }), { status: 401 }),
      ),
    );

    await expect(DangNhap('NguyenAn', 'Sai')).rejects.toMatchObject({ trangThai: 401 });
  });

  it('LayDanhSachNguoiDung gửi kèm Bearer token và trả về danh sách', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(JSON.stringify([{ id: '1', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }]), { status: 200 }),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const danhSach = await LayDanhSachNguoiDung('token-gia-lap');

    expect(danhSach).toHaveLength(1);
    expect(danhSach[0].tenTaiKhoan).toBe('TranBinh');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung'),
      expect.objectContaining({ headers: { Authorization: 'Bearer token-gia-lap' } }),
    );
  });

  it('LayDanhSachNguoiDung ném LoiGoiApi khi không có token hợp lệ (401)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 401 })));

    await expect(LayDanhSachNguoiDung('token-sai')).rejects.toMatchObject({ trangThai: 401 });
  });
});
```

- [ ] **Bước 3: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend`
Expected: lỗi vì `./DichVuApi` chưa tồn tại.

- [ ] **Bước 4: Tạo `frontend/src/DichVuApi.ts`**

```ts
import type { KetQuaDangKy, KetQuaDangNhap, NguoiDungTomTat } from './KieuDuLieu';

const DIA_CHI_GOC_API = 'http://localhost:5231/api';

export class LoiGoiApi extends Error {
  trangThai: number;

  constructor(trangThai: number, message: string) {
    super(message);
    this.name = 'LoiGoiApi';
    this.trangThai = trangThai;
  }
}

async function xuLyPhanHoi<T>(phanHoi: Response): Promise<T> {
  const vanBan = await phanHoi.text();
  const duLieu = vanBan ? JSON.parse(vanBan) : null;

  if (!phanHoi.ok) {
    const thongBao = duLieu?.thongBao ?? 'Đã có lỗi xảy ra, vui lòng thử lại.';
    throw new LoiGoiApi(phanHoi.status, thongBao);
  }

  return duLieu as T;
}

export async function DangKy(tenTaiKhoan: string, email: string, matKhau: string): Promise<KetQuaDangKy> {
  const phanHoi = await fetch(`${DIA_CHI_GOC_API}/nguoidung/dang-ky`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenTaiKhoan, email, matKhau }),
  });
  return xuLyPhanHoi<KetQuaDangKy>(phanHoi);
}

export async function DangNhap(tenDangNhap: string, matKhau: string): Promise<KetQuaDangNhap> {
  const phanHoi = await fetch(`${DIA_CHI_GOC_API}/nguoidung/dang-nhap`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenDangNhap, matKhau }),
  });
  return xuLyPhanHoi<KetQuaDangNhap>(phanHoi);
}

export async function LayDanhSachNguoiDung(token: string): Promise<NguoiDungTomTat[]> {
  const phanHoi = await fetch(`${DIA_CHI_GOC_API}/nguoidung`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  return xuLyPhanHoi<NguoiDungTomTat[]>(phanHoi);
}
```

- [ ] **Bước 5: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: 6/6 pass.

- [ ] **Bước 6: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm kiểu dữ liệu dùng chung và lớp gọi API (DichVuApi)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Ngữ cảnh xác thực (NguCanhXacThuc)

**Files:**
- Create: `frontend/src/NguCanh/NguCanhXacThuc.tsx`
- Create: `frontend/src/NguCanh/NguCanhXacThuc.test.tsx`

**Interfaces:**
- Consumes: `DangNhap` từ `DichVuApi` (Task 2).
- Produces: component `NhaCungCapXacThuc({ children })`; hook `useXacThuc(): { token: string | null; daDangNhap: boolean; dangNhap(tenDangNhap, matKhau): Promise<void>; dangXuat(): void }`. Task 5-7 dùng hook này.

- [ ] **Bước 1: Viết test trước — tạo `frontend/src/NguCanh/NguCanhXacThuc.test.tsx`**

```tsx
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NhaCungCapXacThuc, useXacThuc } from './NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function ThanhPhanKiemThu() {
  const { daDangNhap, dangNhap, dangXuat } = useXacThuc();
  return (
    <div>
      <span>{daDangNhap ? 'da-dang-nhap' : 'chua-dang-nhap'}</span>
      <button onClick={() => dangNhap('NguyenAn', 'MatKhau123')}>Đăng nhập</button>
      <button onClick={dangXuat}>Đăng xuất</button>
    </div>
  );
}

describe('NguCanhXacThuc', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it('ban đầu chưa đăng nhập nếu localStorage trống', () => {
    render(
      <NhaCungCapXacThuc>
        <ThanhPhanKiemThu />
      </NhaCungCapXacThuc>,
    );
    expect(screen.getByText('chua-dang-nhap')).toBeInTheDocument();
  });

  it('dangNhap gọi API, lưu token vào localStorage và cập nhật trạng thái', async () => {
    vi.spyOn(DichVuApi, 'DangNhap').mockResolvedValue({ token: 'token-gia-lap' });
    render(
      <NhaCungCapXacThuc>
        <ThanhPhanKiemThu />
      </NhaCungCapXacThuc>,
    );

    await act(async () => {
      await userEvent.click(screen.getByText('Đăng nhập'));
    });

    expect(screen.getByText('da-dang-nhap')).toBeInTheDocument();
    expect(localStorage.getItem('haloChatToken')).toBe('token-gia-lap');
  });

  it('dangXuat xóa token khỏi localStorage và cập nhật trạng thái', async () => {
    localStorage.setItem('haloChatToken', 'token-cu');
    render(
      <NhaCungCapXacThuc>
        <ThanhPhanKiemThu />
      </NhaCungCapXacThuc>,
    );
    expect(screen.getByText('da-dang-nhap')).toBeInTheDocument();

    await act(async () => {
      await userEvent.click(screen.getByText('Đăng xuất'));
    });

    expect(screen.getByText('chua-dang-nhap')).toBeInTheDocument();
    expect(localStorage.getItem('haloChatToken')).toBeNull();
  });
});
```

- [ ] **Bước 2: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend`
Expected: lỗi vì `./NguCanhXacThuc` chưa tồn tại.

- [ ] **Bước 3: Tạo `frontend/src/NguCanh/NguCanhXacThuc.tsx`**

```tsx
import { createContext, useContext, useState, type ReactNode } from 'react';
import { DangNhap as GoiDangNhap } from '../DichVuApi';

interface TrangThaiXacThuc {
  token: string | null;
  daDangNhap: boolean;
  dangNhap: (tenDangNhap: string, matKhau: string) => Promise<void>;
  dangXuat: () => void;
}

const KHOA_LUU_TOKEN = 'haloChatToken';

const BoiCanhXacThuc = createContext<TrangThaiXacThuc | undefined>(undefined);

export function NhaCungCapXacThuc({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(KHOA_LUU_TOKEN));

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
    <BoiCanhXacThuc.Provider value={{ token, daDangNhap: token !== null, dangNhap, dangXuat }}>
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

- [ ] **Bước 4: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: 3/3 pass (file này) — tổng cộng 9/9 pass toàn dự án.

- [ ] **Bước 5: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm ngữ cảnh xác thực (NguCanhXacThuc) lưu JWT ở localStorage

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Hệ thống thiết kế dùng chung (màu sắc, font, khung xác thực, ô nhập, biểu tượng)

Task này dựng bộ giao diện dùng chung cho 2 trang xác thực (đăng ký/đăng nhập) theo đúng ảnh mẫu
đã duyệt: thẻ trắng bo tròn giữa nền gradient xanh nhạt, logo HaloChat + tên + khẩu hiệu ở đầu thẻ,
tab chuyển đổi Đăng nhập/Đăng ký, ô nhập có icon bên trái + nút ẩn/hiện mật khẩu.

**Files:**
- Create: `frontend/src/index.css` (viết lại toàn bộ — token màu/font + reset)
- Create: `frontend/src/ThanhPhan/BieuTuong.tsx` (icon SVG: người dùng, email, khóa, mũi tên)
- Create: `frontend/src/ThanhPhan/TruongNhap.tsx` + `TruongNhap.css`
- Create: `frontend/src/ThanhPhan/TruongNhap.test.tsx`
- Create: `frontend/src/ThanhPhan/KhungXacThuc.tsx` + `KhungXacThuc.css`

**Interfaces:**
- Consumes: `frontend/public/halochat-logo.png` (Task 1, đường dẫn tĩnh `/halochat-logo.png`).
- Produces: `TruongNhap({ nhan, bieuTuong, coTheAn?, ...các thuộc tính input khác }): JSX` — input có nhãn + icon, `coTheAn` bật nút ẩn/hiện mật khẩu; `BieuTuongNguoiDung`, `BieuTuongEmail`, `BieuTuongKhoa`, `BieuTuongMuiTen` (component icon không nhận props); `KhungXacThuc({ children }): JSX` — khung thẻ + logo + tab Đăng nhập/Đăng ký, `children` là nội dung form. Các class CSS dùng chung mà Task 5-6 sẽ dùng trên form/nút/thông báo lỗi của chính chúng: `.nut-chinh` (nút submit gradient), `.thong-bao-loi` (hộp lỗi). Task 5 và 6 import `KhungXacThuc` và `TruongNhap`, bọc form của chúng trong `KhungXacThuc`, dùng class `.nut-chinh`/`.thong-bao-loi` — không đổi nhãn (label text) hay chữ trên nút mà Task 5/6 đã quy định, chỉ đổi cách trình bày.

- [ ] **Bước 1: Viết lại toàn bộ `frontend/src/index.css`**

```css
@import url('https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700;800&display=swap');

:root {
  --mau-nen-tren: #eaf1fd;
  --mau-nen-duoi: #dde9fc;
  --mau-chinh-nhat: #55a6ff;
  --mau-chinh: #2f7bf6;
  --mau-chinh-dam: #1a56c4;
  --mau-chu-dam: #101b33;
  --mau-chu-phu: #6b7684;
  --mau-vien: #e1e6ee;
  --mau-nen-the: #ffffff;
  --mau-loi: #d64545;
  --ban-kinh-the: 24px;
  --ban-kinh-o: 14px;
  --bong-the: 0 24px 60px -20px rgba(31, 66, 135, 0.28);
}

* {
  box-sizing: border-box;
}

html,
body,
#root {
  min-height: 100%;
}

body {
  margin: 0;
  font-family: 'Be Vietnam Pro', system-ui, sans-serif;
  color: var(--mau-chu-dam);
  background: linear-gradient(180deg, var(--mau-nen-tren) 0%, var(--mau-nen-duoi) 100%);
}

.nut-chinh {
  width: 100%;
  padding: 14px;
  border: none;
  border-radius: var(--ban-kinh-o);
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  font-weight: 700;
  font-size: 15px;
  font-family: inherit;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  cursor: pointer;
  transition: filter 0.15s ease, transform 0.05s ease;
}

.nut-chinh:hover {
  filter: brightness(1.05);
}

.nut-chinh:active {
  transform: translateY(1px);
}

.nut-chinh:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.thong-bao-loi {
  background: #fdecec;
  color: var(--mau-loi);
  border-radius: 10px;
  padding: 10px 14px;
  font-size: 13px;
  margin: 0 0 16px;
  text-align: left;
}
```

- [ ] **Bước 2: Tạo `frontend/src/ThanhPhan/BieuTuong.tsx`**

```tsx
export function BieuTuongNguoiDung() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <circle cx="12" cy="8" r="4" />
      <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" />
    </svg>
  );
}

export function BieuTuongEmail() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <rect x="2" y="4" width="20" height="16" rx="2" />
      <path d="m2 6 10 7 10-7" />
    </svg>
  );
}

export function BieuTuongKhoa() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <rect x="4" y="11" width="16" height="10" rx="2" />
      <path d="M8 11V7a4 4 0 0 1 8 0v4" />
    </svg>
  );
}

export function BieuTuongMuiTen() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M5 12h14M13 6l6 6-6 6" />
    </svg>
  );
}

export function BieuTuongMat() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M1 12s4-7 11-7 11 7 11 7-4 7-11 7-11-7-11-7Z" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

export function BieuTuongAnMat() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M17.94 17.94A10.94 10.94 0 0 1 12 19c-7 0-11-7-11-7a21.6 21.6 0 0 1 5.06-5.94M9.9 4.24A10.6 10.6 0 0 1 12 4c7 0 11 7 11 7a21.6 21.6 0 0 1-2.61 3.53M14.12 14.12a3 3 0 1 1-4.24-4.24" />
      <line x1="1" y1="1" x2="23" y2="23" />
    </svg>
  );
}
```

- [ ] **Bước 3: Viết test trước cho `TruongNhap` — tạo `frontend/src/ThanhPhan/TruongNhap.test.tsx`**

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect } from 'vitest';
import { TruongNhap } from './TruongNhap';
import { BieuTuongKhoa } from './BieuTuong';

describe('TruongNhap', () => {
  it('mặc định ẩn mật khẩu, bấm biểu tượng để hiện rồi ẩn lại', async () => {
    render(
      <TruongNhap nhan="Mật khẩu" bieuTuong={<BieuTuongKhoa />} coTheAn value="MatKhau123" onChange={() => {}} />,
    );

    const oNhap = screen.getByLabelText('Mật khẩu');
    expect(oNhap).toHaveAttribute('type', 'password');

    await userEvent.click(screen.getByRole('button', { name: 'Hiện mật khẩu' }));
    expect(oNhap).toHaveAttribute('type', 'text');

    await userEvent.click(screen.getByRole('button', { name: 'Ẩn mật khẩu' }));
    expect(oNhap).toHaveAttribute('type', 'password');
  });

  it('trường không bật coTheAn thì không có nút ẩn/hiện', () => {
    render(<TruongNhap nhan="Tên tài khoản" bieuTuong={<BieuTuongKhoa />} value="" onChange={() => {}} />);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
```

- [ ] **Bước 4: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend`
Expected: lỗi vì `./TruongNhap` chưa tồn tại.

- [ ] **Bước 5: Tạo `frontend/src/ThanhPhan/TruongNhap.css`**

```css
.truong-nhap {
  display: block;
  text-align: left;
  margin-bottom: 16px;
}

.truong-nhap__nhan {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: var(--mau-chu-dam);
  margin-bottom: 6px;
}

.truong-nhap__o {
  display: flex;
  align-items: center;
  border: 1px solid var(--mau-vien);
  border-radius: var(--ban-kinh-o);
  padding: 0 12px;
  background: #fbfcfe;
}

.truong-nhap__o:focus-within {
  border-color: var(--mau-chinh);
  box-shadow: 0 0 0 3px rgba(47, 123, 246, 0.15);
}

.truong-nhap__bieu-tuong {
  color: var(--mau-chu-phu);
  display: flex;
}

.truong-nhap__input {
  flex: 1;
  min-width: 0;
  border: none;
  background: transparent;
  padding: 12px 10px;
  font-size: 15px;
  font-family: inherit;
  color: var(--mau-chu-dam);
  outline: none;
}

.truong-nhap__nut-an {
  border: none;
  background: transparent;
  color: var(--mau-chu-phu);
  cursor: pointer;
  display: flex;
  padding: 4px;
}
```

- [ ] **Bước 6: Tạo `frontend/src/ThanhPhan/TruongNhap.tsx`**

```tsx
import { useId, useState, type InputHTMLAttributes, type ReactNode } from 'react';
import { BieuTuongMat, BieuTuongAnMat } from './BieuTuong';
import './TruongNhap.css';

interface TruongNhapProps extends InputHTMLAttributes<HTMLInputElement> {
  nhan: string;
  bieuTuong: ReactNode;
  coTheAn?: boolean;
}

export function TruongNhap({ nhan, bieuTuong, coTheAn, type, id, ...conLai }: TruongNhapProps) {
  const idTuSinh = useId();
  const [hienMatKhau, setHienMatKhau] = useState(false);
  const maId = id ?? idTuSinh;
  const loaiThucTe = coTheAn ? (hienMatKhau ? 'text' : 'password') : type;

  return (
    <label className="truong-nhap" htmlFor={maId}>
      <span className="truong-nhap__nhan">{nhan}</span>
      <span className="truong-nhap__o">
        <span className="truong-nhap__bieu-tuong">{bieuTuong}</span>
        <input id={maId} type={loaiThucTe} className="truong-nhap__input" {...conLai} />
        {coTheAn && (
          <button
            type="button"
            className="truong-nhap__nut-an"
            onClick={() => setHienMatKhau((truoc) => !truoc)}
            aria-label={hienMatKhau ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          >
            {hienMatKhau ? <BieuTuongAnMat /> : <BieuTuongMat />}
          </button>
        )}
      </span>
    </label>
  );
}
```

- [ ] **Bước 7: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend -- TruongNhap`
Expected: 2/2 pass.

- [ ] **Bước 8: Tạo `frontend/src/ThanhPhan/KhungXacThuc.css`**

```css
.khung-xac-thuc {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32px 16px;
}

.the-xac-thuc {
  width: 100%;
  max-width: 400px;
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-the);
  box-shadow: var(--bong-the);
  padding: 40px 32px;
  text-align: center;
}

.logo-xac-thuc {
  width: 72px;
  height: 72px;
  object-fit: contain;
  margin-bottom: 12px;
}

.tieu-de-xac-thuc {
  font-size: 26px;
  font-weight: 800;
  margin: 0;
}

.khau-hieu-xac-thuc {
  color: var(--mau-chu-phu);
  margin: 6px 0 24px;
  font-size: 14px;
}

.tab-xac-thuc {
  display: flex;
  background: #eef2f9;
  border-radius: 999px;
  padding: 4px;
  margin-bottom: 24px;
}

.tab-xac-thuc__nut {
  flex: 1;
  padding: 10px 0;
  border-radius: 999px;
  text-decoration: none;
  color: var(--mau-chu-phu);
  font-weight: 600;
  font-size: 14px;
  transition: background-color 0.15s ease, color 0.15s ease;
}

.tab-xac-thuc__nut--dang-chon {
  background: var(--mau-chinh);
  color: #fff;
}
```

- [ ] **Bước 9: Tạo `frontend/src/ThanhPhan/KhungXacThuc.tsx`**

```tsx
import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import './KhungXacThuc.css';

export function KhungXacThuc({ children }: { children: ReactNode }) {
  return (
    <div className="khung-xac-thuc">
      <div className="the-xac-thuc">
        <img src="/halochat-logo.png" alt="HaloChat" className="logo-xac-thuc" />
        <h1 className="tieu-de-xac-thuc">HaloChat</h1>
        <p className="khau-hieu-xac-thuc">Kết nối và trò chuyện an toàn</p>
        <nav className="tab-xac-thuc" aria-label="Chuyển đổi đăng nhập hoặc đăng ký">
          <NavLink
            to="/dang-nhap"
            className={({ isActive }) => `tab-xac-thuc__nut${isActive ? ' tab-xac-thuc__nut--dang-chon' : ''}`}
          >
            Đăng nhập
          </NavLink>
          <NavLink
            to="/dang-ky"
            className={({ isActive }) => `tab-xac-thuc__nut${isActive ? ' tab-xac-thuc__nut--dang-chon' : ''}`}
          >
            Đăng ký
          </NavLink>
        </nav>
        {children}
      </div>
    </div>
  );
}
```

- [ ] **Bước 10: Build + chạy toàn bộ test**

Run: `npm run build --prefix frontend`
Expected: biên dịch thành công.

Run: `npm run test --prefix frontend`
Expected: tất cả pass — tổng cộng 11/11 toàn dự án (9 từ Task 2-3 + 2 từ `TruongNhap`).

- [ ] **Bước 11: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm hệ thống thiết kế dùng chung: KhungXacThuc, TruongNhap, biểu tượng

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Trang đăng ký (TrangDangKy)

**Files:**
- Create: `frontend/src/Trang/TrangDangKy.tsx`
- Create: `frontend/src/Trang/TrangDangKy.test.tsx`

**Interfaces:**
- Consumes: `DangKy` từ `DichVuApi` (Task 2); `KhungXacThuc`, `TruongNhap`, `BieuTuongNguoiDung`/`BieuTuongEmail`/`BieuTuongKhoa`/`BieuTuongMuiTen`, class `.nut-chinh`/`.thong-bao-loi` (Task 4).
- Produces: component `TrangDangKy` — điều hướng sang `/dang-nhap` sau khi đăng ký thành công. Task 8 gắn route `/dang-ky` vào component này.

**Lưu ý quan trọng:** `TruongNhap` (Task 4) render input bên trong `<label>` — `screen.getByLabelText('Tên tài khoản')` vẫn hoạt động đúng vì toàn bộ chữ trong `<label>` (trừ giá trị input) tính vào tên nhãn. Nút submit vẫn phải giữ đúng chữ `"Đăng ký"` (có thể kèm icon mũi tên cạnh chữ, icon không có text nên không ảnh hưởng đến tên accessible của nút) để khớp với test bên dưới.

- [ ] **Bước 1: Viết test trước — tạo `frontend/src/Trang/TrangDangKy.test.tsx`**

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangDangKy } from './TrangDangKy';
import * as DichVuApi from '../DichVuApi';

function renderVoiRouter() {
  return render(
    <MemoryRouter initialEntries={['/dang-ky']}>
      <Routes>
        <Route path="/dang-ky" element={<TrangDangKy />} />
        <Route path="/dang-nhap" element={<div>Trang đăng nhập</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('TrangDangKy', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('gửi đúng dữ liệu và điều hướng sang /dang-nhap khi đăng ký thành công', async () => {
    const dangKyGiaLap = vi.spyOn(DichVuApi, 'DangKy').mockResolvedValue({ thongBao: 'Đăng ký thành công.' });
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Email'), 'nguyenan@gmail.com');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'MatKhau123');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng ký' }));

    expect(dangKyGiaLap).toHaveBeenCalledWith('NguyenAn', 'nguyenan@gmail.com', 'MatKhau123');
    expect(await screen.findByText('Trang đăng nhập')).toBeInTheDocument();
  });

  it('hiển thị thông báo lỗi khi API trả lỗi, không điều hướng', async () => {
    vi.spyOn(DichVuApi, 'DangKy').mockRejectedValue(new Error('Tên tài khoản đã tồn tại.'));
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Email'), 'nguyenan@gmail.com');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'MatKhau123');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng ký' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Tên tài khoản đã tồn tại.');
  });
});
```

- [ ] **Bước 2: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend`
Expected: lỗi vì `./TrangDangKy` chưa tồn tại.

- [ ] **Bước 3: Tạo `frontend/src/Trang/TrangDangKy.tsx`**

```tsx
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { DangKy } from '../DichVuApi';
import { KhungXacThuc } from '../ThanhPhan/KhungXacThuc';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongNguoiDung, BieuTuongEmail, BieuTuongKhoa, BieuTuongMuiTen } from '../ThanhPhan/BieuTuong';

export function TrangDangKy() {
  const [tenTaiKhoan, setTenTaiKhoan] = useState('');
  const [email, setEmail] = useState('');
  const [matKhau, setMatKhau] = useState('');
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);
  const dieuHuong = useNavigate();

  async function xuLySubmit(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setDangGui(true);
    try {
      await DangKy(tenTaiKhoan, email, matKhau);
      dieuHuong('/dang-nhap');
    } catch (loiBat) {
      setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  return (
    <KhungXacThuc>
      <form onSubmit={xuLySubmit}>
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        <TruongNhap
          nhan="Tên tài khoản"
          bieuTuong={<BieuTuongNguoiDung />}
          value={tenTaiKhoan}
          onChange={(e) => setTenTaiKhoan(e.target.value)}
          placeholder="Nhập tên tài khoản..."
          required
        />
        <TruongNhap
          nhan="Email"
          bieuTuong={<BieuTuongEmail />}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Nhập email..."
          required
        />
        <TruongNhap
          nhan="Mật khẩu"
          bieuTuong={<BieuTuongKhoa />}
          coTheAn
          value={matKhau}
          onChange={(e) => setMatKhau(e.target.value)}
          placeholder="Nhập mật khẩu..."
          required
        />
        <button type="submit" className="nut-chinh" disabled={dangGui}>
          Đăng ký <BieuTuongMuiTen />
        </button>
      </form>
    </KhungXacThuc>
  );
}
```

- [ ] **Bước 4: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: 2/2 pass (file này) — tổng cộng 13/13 pass toàn dự án.

- [ ] **Bước 5: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm trang đăng ký (TrangDangKy) theo giao diện HaloChat

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Trang đăng nhập (TrangDangNhap)

**Files:**
- Create: `frontend/src/Trang/TrangDangNhap.tsx`
- Create: `frontend/src/Trang/TrangDangNhap.test.tsx`

**Interfaces:**
- Consumes: `useXacThuc` từ `NguCanhXacThuc` (Task 3); `KhungXacThuc`, `TruongNhap`, biểu tượng, class dùng chung (Task 4).
- Produces: component `TrangDangNhap` — điều hướng sang `/nguoi-dung` sau khi đăng nhập thành công. Task 8 gắn route `/dang-nhap` vào component này.

- [ ] **Bước 1: Viết test trước — tạo `frontend/src/Trang/TrangDangNhap.test.tsx`**

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangDangNhap } from './TrangDangNhap';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderVoiRouter() {
  return render(
    <MemoryRouter initialEntries={['/dang-nhap']}>
      <NhaCungCapXacThuc>
        <Routes>
          <Route path="/dang-nhap" element={<TrangDangNhap />} />
          <Route path="/nguoi-dung" element={<div>Trang người dùng</div>} />
        </Routes>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TrangDangNhap', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('đăng nhập thành công điều hướng sang /nguoi-dung', async () => {
    vi.spyOn(DichVuApi, 'DangNhap').mockResolvedValue({ token: 'token-gia-lap' });
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản hoặc Email'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'MatKhau123');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }));

    expect(await screen.findByText('Trang người dùng')).toBeInTheDocument();
  });

  it('sai mật khẩu hiển thị lỗi, không điều hướng', async () => {
    vi.spyOn(DichVuApi, 'DangNhap').mockRejectedValue(new Error('Sai tên đăng nhập hoặc mật khẩu.'));
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản hoặc Email'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'Sai');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Sai tên đăng nhập hoặc mật khẩu.');
  });
});
```

- [ ] **Bước 2: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend`
Expected: lỗi vì `./TrangDangNhap` chưa tồn tại.

- [ ] **Bước 3: Tạo `frontend/src/Trang/TrangDangNhap.tsx`**

```tsx
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { KhungXacThuc } from '../ThanhPhan/KhungXacThuc';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongNguoiDung, BieuTuongKhoa, BieuTuongMuiTen } from '../ThanhPhan/BieuTuong';

export function TrangDangNhap() {
  const [tenDangNhap, setTenDangNhap] = useState('');
  const [matKhau, setMatKhau] = useState('');
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);
  const { dangNhap } = useXacThuc();
  const dieuHuong = useNavigate();

  async function xuLySubmit(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setDangGui(true);
    try {
      await dangNhap(tenDangNhap, matKhau);
      dieuHuong('/nguoi-dung');
    } catch (loiBat) {
      setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  return (
    <KhungXacThuc>
      <form onSubmit={xuLySubmit}>
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        <TruongNhap
          nhan="Tên tài khoản hoặc Email"
          bieuTuong={<BieuTuongNguoiDung />}
          value={tenDangNhap}
          onChange={(e) => setTenDangNhap(e.target.value)}
          placeholder="Nhập tên đăng nhập..."
          required
        />
        <TruongNhap
          nhan="Mật khẩu"
          bieuTuong={<BieuTuongKhoa />}
          coTheAn
          value={matKhau}
          onChange={(e) => setMatKhau(e.target.value)}
          placeholder="Nhập mật khẩu..."
          required
        />
        <button type="submit" className="nut-chinh" disabled={dangGui}>
          Đăng nhập <BieuTuongMuiTen />
        </button>
      </form>
    </KhungXacThuc>
  );
}
```

- [ ] **Bước 4: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: 2/2 pass (file này) — tổng cộng 15/15 pass toàn dự án.

- [ ] **Bước 5: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm trang đăng nhập (TrangDangNhap) theo giao diện HaloChat

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Route bảo vệ + Trang danh sách người dùng

**Files:**
- Create: `frontend/src/ThanhPhan/TuyenDuongRieng.tsx`
- Create: `frontend/src/ThanhPhan/TuyenDuongRieng.test.tsx`
- Create: `frontend/src/Trang/TrangDanhSachNguoiDung.tsx` + `TrangDanhSachNguoiDung.css`
- Create: `frontend/src/Trang/TrangDanhSachNguoiDung.test.tsx`

**Interfaces:**
- Consumes: `useXacThuc` (Task 3), `LayDanhSachNguoiDung` + `NguoiDungTomTat` (Task 2).
- Produces: component `TuyenDuongRieng({ children })` (điều hướng về `/dang-nhap` nếu chưa đăng nhập); component `TrangDanhSachNguoiDung`. Task 8 gắn route `/nguoi-dung` vào `<TuyenDuongRieng><TrangDanhSachNguoiDung /></TuyenDuongRieng>`.

- [ ] **Bước 1: Viết test trước cho `TuyenDuongRieng` — tạo `frontend/src/ThanhPhan/TuyenDuongRieng.test.tsx`**

```tsx
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, beforeEach } from 'vitest';
import { TuyenDuongRieng } from './TuyenDuongRieng';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';

function renderVoiTuyenDuong() {
  return render(
    <MemoryRouter initialEntries={['/nguoi-dung']}>
      <NhaCungCapXacThuc>
        <Routes>
          <Route path="/dang-nhap" element={<div>Trang đăng nhập</div>} />
          <Route
            path="/nguoi-dung"
            element={
              <TuyenDuongRieng>
                <div>Bí mật</div>
              </TuyenDuongRieng>
            }
          />
        </Routes>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TuyenDuongRieng', () => {
  beforeEach(() => localStorage.clear());

  it('điều hướng về /dang-nhap khi chưa đăng nhập', () => {
    renderVoiTuyenDuong();
    expect(screen.getByText('Trang đăng nhập')).toBeInTheDocument();
  });

  it('hiển thị nội dung khi đã đăng nhập', () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    renderVoiTuyenDuong();
    expect(screen.getByText('Bí mật')).toBeInTheDocument();
  });
});
```

- [ ] **Bước 2: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend`
Expected: lỗi vì `./TuyenDuongRieng` chưa tồn tại.

- [ ] **Bước 3: Tạo `frontend/src/ThanhPhan/TuyenDuongRieng.tsx`**

```tsx
import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';

export function TuyenDuongRieng({ children }: { children: ReactNode }) {
  const { daDangNhap } = useXacThuc();
  if (!daDangNhap) {
    return <Navigate to="/dang-nhap" replace />;
  }
  return <>{children}</>;
}
```

- [ ] **Bước 4: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend -- TuyenDuongRieng`
Expected: 2/2 pass.

- [ ] **Bước 5: Viết test trước cho `TrangDanhSachNguoiDung` — tạo `frontend/src/Trang/TrangDanhSachNguoiDung.test.tsx`**

```tsx
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangDanhSachNguoiDung } from './TrangDanhSachNguoiDung';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderVoiNguCanh() {
  return render(
    <NhaCungCapXacThuc>
      <TrangDanhSachNguoiDung />
    </NhaCungCapXacThuc>,
  );
}

describe('TrangDanhSachNguoiDung', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('hiển thị danh sách người dùng lấy từ API', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: '1', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' },
    ]);

    renderVoiNguCanh();

    expect(await screen.findByText('TranBinh (b@gmail.com)')).toBeInTheDocument();
  });

  it('hiển thị lỗi khi API thất bại', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockRejectedValue(new Error('Không thể tải danh sách.'));

    renderVoiNguCanh();

    expect(await screen.findByRole('alert')).toHaveTextContent('Không thể tải danh sách.');
  });
});
```

- [ ] **Bước 6: Chạy test, xác nhận thất bại**

Run: `npm run test --prefix frontend -- TrangDanhSachNguoiDung`
Expected: lỗi vì `./TrangDanhSachNguoiDung` chưa tồn tại.

- [ ] **Bước 7: Tạo `frontend/src/Trang/TrangDanhSachNguoiDung.css`**

```css
.trang-danh-sach {
  min-height: 100vh;
  padding: 32px 16px;
  display: flex;
  justify-content: center;
}

.trang-danh-sach__the {
  width: 100%;
  max-width: 480px;
  background: var(--mau-nen-the);
  border-radius: var(--ban-kinh-the);
  box-shadow: var(--bong-the);
  padding: 32px;
}

.trang-danh-sach__dau {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.trang-danh-sach__dau h1 {
  font-size: 20px;
  margin: 0;
}

.trang-danh-sach__nut-dang-xuat {
  border: 1px solid var(--mau-vien);
  background: #fff;
  color: var(--mau-chu-dam);
  border-radius: 999px;
  padding: 8px 16px;
  font-family: inherit;
  font-weight: 600;
  font-size: 13px;
  cursor: pointer;
}

.trang-danh-sach__danh-sach {
  list-style: none;
  margin: 0;
  padding: 0;
}

.trang-danh-sach__muc {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 0;
  border-bottom: 1px solid var(--mau-vien);
}

.trang-danh-sach__muc:last-child {
  border-bottom: none;
}

.trang-danh-sach__avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--mau-chinh-nhat), var(--mau-chinh-dam));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  flex-shrink: 0;
}

.trang-danh-sach__thong-tin {
  display: flex;
  flex-direction: column;
}

.trang-danh-sach__ten {
  font-weight: 600;
}

.trang-danh-sach__email {
  color: var(--mau-chu-phu);
  font-size: 13px;
}
```

- [ ] **Bước 8: Tạo `frontend/src/Trang/TrangDanhSachNguoiDung.tsx`**

```tsx
import { useEffect, useState } from 'react';
import { LayDanhSachNguoiDung } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import type { NguoiDungTomTat } from '../KieuDuLieu';
import './TrangDanhSachNguoiDung.css';

export function TrangDanhSachNguoiDung() {
  const { token, dangXuat } = useXacThuc();
  const [danhSach, setDanhSach] = useState<NguoiDungTomTat[]>([]);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangTai, setDangTai] = useState(true);

  useEffect(() => {
    if (!token) return;
    LayDanhSachNguoiDung(token)
      .then(setDanhSach)
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.'))
      .finally(() => setDangTai(false));
  }, [token]);

  return (
    <div className="trang-danh-sach">
      <div className="trang-danh-sach__the">
        <div className="trang-danh-sach__dau">
          <h1>Danh sách người dùng</h1>
          <button className="trang-danh-sach__nut-dang-xuat" onClick={dangXuat}>
            Đăng xuất
          </button>
        </div>
        {dangTai && <p>Đang tải...</p>}
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        <ul className="trang-danh-sach__danh-sach">
          {danhSach.map((nd) => (
            <li key={nd.id} className="trang-danh-sach__muc">
              <span className="trang-danh-sach__avatar">{nd.tenTaiKhoan.charAt(0).toUpperCase()}</span>
              <span className="trang-danh-sach__thong-tin">
                <span className="trang-danh-sach__ten">
                  {nd.tenTaiKhoan} ({nd.email})
                </span>
              </span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
```

- [ ] **Bước 9: Chạy lại toàn bộ test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: tất cả pass — tổng cộng 19/19 toàn dự án.

- [ ] **Bước 10: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm route bảo vệ (TuyenDuongRieng) và trang danh sách người dùng

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Gắn router (App.tsx) + kiểm tra đầu-cuối với backend thật

**Files:**
- Modify: `frontend/src/App.tsx` (viết lại toàn bộ)
- Delete: `frontend/src/App.css` (không còn dùng sau khi viết lại App.tsx)
- Create: `README.md` (gốc repo — hướng dẫn chạy backend + frontend cùng lúc)

**Interfaces:**
- Consumes: `NhaCungCapXacThuc` (Task 3), `TuyenDuongRieng` (Task 7), `TrangDangKy` (Task 5), `TrangDangNhap` (Task 6), `TrangDanhSachNguoiDung` (Task 7).
- Produces: ứng dụng hoàn chỉnh chạy được tại `http://localhost:5173`, điều hướng `/dang-ky`, `/dang-nhap`, `/nguoi-dung` (bảo vệ), mọi route khác chuyển về `/dang-nhap`.

- [ ] **Bước 1: Viết lại toàn bộ `frontend/src/App.tsx`**

```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { NhaCungCapXacThuc } from './NguCanh/NguCanhXacThuc';
import { TuyenDuongRieng } from './ThanhPhan/TuyenDuongRieng';
import { TrangDangKy } from './Trang/TrangDangKy';
import { TrangDangNhap } from './Trang/TrangDangNhap';
import { TrangDanhSachNguoiDung } from './Trang/TrangDanhSachNguoiDung';

function App() {
  return (
    <BrowserRouter>
      <NhaCungCapXacThuc>
        <Routes>
          <Route path="/dang-ky" element={<TrangDangKy />} />
          <Route path="/dang-nhap" element={<TrangDangNhap />} />
          <Route
            path="/nguoi-dung"
            element={
              <TuyenDuongRieng>
                <TrangDanhSachNguoiDung />
              </TuyenDuongRieng>
            }
          />
          <Route path="*" element={<Navigate to="/dang-nhap" replace />} />
        </Routes>
      </NhaCungCapXacThuc>
    </BrowserRouter>
  );
}

export default App;
```

- [ ] **Bước 2: Xóa file CSS demo không còn dùng**

```bash
rm -f frontend/src/App.css
```

(Nếu `frontend/src/main.tsx` hoặc file nào khác còn `import './App.css'`, xóa dòng import đó — chỉ `App.tsx` mới có khả năng import file này theo template mặc định của Vite, và `App.tsx` vừa được viết lại ở Bước 1 nên không còn import nó nữa. `frontend/src/main.tsx` mặc định import `./index.css` — giữ nguyên dòng đó vì `index.css` đã được viết lại có nội dung thật ở Task 4.)

- [ ] **Bước 3: Build và chạy toàn bộ test**

Run: `npm run build --prefix frontend`
Expected: biên dịch thành công, không lỗi TypeScript.

Run: `npm run test --prefix frontend`
Expected: tất cả 19 test vẫn pass.

- [ ] **Bước 4: Kiểm tra đầu-cuối với backend thật**

Chạy backend (dùng đúng profile `http` để tránh việc `UseHttpsRedirection` chuyển hướng làm rối phần kiểm tra CORS bằng curl):

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/HaloChat.Api --launch-profile http
```

Xác nhận CORS cho phép origin của Vite dev server (`http://localhost:5173`):

```bash
curl -i -H "Origin: http://localhost:5173" http://localhost:5231/api/kiem-tra-suc-khoe
```

Expected: HTTP 200, có header `Access-Control-Allow-Origin: http://localhost:5173` trong response.

Chạy frontend (terminal khác, backend vẫn đang chạy):

```bash
npm run dev --prefix frontend -- --port 5173
```

**Nếu môi trường có công cụ trình duyệt thật (vd. Playwright MCP)**: dùng nó để mở `http://localhost:5173`, xác nhận logo + tên "HaloChat" + tab Đăng nhập/Đăng ký hiển thị đúng theo thiết kế; thao tác thật: đăng ký 1 tài khoản mới → điều hướng sang trang đăng nhập → đăng nhập → thấy trang danh sách người dùng → bấm biểu tượng ẩn/hiện mật khẩu trên trang đăng nhập để xác nhận hoạt động → đăng xuất → xác nhận quay lại trang đăng nhập. Chụp ảnh màn hình trang đăng nhập để xác nhận trực quan khớp thiết kế.

**Nếu không có công cụ trình duyệt**: xác nhận bằng `curl http://localhost:5173` → HTML chứa `<div id="root">` và `<title>HaloChat</title>`; 19 test Vitest (đã bao phủ toàn bộ logic tương tác qua React Testing Library) cộng với bước kiểm tra CORS ở trên là đủ bằng chứng — không cần cố tìm cách khác để "thấy" giao diện.

Dừng cả 2 tiến trình sau khi xác nhận xong.

- [ ] **Bước 5: Tạo `README.md` ở gốc repo**

```markdown
# HaloChat

Ứng dụng chat an toàn dùng mô hình mã hóa lai RSA-AES (đồ án). Xem thiết kế đầy đủ tại
`docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md`.

## Chạy backend

```bash
cd backend
dotnet user-secrets set "MongoDb:ChuoiKetNoi" "<chuoi-ket-noi-mongodb-that-cua-ban>" --project HaloChat.Api
dotnet user-secrets set "Jwt:ChuoiBiMat" "<chuoi-ngau-nhien-toi-thieu-32-ky-tu>" --project HaloChat.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --project HaloChat.Api --launch-profile http
```

Xem `backend/HaloChat.Api/appsettings.Development.json.example` để biết đúng định dạng 2 giá trị trên.
API chạy tại `http://localhost:5231`, Swagger UI tại `http://localhost:5231/swagger`.

Chạy test: `dotnet test backend/HaloChat.sln`

## Chạy frontend

```bash
cd frontend
npm install
npm run dev
```

Frontend chạy tại `http://localhost:5173`, gọi thẳng vào backend ở `http://localhost:5231`.

Chạy test: `npm run test --prefix frontend`

## Thương hiệu

Tên sản phẩm: **HaloChat**. Logo tại `assets/halochat-logo.png`. Màu chủ đạo xanh dương gradient
(`#2F7BF6` → `#1A56C4`), font "Be Vietnam Pro".

## Trạng thái các giai đoạn

- GĐ3 (Backend nền tảng — đăng ký/đăng nhập/JWT/danh sách người dùng): hoàn thành.
- GĐ4 (Frontend nền tảng — trang đăng ký/đăng nhập/danh sách người dùng, giao diện HaloChat): hoàn thành.
- GĐ5 (Chat realtime + gửi ảnh/file + kết bạn + nhóm chat — phạm vi đã mở rộng, xem spec), GĐ6
  (Bảo mật AES/RSA — nhóm tự viết), GĐ7 (Quên mật khẩu): chưa bắt đầu.
```

- [ ] **Bước 6: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Gắn router (App.tsx) + README hướng dẫn chạy backend/frontend

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```
