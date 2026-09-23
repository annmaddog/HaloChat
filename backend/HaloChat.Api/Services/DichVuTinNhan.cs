using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
using HaloChat.Security;
using MongoDB.Bson;

namespace HaloChat.Api.Services;

public class DichVuTinNhan : IDichVuTinNhan
{
    // [Sửa lỗi] File đính kèm giờ lưu qua MongoDB GridFS
    // (Services/DichVuLuuTruFileGridFs.cs), trả về đường dẫn dạng
    // /api/tinnhan/file/<ObjectId 24 ký tự hex> — KHÔNG còn dạng
    // /uploads/<guid>.<ext> cũ (ổ đĩa container, bị Render xóa sạch mỗi lần
    // deploy). Quên cập nhật pattern này khi đổi nơi lưu file là nguyên nhân
    // khiến GuiTinNhan luôn báo "Đường dẫn file không hợp lệ" dù upload đã
    // thành công.
    private static readonly System.Text.RegularExpressions.Regex MauDuongDanFileHopLe = new(
        @"^/api/tinnhan/file/[0-9a-fA-F]{24}$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private readonly ITinNhanRepository _khoTinNhan;
    private readonly INguoiDungRepository _khoNguoiDung;
    private readonly ILoiMoiKetBanRepository _khoLoiMoiKetBan;
    private readonly INhomRepository _khoNhom;
    private readonly IDocNhomRepository _khoDocNhom;
    private readonly IQuanLyKetNoiChat _quanLyKetNoi;
    private readonly ITinNhanAnRepository _khoTinNhanAn;
    private readonly IDichVuMaHoa _dichVuMaHoa;

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IDocNhomRepository khoDocNhom, IQuanLyKetNoiChat quanLyKetNoi,
        ITinNhanAnRepository khoTinNhanAn, IDichVuMaHoa dichVuMaHoa)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _khoDocNhom = khoDocNhom;
        _quanLyKetNoi = quanLyKetNoi;
        _khoTinNhanAn = khoTinNhanAn;
        _dichVuMaHoa = dichVuMaHoa;
    }

    public async Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile, string? traLoiId)
    {
        // Chuẩn hóa chuỗi rỗng thành null trước khi kiểm tra, để bảo đảm điều
        // kiện XOR ở đây và nhánh rẽ "nhomId is not null" bên dưới luôn đồng
        // nhất về khái niệm "đã chỉ định" — tránh trường hợp nhomId = ""
        // (không phải null) lọt qua XOR rồi bị định tuyến nhầm sang nhánh nhóm.
        nguoiNhanId = string.IsNullOrEmpty(nguoiNhanId) ? null : nguoiNhanId;
        nhomId = string.IsNullOrEmpty(nhomId) ? null : nhomId;

        if ((nguoiNhanId is null) == (nhomId is null))
        {
            throw new TinNhanKhongHopLeException("Phải chỉ định đúng 1 trong 2: người nhận hoặc nhóm.");
        }

        if (!Enum.TryParse<LoaiTinNhan>(loaiTinNhan, ignoreCase: true, out var loai))
        {
            throw new TinNhanKhongHopLeException($"Loại tin nhắn không hợp lệ: {loaiTinNhan}.");
        }

        if (loai == LoaiTinNhan.Text && string.IsNullOrWhiteSpace(noiDungTinNhan))
        {
            throw new TinNhanKhongHopLeException("Nội dung tin nhắn không được để trống.");
        }

        if (loai != LoaiTinNhan.Text)
        {
            if (string.IsNullOrWhiteSpace(duongDanFile) || !MauDuongDanFileHopLe.IsMatch(duongDanFile))
            {
                throw new TinNhanKhongHopLeException("Đường dẫn file không hợp lệ.");
            }

            if (kichThuocFile is < 0)
            {
                throw new TinNhanKhongHopLeException("Kích thước file không hợp lệ.");
            }
        }

        var tinNhan = new TinNhan
        {
            NguoiGuiId = nguoiGuiId,
            LoaiTinNhan = loai,
            NoiDungTinNhan = noiDungTinNhan ?? string.Empty,
            DuongDanFile = duongDanFile,
            TenFileGoc = tenFileGoc,
            KichThuocFile = kichThuocFile,
            LoaiFile = loaiFile,
        };

        // [GĐ6] Cần biết chính người gửi (không chỉ id) để: (a) đưa vào danh
        // sách người tham gia được mã hóa khóa phiên riêng — nếu không, chính
        // người gửi sẽ không đọc lại được tin mình vừa gửi; (b) giải mã tin
        // gốc khi tin này là 1 câu trả lời (xem đoạn TraLoi bên dưới).
        var nguoiGui = await _khoNguoiDung.TimTheoIdAsync(nguoiGuiId);
        List<NguoiDung> nguoiThamGia;
        int soNguoiThamGiaDuKien;

        if (nhomId is not null)
        {
            if (!ObjectId.TryParse(nhomId, out _))
            {
                throw new NhomKhongTonTaiException();
            }

            var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
            if (!nhom.ThanhVienIds.Contains(nguoiGuiId))
            {
                throw new KhongPhaiThanhVienNhomException();
            }

            tinNhan.NhomId = nhomId;

            nguoiThamGia = new List<NguoiDung>();
            foreach (var idThanhVien in nhom.ThanhVienIds)
            {
                var thanhVien = await _khoNguoiDung.TimTheoIdAsync(idThanhVien);
                if (thanhVien is not null)
                {
                    nguoiThamGia.Add(thanhVien);
                }
            }

            // [GĐ6] Số lượng THÀNH VIÊN DUY NHẤT kỳ vọng trong nhóm — nếu
            // nguoiThamGia.Count nhỏ hơn (do 1 id không tra được NguoiDung),
            // đây là tín hiệu "thiếu 1 người tham gia", phải chặn mã hóa
            // ngay tại MaHoaNoiDungNeuCoThe (Distinct vì ThanhVienIds về lý
            // thuyết không nên trùng, nhưng phòng hờ dữ liệu bất thường).
            soNguoiThamGiaDuKien = nhom.ThanhVienIds.Distinct().Count();
        }
        else
        {
            if (!ObjectId.TryParse(nguoiNhanId, out _))
            {
                throw new NguoiNhanKhongTonTaiException(nguoiNhanId!);
            }

            var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(nguoiNhanId!);
            if (nguoiNhan is null)
            {
                throw new NguoiNhanKhongTonTaiException(nguoiNhanId!);
            }

            if (nguoiGuiId != nguoiNhanId)
            {
                var laBanBe = await _khoLoiMoiKetBan.LaBanBeAsync(nguoiGuiId, nguoiNhanId!);
                if (!laBanBe && !nguoiNhan.ChoPhepTinNhanTuNguoiLa)
                {
                    throw new TinNhanKhongHopLeException("Người này chỉ nhận tin nhắn từ bạn bè. Hãy gửi lời mời kết bạn trước.");
                }
            }

            tinNhan.NguoiNhanId = nguoiNhanId;
            tinNhan.DaNhan = _quanLyKetNoi.DangOnline(nguoiNhanId!);

            nguoiThamGia = new List<NguoiDung>();
            if (nguoiGui is not null)
            {
                nguoiThamGia.Add(nguoiGui);
            }

            if (nguoiNhan.Id != nguoiGui?.Id)
            {
                nguoiThamGia.Add(nguoiNhan);
            }

            soNguoiThamGiaDuKien = nguoiGuiId == nguoiNhanId ? 1 : 2;
        }

        traLoiId = string.IsNullOrEmpty(traLoiId) ? null : traLoiId;
        if (traLoiId is not null)
        {
            if (!ObjectId.TryParse(traLoiId, out _))
            {
                throw new TinNhanKhongHopLeException("Tin nhắn được trả lời không hợp lệ.");
            }

            var tinGoc = await _khoTinNhan.TimTheoIdAsync(traLoiId)
                ?? throw new TinNhanKhongHopLeException("Tin nhắn được trả lời không tồn tại.");

            var thamGiaTinGoc = new HashSet<string?> { tinGoc.NguoiGuiId, tinGoc.NguoiNhanId };
            var thamGiaTinMoi = new HashSet<string?> { nguoiGuiId, nguoiNhanId };
            var cungHoiThoai = nhomId is not null
                ? tinGoc.NhomId == nhomId
                : tinGoc.NhomId is null && thamGiaTinGoc.SetEquals(thamGiaTinMoi);
            if (!cungHoiThoai)
            {
                throw new TinNhanKhongHopLeException("Tin nhắn được trả lời không thuộc cuộc trò chuyện này.");
            }

            var nguoiGuiGoc = await _khoNguoiDung.TimTheoIdAsync(tinGoc.NguoiGuiId);
            var tenNguoiGuiGoc = nguoiGuiGoc?.TenHienThiThucTe() ?? "Người dùng đã xoá";
            // [GĐ6] tinGoc.NoiDungTinNhan có thể là bản mã (đã mã hóa) —
            // giải mã lại bằng khóa của CHÍNH người đang trả lời (nguoiGuiId
            // của tin MỚI này), vì họ chắc chắn là 1 bên tham gia hội thoại
            // chứa tinGoc nên luôn có 1 bản khóa phiên dành riêng cho mình.
            var noiDungGocThucTe = GiaiMaNoiDungThucTe(tinGoc, nguoiGuiId, nguoiGui?.KhoaBiMat);
            var noiDungTomTat = tinGoc.LoaiTinNhan switch
            {
                LoaiTinNhan.Text => noiDungGocThucTe.Length > 80
                    ? noiDungGocThucTe[..80] + "…"
                    : noiDungGocThucTe,
                LoaiTinNhan.Anh => "[Ảnh]",
                _ => $"[File] {tinGoc.TenFileGoc}",
            };

            tinNhan.TraLoi = new TraLoiThongTin
            {
                Id = tinGoc.Id,
                TenNguoiGui = tenNguoiGuiGoc,
                NoiDungTomTat = noiDungTomTat,
                LoaiTinNhan = tinGoc.LoaiTinNhan,
            };
        }

        MaHoaNoiDungNeuCoThe(tinNhan, tinNhan.NoiDungTinNhan, nguoiThamGia, nguoiGui, soNguoiThamGiaDuKien);

        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return AnhXaDto(tinNhan, nguoiGuiId, nguoiGui?.KhoaBiMat);
    }

    /// <summary>
    /// [GĐ6] Mã hóa NỘI DUNG THẬT (đã lưu ở tinNhan.NoiDungTinNhan trước khi
    /// gọi hàm này) bằng AES-256-GCM, rồi mã hóa khóa AES đó riêng cho từng
    /// người trong nguoiThamGia bằng RSA-OAEP — CHỈ khi TẤT CẢ đều có sẵn
    /// Public Key hợp lệ. Nếu bất kỳ ai thiếu khóa (tài khoản tạo trước GĐ6,
    /// dữ liệu giả lập trong test...), giữ nguyên hành vi trước GĐ6: lưu
    /// thẳng plaintext vào NoiDungTinNhan, không đụng gì thêm — không có
    /// "mã hóa 1 nửa" (vd. mã hóa được cho người này nhưng không cho người
    /// kia đọc lại). soNguoiThamGiaDuKien bắt đúng trường hợp CHÍNH NGƯỜI GỬI
    /// (hoặc 1 thành viên nhóm) không có bản ghi NguoiDung — vòng lặp build
    /// nguoiThamGia ở trên chỉ ÂM THẦM bỏ qua id không tìm thấy thay vì báo
    /// lỗi, nên nếu chỉ dựa vào nguoiThamGia.Count/Any thì 1 người nhận hợp
    /// lệ vẫn lọt qua điều kiện dù người gửi bị thiếu — mã hóa sai cho 1 nửa
    /// số người tham gia thực tế.
    /// </summary>
    private void MaHoaNoiDungNeuCoThe(TinNhan tinNhan, string noiDungGoc, List<NguoiDung> nguoiThamGia, NguoiDung? nguoiGui, int soNguoiThamGiaDuKien)
    {
        if (tinNhan.LoaiTinNhan != LoaiTinNhan.Text)
        {
            return;
        }

        if (nguoiGui is null || nguoiThamGia.Count != soNguoiThamGiaDuKien ||
            nguoiThamGia.Any(nd => string.IsNullOrWhiteSpace(nd.KhoaCongKhai)))
        {
            return;
        }

        var khoaPhien = _dichVuMaHoa.SinhKhoaPhienAes();
        var ketQuaMaHoa = _dichVuMaHoa.MaHoaTinNhan(noiDungGoc, khoaPhien);

        tinNhan.CiphertextTinNhan = ketQuaMaHoa.Ciphertext;
        tinNhan.Nonce = ketQuaMaHoa.Nonce;
        tinNhan.AuthTag = ketQuaMaHoa.AuthTag;
        tinNhan.DanhSachKhoaPhien = nguoiThamGia
            .Select(nd => new KhoaPhienNguoiDung
            {
                NguoiDungId = nd.Id,
                KhoaPhienDaMaHoa = _dichVuMaHoa.MaHoaKhoaPhien(khoaPhien, nd.KhoaCongKhai),
            })
            .ToList();
        // Trường nội dung lưu chính bản mã (Base64) — trong MongoDB chỉ thấy chuỗi vô nghĩa, không có plaintext.
        // Phân biệt tin đã mã hóa bằng DanhSachKhoaPhien không rỗng, KHÔNG dựa vào NoiDungTinNhan.
        tinNhan.NoiDungTinNhan = ketQuaMaHoa.Ciphertext;
    }

    /// <summary>
    /// [GĐ6] Đọc lại nội dung THẬT của 1 tin nhắn Text dưới góc nhìn của
    /// idHienTai. Rỗng/không có DanhSachKhoaPhien = tin chưa mã hóa (loại
    /// khác Text, tin cũ, hoặc lúc gửi có bên thiếu khóa) — đọc thẳng
    /// NoiDungTinNhan như trước GĐ6, không cần giải mã gì.
    /// </summary>
    private string GiaiMaNoiDungThucTe(TinNhan t, string idHienTai, string? khoaBiMatHienTai)
    {
        if (t.LoaiTinNhan != LoaiTinNhan.Text || t.DanhSachKhoaPhien.Count == 0)
        {
            return t.NoiDungTinNhan;
        }

        var khoaCuaMinh = t.DanhSachKhoaPhien.FirstOrDefault(k => k.NguoiDungId == idHienTai);
        if (khoaCuaMinh is null || string.IsNullOrEmpty(khoaBiMatHienTai) ||
            string.IsNullOrEmpty(t.CiphertextTinNhan) || string.IsNullOrEmpty(t.Nonce) || string.IsNullOrEmpty(t.AuthTag))
        {
            return "[Không thể giải mã]";
        }

        try
        {
            var khoaPhien = _dichVuMaHoa.GiaiMaKhoaPhien(khoaCuaMinh.KhoaPhienDaMaHoa, khoaBiMatHienTai);
            return _dichVuMaHoa.GiaiMaTinNhan(t.CiphertextTinNhan, t.Nonce, t.AuthTag, khoaPhien);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return "[Không thể giải mã]";
        }
    }

    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(nguoiHienTaiId);
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, nguoiHienTaiId, khoaBiMat)).ToList();
    }

    public async Task<List<TinNhanDto>> LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var lichSu = await _khoTinNhan.LayLichSuNhomAsync(nhomId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(nguoiHienTaiId);
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, nguoiHienTaiId, khoaBiMat)).ToList();
    }

    private async Task KiemTraQuyenTrenTinNhanAsync(string idHienTai, TinNhan tinNhan)
    {
        if (tinNhan.NhomId is not null)
        {
            var nhom = await _khoNhom.TimTheoIdAsync(tinNhan.NhomId) ?? throw new NhomKhongTonTaiException();
            if (!nhom.ThanhVienIds.Contains(idHienTai))
            {
                throw new KhongPhaiThanhVienNhomException();
            }
        }
        else if (tinNhan.NguoiGuiId != idHienTai && tinNhan.NguoiNhanId != idHienTai)
        {
            throw new KhongCoQuyenTrenTinNhanException();
        }
    }

    public async Task<TinNhanDto> ThuHoiAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        if (tinNhan.NguoiGuiId != idHienTai)
        {
            throw new KhongPhaiNguoiGuiException();
        }

        await _khoTinNhan.DanhDauThuHoiAsync(tinNhanId);
        tinNhan.DaThuHoi = true;
        return AnhXaDto(tinNhan, idHienTai, await LayKhoaBiMatAsync(idHienTai));
    }

    public async Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        var thoiGian = DateTime.UtcNow;
        await _khoTinNhan.DatGhimAsync(tinNhanId, true, thoiGian);
        tinNhan.DaGhim = true;
        tinNhan.ThoiGianGhim = thoiGian;
        return AnhXaDto(tinNhan, idHienTai, await LayKhoaBiMatAsync(idHienTai));
    }

    public async Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.DatGhimAsync(tinNhanId, false, null);
        tinNhan.DaGhim = false;
        tinNhan.ThoiGianGhim = null;
        return AnhXaDto(tinNhan, idHienTai, await LayKhoaBiMatAsync(idHienTai));
    }

    public async Task AnAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);
        await _khoTinNhanAn.AnAsync(idHienTai, tinNhanId);
    }

    public async Task<List<TinNhanDto>> LayTinDaGhimTheoNguoiDungAsync(string idHienTai, string doiTacId)
    {
        var ghim = await _khoTinNhan.LayTinDaGhimTheoNguoiDungAsync(idHienTai, doiTacId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ghim.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(idHienTai);
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList();
    }

    public async Task<List<TinNhanDto>> LayTinDaGhimTheoNhomAsync(string idHienTai, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var ghim = await _khoTinNhan.LayTinDaGhimTheoNhomAsync(nhomId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ghim.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(idHienTai);
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList();
    }

    public Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId) =>
        _khoTinNhan.DanhDauDaDocAsync(nguoiGuiId, nguoiHienTaiId);

    public async Task DanhDauDaDocNhomAsync(string nguoiHienTaiId, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var tinMoiNhat = await _khoTinNhan.LayLichSuNhomAsync(nhomId, null, 1);
        if (tinMoiNhat.Count > 0)
        {
            await _khoDocNhom.DanhDauDaDocAsync(nguoiHienTaiId, nhomId, tinMoiNhat[0].Id);
        }
    }

    public async Task<List<HoiThoaiTomTatDto>> LayDanhSachHoiThoaiAsync(string nguoiDungId)
    {
        var tatCaTinNhan = await _khoTinNhan.LayTatCaLienQuanAsync(nguoiDungId);
        var tatCaNguoiDung = await _khoNguoiDung.LayTatCaAsync();
        var mapNguoiDung = tatCaNguoiDung.ToDictionary(nd => nd.Id);

        var ketQua = new List<HoiThoaiTomTatDto>();
        var daXuLy = new HashSet<string>();

        foreach (var tn in tatCaTinNhan)
        {
            var idKia = tn.NguoiGuiId == nguoiDungId ? tn.NguoiNhanId : tn.NguoiGuiId;
            if (idKia is null || !daXuLy.Add(idKia))
            {
                continue;
            }

            if (!mapNguoiDung.TryGetValue(idKia, out var nguoiKia))
            {
                continue;
            }

            var soChuaDoc = tatCaTinNhan.Count(t => t.NguoiGuiId == idKia && t.NguoiNhanId == nguoiDungId && !t.DaDoc);
            // [GĐ6] tn.NoiDungTinNhan có thể là bản mã (đã mã hóa) — giải mã dưới
            // góc nhìn của nguoiDungId (chính người xem danh sách hội thoại
            // này); mapNguoiDung đã có sẵn toàn bộ user nên không cần query
            // thêm để lấy KhoaBiMat của họ.
            mapNguoiDung.TryGetValue(nguoiDungId, out var nguoiXemDanhSach);
            var xemTruoc = tn.DaThuHoi
                ? "Tin nhắn đã được thu hồi."
                : tn.LoaiTinNhan == LoaiTinNhan.Text
                    ? GiaiMaNoiDungThucTe(tn, nguoiDungId, nguoiXemDanhSach?.KhoaBiMat)
                    : tn.LoaiTinNhan == LoaiTinNhan.Anh ? "[Ảnh]" : "[File]";

            ketQua.Add(new HoiThoaiTomTatDto(
                new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa, nguoiKia.TenHienThiThucTe(), nguoiKia.DuongDanAnhDaiDien),
                xemTruoc,
                tn.ThoiGianTao,
                soChuaDoc));
        }

        return ketQua;
    }

    public async Task<List<TinNhanDto>> LayMediaTheoNguoiDungAsync(string idHienTai, string doiTacId)
    {
        var media = await _khoTinNhan.LayMediaTheoNguoiDungAsync(idHienTai, doiTacId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, media.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(idHienTai);
        return media.Where(t => !t.DaThuHoi && !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList();
    }

    public async Task<List<TinNhanDto>> LayMediaTheoNhomAsync(string idHienTai, string nhomId)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var media = await _khoTinNhan.LayMediaTheoNhomAsync(nhomId);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, media.Select(t => t.Id));
        var khoaBiMat = await LayKhoaBiMatAsync(idHienTai);
        return media.Where(t => !t.DaThuHoi && !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList();
    }

    public async Task<List<TinNhanDto>> TimKiemTheoNguoiDungAsync(string idHienTai, string doiTacId, string tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return new List<TinNhanDto>();
        }

        var tuKhoaChuan = tuKhoa.Trim();
        var khoaBiMat = await LayKhoaBiMatAsync(idHienTai);
        var ketQuaPlaintext = await _khoTinNhan.TimKiemTheoNguoiDungAsync(idHienTai, doiTacId, tuKhoaChuan);
        var ungVienMaHoa = await _khoTinNhan.LayTinDaMaHoaTheoNguoiDungAsync(idHienTai, doiTacId);
        var ketQua = GomKetQuaTimKiem(ketQuaPlaintext, ungVienMaHoa, idHienTai, khoaBiMat, tuKhoaChuan);

        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ketQua.Select(t => t.Id));
        return ketQua.Where(t => !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList();
    }

    /// <summary>
    /// [GĐ6] TimKiemTheoNguoiDungAsync/TimKiemTheoNhomAsync ở kho (regex trên NoiDungTinNhan) chỉ khớp
    /// được tin CHƯA mã hóa — tin đã mã hóa chỉ lưu bản mã trong NoiDungTinNhan nên không thể lọc theo từ khóa bằng Mongo.
    /// Gộp thêm "ứng viên đã mã hóa" (đã fetch riêng), tự giải mã dưới góc nhìn idHienTai rồi lọc theo
    /// tuKhoa (Contains, không phân biệt hoa/thường) — cùng tiêu chí với query Mongo phía trên.
    /// </summary>
    private List<TinNhan> GomKetQuaTimKiem(List<TinNhan> ketQuaPlaintext, List<TinNhan> ungVienMaHoa, string idHienTai, string? khoaBiMat, string tuKhoa)
    {
        var tuMaHoaKhopTuKhoa = ungVienMaHoa
            .Where(t => GiaiMaNoiDungThucTe(t, idHienTai, khoaBiMat).Contains(tuKhoa, StringComparison.OrdinalIgnoreCase));

        return ketQuaPlaintext
            .Concat(tuMaHoaKhopTuKhoa)
            .OrderByDescending(t => t.ThoiGianTao)
            .Take(50)
            .ToList();
    }

    public async Task<List<TinNhanDto>> TimKiemTheoNhomAsync(string idHienTai, string nhomId, string tuKhoa)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(idHienTai))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return new List<TinNhanDto>();
        }

        var tuKhoaChuan = tuKhoa.Trim();
        var khoaBiMat = await LayKhoaBiMatAsync(idHienTai);
        var ketQuaPlaintext = await _khoTinNhan.TimKiemTheoNhomAsync(nhomId, tuKhoaChuan);
        var ungVienMaHoa = await _khoTinNhan.LayTinDaMaHoaTheoNhomAsync(nhomId);
        var ketQua = GomKetQuaTimKiem(ketQuaPlaintext, ungVienMaHoa, idHienTai, khoaBiMat, tuKhoaChuan);

        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(idHienTai, ketQua.Select(t => t.Id));
        return ketQua.Where(t => !idDaAn.Contains(t.Id)).Select(t => AnhXaDto(t, idHienTai, khoaBiMat)).ToList();
    }

    public async Task<TinNhanDto> ThaCamXucAsync(string idHienTai, string tinNhanId, string loaiCamXuc)
    {
        if (!Enum.TryParse<LoaiCamXuc>(loaiCamXuc, ignoreCase: true, out var loai) || !Enum.IsDefined(loai))
        {
            throw new TinNhanKhongHopLeException($"Loại cảm xúc không hợp lệ: {loaiCamXuc}.");
        }

        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.ThaCamXucAsync(tinNhanId, idHienTai, loai);
        tinNhan.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == idHienTai);
        tinNhan.DanhSachCamXuc.Add(new CamXucTinNhan { NguoiDungId = idHienTai, LoaiCamXuc = loai });
        return AnhXaDto(tinNhan, idHienTai, await LayKhoaBiMatAsync(idHienTai));
    }

    public async Task<TinNhanDto> BoCamXucAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.BoCamXucAsync(tinNhanId, idHienTai);
        tinNhan.DanhSachCamXuc.RemoveAll(cx => cx.NguoiDungId == idHienTai);
        return AnhXaDto(tinNhan, idHienTai, await LayKhoaBiMatAsync(idHienTai));
    }

    /// <summary>Lấy KhoaBiMat của idHienTai — dùng để giải mã tin nhắn dưới góc nhìn của họ.</summary>
    private async Task<string?> LayKhoaBiMatAsync(string idHienTai) =>
        (await _khoNguoiDung.TimTheoIdAsync(idHienTai))?.KhoaBiMat;

    private TinNhanDto AnhXaDto(TinNhan t, string idHienTai, string? khoaBiMatHienTai)
    {
        var traLoi = t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString());
        var danhSachCamXuc = t.DaThuHoi
            ? new List<CamXucDto>()
            : t.DanhSachCamXuc.Select(cx => new CamXucDto(cx.NguoiDungId, cx.LoaiCamXuc.ToString())).ToList();

        if (t.DaThuHoi)
        {
            return new(
                t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(),
                "Tin nhắn đã được thu hồi.", null, null, null, null,
                t.DaDoc, t.DaNhan, t.ThoiGianTao, traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim, danhSachCamXuc);
        }

        // [GĐ6] Giải mã (nếu tin này có mã hóa) dưới góc nhìn của idHienTai
        // trước khi trả DTO — client không bao giờ thấy Ciphertext/khóa RSA,
        // chỉ thấy đúng NoiDungTinNhan như thể chưa từng mã hóa.
        var noiDungThucTe = GiaiMaNoiDungThucTe(t, idHienTai, khoaBiMatHienTai);

        return new(
            t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), noiDungThucTe,
            t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
            traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim, danhSachCamXuc);
    }
}
