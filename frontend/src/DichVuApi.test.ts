import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
  DangKy, DangNhap, LayDanhSachNguoiDung, LayLichSuTinNhan, TaiLenTep, LoiGoiApi,
  GuiLoiMoiKetBan, ChapNhanLoiMoiKetBan, LayBanBe, LayLoiMoiDen, CapNhatCaiDat, LayDanhSachHoiThoai,
} from './DichVuApi';

describe('DichVuApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('DangKy gửi đúng request và trả về kết quả khi thành công', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ thongBao: 'Đăng ký thành công.' }), { status: 200 }),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const ketQua = await DangKy('NguyenAn', 'nguyenan@gmail.com', 'MatKhau123');

    expect(ketQua.thongBao).toBe('Đăng ký thành công.');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung/dang-ky'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ tenTaiKhoan: 'NguyenAn', email: 'nguyenan@gmail.com', matKhau: 'MatKhau123' }),
      }),
    );
  });

  it('DangKy ném LoiGoiApi kèm thongBao khi trùng tên tài khoản (409)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ thongBao: 'Tên tài khoản đã tồn tại.' }), { status: 409 }),
      ),
    );

    await expect(DangKy('NguyenAn', 'a@gmail.com', 'x')).rejects.toMatchObject({
      trangThai: 409,
      message: 'Tên tài khoản đã tồn tại.',
    });
  });

  it('DangNhap trả về token khi đăng nhập đúng', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ token: 'abc.def.ghi' }), { status: 200 })),
    );

    const ketQua = await DangNhap('NguyenAn', 'MatKhau123');

    expect(ketQua.token).toBe('abc.def.ghi');
  });

  it('DangNhap ném LoiGoiApi khi sai mật khẩu (401)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ thongBao: 'Sai tên đăng nhập hoặc mật khẩu.' }), { status: 401 }),
      ),
    );

    await expect(DangNhap('NguyenAn', 'Sai')).rejects.toMatchObject({ trangThai: 401 });
  });

  it('LayDanhSachNguoiDung gửi kèm Bearer token và trả về danh sách', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(JSON.stringify([{ id: '1', tenTaiKhoan: 'TranBinh', email: 'b@gmail.com' }]), { status: 200 }),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const danhSach = await LayDanhSachNguoiDung('token-gia-lap');

    expect(danhSach).toHaveLength(1);
    expect(danhSach[0].tenTaiKhoan).toBe('TranBinh');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung'),
      expect.objectContaining({ headers: { Authorization: 'Bearer token-gia-lap' } }),
    );
  });

  it('LayDanhSachNguoiDung ném LoiGoiApi khi không có token hợp lệ (401)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 401 })));

    await expect(LayDanhSachNguoiDung('token-sai')).rejects.toMatchObject({ trangThai: 401 });
  });

  it('ném LoiGoiApi với thông báo kết nối khi fetch thất bại (mất mạng/CORS)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    const loi = await DangNhap('NguyenAn', 'MatKhau123').catch((e) => e);

    expect(loi).toBeInstanceOf(LoiGoiApi);
    expect(loi).toMatchObject({
      trangThai: 0,
      message: 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra backend đang chạy.',
    });
  });

  it('DangKy ném LoiGoiApi với thông báo cụ thể khi backend trả lỗi validation dạng ValidationProblemDetails (400)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { MatKhau: ['Mật khẩu phải có ít nhất 6 ký tự.'] },
          }),
          { status: 400 },
        ),
      ),
    );

    await expect(DangKy('ann', 'ann@gmail.com', 'abc')).rejects.toMatchObject({
      trangThai: 400,
      message: 'Mật khẩu phải có ít nhất 6 ký tự.',
    });
  });

  it('ném LoiGoiApi với thông báo mặc định khi phản hồi không phải JSON hợp lệ', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response('<!DOCTYPE html><html><body>Lỗi máy chủ</body></html>', { status: 500 })),
    );

    const loi = await DangNhap('NguyenAn', 'MatKhau123').catch((e) => e);

    expect(loi).toBeInstanceOf(LoiGoiApi);
    expect(loi).toMatchObject({
      trangThai: 500,
      message: 'Đã có lỗi xảy ra, vui lòng thử lại.',
    });
  });

  it('LayLichSuTinNhan gửi kèm Bearer token và query đúng', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 }));
    vi.stubGlobal('fetch', fetchGiaLap);

    await LayLichSuTinNhan('token-gia-lap', 'nguoi-kia-id', 'truoc-id', 10);

    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/tinnhan/nguoi-dung/nguoi-kia-id?soLuong=10&truoc=truoc-id'),
      expect.objectContaining({ headers: { Authorization: 'Bearer token-gia-lap' } }),
    );
  });

  it('TaiLenTep gửi FormData và trả về metadata khi thành công', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({ duongDanFile: '/uploads/x.png', tenFileGoc: 'x.png', kichThuocFile: 10, loaiFile: 'image/png' }),
        { status: 200 },
      ),
    );
    vi.stubGlobal('fetch', fetchGiaLap);
    const tep = new File(['abc'], 'x.png', { type: 'image/png' });

    const ketQua = await TaiLenTep('token-gia-lap', tep);

    expect(ketQua.duongDanFile).toBe('/uploads/x.png');
    const [, tuyChon] = fetchGiaLap.mock.calls[0];
    expect(tuyChon.body).toBeInstanceOf(FormData);
  });

  it('TaiLenTep ném LoiGoiApi khi file bị từ chối (400)', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify({ thongBao: 'Định dạng file không được hỗ trợ.' }), { status: 400 })),
    );
    const tep = new File(['abc'], 'x.exe', { type: 'application/octet-stream' });

    await expect(TaiLenTep('token-gia-lap', tep)).rejects.toMatchObject({
      trangThai: 400,
      message: 'Định dạng file không được hỗ trợ.',
    });
  });

  it('GuiLoiMoiKetBan gửi đúng POST và trả về lời mời', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: '1',
          nguoiGui: { id: 'a', tenTaiKhoan: 'A', email: 'a@gmail.com' },
          nguoiNhan: { id: 'b', tenTaiKhoan: 'B', email: 'b@gmail.com' },
          trangThai: 'ChoDuyet',
          thoiGianTao: '2026-01-01T00:00:00Z',
        }),
        { status: 200 },
      ),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    const ketQua = await GuiLoiMoiKetBan('token-gia-lap', 'b');

    expect(ketQua.trangThai).toBe('ChoDuyet');
    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/ketban/loi-moi/b'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('ChapNhanLoiMoiKetBan gửi đúng POST theo id', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: '1',
          nguoiGui: { id: 'a', tenTaiKhoan: 'A', email: 'a@gmail.com' },
          nguoiNhan: { id: 'b', tenTaiKhoan: 'B', email: 'b@gmail.com' },
          trangThai: 'DaChapNhan',
          thoiGianTao: '2026-01-01T00:00:00Z',
        }),
        { status: 200 },
      ),
    );
    vi.stubGlobal('fetch', fetchGiaLap);

    await ChapNhanLoiMoiKetBan('token-gia-lap', '1');

    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/ketban/1/chap-nhan'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('LayBanBe trả về danh sách bạn bè', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(JSON.stringify([{ id: 'b', tenTaiKhoan: 'B', email: 'b@gmail.com' }]), { status: 200 })),
    );

    const danhSach = await LayBanBe('token-gia-lap');

    expect(danhSach).toHaveLength(1);
  });

  it('LayLoiMoiDen trả về danh sách lời mời đến', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

    const danhSach = await LayLoiMoiDen('token-gia-lap');

    expect(danhSach).toEqual([]);
  });

  it('CapNhatCaiDat gửi đúng PUT với body choPhepTinNhanTuNguoiLa', async () => {
    const fetchGiaLap = vi.fn().mockResolvedValue(new Response(JSON.stringify({ thongBao: 'OK' }), { status: 200 }));
    vi.stubGlobal('fetch', fetchGiaLap);

    await CapNhatCaiDat('token-gia-lap', true);

    expect(fetchGiaLap).toHaveBeenCalledWith(
      expect.stringContaining('/nguoidung/cai-dat'),
      expect.objectContaining({ method: 'PUT', body: JSON.stringify({ choPhepTinNhanTuNguoiLa: true }) }),
    );
  });

  it('LayDanhSachHoiThoai trả về danh sách hội thoại', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([]), { status: 200 })));

    const danhSach = await LayDanhSachHoiThoai('token-gia-lap');

    expect(danhSach).toEqual([]);
  });
});
