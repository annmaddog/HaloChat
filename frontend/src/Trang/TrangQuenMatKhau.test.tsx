import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangQuenMatKhau } from './TrangQuenMatKhau';
import * as DichVuApi from '../DichVuApi';

function renderVoiRouter() {
  return render(
    <MemoryRouter initialEntries={['/quen-mat-khau']}>
      <Routes>
        <Route path="/quen-mat-khau" element={<TrangQuenMatKhau />} />
        <Route path="/dang-nhap" element={<div>Trang đăng nhập</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('TrangQuenMatKhau', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('gửi email thành công hiện thêm ô nhập OTP và mật khẩu mới', async () => {
    vi.spyOn(DichVuApi, 'GuiYeuCauQuenMatKhau').mockResolvedValue({
      thongBao: 'Nếu email tồn tại trong hệ thống, mã OTP đã được gửi.',
    });

    renderVoiRouter();
    await userEvent.type(screen.getByLabelText('Email'), 'a@gmail.com');
    await userEvent.click(screen.getByRole('button', { name: /Gửi mã OTP/ }));

    expect(await screen.findByLabelText('Mã OTP')).toBeInTheDocument();
    expect(screen.getByLabelText('Mật khẩu mới')).toBeInTheDocument();
    expect(DichVuApi.GuiYeuCauQuenMatKhau).toHaveBeenCalledWith('a@gmail.com');
  });

  it('đặt lại mật khẩu thành công điều hướng sang /dang-nhap', async () => {
    vi.spyOn(DichVuApi, 'GuiYeuCauQuenMatKhau').mockResolvedValue({ thongBao: 'Đã gửi.' });
    vi.spyOn(DichVuApi, 'DatLaiMatKhau').mockResolvedValue({ thongBao: 'Đặt lại mật khẩu thành công.' });

    renderVoiRouter();
    await userEvent.type(screen.getByLabelText('Email'), 'a@gmail.com');
    await userEvent.click(screen.getByRole('button', { name: /Gửi mã OTP/ }));
    await screen.findByLabelText('Mã OTP');

    await userEvent.type(screen.getByLabelText('Mã OTP'), '123456');
    await userEvent.type(screen.getByLabelText('Mật khẩu mới'), 'MatKhauMoi123');
    await userEvent.click(screen.getByRole('button', { name: /Đặt lại mật khẩu/ }));

    expect(await screen.findByText('Trang đăng nhập')).toBeInTheDocument();
    expect(DichVuApi.DatLaiMatKhau).toHaveBeenCalledWith('a@gmail.com', '123456', 'MatKhauMoi123');
  });

  it('OTP sai hiển thị lỗi, không điều hướng', async () => {
    vi.spyOn(DichVuApi, 'GuiYeuCauQuenMatKhau').mockResolvedValue({ thongBao: 'Đã gửi.' });
    vi.spyOn(DichVuApi, 'DatLaiMatKhau').mockRejectedValue(new Error('Mã OTP không đúng.'));

    renderVoiRouter();
    await userEvent.type(screen.getByLabelText('Email'), 'a@gmail.com');
    await userEvent.click(screen.getByRole('button', { name: /Gửi mã OTP/ }));
    await screen.findByLabelText('Mã OTP');

    await userEvent.type(screen.getByLabelText('Mã OTP'), '000000');
    await userEvent.type(screen.getByLabelText('Mật khẩu mới'), 'MatKhauMoi123');
    await userEvent.click(screen.getByRole('button', { name: /Đặt lại mật khẩu/ }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Mã OTP không đúng.');
  });
});
