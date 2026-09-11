import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';
import { useXacThuc } from '../NguCanh/NguCanhXacThuc';

export function TuyenDuongRieng({ children }: { children: ReactNode }) {
  const { daDangNhap } = useXacThuc();
  if (!daDangNhap) {
    return <Navigate to="/dang-nhap" replace />;
  }
  return <>{children}</>;
}
