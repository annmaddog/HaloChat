import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DinhTuyen } from './DinhTuyen';
import { NhaCungCapXacThuc } from './NguCanh/NguCanhXacThuc';
import * as DichVuApi from './DichVuApi';

function renderDinhTuyen(duongDanBanDau: string) {
  return render(
    <MemoryRouter initialEntries={[duongDanBanDau]}>
      <NhaCungCapXacThuc>
        <DinhTuyen />
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('DinhTuyen', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('đường dẫn không tồn tại điều hướng về trang đăng nhập', () => {
    renderDinhTuyen('/khong-ton-tai');

    expect(screen.getByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument();
  });

  it('vào /nguoi-dung khi chưa đăng nhập bị điều hướng về trang đăng nhập', () => {
    renderDinhTuyen('/nguoi-dung');

    expect(screen.getByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument();
  });

  it('vào /nguoi-dung khi đã đăng nhập rồi đăng xuất sẽ quay lại trang đăng nhập', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([]);

    renderDinhTuyen('/nguoi-dung');

    const nutDangXuat = await screen.findByRole('button', { name: 'Đăng xuất' });
    await userEvent.click(nutDangXuat);

    expect(await screen.findByRole('button', { name: 'Đăng nhập' })).toBeInTheDocument();
  });
});
