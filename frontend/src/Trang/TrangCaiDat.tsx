import { useEffect, useState } from 'react';
import { LayThongTinCaNhan, CapNhatCaiDat, DoiMatKhau, DoiTenHienThi, DoiAnhDaiDien, TaiLenTep, LoiGoiApi } from '../DichVuApi';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { CongTac } from '../ThanhPhan/CongTac';
import { Avatar } from '../ThanhPhan/Avatar';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongNguoiDung, BieuTuongEmail, BieuTuongKhoa, BieuTuongMayAnh, BieuTuongLuu } from '../ThanhPhan/BieuTuong';
import { apDungGiaoDien, layGiaoDienDaLuu, type GiaoDien } from '../NguCanh/GiaoDien';
import './TrangCaiDat.css';

const GIOI_HAN_ANH_BYTES = 5 * 1024 * 1024;

type MucCaiDat = 'tai-khoan' | 'quyen-rieng-tu' | 'thong-bao' | 'bao-mat' | 'giao-dien';

export function TrangCaiDat() {
  const { token, nguoiDungHienTai } = useXacThuc();
  const [mucDangChon, setMucDangChon] = useState<MucCaiDat>('tai-khoan');

  const [choPhepTinNhanTuNguoiLa, setChoPhepTinNhanTuNguoiLa] = useState(false);
  const [hienThiTrangThaiHoatDong, setHienThiTrangThaiHoatDong] = useState(true);
  const [choPhepThemVaoNhom, setChoPhepThemVaoNhom] = useState(true);
  const [thongBaoTinNhanMoi, setThongBaoTinNhanMoi] = useState(true);
  const [thongBaoLoiMoiKetBan, setThongBaoLoiMoiKetBan] = useState(true);
  const [thongBaoNhom, setThongBaoNhom] = useState(true);

  const [tenHienThi, setTenHienThi] = useState('');
  const [tenHienThiGoc, setTenHienThiGoc] = useState('');
  const [dangLuuTen, setDangLuuTen] = useState(false);
  const [daLuuTen, setDaLuuTen] = useState(false);
  const [loiDoiTen, setLoiDoiTen] = useState<string | null>(null);

  const [duongDanAnhDaiDien, setDuongDanAnhDaiDien] = useState<string | null>(null);
  const [dangTaiAnh, setDangTaiAnh] = useState(false);
  const [loiAnh, setLoiAnh] = useState<string | null>(null);

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
        setTenHienThi(hoSo.tenHienThi);
        setTenHienThiGoc(hoSo.tenHienThi);
        setDuongDanAnhDaiDien(hoSo.duongDanAnhDaiDien ?? null);
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

  async function xuLyDoiTenHienThi() {
    if (!token || !tenHienThi.trim() || tenHienThi.trim() === tenHienThiGoc) return;
    setLoiDoiTen(null);
    setDaLuuTen(false);
    setDangLuuTen(true);
    try {
      const hoSoMoi = await DoiTenHienThi(token, tenHienThi.trim());
      setTenHienThi(hoSoMoi.tenHienThi);
      setTenHienThiGoc(hoSoMoi.tenHienThi);
      setDaLuuTen(true);
    } catch (loiBat) {
      setLoiDoiTen(loiBat instanceof LoiGoiApi ? loiBat.message : 'Đổi tên hiển thị thất bại.');
    } finally {
      setDangLuuTen(false);
    }
  }

  function doiAnhDaiDien(tep: File) {
    if (!token) return;
    if (!tep.type.startsWith('image/')) {
      setLoiAnh('Chỉ chấp nhận file ảnh.');
      return;
    }
    if (tep.size > GIOI_HAN_ANH_BYTES) {
      setLoiAnh(`Ảnh vượt quá giới hạn ${GIOI_HAN_ANH_BYTES / 1024 / 1024}MB.`);
      return;
    }
    setLoiAnh(null);
    setDangTaiAnh(true);
    TaiLenTep(token, tep)
      .then((daTaiLen) => DoiAnhDaiDien(token, daTaiLen.duongDanFile))
      .then((hoSoMoi) => setDuongDanAnhDaiDien(hoSoMoi.duongDanAnhDaiDien ?? null))
      .catch(() => setLoiAnh('Đổi ảnh đại diện thất bại.'))
      .finally(() => setDangTaiAnh(false));
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
            <div className="trang-cai-dat__the trang-cai-dat__the-tk">
              <div className="trang-cai-dat__ngan">
                <span className="trang-cai-dat__ngan-icon"><BieuTuongNguoiDung /></span>
                <h2 className="trang-cai-dat__ngan-tieu-de">Thông tin tài khoản</h2>
              </div>

              <div className="trang-cai-dat__than">
                <div className="trang-cai-dat__hang-tk">
                  <Avatar id={nguoiDungHienTai?.id ?? ''} ten={tenHienThi || '?'} kichThuoc="lon" duongDanAnh={duongDanAnhDaiDien} />

                  <div className="trang-cai-dat__thong-tin-co-ban">
                    <p className="trang-cai-dat__dong-tt">
                      <BieuTuongNguoiDung />
                      <span>Tên tài khoản<br /><strong>{nguoiDungHienTai?.tenTaiKhoan}</strong></span>
                    </p>
                    <p className="trang-cai-dat__dong-tt">
                      <BieuTuongEmail />
                      <span>Email<br /><strong>{nguoiDungHienTai?.email}</strong></span>
                    </p>
                  </div>

                  <div className="trang-cai-dat__ngan-doc" />

                  <div className="trang-cai-dat__cot-ten">
                    <label className="trang-cai-dat__nhan-ten" htmlFor="trang-cai-dat-ten-hien-thi">Tên hiển thị</label>
                    {loiDoiTen && (
                      <p className="thong-bao-loi" role="alert">
                        {loiDoiTen}
                      </p>
                    )}
                    {daLuuTen && <p className="trang-cai-dat__da-luu">Đã lưu tên hiển thị.</p>}
                    <input
                      id="trang-cai-dat-ten-hien-thi"
                      type="text"
                      className="trang-cai-dat__o-ten"
                      value={tenHienThi}
                      onChange={(su) => { setTenHienThi(su.target.value); setDaLuuTen(false); }}
                      disabled={dangLuuTen}
                      maxLength={50}
                    />
                    <span className="trang-cai-dat__dem-ky-tu">{tenHienThi.length}/50</span>
                  </div>
                </div>

                <hr className="trang-cai-dat__gach" />

                <div className="trang-cai-dat__hang-duoi">
                  <div className="trang-cai-dat__doi-anh-cum">
                    <label className="trang-cai-dat__nut-doi-anh">
                      <BieuTuongMayAnh />
                      {dangTaiAnh ? 'Đang tải...' : 'Đổi ảnh đại diện'}
                      <input
                        type="file"
                        accept="image/jpeg,image/png,image/gif,image/webp"
                        hidden
                        disabled={dangTaiAnh}
                        onChange={(su) => {
                          const tep = su.target.files?.[0];
                          if (tep) doiAnhDaiDien(tep);
                          su.target.value = '';
                        }}
                      />
                    </label>
                    <span className="trang-cai-dat__anh-goi-y">JPG, PNG, WEBP • Tối đa 5MB</span>
                    {loiAnh && <p className="thong-bao-loi" role="alert">{loiAnh}</p>}
                  </div>
                  <button
                    className="nut-chinh trang-cai-dat__nut-nho"
                    onClick={xuLyDoiTenHienThi}
                    disabled={dangLuuTen || !tenHienThi.trim() || tenHienThi.trim() === tenHienThiGoc}
                  >
                    <BieuTuongLuu /> {dangLuuTen ? 'Đang lưu...' : 'Lưu thay đổi'}
                  </button>
                </div>
              </div>
            </div>

            <form className="trang-cai-dat__the" onSubmit={xuLyDoiMatKhau}>
              <div className="trang-cai-dat__the-dau">
                <span className="trang-cai-dat__the-icon"><BieuTuongKhoa /></span>
                <h2 className="trang-cai-dat__the-tieu-de">Đổi mật khẩu</h2>
              </div>
              <p className="trang-cai-dat__the-phu-de">Để đảm bảo an toàn cho tài khoản, vui lòng đặt mật khẩu mạnh.</p>
              {loiDoiMatKhau && (
                <p className="thong-bao-loi" role="alert">
                  {loiDoiMatKhau}
                </p>
              )}
              {thanhCongDoiMatKhau && <p className="trang-cai-dat__da-luu">Đã đổi mật khẩu thành công.</p>}
              <div className="trang-cai-dat__khoi-hep">
                <TruongNhap
                  nhan="Mật khẩu hiện tại"
                  anNhan
                  bieuTuong={<BieuTuongKhoa />}
                  coTheAn
                  placeholder="Mật khẩu hiện tại"
                  value={matKhauCu}
                  onChange={(su) => setMatKhauCu(su.target.value)}
                  required
                />
                <TruongNhap
                  nhan="Mật khẩu mới"
                  anNhan
                  bieuTuong={<BieuTuongKhoa />}
                  coTheAn
                  placeholder="Mật khẩu mới"
                  value={matKhauMoi}
                  onChange={(su) => setMatKhauMoi(su.target.value)}
                  required
                  minLength={6}
                />
                <TruongNhap
                  nhan="Xác nhận mật khẩu mới"
                  anNhan
                  bieuTuong={<BieuTuongKhoa />}
                  coTheAn
                  placeholder="Xác nhận mật khẩu mới"
                  value={xacNhanMatKhauMoi}
                  onChange={(su) => setXacNhanMatKhauMoi(su.target.value)}
                  required
                  minLength={6}
                />
                <div className="trang-cai-dat__hang-nut">
                  <button type="submit" className="nut-chinh trang-cai-dat__nut-nho" disabled={dangDoiMatKhau}>
                    <BieuTuongKhoa /> {dangDoiMatKhau ? 'Đang đổi...' : 'Cập nhật mật khẩu'}
                  </button>
                </div>
              </div>
            </form>
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
