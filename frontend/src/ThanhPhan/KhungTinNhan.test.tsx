import { render, screen, within, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { KhungTinNhan } from './KhungTinNhan';
import type { TinNhan } from '../KieuDuLieu';

const PROPS_MAC_DINH = {
  loaiHoiThoai: 'nhom' as const,
  tenHienThi: 'Nhóm CNTT',
  phuDe: '3 thành viên',
  danhSachTinNhan: [],
  idHienTai: '1',
  dangKetNoi: true,
  dangTaiLichSu: false,
  coTheTaiThem: false,
  onTaiThemLichSuCu: () => {},
  onGuiVanBan: () => {},
  onGuiTep: () => {},
  dangTaiTep: false,
  loi: null,
  onThuHoi: () => {},
  onGhim: () => {},
  onBoGhim: () => {},
  onAn: () => {},
  danhSachTinNhanGhim: [],
  onMoKhoMedia: () => {},
  onTimKiem: () => Promise.resolve([]),
  onNhayToiTinNhan: () => Promise.resolve(true),
  onThaCamXuc: () => {},
  onBoCamXuc: () => {},
  duongDanAnh: null,
};

const TIN_NHAN_MAU = {
  id: 'm1',
  nguoiGuiId: '1',
  nguoiNhanId: null,
  nhomId: 'n1',
  loaiTinNhan: 'Text' as const,
  noiDungTinNhan: 'Chào mọi người',
  duongDanFile: null,
  tenFileGoc: null,
  kichThuocFile: null,
  loaiFile: null,
  daDoc: false,
  daNhan: false,
  thoiGianTao: '2026-01-01T10:30:00.000Z',
  traLoi: null,
  daThuHoi: false,
  daGhim: false,
  thoiGianGhim: null,
  danhSachCamXuc: [],
};

describe('KhungTinNhan', () => {
  it('khong co onBamTieuDe: tieu de la span tinh, khong phai nut bam', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} />);

    expect(screen.getByText('Nhóm CNTT')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Nhóm CNTT/ })).not.toBeInTheDocument();
  });

  it('co onBamTieuDe: tieu de la nut bam duoc, bam goi dung ham', async () => {
    const onBamTieuDe = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onBamTieuDe={onBamTieuDe} />);

    await userEvent.click(screen.getByRole('button', { name: /Nhóm CNTT/ }));

    expect(onBamTieuDe).toHaveBeenCalledTimes(1);
  });

  it('mac dinh khong hien gio:phut va khong hien trang thai da gui/da nhan/da xem', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[TIN_NHAN_MAU]} />);

    expect(screen.getByText('Chào mọi người')).toBeInTheDocument();
    expect(screen.queryByText(/^\d{1,2}:\d{2}$/)).not.toBeInTheDocument();
    expect(screen.queryByText('Đã gửi')).not.toBeInTheDocument();
    expect(screen.queryByText('Đã nhận')).not.toBeInTheDocument();
    expect(screen.queryByText('Đã xem')).not.toBeInTheDocument();
  });

  it('bam vao tin nhan hien gio:phut, bam lai lan nua thi an di', async () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[TIN_NHAN_MAU]} />);

    await userEvent.click(screen.getByText('Chào mọi người'));
    expect(screen.getByText(/^\d{1,2}:\d{2}$/)).toBeInTheDocument();

    await userEvent.click(screen.getByText('Chào mọi người'));
    expect(screen.queryByText(/^\d{1,2}:\d{2}$/)).not.toBeInTheDocument();
  });

  it('bam icon Tra loi hien khoi dang tra loi voi ten va trich dan dung', async () => {
    const tinGoc: TinNhan = {
      id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    const khoiDangTraLoi = screen.getByText(/↩ Trả lời/).closest('.khung-tin-nhan__dang-tra-loi') as HTMLElement;
    expect(khoiDangTraLoi).toBeInTheDocument();
    expect(within(khoiDangTraLoi).getByText('Xin chào bạn')).toBeInTheDocument();
  });

  it('huy tra loi an khoi preview', async () => {
    const tinGoc: TinNhan = {
      id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" />);
    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    await userEvent.click(screen.getByRole('button', { name: 'Hủy trả lời' }));

    expect(screen.queryByText(/↩ Trả lời/)).not.toBeInTheDocument();
  });

  it('gui tin nhan luc dang tra loi goi onGuiVanBan voi dung traLoiId', async () => {
    const tinGoc: TinNhan = {
      id: 'm1', nguoiGuiId: 'nguoi-kia', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Xin chào bạn', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    const onGuiVanBan = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinGoc]} idHienTai="toi" tenHienThi="Nguoi Kia" onGuiVanBan={onGuiVanBan} />);
    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Đây là câu trả lời{enter}');

    expect(onGuiVanBan).toHaveBeenCalledWith('Đây là câu trả lời', 'm1');
  });

  it('tin nhan co truong traLoi hien khoi trich dan trong bong bong', () => {
    const tinTraLoi: TinNhan = {
      id: 'm2', nguoiGuiId: 'toi', nguoiNhanId: 'nguoi-kia', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Đây là câu trả lời', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: false, daNhan: false,
      thoiGianTao: '2026-01-01T00:01:00Z',
      traLoi: { id: 'm1', tenNguoiGui: 'Nguoi Kia', noiDungTomTat: 'Xin chào bạn', loaiTinNhan: 'Text' },
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinTraLoi]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

    const khoiTrichDan = document.querySelector('.khung-tin-nhan__trich-dan') as HTMLElement;
    expect(khoiTrichDan).toBeInTheDocument();
    expect(within(khoiTrichDan).getByText('Nguoi Kia')).toBeInTheDocument();
    expect(within(khoiTrichDan).getByText('Xin chào bạn')).toBeInTheDocument();
  });

  it('doi hoi thoai (tenHienThi doi) reset trang thai dang tra loi', async () => {
    const tinA: TinNhan = {
      id: 'a1', nguoiGuiId: 'nguoi-a', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Tin cua hoi thoai A', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    const tinB: TinNhan = {
      id: 'b1', nguoiGuiId: 'nguoi-b', nguoiNhanId: 'toi', nhomId: null,
      loaiTinNhan: 'Text', noiDungTinNhan: 'Tin cua hoi thoai B', duongDanFile: null, tenFileGoc: null,
      kichThuocFile: null, loaiFile: null, daDoc: true, daNhan: true,
      thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    const onGuiVanBan = vi.fn();
    const { rerender } = render(
      <KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinA]} idHienTai="toi" tenHienThi="Hoi thoai A" onGuiVanBan={onGuiVanBan} />,
    );
    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));
    expect(screen.getByText(/↩ Trả lời/)).toBeInTheDocument();

    rerender(
      <KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinB]} idHienTai="toi" tenHienThi="Hoi thoai B" onGuiVanBan={onGuiVanBan} />,
    );

    expect(screen.queryByText(/↩ Trả lời/)).not.toBeInTheDocument();

    await userEvent.type(screen.getByPlaceholderText('Nhập tin nhắn...'), 'Tin nhan moi{enter}');
    expect(onGuiVanBan).toHaveBeenCalledWith('Tin nhan moi', null);
  });

  it('card File hien nut tron tai xuong rieng biet', () => {
    const tinFile: TinNhan = {
      id: 'm3', nguoiGuiId: 'toi', nguoiNhanId: 'nguoi-kia', nhomId: null,
      loaiTinNhan: 'File', noiDungTinNhan: '', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439099',
      tenFileGoc: 'bao-cao.docx', kichThuocFile: 15360, loaiFile: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinFile]} idHienTai="toi" tenHienThi="Nguoi Kia" />);

    expect(screen.getByText('bao-cao.docx')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Tải xuống bao-cao.docx' })).toBeInTheDocument();
  });

  it('bam ... hien menu voi dung cac muc theo trang thai tin nhan', async () => {
    const tinCuaMinh = { ...TIN_NHAN_MAU, id: 'm1', nguoiGuiId: '1', loaiTinNhan: 'Text' as const };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinCuaMinh]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);

    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

    expect(screen.getByRole('button', { name: 'Ghim' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Thu hồi tin nhắn' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Xóa' })).toBeInTheDocument();
  });

  it('tin da thu hoi an placeholder thay vi noi dung goc', () => {
    const tinDaThuHoi = { ...TIN_NHAN_MAU, id: 'm2', daThuHoi: true, noiDungTinNhan: 'noi dung cu' };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinDaThuHoi]} idHienTai="1" danhSachTinNhanGhim={[]} onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} />);

    expect(screen.getByText('Tin nhắn đã được thu hồi.')).toBeInTheDocument();
    expect(screen.queryByText('noi dung cu')).not.toBeInTheDocument();
  });

  it('tin da thu hoi khong con muc Thu hoi/Ghim/Luu trong menu', async () => {
    const tinDaThuHoi = { ...TIN_NHAN_MAU, id: 'm2', nguoiGuiId: '1', daThuHoi: true };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinDaThuHoi]} idHienTai="1" danhSachTinNhanGhim={[]} onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} />);

    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

    expect(screen.queryByRole('button', { name: 'Ghim' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Thu hồi tin nhắn' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Xóa' })).toBeInTheDocument();
  });

  it('bam Thu hoi goi onThuHoi dung id', async () => {
    const tinCuaMinh = { ...TIN_NHAN_MAU, id: 'm1', nguoiGuiId: '1' };
    const onThuHoi = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinCuaMinh]} idHienTai="1" onThuHoi={onThuHoi} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

    await userEvent.click(screen.getByRole('button', { name: 'Thu hồi tin nhắn' }));

    expect(onThuHoi).toHaveBeenCalledWith('m1');
  });

  it('bam Ghim goi onGhim, bam lai (da ghim) goi onBoGhim', async () => {
    const tinChuaGhim = { ...TIN_NHAN_MAU, id: 'm1', daGhim: false };
    const onGhim = vi.fn();
    const { rerender } = render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinChuaGhim]} idHienTai="1" onThuHoi={() => {}} onGhim={onGhim} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));
    await userEvent.click(screen.getByRole('button', { name: 'Ghim' }));
    expect(onGhim).toHaveBeenCalledWith('m1');

    const onBoGhim = vi.fn();
    const tinDaGhim = { ...TIN_NHAN_MAU, id: 'm1', daGhim: true };
    rerender(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tinDaGhim]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={onBoGhim} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));
    await userEvent.click(screen.getByRole('button', { name: 'Bỏ ghim' }));
    expect(onBoGhim).toHaveBeenCalledWith('m1');
  });

  it('bam Xoa goi onAn dung id', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1' };
    const onAn = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={onAn} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

    await userEvent.click(screen.getByRole('button', { name: 'Xóa' }));

    expect(onAn).toHaveBeenCalledWith('m1');
  });

  it('bam ra ngoai menu dang mo se dong menu lai', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1' };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));
    expect(screen.getByRole('button', { name: 'Xóa' })).toBeInTheDocument();

    await userEvent.click(document.body);

    expect(screen.queryByRole('button', { name: 'Xóa' })).not.toBeInTheDocument();
  });

  it('bam phim Escape khi menu dang mo se dong menu lai', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1' };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));
    expect(screen.getByRole('button', { name: 'Xóa' })).toBeInTheDocument();

    await userEvent.keyboard('{Escape}');

    expect(screen.queryByRole('button', { name: 'Xóa' })).not.toBeInTheDocument();
  });

  it('bam vao nut Tra loi trong khi menu dang mo khong lam menu tu dong dong truoc khi bam', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1' };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    await userEvent.click(screen.getByRole('button', { name: 'Thêm tùy chọn' }));

    await userEvent.click(screen.getByRole('button', { name: 'Trả lời tin nhắn này' }));

    expect(screen.getByText(/↩ Trả lời/)).toBeInTheDocument();
  });

  it('banner ghim hien dung danh sach tin da ghim, an khi rong', () => {
    const { rerender } = render(<KhungTinNhan {...PROPS_MAC_DINH} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[]} />);
    expect(screen.queryByText(/^\[File\]|^\[Ảnh\]/)).not.toBeInTheDocument();

    const tinGhim = { ...TIN_NHAN_MAU, id: 'm1', nguoiGuiId: 'nguoi-kia', noiDungTinNhan: 'Nhớ nộp báo cáo' };
    rerender(<KhungTinNhan {...PROPS_MAC_DINH} idHienTai="1" onThuHoi={() => {}} onGhim={() => {}} onBoGhim={() => {}} onAn={() => {}} danhSachTinNhanGhim={[tinGhim]} />);

    expect(screen.getByText(/Nhớ nộp báo cáo/)).toBeInTheDocument();
  });

  it('dan anh tu clipboard (Ctrl+V) vao o nhap goi onGuiTep voi file anh', () => {
    const onGuiTep = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onGuiTep={onGuiTep} />);
    const oNhap = screen.getByPlaceholderText('Nhập tin nhắn...');
    const anhGia = new File(['anh'], 'clipboard.png', { type: 'image/png' });

    fireEvent.paste(oNhap, {
      clipboardData: { items: [{ type: 'image/png', kind: 'file', getAsFile: () => anhGia }] },
    });

    expect(onGuiTep).toHaveBeenCalledWith(anhGia, null);
  });

  it('dan file khong phai anh tu clipboard khong goi onGuiTep', () => {
    const onGuiTep = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onGuiTep={onGuiTep} />);
    const oNhap = screen.getByPlaceholderText('Nhập tin nhắn...');
    const fileGia = new File(['x'], 'tep.pdf', { type: 'application/pdf' });

    fireEvent.paste(oNhap, {
      clipboardData: { items: [{ type: 'application/pdf', kind: 'file', getAsFile: () => fileGia }] },
    });

    expect(onGuiTep).not.toHaveBeenCalled();
  });

  it('dan van ban thuong tu clipboard khong goi onGuiTep', () => {
    const onGuiTep = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onGuiTep={onGuiTep} />);
    const oNhap = screen.getByPlaceholderText('Nhập tin nhắn...');

    fireEvent.paste(oNhap, {
      clipboardData: { items: [{ type: 'text/plain', kind: 'string', getAsFile: () => null }] },
    });

    expect(onGuiTep).not.toHaveBeenCalled();
  });

  it('bam icon mat cuoi hien bang emoji, bam 1 emoji chen vao o nhap', async () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} />);

    await userEvent.click(screen.getByRole('button', { name: 'Chọn emoji' }));
    const oNhap = screen.getByPlaceholderText('Nhập tin nhắn...') as HTMLInputElement;
    const nutEmojiDauTien = screen.getByTestId('bang-emoji').querySelector('button') as HTMLButtonElement;

    await userEvent.click(nutEmojiDauTien);

    expect(oNhap.value.length).toBeGreaterThan(0);
  });

  it('bam ra ngoai bang emoji dang mo se dong bang lai', async () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} />);
    await userEvent.click(screen.getByRole('button', { name: 'Chọn emoji' }));
    expect(screen.getByTestId('bang-emoji')).toBeInTheDocument();

    await userEvent.click(document.body);

    expect(screen.queryByTestId('bang-emoji')).not.toBeInTheDocument();
  });

  it('bam icon kho media goi onMoKhoMedia', async () => {
    const onMoKhoMedia = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} onMoKhoMedia={onMoKhoMedia} />);

    await userEvent.click(screen.getByRole('button', { name: 'Kho lưu trữ Media & Tệp' }));

    expect(onMoKhoMedia).toHaveBeenCalledTimes(1);
  });

  it('bam icon kinh lup hien o tim kiem, go tu khoa goi onTimKiem', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const onTimKiem = vi.fn().mockResolvedValue([]);
    render(<KhungTinNhan {...PROPS_MAC_DINH} onTimKiem={onTimKiem} />);

    await userEvent.setup({ delay: null }).click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
    await userEvent.setup({ delay: null }).type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin chao');
    vi.advanceTimersByTime(350);

    expect(onTimKiem).toHaveBeenCalledWith('xin chao');
    vi.useRealTimers();
  });

  it('bam 1 ket qua da co san trong danh sach thi cuon toi va noi bat, khong goi onNhayToiTinNhan', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1', noiDungTinNhan: 'Xin chao ban' };
    const onTimKiem = vi.fn().mockResolvedValue([tin]);
    const onNhayToiTinNhan = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} onTimKiem={onTimKiem} onNhayToiTinNhan={onNhayToiTinNhan} />);
    await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
    await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin');
    const ketQua = await screen.findByTestId('ket-qua-tim-m1');

    await userEvent.click(ketQua);

    expect(onNhayToiTinNhan).not.toHaveBeenCalled();
    expect(screen.queryByPlaceholderText('Tìm tin nhắn...')).not.toBeInTheDocument();
  });

  it('bam 1 ket qua CHUA co trong danh sach thi goi onNhayToiTinNhan', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm-xa', noiDungTinNhan: 'Xin chao ban cu' };
    const onTimKiem = vi.fn().mockResolvedValue([tin]);
    const onNhayToiTinNhan = vi.fn().mockResolvedValue(true);
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[]} onTimKiem={onTimKiem} onNhayToiTinNhan={onNhayToiTinNhan} />);
    await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
    await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin');
    const ketQua = await screen.findByTestId('ket-qua-tim-m-xa');

    await userEvent.click(ketQua);

    expect(onNhayToiTinNhan).toHaveBeenCalledWith('m-xa');
  });

  it('khong tim thay tin sau khi tai het lich su thi hien thong bao', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm-mat', noiDungTinNhan: 'Tin da mat' };
    const onTimKiem = vi.fn().mockResolvedValue([tin]);
    const onNhayToiTinNhan = vi.fn().mockResolvedValue(false);
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[]} onTimKiem={onTimKiem} onNhayToiTinNhan={onNhayToiTinNhan} />);
    await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
    await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin');
    const ketQua = await screen.findByTestId('ket-qua-tim-m-mat');

    await userEvent.click(ketQua);

    expect(await screen.findByText('Không tìm thấy tin nhắn này trong lịch sử.')).toBeInTheDocument();
  });

  it('bam nhanh icon like khi chua co cam xuc thi tha Thich', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1', danhSachCamXuc: [] };
    const onThaCamXuc = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThaCamXuc={onThaCamXuc} />);

    await userEvent.click(screen.getByRole('button', { name: 'Thích tin nhắn này' }));

    expect(onThaCamXuc).toHaveBeenCalledWith('m1', 'Thich');
  });

  it('bam nhanh icon like khi DA co cam xuc cua minh thi bo cam xuc', async () => {
    const tin = { ...TIN_NHAN_MAU, id: 'm1', danhSachCamXuc: [{ nguoiDungId: '1', loaiCamXuc: 'Haha' as const }] };
    const onBoCamXuc = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onBoCamXuc={onBoCamXuc} />);

    await userEvent.click(screen.getByRole('button', { name: 'Thích tin nhắn này' }));

    expect(onBoCamXuc).toHaveBeenCalledWith('m1');
  });

  it('hover icon like hien popup 6 cam xuc, bam 1 cai goi dung loai', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const tin = { ...TIN_NHAN_MAU, id: 'm1', danhSachCamXuc: [] };
    const onThaCamXuc = vi.fn();
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" onThaCamXuc={onThaCamXuc} />);

    await userEvent.setup({ delay: null }).hover(screen.getByRole('button', { name: 'Thích tin nhắn này' }));
    vi.advanceTimersByTime(450);

    const nutWow = await screen.findByRole('button', { name: 'Thả cảm xúc Wow' });
    await userEvent.setup({ delay: null }).click(nutWow);

    expect(onThaCamXuc).toHaveBeenCalledWith('m1', 'Wow');
    vi.useRealTimers();
  });

  it('tin co cam xuc hien badge tong hop dung so luong', () => {
    const tin = {
      ...TIN_NHAN_MAU, id: 'm1',
      danhSachCamXuc: [
        { nguoiDungId: '1', loaiCamXuc: 'Thich' as const },
        { nguoiDungId: '2', loaiCamXuc: 'YeuThich' as const },
      ],
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" />);

    expect(screen.getByText('👍❤️ 2')).toBeInTheDocument();
  });

  it('tin da thu hoi khong hien badge cam xuc du danhSachCamXuc khong rong', () => {
    const tin = {
      ...TIN_NHAN_MAU, id: 'm1', daThuHoi: true,
      danhSachCamXuc: [{ nguoiDungId: '2', loaiCamXuc: 'Thich' as const }],
    };
    render(<KhungTinNhan {...PROPS_MAC_DINH} danhSachTinNhan={[tin]} idHienTai="1" />);

    expect(screen.queryByText(/👍/)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Thích tin nhắn này' })).not.toBeInTheDocument();
  });

  it('co duongDanAnh thi hien anh that trong avatar dau de, khong hien chu cai dau', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} duongDanAnh="/api/tinnhan/file/abc" />);

    const anh = screen.getByAltText('Nhóm CNTT');
    expect(anh).toHaveAttribute('src', expect.stringContaining('/api/tinnhan/file/abc'));
  });

  it('khong co duongDanAnh thi hien chu cai dau nhu cu', () => {
    render(<KhungTinNhan {...PROPS_MAC_DINH} duongDanAnh={null} />);

    expect(screen.queryByAltText('Nhóm CNTT')).not.toBeInTheDocument();
  });
});
