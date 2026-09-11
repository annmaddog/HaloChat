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
