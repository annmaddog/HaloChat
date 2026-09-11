import { render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, afterEach } from 'vitest';
import { TruongNhap } from './TruongNhap';
import { BieuTuongKhoa } from './BieuTuong';

describe('TruongNhap', () => {
  afterEach(() => {
    cleanup();
  });

  it('mặc định ẩn mật khẩu, bấm biểu tượng để hiện rồi ẩn lại', async () => {
    render(
      <TruongNhap nhan="Mật khẩu" bieuTuong={<BieuTuongKhoa />} coTheAn value="MatKhau123" onChange={() => {}} />,
    );

    const oNhap = screen.getByLabelText('Mật khẩu');
    expect(oNhap).toHaveAttribute('type', 'password');

    await userEvent.click(screen.getByRole('button', { name: 'Hiện mật khẩu' }));
    expect(oNhap).toHaveAttribute('type', 'text');

    await userEvent.click(screen.getByRole('button', { name: 'Ẩn mật khẩu' }));
    expect(oNhap).toHaveAttribute('type', 'password');
  });

  it('trường không bật coTheAn thì không có nút ẩn/hiện', () => {
    render(<TruongNhap nhan="Tên tài khoản" bieuTuong={<BieuTuongKhoa />} value="" onChange={() => {}} />);
    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
