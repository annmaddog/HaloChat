import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { PanelKhoMedia } from './PanelKhoMedia';
import type { TinNhan } from '../KieuDuLieu';

const TIN_ANH: TinNhan = {
  id: 'm1', nguoiGuiId: '1', nguoiNhanId: '2', nhomId: null, loaiTinNhan: 'Anh',
  noiDungTinNhan: '', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439001', tenFileGoc: 'a.png',
  kichThuocFile: 1024, loaiFile: 'image/png', daDoc: true, daNhan: true, thoiGianTao: '2026-01-01T00:00:00Z',
  traLoi: null, daThuHoi: false, daGhim: false, thoiGianGhim: null,
};

const TIN_FILE: TinNhan = {
  ...TIN_ANH, id: 'm2', loaiTinNhan: 'File', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439002',
  tenFileGoc: 'bao-cao.docx', loaiFile: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
};

describe('PanelKhoMedia', () => {
  it('hien dung so luong tung tab va noi dung tab Hinh anh mac dinh', () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_ANH, TIN_FILE]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    expect(screen.getByText('Hình ảnh (1)')).toBeInTheDocument();
    expect(screen.getByText('Tài liệu & Tệp (1)')).toBeInTheDocument();
    expect(screen.getByAltText('a.png')).toBeInTheDocument();
  });

  it('doi sang tab Tai lieu hien dung tep, khong hien anh', async () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_ANH, TIN_FILE]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    await userEvent.click(screen.getByText('Tài liệu & Tệp (1)'));

    expect(screen.getByText('bao-cao.docx')).toBeInTheDocument();
    expect(screen.queryByAltText('a.png')).not.toBeInTheDocument();
  });

  it('tab rong hien dung thong bao rieng cho tung tab', async () => {
    render(<PanelKhoMedia danhSachMedia={[]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    expect(screen.getByText('Chưa có hình ảnh nào được chia sẻ trong đoạn chat này')).toBeInTheDocument();

    await userEvent.click(screen.getByText('Tài liệu & Tệp (0)'));

    expect(screen.getByText('Chưa có tài liệu nào được chia sẻ trong đoạn chat này')).toBeInTheDocument();
  });

  it('bam anh mo lightbox, bam dong lightbox tra ve panel', async () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_ANH]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);

    await userEvent.click(screen.getByAltText('a.png'));

    expect(screen.getByRole('img', { name: 'Xem ảnh lớn a.png' })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Đóng ảnh lớn' }));

    expect(screen.queryByRole('img', { name: 'Xem ảnh lớn a.png' })).not.toBeInTheDocument();
  });

  it('bam nut dong goi onDong', async () => {
    const onDong = vi.fn();
    render(<PanelKhoMedia danhSachMedia={[]} tenCuocTroChuyen="TranBinh" onDong={onDong} />);

    await userEvent.click(screen.getByRole('button', { name: 'Đóng' }));

    expect(onDong).toHaveBeenCalledTimes(1);
  });

  it('tep hien duoi dang link tai xuong dung duong dan', async () => {
    render(<PanelKhoMedia danhSachMedia={[TIN_FILE]} tenCuocTroChuyen="TranBinh" onDong={() => {}} />);
    await userEvent.click(screen.getByText('Tài liệu & Tệp (1)'));

    const lienKet = screen.getByRole('link', { name: /bao-cao.docx/ });

    expect(lienKet).toHaveAttribute('href', expect.stringContaining('/api/tinnhan/file/507f1f77bcf86cd799439002'));
  });
});
