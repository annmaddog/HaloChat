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
});
