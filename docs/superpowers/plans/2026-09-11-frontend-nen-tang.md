# HaloChat Frontend Nền Tảng — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng React + TypeScript (Vite) trong `frontend/`, hoàn thiện GĐ4 của tài liệu: trang đăng ký, trang đăng nhập, danh sách người dùng (route được bảo vệ) — gọi thật vào backend GĐ3 đã hoàn thành (`backend/HaloChat.Api`, đang chạy tại `http://localhost:5231`).

**Architecture:** Vite + React 19 + TypeScript. Một lớp `DichVuApi` (fetch thuần, không dùng thư viện HTTP ngoài) bọc 3 endpoint đã có (`dang-ky`, `dang-nhap`, `nguoidung`). Một `NguCanhXacThuc` (React Context) giữ JWT trong `localStorage` và cung cấp `dangNhap`/`dangXuat` cho toàn app. `react-router-dom` điều hướng giữa 3 trang, với `TuyenDuongRieng` chặn truy cập trang danh sách người dùng khi chưa đăng nhập. Test bằng Vitest + React Testing Library (không cần chạy backend thật để chạy test).

**Tech Stack:** Vite, React 19 + TypeScript, react-router-dom, Vitest, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, jsdom.

**Spec:** `docs/superpowers/specs/2026-09-10-halochat-rsa-aes-design.md`

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

---

## Task 1: Scaffold frontend (Vite + React + TypeScript + Vitest)

**Files:**
- Create: `frontend/` (toàn bộ do `npm create vite` sinh ra)
- Modify: `frontend/vite.config.ts`
- Create: `frontend/src/thietLapKiemThu.ts`
- Modify: `frontend/package.json` (thêm script `test`)

**Interfaces:**
- Consumes: không có (task đầu tiên của plan này).
- Produces: dev server chạy được tại `http://localhost:5173`; `npm run test --prefix frontend` chạy được (chưa có test nào — pass nhờ `passWithNoTests`); `npm run build --prefix frontend` biên dịch TypeScript sạch. Các task sau tạo file trong `frontend/src/`.

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

- [ ] **Bước 6: Xác nhận build và test chạy được**

Run: `npm run build --prefix frontend`
Expected: biên dịch thành công, không lỗi TypeScript.

Run: `npm run test --prefix frontend`
Expected: pass (chưa có file test nào, `passWithNoTests: true` nên không báo lỗi).

- [ ] **Bước 7: Xác nhận dev server chạy được**

Run: `npm run dev --prefix frontend -- --port 5173` (dùng timeout ngắn, đây là tiến trình chạy mãi — dừng sau khi xác nhận)
Expected: log hiện `Local: http://localhost:5173/`. Gọi `curl http://localhost:5173` → HTML chứa `<div id="root">`.

- [ ] **Bước 8: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Scaffold frontend: Vite + React + TypeScript + Vitest

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
- Produces: `NguoiDungTomTat { id, tenTaiKhoan, email }`; `KetQuaDangKy { thongBao }`; `KetQuaDangNhap { token }`; `class LoiGoiApi extends Error { trangThai: number }`; hàm `DangKy(tenTaiKhoan, email, matKhau): Promise<KetQuaDangKy>`, `DangNhap(tenDangNhap, matKhau): Promise<KetQuaDangNhap>`, `LayDanhSachNguoiDung(token): Promise<NguoiDungTomTat[]>`. Task 3-6 import các hàm và kiểu này.

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
- Produces: component `NhaCungCapXacThuc({ children })`; hook `useXacThuc(): { token: string | null; daDangNhap: boolean; dangNhap(tenDangNhap, matKhau): Promise<void>; dangXuat(): void }`. Task 4-6 dùng hook này (`TrangDangNhap` gọi `dangNhap`, `TuyenDuongRieng`/`TrangDanhSachNguoiDung` đọc `daDangNhap`/`token`/`dangXuat`).

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

## Task 4: Trang đăng ký (TrangDangKy)

**Files:**
- Create: `frontend/src/Trang/TrangDangKy.tsx`
- Create: `frontend/src/Trang/TrangDangKy.test.tsx`

