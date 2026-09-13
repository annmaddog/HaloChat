import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangChat } from './TrangChat';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';
import type { NguoiDungTomTat } from '../KieuDuLieu';

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

function renderTrangChat(trangThaiDieuHuong?: { moNguoiDung: NguoiDungTomTat }) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: '/nguoi-dung', state: trangThaiDieuHuong }]}>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangChat />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

function taoTinNhanGiaLap(gan: Partial<Awaited<ReturnType<typeof DichVuApi.LayLichSuTinNhan>>[number]>) {
  return {
    id: 'm1',
    nguoiGuiId: '2',
    nguoiNhanId: '1',
    nhomId: null,
    loaiTinNhan: 'Text' as const,
    noiDungTinNhan: 'Chào bạn',
    duongDanFile: null,
    tenFileGoc: null,
    kichThuocFile: null,
    loaiFile: null,
    daDoc: false,
    daNhan: false,
    thoiGianTao: new Date().toISOString(),
    ...gan,
  };
}

function taoHoiThoaiGiaLap(nguoiDung: NguoiDungTomTat) {
  return { nguoiDung, tinNhanCuoi: 'Chào bạn', thoiGianTinNhanCuoi: new Date().toISOString(), soTinChuaDoc: 0 };
}

describe('TrangChat', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    const phanThanToken = btoa(JSON.stringify({ sub: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${phanThanToken}.chuky`);
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([
      taoHoiThoaiGiaLap({ id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }),
    ]);
    vi.spyOn(DichVuApi, 'LayTrangThaiHoatDong').mockResolvedValue({});
  });

  it('hiển thị danh sách hội thoại sau khi tải', async () => {
    renderTrangChat();

    expect(await screen.findByText('TranBinh')).toBeInTheDocument();
  });

  it('chọn 1 người thì tải và hiển thị lịch sử tin nhắn', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([taoTinNhanGiaLap({})]);

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));

    expect(await screen.findByText('Chào bạn')).toBeInTheDocument();
  });

  it('gửi tin nhắn văn bản hiện ngay "Đang gửi" rồi cập nhật khi Hub xác nhận', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    let phanGiai: (tn: unknown) => void = () => {};
    ketNoiGiaLap.invoke.mockReturnValue(new Promise((resolve) => { phanGiai = resolve; }));

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Xin chào');
    await userEvent.click(screen.getByRole('button', { name: 'Gửi' }));

    expect(await screen.findByText('Đang gửi')).toBeInTheDocument();

    phanGiai(taoTinNhanGiaLap({ id: 'm2', nguoiGuiId: '1', nguoiNhanId: '2', noiDungTinNhan: 'Xin chào', daNhan: true }));

    await waitFor(() => expect(screen.getByText('Đã nhận')).toBeInTheDocument());
    expect(screen.getByText('Xin chào')).toBeInTheDocument();
  });

  it('nhận tin nhắn realtime qua sự kiện NhanTinNhan hiển thị ngay trong khung đang mở', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.on).toHaveBeenCalledWith('NhanTinNhan', expect.any(Function)));

    const handler = ketNoiGiaLap.on.mock.calls.find((cuocGoi) => cuocGoi[0] === 'NhanTinNhan')![1];
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

    const handler = ketNoiGiaLap.on.mock.calls.find((cuocGoi) => cuocGoi[0] === 'NhanTinNhan')![1];
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
    const inputTep = document.querySelector('.khung-tin-nhan__input-tep') as HTMLInputElement;
    await userEvent.upload(inputTep, tep);

    await waitFor(() =>
      expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith(
        'GuiTinNhan', '2', null, 'Anh', '', '/uploads/abc.png', 'anh.png', 1024, 'image/png',
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
    const inputTep = document.querySelector('.khung-tin-nhan__input-tep') as HTMLInputElement;
    await userEvent.upload(inputTep, tepQuaKho);

    expect(taiLenSpy).not.toHaveBeenCalled();
    expect(await screen.findByText(/vượt quá giới hạn/)).toBeInTheDocument();
  });

  it('ô tìm kiếm lọc đúng danh sách hội thoại theo tên', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([
      taoHoiThoaiGiaLap({ id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }),
      taoHoiThoaiGiaLap({ id: '3', tenTaiKhoan: 'LeCuong', email: 'c@gmail.com' }),
    ]);

    renderTrangChat();
    await screen.findByText('TranBinh');
    expect(screen.getByText('LeCuong')).toBeInTheDocument();

    await userEvent.type(screen.getByPlaceholderText('Tìm cuộc trò chuyện...'), 'Cuong');

    expect(screen.getByText('LeCuong')).toBeInTheDocument();
    expect(screen.queryByText('TranBinh')).not.toBeInTheDocument();
  });

  it('class "trang-chat--da-chon" chỉ xuất hiện trên phần tử gốc sau khi đã chọn hội thoại', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    const { container } = renderTrangChat();

    await screen.findByText('TranBinh');
    expect(container.querySelector('.trang-chat')).not.toHaveClass('trang-chat--da-chon');

    await userEvent.click(screen.getByText('TranBinh'));

    expect(container.querySelector('.trang-chat')).toHaveClass('trang-chat--da-chon');
  });

  it('mở hội thoại mới từ điều hướng (chưa có trong danh sách hội thoại) tự động được chọn', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);

    renderTrangChat({ moNguoiDung: { id: '9', tenTaiKhoan: 'NguoiMoi', email: 'moi@gmail.com' } });

    expect(await screen.findByText('NguoiMoi')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập tin nhắn...')).toBeInTheDocument();
  });

  it('Hub từ chối gửi tin (chưa là bạn bè) hiển thị đúng thông báo lỗi', async () => {
    vi.spyOn(DichVuApi, 'LayLichSuTinNhan').mockResolvedValue([]);
    ketNoiGiaLap.invoke.mockRejectedValue(new Error('Người này chỉ nhận tin nhắn từ bạn bè.'));

    renderTrangChat();
    await userEvent.click(await screen.findByText('TranBinh'));
    await waitFor(() => expect(ketNoiGiaLap.start).toHaveBeenCalled());

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Xin chào');
    await userEvent.click(screen.getByRole('button', { name: 'Gửi' }));

    expect(await screen.findByText('Người này chỉ nhận tin nhắn từ bạn bè.')).toBeInTheDocument();
  });
});
