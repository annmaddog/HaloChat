import { renderHook, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import type { ReactNode } from 'react';
import * as DichVuApi from '../DichVuApi';
import { NhaCungCapXacThuc } from './NguCanhXacThuc';
import { NhaCungCapChat } from './NguCanhChat';
import { SuDungSoLuongChuaDoc } from './SuDungSoLuongChuaDoc';

const ketNoiGiaLap = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  off: vi.fn(),
  invoke: vi.fn(),
  onreconnected: vi.fn(),
  onreconnecting: vi.fn(),
  onclose: vi.fn(),
};

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      configureLogging: vi.fn().mockReturnThis(),
      build: vi.fn().mockReturnValue(ketNoiGiaLap),
    };
  }),
  LogLevel: { Warning: 2 },
}));

function boc({ children }: { children: ReactNode }) {
  return (
    <NhaCungCapXacThuc>
      <NhaCungCapChat>{children}</NhaCungCapChat>
    </NhaCungCapXacThuc>
  );
}

describe('SuDungSoLuongChuaDoc', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    localStorage.setItem('haloChatToken', 'token-gia-lap');
  });

  it('cong dung so tin chua doc cua tat ca hoi thoai vao tinNhan', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com',
      choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai').mockResolvedValue([
      { nguoiDung: { id: 'a', tenTaiKhoan: 'A', email: 'a@gmail.com' }, tinNhanCuoi: 'hi', thoiGianTinNhanCuoi: '', soTinChuaDoc: 2 },
      { nguoiDung: { id: 'b', tenTaiKhoan: 'B', email: 'b@gmail.com' }, tinNhanCuoi: 'hi', thoiGianTinNhanCuoi: '', soTinChuaDoc: 3 },
    ]);
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LaySoTinNhomChuaDoc').mockResolvedValue({ soTinChuaDoc: 0 });

    const { result } = renderHook(() => SuDungSoLuongChuaDoc(), { wrapper: boc });

    await waitFor(() => expect(result.current.tinNhan).toBe(5));
  });

  it('tra ve 0 cho tinNhan khi thongBaoTinNhanMoi tat, khong goi LayDanhSachHoiThoai', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com',
      choPhepTinNhanTuNguoiLa: true, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: false, thongBaoLoiMoiKetBan: true, thongBaoNhom: true,
    });
    const spyHoiThoai = vi.spyOn(DichVuApi, 'LayDanhSachHoiThoai');
    vi.spyOn(DichVuApi, 'LayLoiMoiDen').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LaySoTinNhomChuaDoc').mockResolvedValue({ soTinChuaDoc: 0 });

    const { result } = renderHook(() => SuDungSoLuongChuaDoc(), { wrapper: boc });

    await waitFor(() => expect(result.current.banBe).toBe(0));
    expect(spyHoiThoai).not.toHaveBeenCalled();
    expect(result.current.tinNhan).toBe(0);
  });
});
