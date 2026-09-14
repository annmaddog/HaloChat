import { render, screen, waitFor, within } from '@testing-library/react';
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
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([
      { id: 'b', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([
      {
        id: 'l1',
        nguoiGui: { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
        nguoiNhan: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com', choPhepTinNhanTuNguoiLa: true },
        trangThai: 'ChoDuyet',
        thoiGianTao: new Date().toISOString(),
      },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiGui').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: 'b', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
      { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
      { id: 'd', tenTaiKhoan: 'PhamD', email: 'd@gmail.com', choPhepTinNhanTuNguoiLa: true },
      { id: 'e', tenTaiKhoan: 'HoangE', email: 'e@gmail.com', choPhepTinNhanTuNguoiLa: false },
    ]);
    vi.spyOn(DichVuApi, 'LayTrangThaiHoatDong').mockResolvedValue({});
  });

  it('mac dinh hien tab Ban be voi danh sach ban be', async () => {
    renderTrangBanBe();
    expect(await screen.findByText('Bạn bè của tôi (1)')).toBeInTheDocument();
    expect(screen.getByText('TranBinh')).toBeInTheDocument();
  });

  it('chuyen sang tab Loi moi hien dung danh sach loi moi den', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Lời mời \(1\)/ }));

    expect(await screen.findByText('Lời mời kết bạn (1)')).toBeInTheDocument();
    expect(screen.getByText('LeC')).toBeInTheDocument();
    expect(screen.getByText('Muốn kết bạn với bạn')).toBeInTheDocument();
  });

  it('tab Loi moi rong hien dung empty state', async () => {
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Lời mời \(0\)/ }));

    expect(await screen.findByText('Không có lời mời kết bạn')).toBeInTheDocument();
  });

  it('go tu khoa tim kiem chi hien Ket qua tim kiem, an ca 2 tab', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'Pham');

    expect(await screen.findByText('Kết quả tìm kiếm')).toBeInTheDocument();
    expect(screen.getByText('PhamD')).toBeInTheDocument();
    expect(screen.queryByText('LeC')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Lời mời/ })).not.toBeInTheDocument();
  });

  it('nguoi khong cho phep nguoi la nhan tin: chi hien nut Ket ban, khong hien Nhan tin', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'HoangE');

    const the = (await screen.findByText('HoangE')).closest('li')!;
    expect(the.textContent).toContain('Chỉ nhận tin nhắn từ bạn bè');
    expect(within(the).queryByRole('button', { name: 'Nhắn tin' })).not.toBeInTheDocument();
    expect(within(the).getByRole('button', { name: 'Kết bạn' })).toBeInTheDocument();
  });

  it('nguoi cho phep nguoi la nhan tin: hien ca Nhan tin va Ket ban', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'PhamD');

    const the = (await screen.findByText('PhamD')).closest('li')!;
    expect(within(the).getByRole('button', { name: 'Nhắn tin' })).toBeInTheDocument();
    expect(within(the).getByRole('button', { name: 'Kết bạn' })).toBeInTheDocument();
  });

  it('sau khi gui loi moi, nut doi thanh Da gui loi moi', async () => {
    const loiMoiMoi = {
      id: 'l2',
      nguoiGui: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com', choPhepTinNhanTuNguoiLa: true },
      nguoiNhan: { id: 'd', tenTaiKhoan: 'PhamD', email: 'd@gmail.com', choPhepTinNhanTuNguoiLa: true },
      trangThai: 'ChoDuyet' as const,
      thoiGianTao: new Date().toISOString(),
    };
    vi.spyOn(DichVuApi, 'GuiLoiMoiKetBan').mockResolvedValue(loiMoiMoi);
    // Lan goi dau (khi mount) tra ve rong, lan goi sau (sau khi gui loi moi
    // va taiLaiTatCa refetch) tra ve co loi moi moi vua gui, mo phong dung
    // hanh vi API that.
    vi.spyOn(DichVuApi, 'LayLoiMoiGui').mockResolvedValueOnce([]).mockResolvedValueOnce([loiMoiMoi]);
    renderTrangBanBe();
    await screen.findByText('TranBinh');
    await userEvent.type(screen.getByPlaceholderText(/Tìm bạn bè/), 'PhamD');
    await screen.findByText('PhamD');

    await userEvent.click(screen.getByRole('button', { name: 'Kết bạn' }));

    expect(await screen.findByRole('button', { name: 'Đã gửi lời mời' })).toBeInTheDocument();
  });

  it('bam Chap nhan goi ChapNhanLoiMoiKetBan', async () => {
    const chapNhanSpy = vi.spyOn(DichVuApi, 'ChapNhanLoiMoiKetBan').mockResolvedValue({
      id: 'l1',
      nguoiGui: { id: 'c', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
      nguoiNhan: { id: 'toi', tenTaiKhoan: 'Toi', email: 't@gmail.com', choPhepTinNhanTuNguoiLa: true },
      trangThai: 'DaChapNhan',
      thoiGianTao: new Date().toISOString(),
    });
    renderTrangBanBe();
    await screen.findByText('TranBinh');
    await userEvent.click(screen.getByRole('button', { name: /Lời mời/ }));
    await screen.findByText('LeC');

    await userEvent.click(screen.getByRole('button', { name: 'Chấp nhận' }));

    await waitFor(() => expect(chapNhanSpy).toHaveBeenCalledWith('token-gia-lap', 'l1'));
  });

  it('bam nut menu roi Xem thong tin mo khung ho so', async () => {
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Thêm thao tác cho TranBinh/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Xem thông tin' }));

    expect(screen.getByText('b@gmail.com')).toBeInTheDocument();
  });

  it('bam Xoa ban trong menu goi XoaBanBe sau khi xac nhan', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const xoaBanSpy = vi.spyOn(DichVuApi, 'XoaBanBe').mockResolvedValue({ thongBao: 'Đã xóa bạn.' });
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Thêm thao tác cho TranBinh/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Xóa bạn' }));

    await waitFor(() => expect(xoaBanSpy).toHaveBeenCalledWith('token-gia-lap', 'b'));
    await waitFor(() => expect(screen.queryByText('TranBinh')).not.toBeInTheDocument());
  });

  it('bam Xoa ban nhung huy xac nhan thi khong goi API', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    const xoaBanSpy = vi.spyOn(DichVuApi, 'XoaBanBe');
    renderTrangBanBe();
    await screen.findByText('TranBinh');

    await userEvent.click(screen.getByRole('button', { name: /Thêm thao tác cho TranBinh/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Xóa bạn' }));

    expect(xoaBanSpy).not.toHaveBeenCalled();
  });

  it('chua co ban be nao hien empty state toan trang', async () => {
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([]);
    renderTrangBanBe();

    expect(await screen.findByText('Bạn chưa có người bạn nào')).toBeInTheDocument();
  });
});
