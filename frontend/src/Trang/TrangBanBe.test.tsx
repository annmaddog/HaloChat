import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangBanBe } from './TrangBanBe';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderTrangBanBe() {
  return render(
    <MemoryRouter>
      <NhaCungCapXacThuc>
        <TrangBanBe />
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TrangBanBe', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([{ id: 'b', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }]);
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      {
        id: 'l1',
        nguoiGui: { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com' },
        nguoiNhan: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com' },
        trangThai: 'ChoDuyet',
        thoiGianTao: new Date().toISOString(),
      },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiGui').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: 'b', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' },
      { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com' },
      { id: 'd', tenTaiKhoan: 'PhamD', email: 'd@gmail.com' },
    ]);
  });

  it('hiển thị danh sách bạn bè, lời mời đến, và người có thể kết bạn', async () => {
    renderTrangBanBe();

    expect(await screen.findByText('TranBinh')).toBeInTheDocument();
    expect(await screen.findByText('LeC')).toBeInTheDocument();
    expect(await screen.findByText('PhamD')).toBeInTheDocument();
  });

  it('người đã là bạn bè hoặc đang có lời mời không xuất hiện ở mục Tìm người để kết bạn', async () => {
    renderTrangBanBe();
    await screen.findByText('PhamD');

    const phanTimKetBan = screen.getByText('Tìm người để kết bạn').closest('section')!;
    expect(phanTimKetBan.textContent).not.toContain('TranBinh');
    expect(phanTimKetBan.textContent).not.toContain('LeC');
    expect(phanTimKetBan.textContent).toContain('PhamD');
  });

  it('bấm Kết bạn gọi GuiLoiMoiKetBan', async () => {
    const guiLoiMoiSpy = vi.spyOn(DichVuApi, 'GuiLoiMoiKetBan').mockResolvedValue({
      id: 'l2',
      nguoiGui: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com' },
      nguoiNhan: { id: 'd', tenTaiKhoan: 'PhamD', email: 'd@gmail.com' },
      trangThai: 'ChoDuyet',
      thoiGianTao: new Date().toISOString(),
    });

    renderTrangBanBe();
    await screen.findByText('PhamD');
    await userEvent.click(screen.getByRole('button', { name: 'Kết bạn' }));

    await waitFor(() => expect(guiLoiMoiSpy).toHaveBeenCalledWith('token-gia-lap', 'd'));
  });

  it('bấm Chấp nhận gọi ChapNhanLoiMoiKetBan', async () => {
    const chapNhanSpy = vi.spyOn(DichVuApi, 'ChapNhanLoiMoiKetBan').mockResolvedValue({
      id: 'l1',
      nguoiGui: { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com' },
      nguoiNhan: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com' },
      trangThai: 'DaChapNhan',
      thoiGianTao: new Date().toISOString(),
    });

    renderTrangBanBe();
    await screen.findByText('LeC');
    await userEvent.click(screen.getByRole('button', { name: 'Chấp nhận' }));

    await waitFor(() => expect(chapNhanSpy).toHaveBeenCalledWith('token-gia-lap', 'l1'));
  });
});
