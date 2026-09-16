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
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
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
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
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
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
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

  it('mục Tài khoản hiển thị tên tài khoản/email (mục mặc định)', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
    });

    renderTrangCaiDat();

    expect(await screen.findByText('Tên tài khoản:')).toBeInTheDocument();
    expect(screen.getByText('Email:')).toBeInTheDocument();
  });

  it('chuyển sang mục Bảo mật hiển thị nội dung "sắp ra mắt"', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
    });

    renderTrangCaiDat();

    await userEvent.click(screen.getByRole('button', { name: 'Bảo mật' }));
    expect(await screen.findByText(/sắp ra mắt/)).toBeInTheDocument();
  });

  it('chuyển sang mục Thông báo hiển thị 3 công tắc thông báo', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
    });

    renderTrangCaiDat();

    await userEvent.click(screen.getByRole('button', { name: 'Thông báo' }));
    const congTac = await screen.findAllByRole('switch');
    expect(congTac).toHaveLength(3);
  });

  it('chuyển sang mục Giao diện và chọn Tối gọi apDungGiaoDien', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
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
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
    });
    vi.spyOn(DichVuApi, 'DoiMatKhau').mockResolvedValue({ thongBao: 'Đã đổi mật khẩu thành công.' });

    renderTrangCaiDat();
    await screen.findByPlaceholderText('Mật khẩu hiện tại');

    fireEvent.change(screen.getByPlaceholderText('Mật khẩu hiện tại'), { target: { value: 'Cu123456' } });
    fireEvent.change(screen.getByPlaceholderText('Mật khẩu mới'), { target: { value: 'Moi123456' } });
    fireEvent.change(screen.getByPlaceholderText('Xác nhận mật khẩu mới'), { target: { value: 'Moi123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Đổi mật khẩu' }));

    await waitFor(() => expect(screen.getByText('Đã đổi mật khẩu thành công.')).toBeInTheDocument());
  });

  it('xac nhan mat khau moi khong khop hien loi, khong goi API', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'A', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false, hienThiTrangThaiHoatDong: true,
      choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true, thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'A',
    });
    const doiMatKhauSpy = vi.spyOn(DichVuApi, 'DoiMatKhau');

    renderTrangCaiDat();
    await screen.findByPlaceholderText('Mật khẩu hiện tại');

    fireEvent.change(screen.getByPlaceholderText('Mật khẩu hiện tại'), { target: { value: 'Cu123456' } });
    fireEvent.change(screen.getByPlaceholderText('Mật khẩu mới'), { target: { value: 'Moi123456' } });
    fireEvent.change(screen.getByPlaceholderText('Xác nhận mật khẩu mới'), { target: { value: 'Khac123456' } });
    fireEvent.click(screen.getByRole('button', { name: 'Đổi mật khẩu' }));

    expect(screen.getByText('Xác nhận mật khẩu mới không khớp.')).toBeInTheDocument();
    expect(doiMatKhauSpy).not.toHaveBeenCalled();
  });

  it('doi ten hien thi thanh cong cap nhat lai input va thong bao', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Cũ',
    });
    const doiTenSpy = vi.spyOn(DichVuApi, 'DoiTenHienThi').mockResolvedValue({
      id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Mới',
    });

    renderTrangCaiDat();
    const oNhap = await screen.findByDisplayValue('Tên Cũ');
    fireEvent.change(oNhap, { target: { value: 'Tên Mới' } });
    fireEvent.click(screen.getByRole('button', { name: /lưu thay đổi/i }));

    expect(await screen.findByText(/đã lưu tên hiển thị/i)).toBeInTheDocument();
    expect(doiTenSpy).toHaveBeenCalledWith('token-gia-lap', 'Tên Mới');
  });

  it('nut luu ten hien thi bi disable khi rong hoac khong doi', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Cũ',
    });

    renderTrangCaiDat();
    const oNhap = await screen.findByDisplayValue('Tên Cũ');
    const nutLuu = screen.getByRole('button', { name: /lưu thay đổi/i });
    expect(nutLuu).toBeDisabled();

    fireEvent.change(oNhap, { target: { value: '   ' } });
    expect(nutLuu).toBeDisabled();

    fireEvent.change(oNhap, { target: { value: 'Tên Cũ' } });
    expect(nutLuu).toBeDisabled();
  });

  it('khong con nut Dang xuat trong muc Tai khoan', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'nguoia', email: 'a@vi.du', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'Tên Cũ',
    });

    renderTrangCaiDat();
    await screen.findByDisplayValue('Tên Cũ');

    expect(screen.queryByRole('button', { name: /đăng xuất/i })).not.toBeInTheDocument();
  });

  it('doi anh dai dien ca nhan luu ngay khi chon file', async () => {
    vi.spyOn(DichVuApi, 'LayThongTinCaNhan').mockResolvedValue({
      id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', duongDanAnhDaiDien: null,
    });
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/xyz789', tenFileGoc: 'a.png', kichThuocFile: 100, loaiFile: 'image/png' });
    vi.spyOn(DichVuApi, 'DoiAnhDaiDien').mockResolvedValue({
      id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: false,
      hienThiTrangThaiHoatDong: true, choPhepThemVaoNhom: true, thongBaoTinNhanMoi: true,
      thongBaoLoiMoiKetBan: true, thongBaoNhom: true, tenHienThi: 'NguyenAn', duongDanAnhDaiDien: '/api/tinnhan/file/xyz789',
    });
    renderTrangCaiDat();
    await screen.findByText('Tên hiển thị');
    const tep = new File(['noi-dung'], 'a.png', { type: 'image/png' });

    await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện'), tep);

    await waitFor(() => expect(DichVuApi.DoiAnhDaiDien).toHaveBeenCalledWith('token-gia-lap', '/api/tinnhan/file/xyz789'));
  });

  it('chon file khong phai anh thi bao loi, khong goi TaiLenTep', async () => {
    renderTrangCaiDat();
    await screen.findByText('Tên hiển thị');
    const taiLenTep = vi.spyOn(DichVuApi, 'TaiLenTep');
    const tep = new File(['noi-dung'], 'a.pdf', { type: 'application/pdf' });
    // Input có `accept="image/*..."` nên userEvent mặc định tự lọc bỏ file
    // không khớp trước khi bắn sự kiện change (giống hành vi trình duyệt
    // thật). Ở đây ta chủ động tắt `applyAccept` để mô phỏng trường hợp
    // người dùng vẫn chọn được file sai định dạng (vd: kéo-thả, hoặc trình
    // duyệt/OS không lọc accept), qua đó kiểm tra được lớp validate JS.
    const nguoiDung = userEvent.setup({ applyAccept: false });

    await nguoiDung.upload(screen.getByLabelText('Đổi ảnh đại diện'), tep);

    expect(await screen.findByText('Chỉ chấp nhận file ảnh.')).toBeInTheDocument();
    expect(taiLenTep).not.toHaveBeenCalled();
  });
});