**Interfaces:**
- Consumes: `DangKy` từ `DichVuApi` (Task 2).
- Produces: component `TrangDangKy` — điều hướng sang `/dang-nhap` sau khi đăng ký thành công. Task 7 gắn route `/dang-ky` vào component này.

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
import { useNavigate, Link } from 'react-router-dom';
import { DangKy } from '../DichVuApi';

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
    <form onSubmit={xuLySubmit}>
      <h1>Đăng ký</h1>
      {loi && <p role="alert">{loi}</p>}
      <label>
        Tên tài khoản
        <input value={tenTaiKhoan} onChange={(e) => setTenTaiKhoan(e.target.value)} required />
      </label>
      <label>
        Email
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
      </label>
      <label>
        Mật khẩu
        <input type="password" value={matKhau} onChange={(e) => setMatKhau(e.target.value)} required />
      </label>
      <button type="submit" disabled={dangGui}>
        Đăng ký
      </button>
      <p>
        Đã có tài khoản? <Link to="/dang-nhap">Đăng nhập</Link>
      </p>
    </form>
  );
}
```

- [ ] **Bước 4: Chạy lại test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: 2/2 pass (file này) — tổng cộng 11/11 pass toàn dự án.

- [ ] **Bước 5: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm trang đăng ký (TrangDangKy)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Trang đăng nhập (TrangDangNhap)

**Files:**
- Create: `frontend/src/Trang/TrangDangNhap.tsx`
- Create: `frontend/src/Trang/TrangDangNhap.test.tsx`

**Interfaces:**
- Consumes: `useXacThuc` từ `NguCanhXacThuc` (Task 3).
- Produces: component `TrangDangNhap` — điều hướng sang `/nguoi-dung` sau khi đăng nhập thành công. Task 7 gắn route `/dang-nhap` vào component này.

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
import { useNavigate, Link } from 'react-router-dom';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';

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
    <form onSubmit={xuLySubmit}>
      <h1>Đăng nhập</h1>
      {loi && <p role="alert">{loi}</p>}
      <label>
        Tên tài khoản hoặc Email
        <input value={tenDangNhap} onChange={(e) => setTenDangNhap(e.target.value)} required />
      </label>
      <label>
        Mật khẩu
        <input type="password" value={matKhau} onChange={(e) => setMatKhau(e.target.value)} required />
      </label>
      <button type="submit" disabled={dangGui}>
        Đăng nhập
      </button>
      <p>
        Chưa có tài khoản? <Link to="/dang-ky">Đăng ký</Link>
      </p>
    </form>
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
Thêm trang đăng nhập (TrangDangNhap)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Route bảo vệ + Trang danh sách người dùng

**Files:**
- Create: `frontend/src/ThanhPhan/TuyenDuongRieng.tsx`
- Create: `frontend/src/ThanhPhan/TuyenDuongRieng.test.tsx`
- Create: `frontend/src/Trang/TrangDanhSachNguoiDung.tsx`
- Create: `frontend/src/Trang/TrangDanhSachNguoiDung.test.tsx`

**Interfaces:**
- Consumes: `useXacThuc` (Task 3), `LayDanhSachNguoiDung` + `NguoiDungTomTat` (Task 2).
- Produces: component `TuyenDuongRieng({ children })` (điều hướng về `/dang-nhap` nếu chưa đăng nhập); component `TrangDanhSachNguoiDung`. Task 7 gắn route `/nguoi-dung` vào `<TuyenDuongRieng><TrangDanhSachNguoiDung /></TuyenDuongRieng>`.

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

- [ ] **Bước 7: Tạo `frontend/src/Trang/TrangDanhSachNguoiDung.tsx`**

```tsx
import { useEffect, useState } from 'react';
import { LayDanhSachNguoiDung } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import type { NguoiDungTomTat } from '../KieuDuLieu';

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
    <div>
      <h1>Danh sách người dùng</h1>
      <button onClick={dangXuat}>Đăng xuất</button>
      {dangTai && <p>Đang tải...</p>}
      {loi && <p role="alert">{loi}</p>}
      <ul>
        {danhSach.map((nd) => (
          <li key={nd.id}>
            {nd.tenTaiKhoan} ({nd.email})
          </li>
        ))}
      </ul>
    </div>
  );
}
```

- [ ] **Bước 8: Chạy lại toàn bộ test, xác nhận pass**

Run: `npm run test --prefix frontend`
Expected: tất cả pass — tổng cộng 17/17 toàn dự án.

- [ ] **Bước 9: Commit**

```bash
git add -A
git commit -m "$(cat <<'EOF'
Thêm route bảo vệ (TuyenDuongRieng) và trang danh sách người dùng

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: Gắn router (App.tsx) + kiểm tra đầu-cuối với backend thật

