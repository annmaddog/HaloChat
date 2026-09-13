import type {
  KetQuaDangKy, KetQuaDangNhap, NguoiDungTomTat, TinNhan, TepTinDaTaiLen,
  LoiMoiKetBan, HoiThoaiTomTat, HoSoCaNhan, Nhom, KetQuaRoiNhom, KetQuaThongBao,
} from './KieuDuLieu';

// Đọc từ biến môi trường lúc build (VITE_API_BASE_URL) để trỏ đúng backend
// production khi deploy — mặc định về localhost cho môi trường phát triển.
export const DIA_CHI_GOC = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5231';
const DIA_CHI_GOC_API = `${DIA_CHI_GOC}/api`;

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
    throw new LoiGoiApi(phanHoi.status, TrichThongBaoLoi(duLieu));
  }

  return duLieu as T;
}

// ASP.NET Core tự trả lỗi 400 theo dạng ValidationProblemDetails khi dữ liệu
// gửi lên không đạt các ràng buộc [Required]/[MinLength]/... trên DTO —
// dạng { errors: { TenTruong: ["thông báo 1", ...] } }, khác với dạng
// { thongBao } mà các controller trong dự án này tự trả về. Không đọc được
// "errors" thì người dùng chỉ thấy thông báo chung chung, không biết sai ở
// trường nào (VD: mật khẩu ngắn hơn 6 ký tự lúc đăng ký).
function TrichThongBaoLoi(duLieu: unknown): string {
  const object = duLieu as { thongBao?: string; errors?: Record<string, string[]> } | null;
  if (object?.thongBao) {
    return object.thongBao;
  }
  if (object?.errors) {
    const tatCaLoi = Object.values(object.errors).flat();
    if (tatCaLoi.length > 0) {
      return tatCaLoi.join(' ');
    }
  }
  return 'Đã có lỗi xảy ra, vui lòng thử lại.';
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

export async function LayLichSuTinNhan(
  token: string,
  nguoiKiaId: string,
  truoc?: string,
  soLuong = 30,
): Promise<TinNhan[]> {
  const thamSo = new URLSearchParams({ soLuong: String(soLuong) });
  if (truoc) thamSo.set('truoc', truoc);
  return goiApi<TinNhan[]>(`/tinnhan/nguoi-dung/${nguoiKiaId}?${thamSo.toString()}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function TaiLenTep(token: string, tep: File): Promise<TepTinDaTaiLen> {
  const duLieu = new FormData();
  duLieu.append('tep', tep);

  let phanHoi: Response;
  try {
    phanHoi = await fetch(`${DIA_CHI_GOC_API}/tinnhan/upload`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: duLieu,
    });
  } catch {
    throw new LoiGoiApi(0, 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra backend đang chạy.');
  }

  const vanBan = await phanHoi.text();
  let ketQua: unknown = null;
  if (vanBan) {
    try {
      ketQua = JSON.parse(vanBan);
    } catch {
      ketQua = null;
    }
  }

  if (!phanHoi.ok) {
    const thongBao =
      (ketQua as { thongBao?: string } | null)?.thongBao ?? 'Tải file lên thất bại, vui lòng thử lại.';
    throw new LoiGoiApi(phanHoi.status, thongBao);
  }

  return ketQua as TepTinDaTaiLen;
}

export async function GuiLoiMoiKetBan(token: string, nguoiNhanId: string): Promise<LoiMoiKetBan> {
  return goiApi<LoiMoiKetBan>(`/ketban/loi-moi/${nguoiNhanId}`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function ChapNhanLoiMoiKetBan(token: string, id: string): Promise<LoiMoiKetBan> {
  return goiApi<LoiMoiKetBan>(`/ketban/${id}/chap-nhan`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function TuChoiLoiMoiKetBan(token: string, id: string): Promise<void> {
  await goiApi(`/ketban/${id}/tu-choi`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayBanBe(token: string): Promise<NguoiDungTomTat[]> {
  return goiApi<NguoiDungTomTat[]>('/ketban/ban-be', {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayLoiMoiDen(token: string): Promise<LoiMoiKetBan[]> {
  return goiApi<LoiMoiKetBan[]>('/ketban/loi-moi-den', {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayLoiMoiGui(token: string): Promise<LoiMoiKetBan[]> {
  return goiApi<LoiMoiKetBan[]>('/ketban/loi-moi-gui', {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayThongTinCaNhan(token: string): Promise<HoSoCaNhan> {
  return goiApi<HoSoCaNhan>('/nguoidung/toi', {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function CapNhatCaiDat(
  token: string, choPhepTinNhanTuNguoiLa: boolean, hienThiTrangThaiHoatDong: boolean,
): Promise<void> {
  await goiApi('/nguoidung/cai-dat', {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong }),
  });
}

export async function LayDanhSachHoiThoai(token: string): Promise<HoiThoaiTomTat[]> {
  return goiApi<HoiThoaiTomTat[]>('/tinnhan/hoi-thoai', {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayTrangThaiHoatDong(token: string, ids: string[]): Promise<Record<string, boolean>> {
  if (ids.length === 0) return {};
  return goiApi<Record<string, boolean>>(`/nguoidung/trang-thai?ids=${ids.join(',')}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function TaoNhom(
  token: string, tenNhom: string, moTa: string | null, duongDanAnhDaiDien: string | null, thanhVienIds: string[],
): Promise<Nhom> {
  return goiApi<Nhom>('/nhom', {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenNhom, moTa, duongDanAnhDaiDien, thanhVienIds }),
  });
}

export async function LayDanhSachNhom(token: string): Promise<Nhom[]> {
  return goiApi<Nhom[]>('/nhom', { headers: { Authorization: `Bearer ${token}` } });
}

export async function LayChiTietNhom(token: string, id: string): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${id}`, { headers: { Authorization: `Bearer ${token}` } });
}

export async function CapNhatNhom(
  token: string, id: string, tenNhom: string, moTa: string | null, duongDanAnhDaiDien: string | null,
): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${id}`, {
    method: 'PUT',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenNhom, moTa, duongDanAnhDaiDien }),
  });
}

export async function ThemThanhVien(token: string, nhomId: string, thanhVienId: string): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${nhomId}/thanh-vien`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ thanhVienId }),
  });
}

export async function XoaThanhVien(token: string, nhomId: string, userId: string): Promise<Nhom> {
  return goiApi<Nhom>(`/nhom/${nhomId}/thanh-vien/${userId}`, {
    method: 'DELETE',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function RoiNhom(token: string, nhomId: string): Promise<KetQuaRoiNhom> {
  return goiApi<KetQuaRoiNhom>(`/nhom/${nhomId}/roi-nhom`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function LayLichSuNhom(
  token: string, nhomId: string, truoc?: string, soLuong = 30,
): Promise<TinNhan[]> {
  const thamSo = new URLSearchParams({ soLuong: String(soLuong) });
  if (truoc) thamSo.set('truoc', truoc);
  return goiApi<TinNhan[]>(`/tinnhan/nhom/${nhomId}?${thamSo.toString()}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function GuiYeuCauQuenMatKhau(email: string): Promise<KetQuaThongBao> {
  return goiApi<KetQuaThongBao>('/nguoidung/quen-mat-khau', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  });
}

export async function DatLaiMatKhau(email: string, maOtp: string, matKhauMoi: string): Promise<KetQuaThongBao> {
  return goiApi<KetQuaThongBao>('/nguoidung/dat-lai-mat-khau', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, maOtp, matKhauMoi }),
  });
}
