import { useEffect, useState } from 'react';
import { LayThongTinCaNhan, CapNhatCaiDat, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import './TrangCaiDat.css';

type MucCaiDat = 'quyen-rieng-tu' | 'tai-khoan' | 'bao-mat' | 'thong-bao';

export function TrangCaiDat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const [mucDangChon, setMucDangChon] = useState<MucCaiDat>('quyen-rieng-tu');
  const [choPhepTinNhanTuNguoiLa, setChoPhepTinNhanTuNguoiLa] = useState(false);
  const [hienThiTrangThaiHoatDong, setHienThiTrangThaiHoatDong] = useState(true);
  const [dangTai, setDangTai] = useState(true);
  const [dangLuu, setDangLuu] = useState(false);
  const [daLuu, setDaLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;
    LayThongTinCaNhan(token)
      .then((hoSo) => {
        setChoPhepTinNhanTuNguoiLa(hoSo.choPhepTinNhanTuNguoiLa);
        setHienThiTrangThaiHoatDong(hoSo.hienThiTrangThaiHoatDong);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được cài đặt.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function luu(choPhepMoi: boolean, hienThiMoi: boolean) {
    if (!token) return;
    const choPhepCu = choPhepTinNhanTuNguoiLa;
    const hienThiCu = hienThiTrangThaiHoatDong;
    setChoPhepTinNhanTuNguoiLa(choPhepMoi);
    setHienThiTrangThaiHoatDong(hienThiMoi);
    setDangLuu(true);
    setDaLuu(false);
    try {
      await CapNhatCaiDat(token, choPhepMoi, hienThiMoi);
      setDaLuu(true);
    } catch (loiBat) {
      setChoPhepTinNhanTuNguoiLa(choPhepCu);
      setHienThiTrangThaiHoatDong(hienThiCu);
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Lưu cài đặt thất bại.');
    } finally {
      setDangLuu(false);
    }
  }

  return (
    <div className="trang-cai-dat">
      <nav className="trang-cai-dat__menu">
        <button className={mucDangChon === 'quyen-rieng-tu' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('quyen-rieng-tu')}>
          Quyền riêng tư
        </button>
        <button className={mucDangChon === 'tai-khoan' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('tai-khoan')}>
          Tài khoản
        </button>
        <button className={mucDangChon === 'bao-mat' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('bao-mat')}>
          Bảo mật
        </button>
        <button className={mucDangChon === 'thong-bao' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('thong-bao')}>
          Thông báo
        </button>
      </nav>

      <div className="trang-cai-dat__noi-dung">
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}

        {mucDangChon === 'quyen-rieng-tu' && (
          <>
            <h2>Quyền riêng tư</h2>
            <label className="trang-cai-dat__dong">
              <input
                type="checkbox"
                checked={choPhepTinNhanTuNguoiLa}
                disabled={dangTai || dangLuu}
                onChange={(su) => luu(su.target.checked, hienThiTrangThaiHoatDong)}
              />
              <span>Cho phép người lạ (chưa kết bạn) nhắn tin cho tôi</span>
            </label>
            <label className="trang-cai-dat__dong">
              <input
                type="checkbox"
                checked={hienThiTrangThaiHoatDong}
                disabled={dangTai || dangLuu}
                onChange={(su) => luu(choPhepTinNhanTuNguoiLa, su.target.checked)}
              />
              <span>Hiển thị trạng thái hoạt động (online/offline) cho bạn bè</span>
            </label>
            {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
          </>
        )}

        {mucDangChon === 'tai-khoan' && (
          <>
            <h2>Tài khoản</h2>
            <p><strong>Tên tài khoản:</strong> {nguoiDungHienTai?.tenTaiKhoan}</p>
            <p><strong>Email:</strong> {nguoiDungHienTai?.email}</p>
            <button className="trang-cai-dat__nut-sap-ra-mat" disabled title="Sắp ra mắt">Đổi mật khẩu</button>
            <button className="trang-cai-dat__nut-dang-xuat" onClick={dangXuat}>Đăng xuất</button>
          </>
        )}

        {mucDangChon === 'bao-mat' && (
          <>
            <h2>Bảo mật</h2>
            <p className="trang-cai-dat__sap-ra-mat">🔒 Mã hóa tin nhắn AES-256-GCM + quản lý khóa RSA — sắp ra mắt (GĐ6).</p>
          </>
        )}

        {mucDangChon === 'thong-bao' && (
          <>
            <h2>Thông báo</h2>
            <p className="trang-cai-dat__sap-ra-mat">🔔 Tùy chỉnh loại thông báo — sắp ra mắt.</p>
          </>
        )}
      </div>
    </div>
  );
}