**Files:**
- Modify: `frontend/src/App.tsx` (viết lại toàn bộ)
- Delete: `frontend/src/App.css` (không còn dùng sau khi viết lại App.tsx)
- Create: `README.md` (gốc repo — hướng dẫn chạy backend + frontend cùng lúc)

**Interfaces:**
- Consumes: `NhaCungCapXacThuc` (Task 3), `TuyenDuongRieng` (Task 6), `TrangDangKy` (Task 4), `TrangDangNhap` (Task 5), `TrangDanhSachNguoiDung` (Task 6).
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

(Nếu `frontend/src/main.tsx` hoặc file nào khác còn `import './App.css'`, xóa dòng import đó — chỉ `App.tsx` mới có khả năng import file này theo template mặc định của Vite, và `App.tsx` vừa được viết lại ở Bước 1 nên không còn import nó nữa.)

- [ ] **Bước 3: Build và chạy toàn bộ test**

Run: `npm run build --prefix frontend`
Expected: biên dịch thành công, không lỗi TypeScript.

Run: `npm run test --prefix frontend`
Expected: tất cả 17 test vẫn pass.

- [ ] **Bước 4: Kiểm tra đầu-cuối với backend thật**

Chạy backend (dùng đúng profile `http` để tránh việc `UseHttpsRedirection` chuyển hướng làm rối phần kiểm tra CORS bằng curl):

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project backend/HaloChat.Api --launch-profile http
```

Xác nhận CORS cho phép origin của Vite dev server (`http://localhost:5173`) — đây là cách xác minh CORS không cần trình duyệt thật:

```bash
curl -i -H "Origin: http://localhost:5173" http://localhost:5231/api/kiem-tra-suc-khoe
```

Expected: HTTP 200, có header `Access-Control-Allow-Origin: http://localhost:5173` trong response.

Chạy frontend (terminal khác, backend vẫn đang chạy):

```bash
npm run dev --prefix frontend -- --port 5173
```

Xác nhận frontend build ra HTML hợp lệ:

```bash
curl http://localhost:5173
```

Expected: HTML chứa `<div id="root">` và thẻ `<script type="module" src="/src/main.tsx">` (hoặc tương đương do Vite sinh ra).

Dừng cả 2 tiến trình sau khi xác nhận xong.

**Lưu ý cho người thực thi task này:** nếu môi trường có công cụ trình duyệt thật (vd. Playwright), nên tận dụng để thao tác thật trên giao diện (đăng ký → đăng nhập → xem danh sách → đăng xuất) thay vì chỉ dừng ở `curl`. Nếu không có, các bước `curl` ở trên cộng với 17 test Vitest (đã bao phủ toàn bộ logic tương tác qua React Testing Library) là đủ bằng chứng cho task này — không cần cố tìm cách khác để "thấy" giao diện.

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

## Trạng thái các giai đoạn

- GĐ3 (Backend nền tảng — đăng ký/đăng nhập/JWT/danh sách người dùng): hoàn thành.
- GĐ4 (Frontend nền tảng — trang đăng ký/đăng nhập/danh sách người dùng): hoàn thành.
- GĐ5 (Chat realtime + gửi ảnh/file), GĐ6 (Bảo mật AES/RSA — nhóm tự viết), GĐ7 (Quên mật khẩu): chưa bắt đầu.
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
