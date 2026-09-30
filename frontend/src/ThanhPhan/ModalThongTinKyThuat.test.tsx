import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import * as DichVuApi from '../DichVuApi';
import { ModalThongTinKyThuat } from './ModalThongTinKyThuat';

describe('ModalThongTinKyThuat', () => {
  it('hien badge Da ma hoa va Hop le khi tin da ma hoa co chu ky dung', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinKyThuat').mockResolvedValue({
      apDungDuoc: true, daMaHoa: true, thuatToanMaHoa: 'AES-256-GCM',
      kichThuocGocByte: 18, kichThuocMaHoaByte: 34, tyLePhinh: 1.89,
      ciphertextRutGon: 'd3a163f3...bfcc0395', nonceRutGon: '6d0c7f89...c172a', authTagRutGon: '33d5467c...d0152cb',
      thoiGianMaHoaMs: 0.09, thoiGianGiaiMaMs: 0.34,
      coChuKy: true, daXacThucChuKy: true, thuatToanChuKy: 'RSA-PSS / SHA-256',
    });

    render(<ModalThongTinKyThuat token="tok" tinNhanId="m1" onDong={() => {}} />);

    expect(await screen.findByText('Đã mã hóa')).toBeInTheDocument();
    expect(screen.getByText('✓ Hợp lệ')).toBeInTheDocument();
    expect(screen.getByText('AES-256-GCM')).toBeInTheDocument();
  });

  it('hien Khong ap dung khi tin da thu hoi', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinKyThuat').mockResolvedValue({
      apDungDuoc: false, daMaHoa: false, thuatToanMaHoa: null,
      kichThuocGocByte: null, kichThuocMaHoaByte: null, tyLePhinh: null,
      ciphertextRutGon: null, nonceRutGon: null, authTagRutGon: null,
      thoiGianMaHoaMs: null, thoiGianGiaiMaMs: null,
      coChuKy: false, daXacThucChuKy: null, thuatToanChuKy: null,
    });

    render(<ModalThongTinKyThuat token="tok" tinNhanId="m2" onDong={() => {}} />);

    expect(await screen.findByText('Không áp dụng cho loại tin nhắn này.')).toBeInTheDocument();
  });

  it('hien Khong co chu ky khi tin cu chua tung ky', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinKyThuat').mockResolvedValue({
      apDungDuoc: true, daMaHoa: false, thuatToanMaHoa: null,
      kichThuocGocByte: 9, kichThuocMaHoaByte: null, tyLePhinh: null,
      ciphertextRutGon: null, nonceRutGon: null, authTagRutGon: null,
      thoiGianMaHoaMs: null, thoiGianGiaiMaMs: null,
      coChuKy: false, daXacThucChuKy: null, thuatToanChuKy: null,
    });

    render(<ModalThongTinKyThuat token="tok" tinNhanId="m3" onDong={() => {}} />);

    expect(await screen.findByText('Không có chữ ký')).toBeInTheDocument();
    expect(await screen.findByText('Không mã hóa')).toBeInTheDocument();
  });
});
