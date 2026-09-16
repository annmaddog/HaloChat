using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
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

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IDocNhomRepository khoDocNhom, IQuanLyKetNoiChat quanLyKetNoi,
        ITinNhanAnRepository khoTinNhanAn)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _khoDocNhom = khoDocNhom;
        _quanLyKetNoi = quanLyKetNoi;
        _khoTinNhanAn = khoTinNhanAn;
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
            var noiDungTomTat = tinGoc.LoaiTinNhan switch
            {
                LoaiTinNhan.Text => tinGoc.NoiDungTinNhan.Length > 80
                    ? tinGoc.NoiDungTinNhan[..80] + "…"
                    : tinGoc.NoiDungTinNhan,
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

        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return AnhXaDto(tinNhan);
    }

    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        var idDaAn = await _khoTinNhanAn.LayDanhSachIdDaAnAsync(nguoiHienTaiId, lichSu.Select(t => t.Id));
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
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
        return lichSu.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
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
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        var thoiGian = DateTime.UtcNow;
        await _khoTinNhan.DatGhimAsync(tinNhanId, true, thoiGian);
        tinNhan.DaGhim = true;
        tinNhan.ThoiGianGhim = thoiGian;
        return AnhXaDto(tinNhan);
    }

    public async Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId)
    {
        var tinNhan = await _khoTinNhan.TimTheoIdAsync(tinNhanId) ?? throw new TinNhanKhongTonTaiException();
        await KiemTraQuyenTrenTinNhanAsync(idHienTai, tinNhan);

        await _khoTinNhan.DatGhimAsync(tinNhanId, false, null);
        tinNhan.DaGhim = false;
        tinNhan.ThoiGianGhim = null;
        return AnhXaDto(tinNhan);
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
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
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
        return ghim.Where(t => !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
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
            var xemTruoc = tn.DaThuHoi
                ? "Tin nhắn đã được thu hồi."
                : tn.LoaiTinNhan == LoaiTinNhan.Text
                    ? tn.NoiDungTinNhan
                    : tn.LoaiTinNhan == LoaiTinNhan.Anh ? "[Ảnh]" : "[File]";

            ketQua.Add(new HoiThoaiTomTatDto(
                new NguoiDungTomTatDto(nguoiKia.Id, nguoiKia.TenTaiKhoan, nguoiKia.Email, nguoiKia.ChoPhepTinNhanTuNguoiLa, nguoiKia.TenHienThiThucTe()),
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
        return media.Where(t => !t.DaThuHoi && !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
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
        return media.Where(t => !t.DaThuHoi && !idDaAn.Contains(t.Id)).Select(AnhXaDto).ToList();
    }

    private static TinNhanDto AnhXaDto(TinNhan t)
    {
        var traLoi = t.TraLoi is null ? null : new TraLoiThongTinDto(t.TraLoi.Id, t.TraLoi.TenNguoiGui, t.TraLoi.NoiDungTomTat, t.TraLoi.LoaiTinNhan.ToString());

        if (t.DaThuHoi)
        {
            return new(
                t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(),
                "Tin nhắn đã được thu hồi.", null, null, null, null,
                t.DaDoc, t.DaNhan, t.ThoiGianTao, traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim);
        }

        return new(
            t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
            t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao,
            traLoi, t.DaThuHoi, t.DaGhim, t.ThoiGianGhim);
    }
}
