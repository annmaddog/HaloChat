import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangCaiDat } from './TrangCaiDat';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderTrangCaiDat() {
  return render(
    <NhaCungCapXacThuc>
      <TrangCaiDat />
    </NhaCungCapXacThuc>,
  );
}

describe('TrangCaiDat', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('tải và hiển thị đúng trạng thái cài đặt ban đầu', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true,
    });

    renderTrangCaiDat();

    await waitFor(() => expect(screen.getByRole('checkbox')).toBeChecked());
  });

  it('bật toggle gọi CapNhatCaiDat với true', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
    });
    const capNhatSpy = vi.spyOn(DichVuApi, 'CapNhatCaiDat').mockResolvedValue(undefined);

    renderTrangCaiDat();
    await waitFor(() => expect(screen.getByRole('checkbox')).not.toBeChecked());

    await userEvent.click(screen.getByRole('checkbox'));

    await waitFor(() => expect(capNhatSpy).toHaveBeenCalledWith('token-gia-lap', true));
    expect(await screen.findByText('Đã lưu.')).toBeInTheDocument();
  });
});
