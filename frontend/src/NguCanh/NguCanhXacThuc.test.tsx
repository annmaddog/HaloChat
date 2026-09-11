import { render, screen, act, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NhaCungCapXacThuc, useXacThuc } from './NguCanhXacThuc';
import * as DichVuApi from '../DichVuApi';

function ThanhPhanKiemThu() {
  const { daDangNhap, dangNhap, dangXuat } = useXacThuc();
  return (
    <div>
      <span>{daDangNhap ? 'da-dang-nhap' : 'chua-dang-nhap'}</span>
      <button onClick={() => dangNhap('NguyenAn', 'MatKhau123')}>Đăng nhập</button>
      <button onClick={dangXuat}>Đăng xuất</button>
    </div>
  );
}

describe('NguCanhXacThuc', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('ban đầu chưa đăng nhập nếu localStorage trống', () => {
    render(
      <NhaCungCapXacThuc>
        <ThanhPhanKiemThu />
      </NhaCungCapXacThuc>,
    );
    expect(screen.getByText('chua-dang-nhap')).toBeInTheDocument();
  });

  it('dangNhap gọi API, lưu token vào localStorage và cập nhật trạng thái', async () => {
    vi.spyOn(DichVuApi, 'DangNhap').mockResolvedValue({ token: 'token-gia-lap' });
    render(
      <NhaCungCapXacThuc>
        <ThanhPhanKiemThu />
      </NhaCungCapXacThuc>,
    );

    await act(async () => {
      await userEvent.click(screen.getByText('Đăng nhập'));
    });

    expect(screen.getByText('da-dang-nhap')).toBeInTheDocument();
    expect(localStorage.getItem('haloChatToken')).toBe('token-gia-lap');
  });

  it('dangXuat xóa token khỏi localStorage và cập nhật trạng thái', async () => {
    localStorage.setItem('haloChatToken', 'token-cu');
    render(
      <NhaCungCapXacThuc>
        <ThanhPhanKiemThu />
      </NhaCungCapXacThuc>,
    );
    expect(screen.getByText('da-dang-nhap')).toBeInTheDocument();

    await act(async () => {
      await userEvent.click(screen.getByText('Đăng xuất'));
    });

    expect(screen.getByText('chua-dang-nhap')).toBeInTheDocument();
    expect(localStorage.getItem('haloChatToken')).toBeNull();
  });

  it('nguoiDungHienTai giải mã đúng từ token trong localStorage', () => {
    const than = btoa(JSON.stringify({ sub: '42', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${than}.chuky`);

    function ThanhPhanNguoiDungHienTai() {
      const { nguoiDungHienTai } = useXacThuc();
      return <span>{nguoiDungHienTai?.tenTaiKhoan ?? 'khong-co'}</span>;
    }

    render(
      <NhaCungCapXacThuc>
        <ThanhPhanNguoiDungHienTai />
      </NhaCungCapXacThuc>,
    );

    expect(screen.getByText('NguyenAn')).toBeInTheDocument();
  });
});
