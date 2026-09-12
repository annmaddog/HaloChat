using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;

namespace HaloChat.Api.Services;

public class DichVuKetBan : IDichVuKetBan
{
    private readonly ILoiMoiKetBanRepository _khoLoiMoi;
    private readonly INguoiDungRepository _khoNguoiDung;

    public DichVuKetBan(ILoiMoiKetBanRepository khoLoiMoi, INguoiDungRepository khoNguoiDung)
    {
        _khoLoiMoi = khoLoiMoi;
        _khoNguoiDung = khoNguoiDung;
    }

    public async Task<LoiMoiKetBanDto> GuiLoiMoiAsync(string nguoiGuiId, string nguoiNhanId)
    {
        if (nguoiGuiId == nguoiNhanId)
        {
            throw new KhongTheTuKetBanException();
        }

        var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(nguoiNhanId);
        if (nguoiNhan is null)
        {
            throw new NguoiDuocMoiKhongTonTaiException(nguoiNhanId);
        }

        if (await _khoLoiMoi.TonTaiLoiMoiDangHoatDongAsync(nguoiGuiId, nguoiNhanId))
        {
            throw new LoiMoiKetBanDaTonTaiException();
        }

        var nguoiGui = await _khoNguoiDung.TimTheoIdAsync(nguoiGuiId)
            ?? throw new InvalidOperationException("Người gửi không tồn tại — không thể xảy ra với JWT hợp lệ.");

        var loiMoi = new LoiMoiKetBan { NguoiGuiId = nguoiGuiId, NguoiNhanId = nguoiNhanId };
        await _khoLoiMoi.ThemMoiAsync(loiMoi);

        return AnhXaDto(loiMoi, nguoiGui, nguoiNhan);
    }

    public async Task<LoiMoiKetBanDto> ChapNhanAsync(string nguoiHienTaiId, string idLoiMoi)
    {
        var loiMoi = await LayLoiMoiChoDuyetCuaMinhAsync(nguoiHienTaiId, idLoiMoi);
        await _khoLoiMoi.CapNhatTrangThaiAsync(idLoiMoi, TrangThaiLoiMoiKetBan.DaChapNhan);
        loiMoi.TrangThai = TrangThaiLoiMoiKetBan.DaChapNhan;

        var nguoiGui = await _khoNguoiDung.TimTheoIdAsync(loiMoi.NguoiGuiId) ?? throw new NguoiDuocMoiKhongTonTaiException(loiMoi.NguoiGuiId);
        var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(loiMoi.NguoiNhanId) ?? throw new NguoiDuocMoiKhongTonTaiException(loiMoi.NguoiNhanId);
        return AnhXaDto(loiMoi, nguoiGui, nguoiNhan);
    }

    public async Task TuChoiAsync(string nguoiHienTaiId, string idLoiMoi)
    {
        await LayLoiMoiChoDuyetCuaMinhAsync(nguoiHienTaiId, idLoiMoi);
        await _khoLoiMoi.CapNhatTrangThaiAsync(idLoiMoi, TrangThaiLoiMoiKetBan.DaTuChoi);
    }

    private async Task<LoiMoiKetBan> LayLoiMoiChoDuyetCuaMinhAsync(string nguoiHienTaiId, string idLoiMoi)
    {
        var loiMoi = await _khoLoiMoi.TimTheoIdAsync(idLoiMoi);
        if (loiMoi is null)
        {
            throw new LoiMoiKetBanKhongTonTaiException();
        }

        if (loiMoi.NguoiNhanId != nguoiHienTaiId)
        {
            throw new KhongCoQuyenXuLyLoiMoiException();
        }

        if (loiMoi.TrangThai != TrangThaiLoiMoiKetBan.ChoDuyet)
        {
            throw new LoiMoiKetBanDaXuLyException();
        }

        return loiMoi;
    }

    public async Task<List<NguoiDungTomTatDto>> LayBanBeAsync(string nguoiDungId)
    {
        var danhSach = await _khoLoiMoi.LayBanBeAsync(nguoiDungId);
        var ketQua = new List<NguoiDungTomTatDto>();
        foreach (var l in danhSach)
        {
            var idBan = l.NguoiGuiId == nguoiDungId ? l.NguoiNhanId : l.NguoiGuiId;
            var ban = await _khoNguoiDung.TimTheoIdAsync(idBan);
            if (ban is not null)
            {
                ketQua.Add(new NguoiDungTomTatDto(ban.Id, ban.TenTaiKhoan, ban.Email));
            }
        }
        return ketQua;
    }

    public async Task<List<LoiMoiKetBanDto>> LayLoiMoiDenAsync(string nguoiDungId) =>
        await AnhXaDanhSachAsync(await _khoLoiMoi.LayLoiMoiDenAsync(nguoiDungId));

    public async Task<List<LoiMoiKetBanDto>> LayLoiMoiGuiAsync(string nguoiDungId) =>
        await AnhXaDanhSachAsync(await _khoLoiMoi.LayLoiMoiGuiAsync(nguoiDungId));

    private async Task<List<LoiMoiKetBanDto>> AnhXaDanhSachAsync(List<LoiMoiKetBan> danhSach)
    {
        var ketQua = new List<LoiMoiKetBanDto>();
        foreach (var l in danhSach)
        {
            var nguoiGui = await _khoNguoiDung.TimTheoIdAsync(l.NguoiGuiId);
            var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(l.NguoiNhanId);
            if (nguoiGui is not null && nguoiNhan is not null)
            {
                ketQua.Add(AnhXaDto(l, nguoiGui, nguoiNhan));
            }
        }
        return ketQua;
    }

    private static LoiMoiKetBanDto AnhXaDto(LoiMoiKetBan l, NguoiDung nguoiGui, NguoiDung nguoiNhan) => new(
        l.Id,
        new NguoiDungTomTatDto(nguoiGui.Id, nguoiGui.TenTaiKhoan, nguoiGui.Email),
        new NguoiDungTomTatDto(nguoiNhan.Id, nguoiNhan.TenTaiKhoan, nguoiNhan.Email),
        l.TrangThai.ToString(),
        l.ThoiGianTao);
}
