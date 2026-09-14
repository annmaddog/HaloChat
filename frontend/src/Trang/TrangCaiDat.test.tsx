import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangCaiDat } from './TrangCaiDat';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function renderTrangCaiDat() {
  return render(
    <NhaCungCapXacThuc>
      <TrangCaiDat />
    </NhaCungCapXacThuc>,
  );
}

async function chuyenSangQuyenRiengTu() {
  await userEvent.click(screen.getByRole('button', { name: 'Quyền riêng tư' }));
}

describe('TrangCaiDat', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('tải và hiển thị đúng trạng thái cài đặt ban đầu (mục Quyền riêng tư)', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: false,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });

    renderTrangCaiDat();
    await chuyenSangQuyenRiengTu();

    const [choPhep, hienThi] = await screen.findAllByRole('switch');
    await waitFor(() => expect(choPhep).toHaveAttribute('aria-checked', 'true'));
    expect(hienThi).toHaveAttribute('aria-checked', 'false');
  });

  it('bật toggle "cho phép người lạ" gọi CapNhatCaiDat với 6 tham số đúng', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    const capNhatSpy = vi.spyOn(DichVuApi, 'CapNhatCaiDat').mockResolvedValue(undefined);

    renderTrangCaiDat();
    await chuyenSangQuyenRiengTu();
    const [choPhep] = await screen.findAllByRole('switch');
    await waitFor(() => expect(choPhep).toHaveAttribute('aria-checked', 'false'));

    await userEvent.click(choPhep);

    await waitFor(() => expect(capNhatSpy).toHaveBeenCalledWith('token-gia-lap', true, true, true, true, true, true));
    expect(await screen.findByText('Đã lưu.')).toBeInTheDocument();
  });

  it('lưu thất bại thì hoàn tác trạng thái công tắc và hiển thị lỗi', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    vi.spyOn(DichVuApi, 'CapNhatCaiDat').mockRejectedValue(new DichVuApi.LoiGoiApi(500, 'Lưu thất bại.'));

    renderTrangCaiDat();
    await chuyenSangQuyenRiengTu();
    const [choPhep] = await screen.findAllByRole('switch');
    await waitFor(() => expect(choPhep).toHaveAttribute('aria-checked', 'false'));

    await userEvent.click(choPhep);

    expect(await screen.findByText('Lưu thất bại.')).toBeInTheDocument();
    await waitFor(() => expect(choPhep).toHaveAttribute('aria-checked', 'false'));
  });

  it('mục Tài khoản hiển thị tên tài khoản/email và nút Đăng xuất (mục mặc định)', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });

    renderTrangCaiDat();

    expect(await screen.findByRole('button', { name: 'Đăng xuất' })).toBeInTheDocument();
  });

  it('chuyển sang mục Bảo mật hiển thị nội dung "sắp ra mắt"', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });

    renderTrangCaiDat();

    await userEvent.click(screen.getByRole('button', { name: 'Bảo mật' }));
    expect(await screen.findByText(/sắp ra mắt/)).toBeInTheDocument();
  });

  it('chuyển sang mục Thông báo hiển thị 3 công tắc thông báo', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });

    renderTrangCaiDat();

    await userEvent.click(screen.getByRole('button', { name: 'Thông báo' }));
    const congTac = await screen.findAllByRole('switch');
    expect(congTac).toHaveLength(3);
  });

  it('chuyển sang mục Giao diện và chọn Tối gọi apDungGiaoDien', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });

    renderTrangCaiDat();
    await userEvent.click(screen.getByRole('button', { name: 'Giao diện' }));

    const nutToi = screen.getByRole('button', { name: 'Tối' });
    await userEvent.click(nutToi);

    await waitFor(() => expect(document.documentElement.dataset.theme).toBe('toi'));
  });

  it('doi mat khau thanh cong hien thong bao', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    vi.spyOn(DichVuApi, 'DoiMatKhau').mockResolvedValue({ thongBao: 'Đã đổi mật khẩu thành công.' });

    renderTrangCaiDat();
    await screen.findByRole('button', { name: 'Đăng xuất' });

    fireEvent.change(screen.getByPlaceholderText('Mật khẩu cũ'), { target: { value: 'Cu123456' } });
    fireEvent.change(screen.getByPlaceholderText('Mật khẩu mới'), { target: { value: 'Moi123456' } });
    fireEvent.change(screen.getByPlaceholderText('Xác nhận mật khẩu mới'), { target: { value: 'Moi123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Đổi mật khẩu' }));

    await waitFor(() => expect(screen.getByText('Đã đổi mật khẩu thành công.')).toBeInTheDocument());
  });

  it('xac nhan mat khau moi khong khop hien loi, khong goi API', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    const doiMatKhauSpy = vi.spyOn(DichVuApi, 'DoiMatKhau');

    renderTrangCaiDat();
    await screen.findByRole('button', { name: 'Đăng xuất' });

    fireEvent.change(screen.getByPlaceholderText('Mật khẩu cũ'), { target: { value: 'Cu123456' } });
    fireEvent.change(screen.getByPlaceholderText('Mật khẩu mới'), { target: { value: 'Moi123456' } });
    fireEvent.change(screen.getByPlaceholderText('Xác nhận mật khẩu mới'), { target: { value: 'Khac123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Đổi mật khẩu' }));

    expect(screen.getByText('Xác nhận mật khẩu mới không khớp.')).toBeInTheDocument();
    expect(doiMatKhauSpy).not.toHaveBeenCalled();
  });
});
