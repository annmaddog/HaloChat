import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { PanelThongTinNhom } from './PanelThongTinNhom';
import type { Nhom } from '../KieuDuLieu';

function taoNhomGiaLap(soThanhVien: number): Nhom {
  return {
    id: 'n1',
    tenNhom: 'Nhóm CNTT',
    moTa: null,
    duongDanAnhDaiDien: null,
    nguoiTaoId: '1',
    thanhVien: Array.from({ length: soThanhVien }, (_, i) => ({
      id: `${i + 1}`, tenTaiKhoan: `NguoiDung${i + 1}`, email: `nd${i + 1}@gmail.com`, choPhepTinNhanTuNguoiLa: true,
      tenHienThi: `NguoiDung${i + 1}`,
    })),
    thoiGianTao: '2026-01-01T00:00:00Z',
  };
}

describe('PanelThongTinNhom', () => {
  it('hien dung avatar, ten, so thanh vien', () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(3)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    expect(screen.getByText('Nhóm CNTT')).toBeInTheDocument();
    expect(screen.getByText('3 thành viên')).toBeInTheDocument();
  });

  it('khong phai admin: khong hien nut Chinh sua va Quan ly nhom', () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    expect(screen.queryByRole('button', { name: 'Chỉnh sửa' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Quản lý nhóm' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rời nhóm' })).toBeInTheDocument();
  });

  it('la admin: hien nut Chinh sua va Quan ly nhom, nut roi doi thanh Giai tan nhom', async () => {
    const onMoQuanLy = vi.fn();
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin onDong={() => {}} onMoQuanLy={onMoQuanLy} onRoiNhom={() => {}} />);

    expect(screen.getByRole('button', { name: 'Giải tán nhóm' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Chỉnh sửa' }));
    expect(onMoQuanLy).toHaveBeenCalledTimes(1);
  });

  it('hien toi da 8 avatar thanh vien mac dinh, con lai an sau nut', () => {
    const { container } = render(<PanelThongTinNhom nhom={taoNhomGiaLap(10)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    const avatarHang = container.querySelector('.panel-thong-tin-nhom__avatar-hang');
    const soAvatarNho = avatarHang?.querySelectorAll('.avatar--nho').length || 0;
    expect(soAvatarNho).toBeLessThanOrEqual(8);
    expect(screen.getByRole('button', { name: 'Xem tất cả thành viên' })).toBeInTheDocument();
  });

  it('bam Xem tat ca thanh vien hien het avatar, an nut di', async () => {
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(10)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    await userEvent.click(screen.getByRole('button', { name: 'Xem tất cả thành viên' }));

    expect(screen.queryByRole('button', { name: 'Xem tất cả thành viên' })).not.toBeInTheDocument();
  });

  it('bam Roi nhom goi onRoiNhom', async () => {
    const onRoiNhom = vi.fn();
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={onRoiNhom} />);

    await userEvent.click(screen.getByRole('button', { name: 'Rời nhóm' }));

    expect(onRoiNhom).toHaveBeenCalledTimes(1);
  });

  it('bam Nhan tin hoac nut dong goi onDong', async () => {
    const onDong = vi.fn();
    render(<PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={onDong} onMoQuanLy={() => {}} onRoiNhom={() => {}} />);

    await userEvent.click(screen.getByRole('button', { name: 'Nhắn tin' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });
});
