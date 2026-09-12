import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { BieuTuongTinNhan, BieuTuongBanBe, BieuTuongNhom, BieuTuongCaiDat } from './BieuTuong';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import './KhungChinh.css';

function lopMuc({ isActive }: { isActive: boolean }): string {
  return `khung-chinh__muc${isActive ? ' khung-chinh__muc--dang-chon' : ''}`;
}

export function KhungChinh({ children }: { children: ReactNode }) {
  const { dangXuat } = useXacThuc();
  return (
    <div className="khung-chinh">
      <nav className="khung-chinh__rail">
        <div className="khung-chinh__logo">HaloChat</div>
        <NavLink to="/nguoi-dung" className={lopMuc}>
          <BieuTuongTinNhan />
          <span>Tin nhắn</span>
        </NavLink>
        <NavLink to="/ban-be" className={lopMuc}>
          <BieuTuongBanBe />
          <span>Bạn bè</span>
        </NavLink>
        <span className="khung-chinh__muc khung-chinh__muc--sap-ra-mat" title="Sắp ra mắt">
          <BieuTuongNhom />
          <span>Nhóm</span>
        </span>
        <NavLink to="/cai-dat" className={lopMuc}>
          <BieuTuongCaiDat />
          <span>Cài đặt</span>
        </NavLink>
        <button className="khung-chinh__dang-xuat" onClick={dangXuat}>
          Đăng xuất
        </button>
      </nav>
      <div className="khung-chinh__noi-dung">{children}</div>
    </div>
  );
}
