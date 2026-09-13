import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ThongBao } from './ThongBao';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue({
        start: vi.fn().mockResolvedValue(undefined), stop: vi.fn().mockResolvedValue(undefined),
        on: vi.fn(), off: vi.fn(), invoke: vi.fn(),
        onreconnected: vi.fn(), onreconnecting: vi.fn(), onclose: vi.fn(),
      }),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function renderThongBao() {
  return render(
    <MemoryRouter>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <ThongBao />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('ThongBao', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('hiển thị số lượng thông báo gộp từ lời mời kết bạn và hội thoại chưa đọc', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      { id: 'l1', nguoiGui: { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }, nguoiNhan: { id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com' }, trangThai: 'ChoDuyet', thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([
      { nguoiDung: { id: '3', tenTaiKhoan: 'LeC', email: 'c@gmail.com' }, tinNhanCuoi: 'Chào', thoiGianTinNhanCuoi: '2026-01-01T00:00:00Z', soTinChuaDoc: 2 },
    ]);

    renderThongBao();

    expect(await screen.findByText('2')).toBeInTheDocument();
  });

  it('bấm vào 1 lời mời kết bạn mở dropdown và hiện đúng nội dung', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      { id: 'l1', nguoiGui: { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }, nguoiNhan: { id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com' }, trangThai: 'ChoDuyet', thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([]);

    renderThongBao();
    await userEvent.click(await screen.findByLabelText('Thông báo'));

    expect(await screen.findByText(/đã gửi lời mời kết bạn/)).toBeInTheDocument();
  });

  it('không có thông báo hiện đúng dòng trống', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([]);

    renderThongBao();
    await userEvent.click(await screen.findByLabelText('Thông báo'));

    await waitFor(() => expect(screen.getByText('Không có thông báo mới.')).toBeInTheDocument());
  });
});
