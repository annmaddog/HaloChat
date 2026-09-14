import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PanelQuanLyNhom } from './PanelQuanLyNhom';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';
import type { Nhom } from '../KieuDuLieu';

const NHOM_GIA_LAP: Nhom = {
  id: 'n1',
  tenNhom: 'Nhóm CNTT',
  moTa: null,
  duongDanAnhDaiDien: null,
  nguoiTaoId: '1',
  thanhVien: [
    { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
    { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
  ],
  thoiGianTao: '2026-01-01T00:00:00Z',
};

const TAT_CA_NGUOI_DUNG = [
  { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true },
  { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true },
  { id: '3', tenTaiKhoan: 'LeC', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true },
];

function renderPanel(propsGhiDe: Partial<Parameters<typeof PanelQuanLyNhom>[0]> = {}) {
  return render(
    <NhaCungCapXacThuc>
      <PanelQuanLyNhom
        nhom={NHOM_GIA_LAP}
        tatCaNguoiDung={TAT_CA_NGUOI_DUNG}
        onDong={() => {}}
        onThemThanhVien={() => {}}
        onXoaThanhVien={() => {}}
        onCapNhatNhom={() => {}}
        {...propsGhiDe}
      />
    </NhaCungCapXacThuc>,
  );
}

describe('PanelQuanLyNhom', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('hien dung danh sach thanh vien, danh dau Admin cho nguoi tao', () => {
    renderPanel();

    expect(screen.getByText('NguyenAn (Admin)')).toBeInTheDocument();
    expect(screen.getByText('TranBinh')).toBeInTheDocument();
  });

  it('khong hien nut xoa cho nguoi tao nhom', () => {
    renderPanel();

    expect(screen.queryByRole('button', { name: 'Xóa NguyenAn' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Xóa TranBinh' })).toBeInTheDocument();
  });

  it('bam xoa 1 thanh vien goi onXoaThanhVien voi dung id', async () => {
    const onXoaThanhVien = vi.fn();
    renderPanel({ onXoaThanhVien });

    await userEvent.click(screen.getByRole('button', { name: 'Xóa TranBinh' }));

    expect(onXoaThanhVien).toHaveBeenCalledWith('2');
  });

  it('chon 1 nguoi trong dropdown them thanh vien goi onThemThanhVien', async () => {
    const onThemThanhVien = vi.fn();
    renderPanel({ onThemThanhVien });

    await userEvent.selectOptions(screen.getByRole('combobox'), 'LeC');

    expect(onThemThanhVien).toHaveBeenCalledWith('3');
  });

  it('doi ten nhom goi CapNhatNhom voi ten moi', async () => {
    vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({ ...NHOM_GIA_LAP, tenNhom: 'Nhóm mới' });
    const onCapNhatNhom = vi.fn();
    renderPanel({ onCapNhatNhom });

    const oTen = screen.getByDisplayValue('Nhóm CNTT');
    await userEvent.clear(oTen);
    await userEvent.type(oTen, 'Nhóm mới');
    await userEvent.click(screen.getByRole('button', { name: 'Lưu' }));

    await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', 'n1', 'Nhóm mới', null, null));
    await waitFor(() => expect(onCapNhatNhom).toHaveBeenCalledWith({ ...NHOM_GIA_LAP, tenNhom: 'Nhóm mới' }));
  });

  it('nut Luu bi disabled khi ten khong doi', () => {
    renderPanel();

    expect(screen.getByRole('button', { name: 'Lưu' })).toBeDisabled();
  });

  it('chon file anh goi TaiLenTep roi CapNhatNhom voi duongDanFile tra ve', async () => {
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({
      duongDanFile: '/uploads/anh-moi.png', tenFileGoc: 'anh.png', kichThuocFile: 1000, loaiFile: 'image/png',
    });
    vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({ ...NHOM_GIA_LAP, duongDanAnhDaiDien: '/uploads/anh-moi.png' });
    const onCapNhatNhom = vi.fn();
    renderPanel({ onCapNhatNhom });

    const tep = new File(['noi-dung'], 'anh.png', { type: 'image/png' });
    const oChonTep = document.querySelector('input[type="file"]') as HTMLInputElement;
    await userEvent.upload(oChonTep, tep);

    await waitFor(() => expect(DichVuApi.TaiLenTep).toHaveBeenCalledWith('token-gia-lap', tep));
    await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith('token-gia-lap', 'n1', 'Nhóm CNTT', null, '/uploads/anh-moi.png'));
    await waitFor(() => expect(onCapNhatNhom).toHaveBeenCalledWith({ ...NHOM_GIA_LAP, duongDanAnhDaiDien: '/uploads/anh-moi.png' }));
  });

  it('bam nut dong goi onDong', async () => {
    const onDong = vi.fn();
    renderPanel({ onDong });

    await userEvent.click(screen.getByRole('button', { name: 'Quay lại' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });
});
