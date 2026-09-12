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

export type LoaiTinNhan = 'Text' | 'Anh' | 'File';

export interface TinNhan {
  id: string;
  nguoiGuiId: string;
  nguoiNhanId: string | null;
  loaiTinNhan: LoaiTinNhan;
  noiDungTinNhan: string;
  duongDanFile: string | null;
  tenFileGoc: string | null;
  kichThuocFile: number | null;
  loaiFile: string | null;
  daDoc: boolean;
  thoiGianTao: string;
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
}
