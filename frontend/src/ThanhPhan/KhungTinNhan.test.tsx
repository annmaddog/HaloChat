import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { KhungTinNhan } from './KhungTinNhan';

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
});
