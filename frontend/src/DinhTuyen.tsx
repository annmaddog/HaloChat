import { Routes, Route, Navigate } from 'react-router-dom';
import { TuyenDuongRieng } from './ThanhPhan/TuyenDuongRieng';
import { TrangDangKy } from './Trang/TrangDangKy';
import { TrangDangNhap } from './Trang/TrangDangNhap';
import { TrangChat } from './Trang/TrangChat';

export function DinhTuyen() {
  return (
    <Routes>
      <Route path="/dang-ky" element={<TrangDangKy />} />
      <Route path="/dang-nhap" element={<TrangDangNhap />} />
      <Route
        path="/nguoi-dung"
        element={
          <TuyenDuongRieng>
            <TrangChat />
          </TuyenDuongRieng>
        }
      />
      <Route path="*" element={<Navigate to="/dang-nhap" replace />} />
    </Routes>
  );
}
