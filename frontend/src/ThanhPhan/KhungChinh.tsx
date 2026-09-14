import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { BieuTuongTinNhan, BieuTuongBanBe, BieuTuongNhom, BieuTuongCaiDat } from './BieuTuong';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';
import { SuDungSoLuongChuaDoc } from '../NguCanh/SuDungSoLuongChuaDoc';
import { Huy } from './Huy';
import './KhungChinh.css';

function lopMuc({ isActive }: { isActive: boolean }): string {
  return `khung-chinh__muc${isActive ? ' khung-chinh__muc--dang-chon' : ''}`;
}

export function KhungChinh({ children }: { children: ReactNode }) {
  const { dangXuat } = useXacThuc();
  const soLuong = SuDungSoLuongChuaDoc();

  return (
    <div className="khung-chinh">
      <nav className="khung-chinh__rail">
        <div className="khung-chinh__logo">HaloChat</div>
        <NavLink to="/nguoi-dung" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongTinNhan />
            <Huy soLuong={soLuong.tinNhan} />
          </span>
          <span>Tin nhắn</span>
        </NavLink>
        <NavLink to="/ban-be" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongBanBe />
            <Huy soLuong={soLuong.banBe} />
          </span>
          <span>Bạn bè</span>
        </NavLink>
        <NavLink to="/nhom" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongNhom />
            <Huy soLuong={soLuong.nhom} />
          </span>
          <span>Nhóm</span>
        </NavLink>
        <NavLink to="/cai-dat" className={lopMuc}>
          <span className="khung-chinh__icon-cum">
            <BieuTuongCaiDat />
          </span>
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
