import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { DangNhap as GoiDangNhap } from '../DichVuApi';
import { GiaiMaJwt } from '../TienIch/GiaiMaJwt';

interface NguoiDungHienTai {
  id: string;
  tenTaiKhoan: string;
  email: string;
}

interface TrangThaiXacThuc {
  token: string | null;
  daDangNhap: boolean;
  nguoiDungHienTai: NguoiDungHienTai | null;
  dangNhap: (tenDangNhap: string, matKhau: string) => Promise<void>;
  dangXuat: () => void;
}

const KHOA_LUU_TOKEN = 'haloChatToken';

const BoiCanhXacThuc = createContext<TrangThaiXacThuc | undefined>(undefined);

export function NhaCungCapXacThuc({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(KHOA_LUU_TOKEN));

  const nguoiDungHienTai = useMemo<NguoiDungHienTai | null>(() => {
    if (!token) return null;
    const payload = GiaiMaJwt(token);
    if (!payload) return null;
    return { id: payload.sub, tenTaiKhoan: payload.tenTaiKhoan, email: payload.email };
  }, [token]);

  async function dangNhap(tenDangNhap: string, matKhau: string) {
    const ketQua = await GoiDangNhap(tenDangNhap, matKhau);
    localStorage.setItem(KHOA_LUU_TOKEN, ketQua.token);
    setToken(ketQua.token);
  }

  function dangXuat() {
    localStorage.removeItem(KHOA_LUU_TOKEN);
    setToken(null);
  }

  return (
    <BoiCanhXacThuc.Provider value={{ token, daDangNhap: token !== null, nguoiDungHienTai, dangNhap, dangXuat }}>
      {children}
    </BoiCanhXacThuc.Provider>
  );
}

export function useXacThuc(): TrangThaiXacThuc {
  const giaTri = useContext(BoiCanhXacThuc);
  if (!giaTri) {
    throw new Error('useXacThuc phải được dùng bên trong NhaCungCapXacThuc');
  }
  return giaTri;
}
