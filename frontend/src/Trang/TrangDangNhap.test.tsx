import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangDangNhap } from './TrangDangNhap';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderVoiRouter() {
  return render(
    <MemoryRouter initialEntries={['/dang-nhap']}>
      <NhaCungCapXacThuc>
        <Routes>
          <Route path="/dang-nhap" element={<TrangDangNhap />} />
          <Route path="/nguoi-dung" element={<div>Trang người dùng</div>} />
        </Routes>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TrangDangNhap', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('đăng nhập thành công điều hướng sang /nguoi-dung', async () => {
    vi.spyOn(DichVuApi, 'DangNhap').mockResolvedValue({ token: 'token-gia-lap' });
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản hoặc Email'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'MatKhau123');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }));

    expect(await screen.findByText('Trang người dùng')).toBeInTheDocument();
    expect(localStorage.getItem('haloChatToken')).toBe('token-gia-lap');
  });

  it('sai mật khẩu hiển thị lỗi, không điều hướng', async () => {
    vi.spyOn(DichVuApi, 'DangNhap').mockRejectedValue(new Error('Sai tên đăng nhập hoặc mật khẩu.'));
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản hoặc Email'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'Sai');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng nhập' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Sai tên đăng nhập hoặc mật khẩu.');
  });
});
