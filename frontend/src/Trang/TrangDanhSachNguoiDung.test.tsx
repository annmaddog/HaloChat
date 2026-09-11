import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangDanhSachNguoiDung } from './TrangDanhSachNguoiDung';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderVoiNguCanh() {
  return render(
    <NhaCungCapXacThuc>
      <TrangDanhSachNguoiDung />
    </NhaCungCapXacThuc>,
  );
}

describe('TrangDanhSachNguoiDung', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('hiển thị danh sách người dùng lấy từ API', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: '1', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' },
    ]);

    renderVoiNguCanh();

    expect(await screen.findByText('TranBinh (b@gmail.com)')).toBeInTheDocument();
  });

  it('hiển thị lỗi khi API thất bại', async () => {
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockRejectedValue(new Error('Không thể tải danh sách.'));

    renderVoiNguCanh();

    expect(await screen.findByRole('alert')).toHaveTextContent('Không thể tải danh sách.');
  });
});
