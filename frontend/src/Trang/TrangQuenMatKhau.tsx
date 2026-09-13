import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { GuiYeuCauQuenMatKhau, DatLaiMatKhau } from '../DichVuApi';
import { KhungXacThuc } from '../ThanhPhan/KhungXacThuc';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongEmail, BieuTuongKhoa, BieuTuongMuiTen } from '../ThanhPhan/BieuTuong';

export function TrangQuenMatKhau() {
  const [email, setEmail] = useState('');
  const [daGuiOtp, setDaGuiOtp] = useState(false);
  const [maOtp, setMaOtp] = useState('');
  const [matKhauMoi, setMatKhauMoi] = useState('');
  const [thongBao, setThongBao] = useState<string | null>(null);
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);
  const dieuHuong = useNavigate();

  async function xuLyGuiOtp(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setThongBao(null);
    setDangGui(true);
    try {
      const ketQua = await GuiYeuCauQuenMatKhau(email);
      setThongBao(ketQua.thongBao);
      setDaGuiOtp(true);
    } catch (loiBat) {
      setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  async function xuLyDatLaiMatKhau(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setDangGui(true);
    try {
      await DatLaiMatKhau(email, maOtp, matKhauMoi);
      dieuHuong('/dang-nhap', { state: { thongBaoDatLaiMatKhau: 'Đặt lại mật khẩu thành công. Vui lòng đăng nhập.' } });
    } catch (loiBat) {
      setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  return (
    <KhungXacThuc>
      <form onSubmit={daGuiOtp ? xuLyDatLaiMatKhau : xuLyGuiOtp}>
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        {thongBao && !loi && <p>{thongBao}</p>}

        <TruongNhap
          nhan="Email"
          bieuTuong={<BieuTuongEmail />}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Nhập email đã đăng ký..."
          required
          disabled={daGuiOtp}
        />

        {!daGuiOtp && (
          <button type="submit" className="nut-chinh" disabled={dangGui}>
            Gửi mã OTP <BieuTuongMuiTen />
          </button>
        )}

        {daGuiOtp && (
          <>
            <TruongNhap
              nhan="Mã OTP"
              bieuTuong={<BieuTuongKhoa />}
              value={maOtp}
              onChange={(e) => setMaOtp(e.target.value)}
              placeholder="Nhập mã 6 số..."
              required
              inputMode="numeric"
              maxLength={6}
              autoComplete="one-time-code"
            />
            <TruongNhap
              nhan="Mật khẩu mới"
              bieuTuong={<BieuTuongKhoa />}
              coTheAn
              value={matKhauMoi}
              onChange={(e) => setMatKhauMoi(e.target.value)}
              placeholder="Nhập mật khẩu mới..."
              required
            />
            <button type="submit" className="nut-chinh" disabled={dangGui}>
              Đặt lại mật khẩu <BieuTuongMuiTen />
            </button>
            <button
              type="button"
              onClick={() => {
                setDaGuiOtp(false);
                setThongBao(null);
              }}
              disabled={dangGui}
            >
              Sửa lại email / gửi lại mã
            </button>
          </>
        )}
      </form>
    </KhungXacThuc>
  );
}
