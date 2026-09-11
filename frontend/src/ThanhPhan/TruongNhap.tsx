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
