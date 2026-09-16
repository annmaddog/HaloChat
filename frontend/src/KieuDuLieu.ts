export interface NguoiDungTomTat {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  tenHienThi: string;
}

export interface KetQuaDangKy {
  thongBao: string;
}

export interface KetQuaDangNhap {
  token: string;
}

export type LoaiTinNhan = 'Text' | 'Anh' | 'File';

export interface TinNhan {
  id: string;
  nguoiGuiId: string;
  nguoiNhanId: string | null;
  nhomId: string | null;
  loaiTinNhan: LoaiTinNhan;
  noiDungTinNhan: string;
  duongDanFile: string | null;
  tenFileGoc: string | null;
  kichThuocFile: number | null;
  loaiFile: string | null;
  daDoc: boolean;
  daNhan: boolean;
  thoiGianTao: string;
  traLoi: { id: string; tenNguoiGui: string; noiDungTomTat: string; loaiTinNhan: LoaiTinNhan } | null;
}

export interface TepTinDaTaiLen {
  duongDanFile: string;
  tenFileGoc: string;
  kichThuocFile: number;
  loaiFile: string;
}

export interface LoiMoiKetBan {
  id: string;
  nguoiGui: NguoiDungTomTat;
  nguoiNhan: NguoiDungTomTat;
  trangThai: 'ChoDuyet' | 'DaChapNhan' | 'DaTuChoi';
  thoiGianTao: string;
}

export interface HoiThoaiTomTat {
  nguoiDung: NguoiDungTomTat;
  tinNhanCuoi: string;
  thoiGianTinNhanCuoi: string;
  soTinChuaDoc: number;
}

export interface HoSoCaNhan {
  id: string;
  tenTaiKhoan: string;
  email: string;
  choPhepTinNhanTuNguoiLa: boolean;
  hienThiTrangThaiHoatDong: boolean;
  choPhepThemVaoNhom: boolean;
  thongBaoTinNhanMoi: boolean;
  thongBaoLoiMoiKetBan: boolean;
  thongBaoNhom: boolean;
  tenHienThi: string;
}

export interface Nhom {
  id: string;
  tenNhom: string;
  moTa: string | null;
  duongDanAnhDaiDien: string | null;
  nguoiTaoId: string;
  thanhVien: NguoiDungTomTat[];
  thoiGianTao: string;
}

export interface KetQuaRoiNhom {
  daGiaiTan: boolean;
  thanhVienConLai: string[];
}

export interface KetQuaThongBao {
  thongBao: string;
}

export interface SoTinNhomChuaDoc {
  soTinChuaDoc: number;
}
