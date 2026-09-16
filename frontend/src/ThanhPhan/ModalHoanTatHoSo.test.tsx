import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ModalHoanTatHoSo } from './ModalHoanTatHoSo';
import * as DichVuApi from '../DichVuApi';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';

function renderModal(onDong = vi.fn()) {
  localStorage.setItem('haloChatToken', 'token-gia-lap');
  return {
    onDong,
    ...render(
      <NhaCungCapXacThuc>
        <ModalHoanTatHoSo tenHienThiBanDau="NguyenAn" onDong={onDong} />
      </NhaCungCapXacThuc>,
    ),
  };
}

describe('ModalHoanTatHoSo', () => {
  it('hien dung tieu de, phu de va gia tri ten hien thi ban dau', () => {
    renderModal();

    expect(screen.getByText('Hoàn tất hồ sơ')).toBeInTheDocument();
    expect(screen.getByText(/Hãy thiết lập hồ sơ của bạn/)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nhập tên của bạn...')).toHaveValue('NguyenAn');
    expect(screen.getByText('8/50')).toBeInTheDocument();
  });

  it('go ten hien thi cap nhat bo dem dung', async () => {
    renderModal();
    const oNhap = screen.getByPlaceholderText('Nhập tên của bạn...');

    await userEvent.clear(oNhap);
    await userEvent.type(oNhap, 'Tên Mới');

    expect(screen.getByText('7/50')).toBeInTheDocument();
  });

  it('chon anh hien preview ngay bang blob url, chua goi API', async () => {
    const taiLenTep = vi.spyOn(DichVuApi, 'TaiLenTep');
    renderModal();
    const tep = new File(['noi-dung'], 'avatar.png', { type: 'image/png' });

    await userEvent.upload(screen.getByLabelText('Chọn ảnh'), tep);

    expect(taiLenTep).not.toHaveBeenCalled();
    expect(screen.getByAltText('NguyenAn')).toHaveAttribute('src', expect.stringMatching(/^blob:/));
  });

  it('chon file khong phai anh thi bao loi, khong tao preview', async () => {
    renderModal();
    const tep = new File(['noi-dung'], 'a.pdf', { type: 'application/pdf' });

    await userEvent.setup({ applyAccept: false }).upload(screen.getByLabelText('Chọn ảnh'), tep);

    expect(await screen.findByText('Chỉ chấp nhận file ảnh.')).toBeInTheDocument();
    expect(screen.queryByAltText('NguyenAn')).not.toBeInTheDocument();
  });

  it('bam Hoan tat khi co doi anh va ten thi goi du chuoi API roi dong', async () => {
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/abc123', tenFileGoc: 'avatar.png', kichThuocFile: 100, loaiFile: 'image/png' });
    vi.spyOn(DichVuApi, 'DoiAnhDaiDien').mockResolvedValue({ id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', duongDanAnhDaiDien: '/api/tinnhan/file/abc123' });
    vi.spyOn(DichVuApi, 'DoiTenHienThi').mockResolvedValue({ id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới' });
    const hoSoCuoiCung = { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới', duongDanAnhDaiDien: '/api/tinnhan/file/abc123', daXemHoanTatHoSo: true };
    vi.spyOn(DichVuApi, 'DanhDauHoanTatHoSo').mockResolvedValue(hoSoCuoiCung);
    const onDong = vi.fn();
    renderModal(onDong);
    const tep = new File(['noi-dung'], 'avatar.png', { type: 'image/png' });
    await userEvent.upload(screen.getByLabelText('Chọn ảnh'), tep);
    const oNhap = screen.getByPlaceholderText('Nhập tên của bạn...');
    await userEvent.clear(oNhap);
    await userEvent.type(oNhap, 'Tên Mới');

    await userEvent.click(screen.getByRole('button', { name: 'Hoàn tất' }));

    expect(DichVuApi.TaiLenTep).toHaveBeenCalledWith('token-gia-lap', tep);
    expect(DichVuApi.DoiAnhDaiDien).toHaveBeenCalledWith('token-gia-lap', '/api/tinnhan/file/abc123');
    expect(DichVuApi.DoiTenHienThi).toHaveBeenCalledWith('token-gia-lap', 'Tên Mới');
    expect(DichVuApi.DanhDauHoanTatHoSo).toHaveBeenCalledWith('token-gia-lap');
    expect(onDong).toHaveBeenCalledWith(hoSoCuoiCung);
  });

  it('bam X khong goi DoiAnhDaiDien/DoiTenHienThi nhung van goi DanhDauHoanTatHoSo', async () => {
    const daXem = { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', daXemHoanTatHoSo: true };
    vi.spyOn(DichVuApi, 'DanhDauHoanTatHoSo').mockResolvedValue(daXem);
    const taiLenTep = vi.spyOn(DichVuApi, 'TaiLenTep');
    const doiTenHienThi = vi.spyOn(DichVuApi, 'DoiTenHienThi');
    const onDong = vi.fn();
    renderModal(onDong);
    const oNhap = screen.getByPlaceholderText('Nhập tên của bạn...');
    await userEvent.clear(oNhap);
    await userEvent.type(oNhap, 'Tên gõ dở');

    await userEvent.click(screen.getByRole('button', { name: 'Đóng' }));

    expect(taiLenTep).not.toHaveBeenCalled();
    expect(doiTenHienThi).not.toHaveBeenCalled();
    expect(DichVuApi.DanhDauHoanTatHoSo).toHaveBeenCalledWith('token-gia-lap');
    expect(onDong).toHaveBeenCalledWith(daXem);
  });
});
