import { useEffect, useState } from 'react';
import { LayThongTinCaNhan, CapNhatCaiDat, DoiMatKhau, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { CongTac } from '../ThanhPhan/CongTac';
import { apDungGiaoDien, layGiaoDienDaLuu, type GiaoDien } from '../NguCanh/GiaoDien';
import './TrangCaiDat.css';

type MucCaiDat = 'tai-khoan' | 'quyen-rieng-tu' | 'thong-bao' | 'bao-mat' | 'giao-dien';

export function TrangCaiDat() {
  const { token, nguoiDungHienTai, dangXuat } = useXacThuc();
  const [mucDangChon, setMucDangChon] = useState<MucCaiDat>('tai-khoan');

  const [choPhepTinNhanTuNguoiLa, setChoPhepTinNhanTuNguoiLa] = useState(false);
  const [hienThiTrangThaiHoatDong, setHienThiTrangThaiHoatDong] = useState(true);
  const [choPhepThemVaoNhom, setChoPhepThemVaoNhom] = useState(true);
  const [thongBaoTinNhanMoi, setThongBaoTinNhanMoi] = useState(true);
  const [thongBaoLoiMoiKetBan, setThongBaoLoiMoiKetBan] = useState(true);
  const [thongBaoNhom, setThongBaoNhom] = useState(true);

  const [dangTai, setDangTai] = useState(true);
  const [dangLuu, setDangLuu] = useState(false);
  const [daLuu, setDaLuu] = useState(false);
  const [loi, setLoi] = useState<string | null>(null);

  const [giaoDien, setGiaoDien] = useState<GiaoDien>(layGiaoDienDaLuu());

  const [matKhauCu, setMatKhauCu] = useState('');
  const [matKhauMoi, setMatKhauMoi] = useState('');
  const [xacNhanMatKhauMoi, setXacNhanMatKhauMoi] = useState('');
  const [loiDoiMatKhau, setLoiDoiMatKhau] = useState<string | null>(null);
  const [thanhCongDoiMatKhau, setThanhCongDoiMatKhau] = useState(false);
  const [dangDoiMatKhau, setDangDoiMatKhau] = useState(false);

  useEffect(() => {
    if (!token) return;
    LayThongTinCaNhan(token)
      .then((hoSo) => {
        setChoPhepTinNhanTuNguoiLa(hoSo.choPhepTinNhanTuNguoiLa);
        setHienThiTrangThaiHoatDong(hoSo.hienThiTrangThaiHoatDong);
        setChoPhepThemVaoNhom(hoSo.choPhepThemVaoNhom);
        setThongBaoTinNhanMoi(hoSo.thongBaoTinNhanMoi);
        setThongBaoLoiMoiKetBan(hoSo.thongBaoLoiMoiKetBan);
        setThongBaoNhom(hoSo.thongBaoNhom);
      })
      .catch((loiBat) => setLoi(loiBat instanceof Error ? loiBat.message : 'Không tải được cài đặt.'))
      .finally(() => setDangTai(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function luu(giaTriMoi: {
    choPhepTinNhanTuNguoiLa: boolean;
    hienThiTrangThaiHoatDong: boolean;
    choPhepThemVaoNhom: boolean;
    thongBaoTinNhanMoi: boolean;
    thongBaoLoiMoiKetBan: boolean;
    thongBaoNhom: boolean;
  }) {
    if (!token) return;
    const truoc = {
      choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom,
      thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom,
    };
    setChoPhepTinNhanTuNguoiLa(giaTriMoi.choPhepTinNhanTuNguoiLa);
    setHienThiTrangThaiHoatDong(giaTriMoi.hienThiTrangThaiHoatDong);
    setChoPhepThemVaoNhom(giaTriMoi.choPhepThemVaoNhom);
    setThongBaoTinNhanMoi(giaTriMoi.thongBaoTinNhanMoi);
    setThongBaoLoiMoiKetBan(giaTriMoi.thongBaoLoiMoiKetBan);
    setThongBaoNhom(giaTriMoi.thongBaoNhom);
    setDangLuu(true);
    setDaLuu(false);
    try {
      await CapNhatCaiDat(
        token, giaTriMoi.choPhepTinNhanTuNguoiLa, giaTriMoi.hienThiTrangThaiHoatDong,
        giaTriMoi.choPhepThemVaoNhom, giaTriMoi.thongBaoTinNhanMoi,
        giaTriMoi.thongBaoLoiMoiKetBan, giaTriMoi.thongBaoNhom,
      );
      setDaLuu(true);
    } catch (loiBat) {
      setChoPhepTinNhanTuNguoiLa(truoc.choPhepTinNhanTuNguoiLa);
      setHienThiTrangThaiHoatDong(truoc.hienThiTrangThaiHoatDong);
      setChoPhepThemVaoNhom(truoc.choPhepThemVaoNhom);
      setThongBaoTinNhanMoi(truoc.thongBaoTinNhanMoi);
      setThongBaoLoiMoiKetBan(truoc.thongBaoLoiMoiKetBan);
      setThongBaoNhom(truoc.thongBaoNhom);
      setLoi(loiBat instanceof LoiGoiApi ? loiBat.message : 'Lưu cài đặt thất bại.');
    } finally {
      setDangLuu(false);
    }
  }

  function doiGiaoDien(moi: GiaoDien) {
    setGiaoDien(moi);
    apDungGiaoDien(moi);
  }

  async function xuLyDoiMatKhau(su: React.FormEvent) {
    su.preventDefault();
    setLoiDoiMatKhau(null);
    setThanhCongDoiMatKhau(false);

    if (matKhauMoi !== xacNhanMatKhauMoi) {
      setLoiDoiMatKhau('Xác nhận mật khẩu mới không khớp.');
      return;
    }
    if (!token) return;

    setDangDoiMatKhau(true);
    try {
      await DoiMatKhau(token, matKhauCu, matKhauMoi);
      setThanhCongDoiMatKhau(true);
      setMatKhauCu('');
      setMatKhauMoi('');
      setXacNhanMatKhauMoi('');
    } catch (loiBat) {
      setLoiDoiMatKhau(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi mật khẩu thất bại.');
    } finally {
      setDangDoiMatKhau(false);
    }
  }

  return (
    <div className="trang-cai-dat">
      <nav className="trang-cai-dat__menu">
        <button className={mucDangChon === 'tai-khoan' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('tai-khoan')}>
          Tài khoản
        </button>
        <button className={mucDangChon === 'quyen-rieng-tu' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('quyen-rieng-tu')}>
          Quyền riêng tư
        </button>
        <button className={mucDangChon === 'thong-bao' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('thong-bao')}>
          Thông báo
        </button>
        <button className={mucDangChon === 'bao-mat' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('bao-mat')}>
          Bảo mật
        </button>
        <button className={mucDangChon === 'giao-dien' ? 'trang-cai-dat__muc-menu--chon' : 'trang-cai-dat__muc-menu'} onClick={() => setMucDangChon('giao-dien')}>
          Giao diện
        </button>
      </nav>

      <div className="trang-cai-dat__noi-dung">
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}

        {mucDangChon === 'tai-khoan' && (
          <>
            <h2>Tài khoản</h2>
            <p><strong>Tên tài khoản:</strong> {nguoiDungHienTai?.tenTaiKhoan}</p>
            <p><strong>Email:</strong> {nguoiDungHienTai?.email}</p>

            <form className="trang-cai-dat__form-mat-khau" onSubmit={xuLyDoiMatKhau}>
              <h3>Đổi mật khẩu</h3>
              {loiDoiMatKhau && (
                <p className="thong-bao-loi" role="alert">
                  {loiDoiMatKhau}
                </p>
              )}
              {thanhCongDoiMatKhau && <p className="trang-cai-dat__da-luu">Đã đổi mật khẩu thành công.</p>}
              <input
                type="password"
                placeholder="Mật khẩu cũ"
                value={matKhauCu}
                onChange={(su) => setMatKhauCu(su.target.value)}
                required
              />
              <input
                type="password"
                placeholder="Mật khẩu mới"
                value={matKhauMoi}
                onChange={(su) => setMatKhauMoi(su.target.value)}
                required
                minLength={6}
              />
              <input
                type="password"
                placeholder="Xác nhận mật khẩu mới"
                value={xacNhanMatKhauMoi}
                onChange={(su) => setXacNhanMatKhauMoi(su.target.value)}
                required
                minLength={6}
              />
              <button type="submit" className="nut-chinh" disabled={dangDoiMatKhau}>
                {dangDoiMatKhau ? 'Đang đổi...' : 'Đổi mật khẩu'}
              </button>
            </form>

            <button className="trang-cai-dat__nut-dang-xuat" onClick={dangXuat}>Đăng xuất</button>
          </>
        )}

        {mucDangChon === 'quyen-rieng-tu' && (
          <>
            <h2>Quyền riêng tư</h2>
            <label className="trang-cai-dat__dong">
              <span>Cho phép người lạ (chưa kết bạn) nhắn tin cho tôi</span>
              <CongTac
                batTat={choPhepTinNhanTuNguoiLa}
                disabled={dangTai || dangLuu}
                nhan="Cho phép người lạ nhắn tin cho tôi"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa: gt, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Hiển thị trạng thái hoạt động (online/offline) cho bạn bè</span>
              <CongTac
                batTat={hienThiTrangThaiHoatDong}
                disabled={dangTai || dangLuu}
                nhan="Hiển thị trạng thái hoạt động"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong: gt, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Cho phép người khác thêm tôi vào nhóm</span>
              <CongTac
                batTat={choPhepThemVaoNhom}
                disabled={dangTai || dangLuu}
                nhan="Cho phép thêm tôi vào nhóm"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom: gt, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
          </>
        )}

        {mucDangChon === 'thong-bao' && (
          <>
            <h2>Thông báo</h2>
            <label className="trang-cai-dat__dong">
              <span>Tin nhắn mới</span>
              <CongTac
                batTat={thongBaoTinNhanMoi}
                disabled={dangTai || dangLuu}
                nhan="Thông báo tin nhắn mới"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi: gt, thongBaoLoiMoiKetBan, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Lời mời kết bạn</span>
              <CongTac
                batTat={thongBaoLoiMoiKetBan}
                disabled={dangTai || dangLuu}
                nhan="Thông báo lời mời kết bạn"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan: gt, thongBaoNhom })}
              />
            </label>
            <label className="trang-cai-dat__dong">
              <span>Thông báo nhóm</span>
              <CongTac
                batTat={thongBaoNhom}
                disabled={dangTai || dangLuu}
                nhan="Thông báo nhóm"
                onDoi={(gt) => luu({ choPhepTinNhanTuNguoiLa, hienThiTrangThaiHoatDong, choPhepThemVaoNhom, thongBaoTinNhanMoi, thongBaoLoiMoiKetBan, thongBaoNhom: gt })}
              />
            </label>
            {daLuu && <p className="trang-cai-dat__da-luu">Đã lưu.</p>}
          </>
        )}

        {mucDangChon === 'bao-mat' && (
          <>
            <h2>Bảo mật</h2>
            <p className="trang-cai-dat__sap-ra-mat">🔒 Mã hóa tin nhắn AES-256-GCM + quản lý khóa RSA — sắp ra mắt (GĐ6).</p>
          </>
        )}

        {mucDangChon === 'giao-dien' && (
          <>
            <h2>Giao diện</h2>
            <div className="trang-cai-dat__giao-dien">
              <button
                className={`trang-cai-dat__nut-giao-dien${giaoDien === 'sang' ? ' trang-cai-dat__nut-giao-dien--chon' : ''}`}
                onClick={() => doiGiaoDien('sang')}
              >
                Sáng
              </button>
              <button
                className={`trang-cai-dat__nut-giao-dien${giaoDien === 'toi' ? ' trang-cai-dat__nut-giao-dien--chon' : ''}`}
                onClick={() => doiGiaoDien('toi')}
              >
                Tối
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
