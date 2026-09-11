import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, beforeEach } from 'vitest';
import { TuyenDuongRieng } from './TuyenDuongRieng';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';

function renderVoiTuyenDuong() {
  return render(
    <MemoryRouter initialEntries={['/nguoi-dung']}>
      <NhaCungCapXacThuc>
        <Routes>
          <Route path="/dang-nhap" element={<div>Trang đăng nhập</div>} />
          <Route
            path="/nguoi-dung"
            element={
              <TuyenDuongRieng>
                <div>Bí mật</div>
              </TuyenDuongRieng>
            }
          />
        </Routes>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TuyenDuongRieng', () => {
  beforeEach(() => localStorage.clear());

  it('điều hướng về /dang-nhap khi chưa đăng nhập', () => {
    renderVoiTuyenDuong();
    expect(screen.getByText('Trang đăng nhập')).toBeInTheDocument();
  });

  it('hiển thị nội dung khi đã đăng nhập', () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    renderVoiTuyenDuong();
    expect(screen.getByText('Bí mật')).toBeInTheDocument();
  });
});
