import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { KhungXacThuc } from '../ThanhPhan/KhungXacThuc';
import { TruongNhap } from '../ThanhPhan/TruongNhap';
import { BieuTuongNguoiDung, BieuTuongKhoa, BieuTuongMuiTen } from '../ThanhPhan/BieuTuong';

export function TrangDangNhap() {
  const [tenDangNhap, setTenDangNhap] = useState('');
  const [matKhau, setMatKhau] = useState('');
  const [loi, setLoi] = useState<string | null>(null);
  const [dangGui, setDangGui] = useState(false);
  const { dangNhap } = useXacThuc();
  const dieuHuong = useNavigate();

  async function xuLySubmit(suKien: FormEvent) {
    suKien.preventDefault();
    setLoi(null);
    setDangGui(true);
    try {
      await dangNhap(tenDangNhap, matKhau);
      dieuHuong('/nguoi-dung');
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
          nhan="Tên tài khoản hoặc Email"
          bieuTuong={<BieuTuongNguoiDung />}
          value={tenDangNhap}
          onChange={(e) => setTenDangNhap(e.target.value)}
          placeholder="Nhập tên đăng nhập..."
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
          Đăng nhập <BieuTuongMuiTen />
        </button>
      </form>
    </KhungXacThuc>
  );
}
