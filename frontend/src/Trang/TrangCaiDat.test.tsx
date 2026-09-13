import { render, screen, waitFor } from '@testing-library/react';
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

describe('TrangCaiDat', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('tải và hiển thị đúng trạng thái cài đặt ban đầu (mục Quyền riêng tư)', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: false,
    });

    renderTrangCaiDat();

    const [choPhep, hienThi] = await screen.findAllByRole('checkbox');
    await waitFor(() => expect(choPhep).toBeChecked());
    expect(hienThi).not.toBeChecked();
  });

  it('bật toggle "cho phép người lạ" gọi CapNhatCaiDat với 3 tham số đúng', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
    });
    const capNhatSpy = vi.spyOn(DichVuApi, 'CapNhatCaiDat').mockResolvedValue(undefined);

    renderTrangCaiDat();
    const [choPhep] = await screen.findAllByRole('checkbox');
    await waitFor(() => expect(choPhep).not.toBeChecked());

    await userEvent.click(choPhep);

    await waitFor(() => expect(capNhatSpy).toHaveBeenCalledWith('token-gia-lap', true, true));
    expect(await screen.findByText('Đã lưu.')).toBeInTheDocument();
  });

  it('chuyển sang mục Tài khoản hiển thị tên tài khoản/email và nút Đăng xuất', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
    });

    renderTrangCaiDat();
    await userEvent.click(screen.getByRole('button', { name: 'Tài khoản' }));

    expect(await screen.findByRole('button', { name: 'Đăng xuất' })).toBeInTheDocument();
  });

  it('chuyển sang mục Bảo mật và Thông báo hiển thị nội dung "sắp ra mắt"', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
    });

    renderTrangCaiDat();

    await userEvent.click(screen.getByRole('button', { name: 'Bảo mật' }));
    expect(await screen.findByText(/sắp ra mắt/)).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Thông báo' }));
    expect(await screen.findByText(/sắp ra mắt/)).toBeInTheDocument();
  });
});
