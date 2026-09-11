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

async function goiApi<T>(duongDan: string, tuyChon?: RequestInit): Promise<T> {
  let phanHoi: Response;
  try {
    phanHoi = await fetch(`${DIA_CHI_GOC_API}${duongDan}`, tuyChon);
  } catch {
    throw new LoiGoiApi(0, 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra backend đang chạy.');
  }

  const vanBan = await phanHoi.text();
  let duLieu: unknown = null;
  if (vanBan) {
    try {
      duLieu = JSON.parse(vanBan);
    } catch {
      duLieu = null;
    }
  }

  if (!phanHoi.ok) {
    const thongBao =
      (duLieu as { thongBao?: string } | null)?.thongBao ?? 'Đã có lỗi xảy ra, vui lòng thử lại.';
    throw new LoiGoiApi(phanHoi.status, thongBao);
  }

  return duLieu as T;
}

export async function DangKy(tenTaiKhoan: string, email: string, matKhau: string): Promise<KetQuaDangKy> {
  return goiApi<KetQuaDangKy>('/nguoidung/dang-ky', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenTaiKhoan, email, matKhau }),
  });
}

export async function DangNhap(tenDangNhap: string, matKhau: string): Promise<KetQuaDangNhap> {
  return goiApi<KetQuaDangNhap>('/nguoidung/dang-nhap', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenDangNhap, matKhau }),
  });
}

export async function LayDanhSachNguoiDung(token: string): Promise<NguoiDungTomTat[]> {
  return goiApi<NguoiDungTomTat[]>('/nguoidung', {
    headers: { Authorization: `Bearer ${token}` },
  });
}
