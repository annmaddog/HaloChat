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
