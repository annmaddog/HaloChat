import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { DangKy } from '../DichVuApi';
import { KhungXacThuc } from '../ThanhPhan/KhungXacThuc';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongNguoiDung, BieuTuongEmail, BieuTuongKhoa, BieuTuongMuiTen } from '../ThanhPhan/BieuTuong';

export function TrangDangKy() {
  const [tenTaiKhoan, setTenTaiKhoan] = useState('');
  const [email, setEmail] = useState('');
  const [matKhau, setMatKhau] = useState('');
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);
  const dieuHuong = useNavigate();

  async function xuLySubmit(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setDangGui(true);
    try {
      await DangKy(tenTaiKhoan, email, matKhau);
      dieuHuong('/dang-nhap');
    } catch (loiBat) {
      setLoi(loiBat instanceof Error ? loiBat.message : 'Đã có lỗi xảy ra.');
    } finally {
      setDangGui(false);
    }
  }

  return (
    <KhungXacThuc>
      <form onSubmit={xuLySubmit}>
        {loi && (
          <p className="thong-bao-loi" role="alert">
            {loi}
          </p>
        )}
        <TruongNhap
          nhan="Tên tài khoản"
          bieuTuong={<BieuTuongNguoiDung />}
          value={tenTaiKhoan}
          onChange={(e) => setTenTaiKhoan(e.target.value)}
          placeholder="Nhập tên tài khoản..."
          required
        />
        <TruongNhap
          nhan="Email"
          bieuTuong={<BieuTuongEmail />}
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Nhập email..."
          required
        />
        <TruongNhap
          nhan="Mật khẩu"
          bieuTuong={<BieuTuongKhoa />}
          coTheAn
          value={matKhau}
          onChange={(e) => setMatKhau(e.target.value)}
          placeholder="Nhập mật khẩu..."
          required
        />
        <button type="submit" className="nut-chinh" disabled={dangGui}>
          Đăng ký <BieuTuongMuiTen />
        </button>
      </form>
    </KhungXacThuc>
  );
}
