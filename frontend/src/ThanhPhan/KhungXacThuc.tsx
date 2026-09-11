import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import './KhungXacThuc.css';

export function KhungXacThuc({ children }: { children: ReactNode }) {
  return (
    <div className="khung-xac-thuc">
      <div className="the-xac-thuc">
        <img src="/halochat-logo.png" alt="HaloChat" className="logo-xac-thuc" />
        <h1 className="tieu-de-xac-thuc">HaloChat</h1>
        <p className="khau-hieu-xac-thuc">Kết nối và trò chuyện an toàn</p>
        <nav className="tab-xac-thuc" aria-label="Chuyển đổi đăng nhập hoặc đăng ký">
          <NavLink
            to="/dang-nhap"
            className={({ isActive }) => `tab-xac-thuc__nut${isActive ? ' tab-xac-thuc__nut--dang-chon' : ''}`}
          >
            Đăng nhập
          </NavLink>
          <NavLink
            to="/dang-ky"
            className={({ isActive }) => `tab-xac-thuc__nut${isActive ? ' tab-xac-thuc__nut--dang-chon' : ''}`}
          >
            Đăng ký
          </NavLink>
        </nav>
        {children}
      </div>
    </div>
  );
}
