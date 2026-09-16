import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PanelThongTinNhom } from './PanelThongTinNhom';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';
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

function renderPanel(propsGhiDe: Partial<Parameters<typeof PanelThongTinNhom>[0]> = {}) {
  return render(
    <NhaCungCapXacThuc>
      <PanelThongTinNhom
        nhom={taoNhomGiaLap(2)}
        laAdmin={false}
        onDong={() => {}}
        onMoQuanLy={() => {}}
        onRoiNhom={() => {}}
        onCapNhatNhom={() => {}}
        {...propsGhiDe}
      />
    </NhaCungCapXacThuc>,
  );
}

describe('PanelThongTinNhom', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('hien dung avatar, ten, so thanh vien', () => {
    renderPanel({ nhom: taoNhomGiaLap(3), laAdmin: false });

    expect(screen.getByText('Nhóm CNTT')).toBeInTheDocument();
    expect(screen.getByText('3 thành viên')).toBeInTheDocument();
  });

  it('khong phai admin: khong hien nut Chinh sua va Quan ly nhom', () => {
    renderPanel({ nhom: taoNhomGiaLap(2), laAdmin: false });

    expect(screen.queryByRole('button', { name: 'Chỉnh sửa' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Quản lý nhóm' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rời nhóm' })).toBeInTheDocument();
  });

  it('la admin: hien nut Chinh sua va Quan ly nhom, nut roi doi thanh Giai tan nhom', async () => {
    const onMoQuanLy = vi.fn();
    renderPanel({ nhom: taoNhomGiaLap(2), laAdmin: true, onMoQuanLy });

    expect(screen.getByRole('button', { name: 'Giải tán nhóm' })).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Chỉnh sửa' }));
    expect(onMoQuanLy).toHaveBeenCalledTimes(1);
  });

  it('hien toi da 8 avatar thanh vien mac dinh, con lai an sau nut', () => {
    const { container } = renderPanel({ nhom: taoNhomGiaLap(10), laAdmin: false });

    const avatarHang = container.querySelector('.panel-thong-tin-nhom__avatar-hang');
    const soAvatarNho = avatarHang?.querySelectorAll('.avatar--nho').length || 0;
    expect(soAvatarNho).toBeLessThanOrEqual(8);
    expect(screen.getByRole('button', { name: 'Xem tất cả thành viên' })).toBeInTheDocument();
  });

  it('bam Xem tat ca thanh vien hien het avatar, an nut di', async () => {
    renderPanel({ nhom: taoNhomGiaLap(10), laAdmin: false });

    await userEvent.click(screen.getByRole('button', { name: 'Xem tất cả thành viên' }));

    expect(screen.queryByRole('button', { name: 'Xem tất cả thành viên' })).not.toBeInTheDocument();
  });

  it('bam Roi nhom goi onRoiNhom', async () => {
    const onRoiNhom = vi.fn();
    renderPanel({ nhom: taoNhomGiaLap(2), laAdmin: false, onRoiNhom });

    await userEvent.click(screen.getByRole('button', { name: 'Rời nhóm' }));

    expect(onRoiNhom).toHaveBeenCalledTimes(1);
  });

  it('bam Nhan tin hoac nut dong goi onDong', async () => {
    const onDong = vi.fn();
    renderPanel({ nhom: taoNhomGiaLap(2), laAdmin: false, onDong });

    await userEvent.click(screen.getByRole('button', { name: 'Nhắn tin' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });

  it('admin thay nut camera doi anh dai dien, non-admin khong thay', () => {
    const { rerender } = render(
      <NhaCungCapXacThuc>
        <PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={true} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} onCapNhatNhom={() => {}} />
      </NhaCungCapXacThuc>,
    );
    expect(screen.getByLabelText('Đổi ảnh đại diện nhóm')).toBeInTheDocument();

    rerender(
      <NhaCungCapXacThuc>
        <PanelThongTinNhom nhom={taoNhomGiaLap(2)} laAdmin={false} onDong={() => {}} onMoQuanLy={() => {}} onRoiNhom={() => {}} onCapNhatNhom={() => {}} />
      </NhaCungCapXacThuc>,
    );
    expect(screen.queryByLabelText('Đổi ảnh đại diện nhóm')).not.toBeInTheDocument();
  });

  it('admin bam nut camera chon anh thi goi TaiLenTep roi CapNhatNhom', async () => {
    const nhom = taoNhomGiaLap(2);
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/nhom123', tenFileGoc: 'a.png', kichThuocFile: 100, loaiFile: 'image/png' });
    vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({ ...nhom, duongDanAnhDaiDien: '/api/tinnhan/file/nhom123' });
    const onCapNhatNhom = vi.fn();
    renderPanel({ nhom, laAdmin: true, onCapNhatNhom });
    const tep = new File(['noi-dung'], 'a.png', { type: 'image/png' });

    await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện nhóm'), tep);

    await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', nhom.id, nhom.tenNhom, nhom.moTa, '/api/tinnhan/file/nhom123'));
    await waitFor(() => expect(onCapNhatNhom).toHaveBeenCalledWith({ ...nhom, duongDanAnhDaiDien: '/api/tinnhan/file/nhom123' }));
  });
});
