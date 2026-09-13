import { Routes, Route, Navigate } from 'react-router-dom';
import { TuyenDuongRieng } from './ThanhPhan/TuyenDuongRieng';
import { KhungChinh } from './ThanhPhan/KhungChinh';
import { TrangDangKy } from './Trang/TrangDangKy';
import { TrangDangNhap } from './Trang/TrangDangNhap';
import { TrangQuenMatKhau } from './Trang/TrangQuenMatKhau';
import { TrangChat } from './Trang/TrangChat';
import { TrangBanBe } from './Trang/TrangBanBe';
import { TrangCaiDat } from './Trang/TrangCaiDat';
import { TrangNhom } from './Trang/TrangNhom';

export function DinhTuyen() {
  return (
    <Routes>
      <Route path="/dang-ky" element={<TrangDangKy />} />
      <Route path="/dang-nhap" element={<TrangDangNhap />} />
      <Route path="/quen-mat-khau" element={<TrangQuenMatKhau />} />
      <Route
        path="/nguoi-dung"
        element={
          <TuyenDuongRieng>
            <KhungChinh>
              <TrangChat />
            </KhungChinh>
          </TuyenDuongRieng>
        }
      />
      <Route
        path="/ban-be"
        element={
          <TuyenDuongRieng>
            <KhungChinh>
              <TrangBanBe />
            </KhungChinh>
          </TuyenDuongRieng>
        }
      />
      <Route
        path="/nhom"
        element={
          <TuyenDuongRieng>
            <KhungChinh>
              <TrangNhom />
            </KhungChinh>
          </TuyenDuongRieng>
        }
      />
      <Route
        path="/cai-dat"
        element={
          <TuyenDuongRieng>
            <KhungChinh>
              <TrangCaiDat />
            </KhungChinh>
          </TuyenDuongRieng>
        }
      />
      <Route path="*" element={<Navigate to="/dang-nhap" replace />} />
    </Routes>
  );
}
