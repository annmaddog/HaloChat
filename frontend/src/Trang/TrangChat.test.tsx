import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangChat } from './TrangChat';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';

const ketNoiGiaLap = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  onclose: vi.fn(),
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue(ketNoiGiaLap),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function renderTrangChat() {
  return render(
    <NhaCungCapXacThuc>
      <NhaCungCapChat>
        <TrangChat />
      </NhaCungCapChat>
    </NhaCungCapXacThuc>,
  );
}

function taoTinNhanGiaLap(gan: Partial<Awaited<ReturnType<typeof DichVuApi.LayLichSuTinNhan>>[number]>) {
  return {
    id: 'm1',
    nguoiGuiId: '2',
    nguoiNhanId: '1',
    loaiTinNhan: 'Text' as const,
    noiDungTinNhan: 'Chào bạn',
    duongDanFile: null,
    tenFileGoc: null,
    kichThuocFile: null,
    loaiFile: null,
    daDoc: false,
    thoiGianTao: new Date().toISOString(),
    ...gan,
  };
}

describe('TrangChat', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' },
    ]);
  });

  it('hiển thị danh sách người dùng sau khi tải', async () => {
    renderTrangChat();

    expect(await screen.findByText('TranBinh')).toBeInTheDocument();
  });

  it('chọn 1 người thì tải và hiển thị lịch sử tin nhắn', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([taoTinNhanGiaLap({})]);

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));

    expect(await screen.findByText('Chào bạn')).toBeInTheDocument();
  });

  it('gửi tin nhắn văn bản gọi ketNoi.invoke và hiển thị tin nhắn vừa gửi', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    ketNoiGiaLap.invoke.mockResolvedValue(
      taoTinNhanGiaLap({ id: 'm2', nguoiGuiId: '1', nguoiNhanId: '2', noiDungTinNhan: 'Xin chào' }),
    );

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Xin chào');
    await userEvent.click(screen.getByRole('button', { name: 'Gửi' }));

    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('GuiTinNhan', '2', 'Text', 'Xin chào', null, null, null, null),
    );
    expect(await screen.findByText('Xin chào')).toBeInTheDocument();
  });

  it('nhận tin nhắn realtime qua sự kiện NhanTinNhan hiển thị ngay trong khung đang mở', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.on).toHaveBeenCalledWith('NhanTinNhan', expect.any(Function)));

    const handler = ketNoiGiaLap.on.mock.calls.find(([ten]: [string]) => ten === 'NhanTinNhan')![1];
    handler(taoTinNhanGiaLap({ id: 'm3', noiDungTinNhan: 'Tin nhắn realtime' }));

    expect(await screen.findByText('Tin nhắn realtime')).toBeInTheDocument();
  });

  it('nhận tin nhắn realtime từ người chưa chọn không chặn tải lịch sử đầy đủ khi chọn sau đó', async () => {
    const layLichSuSpy = vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([
      taoTinNhanGiaLap({ id: 'm0', noiDungTinNhan: 'Tin nhắn cũ' }),
    ]);

    renderTrangChat();
    await screen.findByText('TranBinh');
    await waitFor(() => expect(ketNoiGiaLap.on).toHaveBeenCalledWith('NhanTinNhan', expect.any(Function)));

    const handler = ketNoiGiaLap.on.mock.calls.find(([ten]: [string]) => ten === 'NhanTinNhan')![1];
    handler(taoTinNhanGiaLap({ id: 'm1', noiDungTinNhan: 'Tin realtime đến trước' }));

    await userEvent.click(screen.getByText('TranBinh'));

    await waitFor(() => expect(layLichSuSpy).toHaveBeenCalled());
    expect(await screen.findByText('Tin nhắn cũ')).toBeInTheDocument();
    expect(screen.getByText('Tin realtime đến trước')).toBeInTheDocument();
  });

  it('chọn file ảnh gọi TaiLenTep rồi GuiTinNhan với loại Anh', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({
      duongDanFile: '/uploads/abc.png',
      tenFileGoc: 'anh.png',
      kichThuocFile: 1024,
      loaiFile: 'image/png',
    });
    ketNoiGiaLap.invoke.mockResolvedValue(
      taoTinNhanGiaLap({
        id: 'm4',
        nguoiGuiId: '1',
        nguoiNhanId: '2',
        loaiTinNhan: 'Anh',
        noiDungTinNhan: '',
        duongDanFile: '/uploads/abc.png',
        tenFileGoc: 'anh.png',
        kichThuocFile: 1024,
        loaiFile: 'image/png',
      }),
    );

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    const tep = new File(['noi-dung-gia-lap'], 'anh.png', { type: 'image/png' });
    const inputTep = document.querySelector('.trang-chat__input-tep') as HTMLInputElement;
    await userEvent.upload(inputTep, tep);

    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith(
        'GuiTinNhan', '2', 'Anh', '', '/uploads/abc.png', 'anh.png', 1024, 'image/png',
      ),
    );
    expect(await screen.findByRole('img')).toHaveAttribute('src', expect.stringContaining('/uploads/abc.png'));
  });

  it('file ảnh vượt quá 5MB bị chặn ở client, không gọi TaiLenTep', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    const taiLenSpy = vi.spyOn(DichVuApi, 'TaiLenTep');

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));

    const tepQuaKho = new File([new Uint8Array(6 * 1024 * 1024)], 'to.png', { type: 'image/png' });
    const inputTep = document.querySelector('.trang-chat__input-tep') as HTMLInputElement;
    await userEvent.upload(inputTep, tepQuaKho);

    expect(taiLenSpy).not.toHaveBeenCalled();
    expect(await screen.findByText(/vượt quá giới hạn/)).toBeInTheDocument();
  });
});
