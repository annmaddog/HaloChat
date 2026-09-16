import { DIA_CHI_GOC } from '../DichVuApi';
import './Avatar.css';

const BANG_MAU = ['#2f7bf6', '#00b894', '#e17055', '#a29bfe', '#fdcb6e', '#00cec9'];

function laySoTuChuoi(chuoi: string): number {
  let tong = 0;
  for (let i = 0; i < chuoi.length; i++) {
    tong += chuoi.charCodeAt(i);
  }
  return tong;
}

interface AvatarProps {
  id: string;
  ten: string;
  kichThuoc?: 'nho' | 'vua' | 'lon';
  duongDanAnh?: string | null;
}

// Chỉ blob:/data: được dùng NGUYÊN VẸN không nối DIA_CHI_GOC — đây là 2
// dạng URL cục bộ trong trình duyệt (preview ảnh chưa tải lên server, xem
// ModalHoanTatHoSo.tsx), không bao giờ được lưu xuống server hay hiển thị
// cho người dùng khác. TUYỆT ĐỐI không thêm http(s): vào danh sách này —
// duongDanAnh là chuỗi tự do do chính người dùng đặt qua API đổi ảnh đại
// diện, nếu cho phép URL ngoài tùy ý thì avatar hiển thị cho người khác sẽ
// biến thành 1 tracking pixel gửi request thật tới máy chủ ngoài.
const MAU_URL_CUC_BO = /^(blob:|data:)/;

export function Avatar({ id, ten, kichThuoc = 'vua', duongDanAnh }: AvatarProps) {
  if (duongDanAnh) {
    const src = MAU_URL_CUC_BO.test(duongDanAnh) ? duongDanAnh : `${DIA_CHI_GOC}${duongDanAnh}`;
    return (
      <img
        className={`avatar avatar--${kichThuoc}`}
        src={src}
        alt={ten}
      />
    );
  }

  const mau = BANG_MAU[laySoTuChuoi(id) % BANG_MAU.length];
  const chuCaiDau = ten.trim().charAt(0).toUpperCase() || '?';
  return (
    <span className={`avatar avatar--${kichThuoc}`} style={{ background: mau }}>
      {chuCaiDau}
    </span>
  );
}
