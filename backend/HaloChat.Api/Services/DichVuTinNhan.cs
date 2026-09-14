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

    public DichVuTinNhan(
        ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung, ILoiMoiKetBanRepository khoLoiMoiKetBan,
        INhomRepository khoNhom, IDocNhomRepository khoDocNhom, IQuanLyKetNoiChat quanLyKetNoi)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
        _khoLoiMoiKetBan = khoLoiMoiKetBan;
        _khoNhom = khoNhom;
        _khoDocNhom = khoDocNhom;
        _quanLyKetNoi = quanLyKetNoi;
    }

    public async Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
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

        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return AnhXaDto(tinNhan);
    }

    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        return lichSu.Select(AnhXaDto).ToList();
    }

    public async Task<List<TinNhanDto>> LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong)
    {
        var nhom = await _khoNhom.TimTheoIdAsync(nhomId) ?? throw new NhomKhongTonTaiException();
        if (!nhom.ThanhVienIds.Contains(nguoiHienTaiId))
        {
            throw new KhongPhaiThanhVienNhomException();
        }

        var lichSu = await _khoTinNhan.LayLichSuNhomAsync(nhomId, truocId, soLuong);
        return lichSu.Select(AnhXaDto).ToList();
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
            var xemTruoc = tn.LoaiTinNhan == LoaiTinNhan.Text
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

    private static TinNhanDto AnhXaDto(TinNhan t) => new(
        t.Id, t.NguoiGuiId, t.NguoiNhanId, t.NhomId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
        t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.DaNhan, t.ThoiGianTao);
}
