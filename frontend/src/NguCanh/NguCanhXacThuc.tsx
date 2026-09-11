import { createContext, useContext, useState, type ReactNode } from 'react';
import { DangNhap as GoiDangNhap } from '../DichVuApi';

interface TrangThaiXacThuc {
  token: string | null;
  daDangNhap: boolean;
  dangNhap: (tenDangNhap: string, matKhau: string) => Promise<void>;
  dangXuat: () => void;
}

const KHOA_LUU_TOKEN = 'haloChatToken';

const BoiCanhXacThuc = createContext<TrangThaiXacThuc | undefined>(undefined);

export function NhaCungCapXacThuc({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(KHOA_LUU_TOKEN));

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
    <BoiCanhXacThuc.Provider value={{ token, daDangNhap: token !== null, dangNhap, dangXuat }}>
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
