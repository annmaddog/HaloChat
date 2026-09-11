import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangDangKy } from './TrangDangKy';
import * as DichVuApi from '../DichVuApi';

function renderVoiRouter() {
  return render(
    <MemoryRouter initialEntries={['/dang-ky']}>
      <Routes>
        <Route path="/dang-ky" element={<TrangDangKy />} />
        <Route path="/dang-nhap" element={<div>Trang đăng nhập</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('TrangDangKy', () => {
  beforeEach(() => vi.restoreAllMocks());

  it('gửi đúng dữ liệu và điều hướng sang /dang-nhap khi đăng ký thành công', async () => {
    const dangKyGiaLap = vi.spyOn(DichVuApi, 'DangKy').mockResolvedValue({ thongBao: 'Đăng ký thành công.' });
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Email'), 'nguyenan@gmail.com');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'MatKhau123');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng ký' }));

    expect(dangKyGiaLap).toHaveBeenCalledWith('NguyenAn', 'nguyenan@gmail.com', 'MatKhau123');
    expect(await screen.findByText('Trang đăng nhập')).toBeInTheDocument();
  });

  it('hiển thị thông báo lỗi khi API trả lỗi, không điều hướng', async () => {
    vi.spyOn(DichVuApi, 'DangKy').mockRejectedValue(new Error('Tên tài khoản đã tồn tại.'));
    renderVoiRouter();

    await userEvent.type(screen.getByLabelText('Tên tài khoản'), 'NguyenAn');
    await userEvent.type(screen.getByLabelText('Email'), 'nguyenan@gmail.com');
    await userEvent.type(screen.getByLabelText('Mật khẩu'), 'MatKhau123');
    await userEvent.click(screen.getByRole('button', { name: 'Đăng ký' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Tên tài khoản đã tồn tại.');
  });
});
