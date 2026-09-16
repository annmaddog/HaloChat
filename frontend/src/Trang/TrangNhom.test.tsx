import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangNhom } from './TrangNhom';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';
import type { CamXuc } from '../KieuDuLieu';

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

function renderTrangNhom() {
  return render(
    <MemoryRouter initialEntries={['/nhom']}>
      <NhaCungCapXacThuc>
        <NhaCungCapChat>
          <TrangNhom />
        </NhaCungCapChat>
      </NhaCungCapXacThuc>
    </MemoryRouter>,
  );
}

describe('TrangNhom', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.clearAllMocks();
    ketNoiGiaLap.invoke.mockResolvedValue(undefined);
    localStorage.setItem('haloChatToken', 'token-gia-lap');
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([{ id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'TranBinh' }]);
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([{ id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'TranBinh' }]);
  });

  it('hiển thị danh sách nhóm đã tham gia', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);

    renderTrangNhom();

    expect(await screen.findByText('Nhóm CNTT')).toBeInTheDocument();
  });

  it('chọn 1 nhóm tải lịch sử và hiển thị tin nhắn', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([{
      id: 'm1', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Text',
      noiDungTinNhan: 'Chào nhóm', duongDanFile: null, tenFileGoc: null, kichThuocFile: null,
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    }]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));

    expect(await screen.findByText('Chào nhóm')).toBeInTheDocument();
    expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('DanhDauDaDoc', null, 'n1');
  });

  it('ô tìm kiếm lọc đúng danh sách nhóm theo tên', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
      { id: 'n2', tenNhom: 'Nhóm Toán', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);

    renderTrangNhom();
    await screen.findByText('Nhóm CNTT');
    expect(screen.getByText('Nhóm Toán')).toBeInTheDocument();

    await userEvent.type(screen.getByPlaceholderText('Tìm nhóm...'), 'Toán');

    expect(screen.getByText('Nhóm Toán')).toBeInTheDocument();
    expect(screen.queryByText('Nhóm CNTT')).not.toBeInTheDocument();
  });

  it('class "trang-nhom--da-chon" chỉ xuất hiện trên phần tử gốc sau khi đã chọn nhóm', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    const { container } = renderTrangNhom();
    await screen.findByText('Nhóm CNTT');
    expect(container.querySelector('.trang-nhom')).not.toHaveClass('trang-nhom--da-chon');

    await userEvent.click(screen.getByText('Nhóm CNTT'));

    expect(container.querySelector('.trang-nhom')).toHaveClass('trang-nhom--da-chon');
  });

  it('tạo nhóm mới gọi TaoNhom với đúng tên và thành viên đã chọn', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TaoNhom').mockResolvedValue({
      id: 'n2', tenNhom: 'Nhóm mới', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z',
    });

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));
    await userEvent.type(screen.getByPlaceholderText('Nhập tên nhóm trò chuyện...'), 'Nhóm mới');
    await userEvent.click(screen.getByText('TranBinh'));
    await userEvent.click(screen.getByRole('button', { name: 'Tạo nhóm' }));

    await waitFor(() => expect(DichVuApi.TaoNhom).toHaveBeenCalledWith('token-gia-lap', 'Nhóm mới', null, null, ['2']));
  });

  it('modal tao nhom chi hien ban be, khong hien tat ca nguoi dung', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayDanhSachNguoiDung').mockResolvedValue([
      { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'TranBinh' },
      { id: '3', tenTaiKhoan: 'LeCam', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'LeCam' },
    ]);
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([
      { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'TranBinh' },
    ]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));

    expect(await screen.findByText('TranBinh')).toBeInTheDocument();
    expect(screen.queryByText('LeCam')).not.toBeInTheDocument();
  });

  it('modal tao nhom hien trang thai trong khi chua co ban be nao', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));

    expect(await screen.findByText(/Bạn chưa có bạn bè nào/)).toBeInTheDocument();
  });

  it('badge "da chon" cap nhat dung so luong khi tick thanh vien', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([
      { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'TranBinh' },
      { id: '3', tenTaiKhoan: 'LeCam', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'LeCam' },
    ]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));
    expect(screen.getByText('0 đã chọn')).toBeInTheDocument();

    await userEvent.click(await screen.findByText('TranBinh'));

    expect(screen.getByText('1 đã chọn')).toBeInTheDocument();
  });

  it('o tim kiem thanh vien trong modal loc dung theo tenHienThi', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([
      { id: '2', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'TranBinh' },
      { id: '3', tenTaiKhoan: 'LeCam', email: 'c@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'LeCam' },
    ]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));
    await screen.findByText('TranBinh');

    await userEvent.type(screen.getByPlaceholderText('Tìm bạn bè...'), 'Cam');

    expect(screen.getByText('LeCam')).toBeInTheDocument();
    expect(screen.queryByText('TranBinh')).not.toBeInTheDocument();
  });

  it('chon anh dai dien nhom trong modal tao goi TaiLenTep va gui kem duong dan khi tao nhom', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayBanBe').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({
      duongDanFile: '/uploads/nhom-moi.png', tenFileGoc: 'nhom.png', kichThuocFile: 1000, loaiFile: 'image/png',
    });
    vi.spyOn(DichVuApi, 'TaoNhom').mockResolvedValue({
      id: 'n3', tenNhom: 'Nhóm ảnh', moTa: null, duongDanAnhDaiDien: '/uploads/nhom-moi.png', nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z',
    });

    renderTrangNhom();
    await userEvent.click(await screen.findByText('+ Tạo nhóm'));
    await userEvent.type(screen.getByPlaceholderText('Nhập tên nhóm trò chuyện...'), 'Nhóm ảnh');

    const tep = new File(['noi-dung'], 'nhom.png', { type: 'image/png' });
    const oChonTep = document.querySelector('.trang-nhom__modal input[type="file"]') as HTMLInputElement;
    await userEvent.upload(oChonTep, tep);

    await waitFor(() => expect(DichVuApi.TaiLenTep).toHaveBeenCalledWith('token-gia-lap', tep));
    await userEvent.click(screen.getByRole('button', { name: 'Tạo nhóm' }));

    await waitFor(() => expect(DichVuApi.TaoNhom).toHaveBeenCalledWith('token-gia-lap', 'Nhóm ảnh', null, '/uploads/nhom-moi.png', []));
  });

  it('bam vao tieu de header mo PanelThongTinNhom', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [
        { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'NguyenAn' },
      ], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    const { container } = renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(container.querySelector('.khung-tin-nhan__tieu-de-bam') as Element);

    expect(container.querySelector('.panel-thong-tin-nhom')).toHaveTextContent('1 thành viên');
  });

  it('la admin (nguoiTaoId trung idHienTai): thay Chinh sua va Quan ly nhom trong PanelThongTinNhom', async () => {
    const phanThanToken = btoa(JSON.stringify({ sub: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${phanThanToken}.chuky`);
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [
        { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'NguyenAn' },
      ], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    const { container } = renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(container.querySelector('.khung-tin-nhan__tieu-de-bam') as Element);

    expect(await screen.findByRole('button', { name: 'Chỉnh sửa' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Quản lý nhóm' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Giải tán nhóm' })).toBeInTheDocument();
  });

  it('bam Chinh sua chuyen sang PanelQuanLyNhom', async () => {
    const phanThanToken = btoa(JSON.stringify({ sub: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    localStorage.setItem('haloChatToken', `header.${phanThanToken}.chuky`);
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [
        { id: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com', choPhepTinNhanTuNguoiLa: true, tenHienThi: 'NguyenAn' },
      ], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    const { container } = renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(container.querySelector('.khung-tin-nhan__tieu-de-bam') as Element);
    await userEvent.click(await screen.findByRole('button', { name: 'Chỉnh sửa' }));

    expect(await screen.findByText('Quản lý nhóm')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Nhóm CNTT')).toBeInTheDocument();
  });

  it('doi nhom dang chon thi dong panel dang mo', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
      { id: 'n2', tenNhom: 'Nhóm Toán', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);

    const { container } = renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(container.querySelector('.khung-tin-nhan__tieu-de-bam') as Element);
    await waitFor(() => expect(container.querySelector('.panel-thong-tin-nhom')).toBeInTheDocument());

    await userEvent.click(screen.getByText('Nhóm Toán'));

    expect(container.querySelector('.panel-thong-tin-nhom')).not.toBeInTheDocument();
  });

  it('bam icon kho media goi LayMediaTheoNhom va hien panel', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'LayMediaTheoNhom').mockResolvedValue([{
      id: 'm-anh', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Anh',
      noiDungTinNhan: '', duongDanFile: '/api/tinnhan/file/507f1f77bcf86cd799439001', tenFileGoc: 'a.png',
      kichThuocFile: 1024, loaiFile: 'image/png', daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z',
      traLoi: null, daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    }]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(screen.getByRole('button', { name: 'Kho lưu trữ Media & Tệp' }));

    expect(await screen.findByText('Kho lưu trữ Media & Tệp')).toBeInTheDocument();
    expect(screen.getByText('Hình ảnh (1)')).toBeInTheDocument();
    expect(DichVuApi.LayMediaTheoNhom).toHaveBeenCalledWith('token-gia-lap', 'n1');
  });

  it('go tim kiem tin nhan goi dung TimKiemTinNhanTheoNhom', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TimKiemTinNhanTheoNhom').mockResolvedValue([]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
    await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin chao');

    await waitFor(() => expect(DichVuApi.TimKiemTinNhanTheoNhom).toHaveBeenCalledWith('token-gia-lap', 'n1', 'xin chao'));
  });

  it('bam ket qua tim kiem chua tai ve thi tu dong tai them lich su toi khi thay', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    const tinCu = {
      id: 'm-cu-nhat', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Text' as const,
      noiDungTinNhan: 'Tin dau tien', duongDanFile: null, tenFileGoc: null, kichThuocFile: null,
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [] as CamXuc[],
    };
    const tinXa = {
      id: 'm-xa-nhat', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Text' as const,
      noiDungTinNhan: 'Xin chao rat xa', duongDanFile: null, tenFileGoc: null, kichThuocFile: null,
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [] as CamXuc[],
    };
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValueOnce([tinCu]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValueOnce([tinXa]);
    vi.spyOn(DichVuApi, 'TimKiemTinNhanTheoNhom').mockResolvedValue([tinXa]);

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await screen.findByText('Tin dau tien');
    await userEvent.click(screen.getByRole('button', { name: 'Tìm tin nhắn' }));
    await userEvent.type(screen.getByPlaceholderText('Tìm tin nhắn...'), 'xin chao');
    const ketQua = await screen.findByTestId('ket-qua-tim-m-xa-nhat');

    await userEvent.click(ketQua);

    expect(await screen.findAllByText('Xin chao rat xa')).not.toHaveLength(0);
  });

  it('bam nhanh nut like goi ThaCamXucTinNhan qua hub', async () => {
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([{
      id: 'm1', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Text',
      noiDungTinNhan: 'Chào nhóm', duongDanFile: null, tenFileGoc: null, kichThuocFile: null,
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null, danhSachCamXuc: [],
    }]);
    ketNoiGiaLap.invoke.mockResolvedValue({
      id: 'm1', nguoiGuiId: '2', nguoiNhanId: null, nhomId: 'n1', loaiTinNhan: 'Text',
      noiDungTinNhan: 'Chào nhóm', duongDanFile: null, tenFileGoc: null, kichThuocFile: null,
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z', traLoi: null,
      daThuHoi: false, daGhim: false, thoiGianGhim: null,
      danhSachCamXuc: [{ nguoiDungId: '1', loaiCamXuc: 'Thich' }] as CamXuc[],
    });

    renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(await screen.findByRole('button', { name: 'Thích tin nhắn này' }));

    await waitFor(() => expect(ketNoiGiaLap.invoke).toHaveBeenCalledWith('ThaCamXucTinNhan', 'm1', 'Thich'));
  });

  it('doi anh dai dien nhom tu Thong tin nhom cap nhat dung state', async () => {
    const phanThanToken = btoa(JSON.stringify({ sub: '1', tenTaiKhoan: 'NguyenAn', email: 'a@gmail.com' }));
    const tokenGiaLap = `header.${phanThanToken}.chuky`;
    localStorage.setItem('haloChatToken', tokenGiaLap);
    vi.spyOn(DichVuApi, 'LayDanhSachNhom').mockResolvedValue([
      { id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: null, nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z' },
    ]);
    vi.spyOn(DichVuApi, 'LayLichSuNhom').mockResolvedValue([]);
    vi.spyOn(DichVuApi, 'TaiLenTep').mockResolvedValue({ duongDanFile: '/api/tinnhan/file/nhom456', tenFileGoc: 'a.png', kichThuocFile: 100, loaiFile: 'image/png' });
    vi.spyOn(DichVuApi, 'CapNhatNhom').mockResolvedValue({
      id: 'n1', tenNhom: 'Nhóm CNTT', moTa: null, duongDanAnhDaiDien: '/api/tinnhan/file/nhom456', nguoiTaoId: '1', thanhVien: [], thoiGianTao: '2026-01-01T00:00:00Z',
    });

    const { container } = renderTrangNhom();
    await userEvent.click(await screen.findByText('Nhóm CNTT'));
    await userEvent.click(container.querySelector('.khung-tin-nhan__tieu-de-bam') as Element);
    const tep = new File(['noi-dung'], 'a.png', { type: 'image/png' });

    await userEvent.upload(screen.getByLabelText('Đổi ảnh đại diện nhóm'), tep);

    await waitFor(() => expect(DichVuApi.CapNhatNhom).toHaveBeenCalledWith(tokenGiaLap, 'n1', 'Nhóm CNTT', null, '/api/tinnhan/file/nhom456'));
  });
});
