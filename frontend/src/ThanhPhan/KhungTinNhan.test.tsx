import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { KhungTinNhan } from './KhungTinNhan';
import type { TinNhan } from '../KieuDuLieu';

const PROPS_MAC_DINH = {
  loaiHoiThoai: 'nhom' as const,
  tenHienThi: 'Nhóm CNTT',
  phuDe: '3 thành viên',
  danhSachTinNhan: [],
  idHienTai: '1',
  dangKetNoi: true,
  dangTaiLichSu: false,
  coTheTaiThem: false,
  onTaiThemLichSuCu: () => {},
  onGuiVanBan: () => {},
  onGuiTep: () => {},
  dangTaiTep: false,
  loi: null,
};

const TIN_NHAN_MAU = {
  id: 'm1',
  nguoiGuiId: '1',
  nguoiNhanId: null,
  nhomId: 'n1',
  loaiTinNhan: 'Text' as const,
  noiDungTinNhan: 'Chào mọi người',
  duongDanFile: null,
  tenFileGoc: null,
  kichThuocFile: null,
  loaiFile: null,
  daDoc: false,
  daNhan: false,
  thoiGianTao: '2026-01-01T10:30:00.000Z',
  traLoi: null,
};

describe('KhungTinNhan', () => {
  it('khong co onBamTieuDe: tieu de la span tinh, khong phai nut bam', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} />);

    expect(screen.getByText('Nhóm CNTT')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Nhóm CNTT/ })).not.toBeInTheDocument();
  });

  it('co onBamTieuDe: tieu de la nut bam duoc, bam goi dung ham', async () => {
    const onBamTieuDe = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onBamTieuDe={onBamTieuDe} />);

    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));

    expect(onBamTieuDe).toHaveBeenCalledTimes(1);
  });

  it('mac dinh khong hien gio:phut va khong hien trang thai da gui/da nhan/da xem', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[TIN_NHAN_MAU]} />);

    expect(screen.getByText('Chào mọi người')).toBeInTheDocument();
    expect(screen.queryByText(/^\d{1,2}:\d{2}$/)).not.toBeInTheDocument();
    expect(screen.queryByText('Đã gửi')).not.toBeInTheDocument();
    expect(screen.queryByText('Đã nhận')).not.toBeInTheDocument();
    expect(screen.queryByText('Đã xem')).not.toBeInTheDocument();
  });

  it('bam vao tin nhan hien gio:phut, bam lai lan nua thi an di', async () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[TIN_NHAN_MAU]} />);

    await userEvent.click(screen.getByText('Chào mọi người'));
    expect(screen.getByText(/^\d{1,2}:\d{2}$/)).toBeInTheDocument();

    await userEvent.click(screen.getByText('Chào mọi người'));
    expect(screen.queryByText(/^\d{1,2}:\d{2}$/)).not.toBeInTheDocument();
  });

  it('bam icon Tra loi hien khoi dang tra loi voi ten va trich dan dung', async () => {
    const tinGoc: TinNhan = {
      id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    const khoiDangTraLoi = screen.getByText(/↩ Trả lời/).closest('.khung-tin-nhan__dang-tra-loi') as HTMLElement;
    expect(khoiDangTraLoi).toBeInTheDocument();
    expect(within(khoiDangTraLoi).getByText('Xin chào bạn')).toBeInTheDocument();
  });

  it('huy tra loi an khoi preview', async () => {
    const tinGoc: TinNhan = {
      id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" />);
    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    await userEvent.click(screen.getByRole('button', { name: 'Hủy trả lời' }));

    expect(screen.queryByText(/↩ Trả lời/)).not.toBeInTheDocument();
  });

  it('gui tin nhan luc dang tra loi goi onGuiVanBan voi dung traLoiId', async () => {
    const tinGoc: TinNhan = {
      id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
    };
    const onGuiVanBan = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" onGuiVanBan={onGuiVanBan} />);
    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Đây là câu trả lời{enter}');

    expect(onGuiVanBan).toHaveBeenCalledWith('Đây là câu trả lời', 'm1');
  });

  it('tin nhan co truong traLoi hien khoi trich dan trong bong bong', () => {
    const tinTraLoi: TinNhan = {
      id: 'm2', nguoiGuiId: 'toi', nguoiNhanId: 'nguoi-kia', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Đây là câu trả lời', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
      thoiGianTao: '2026-01-01T00:01:00Z',
      traLoi: { id: 'm1', tenNguoiGui: 'Nguoi Kia', noiDungTomTat: 'Xin chào bạn', loaiTinNhan: 'Text' },
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinTraLoi]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

    const khoiTrichDan = document.querySelector('.khung-tin-nhan__trich-dan') as HTMLElement;
    expect(khoiTrichDan).toBeInTheDocument();
    expect(within(khoiTrichDan).getByText('Nguoi Kia')).toBeInTheDocument();
    expect(within(khoiTrichDan).getByText('Xin chào bạn')).toBeInTheDocument();
  });

  it('doi hoi thoai (tenHienThi doi) reset trang thai dang tra loi', async () => {
    const tinA: TinNhan = {
      id: 'a1', nguoiGuiId: 'nguoi-a', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Tin cua hoi thoai A', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
    };
    const tinB: TinNhan = {
      id: 'b1', nguoiGuiId: 'nguoi-b', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Tin cua hoi thoai B', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
    };
    const onGuiVanBan = vi.fn();
    const { rerender } = render(
      <KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinA]} idHienTai="toi" tenHienThi="Hoi thoai A" onGuiVanBan={onGuiVanBan} />,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));
    expect(screen.getByText(/↩ Trả lời/)).toBeInTheDocument();

    rerender(
      <KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinB]} idHienTai="toi" tenHienThi="Hoi thoai B" onGuiVanBan={onGuiVanBan} />,
    );

    expect(screen.queryByText(/↩ Trả lời/)).not.toBeInTheDocument();

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Tin nhan moi{enter}');
    expect(onGuiVanBan).toHaveBeenCalledWith('Tin nhan moi', null);
  });

  it('card File hien nut tron tai xuong rieng biet', () => {
    const tinFile: TinNhan = {
      id: 'm3', nguoiGuiId: 'toi', nguoiNhanId: 'nguoi-kia', nhomId: null,
      loaiTinNhan: 'File', noiDungTinNhan: '', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439099',
      tenFileGoc: 'bao-cao.docx', kichThuocFile: 15360, loaiFile: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinFile]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

    expect(screen.getByText('bao-cao.docx')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Tải xuống bao-cao.docx' })).toBeInTheDocument();
  });
});
