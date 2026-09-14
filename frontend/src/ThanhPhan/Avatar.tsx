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

export function Avatar({ id, ten, kichThuoc = 'vua', duongDanAnh }: AvatarProps) {
  if (duongDanAnh) {
    return (
      <img
        className={`avatar avatar--${kichThuoc}`}
        src={`${DIA_CHI_GOC}${duongDanAnh}`}
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
