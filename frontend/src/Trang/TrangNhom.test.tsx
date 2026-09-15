import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TrangNhom } from './TrangNhom';
import { NhaCungCapXacThuc } from '../NguCanh/NguCanhXacThuc';
import { NhaCungCapChat } from '../NguCanh/NguCanhChat';
import * as DichVuApi from '../DichVuApi';

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
      loaiFile: null, daDoc: false, daNhan: false, thoiGianTao: '2026-01-01T00:00:00Z',
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
    await userEvent.type(screen.getByPlaceholderText('Nhập tên nhóm...'), 'Nhóm mới');
    await userEvent.click(screen.getByText('TranBinh'));
    await userEvent.click(screen.getByRole('button', { name: 'Tạo nhóm' }));

    await waitFor(() => expect(DichVuApi.TaoNhom).toHaveBeenCalledWith('token-gia-lap', 'Nhóm mới', null, null, ['2']));
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
});
