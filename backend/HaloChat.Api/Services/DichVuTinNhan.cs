using HaloChat.Api.Dto;
using HaloChat.Api.Models;
using HaloChat.Api.Repositories;
using MongoDB.Bson;

namespace HaloChat.Api.Services;

public class DichVuTinNhan : IDichVuTinNhan
{
    private static readonly System.Text.RegularExpressions.Regex MauDuongDanFileHopLe = new(
        @"^/uploads/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\.(jpg|jpeg|png|gif|webp|pdf|docx|xlsx|zip)$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private readonly ITinNhanRepository _khoTinNhan;
    private readonly INguoiDungRepository _khoNguoiDung;

    public DichVuTinNhan(ITinNhanRepository khoTinNhan, INguoiDungRepository khoNguoiDung)
    {
        _khoTinNhan = khoTinNhan;
        _khoNguoiDung = khoNguoiDung;
    }

    public async Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string nguoiNhanId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile)
    {
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

        if (!ObjectId.TryParse(nguoiNhanId, out _))
        {
            throw new NguoiNhanKhongTonTaiException(nguoiNhanId);
        }

        var nguoiNhan = await _khoNguoiDung.TimTheoIdAsync(nguoiNhanId);
        if (nguoiNhan is null)
        {
            throw new NguoiNhanKhongTonTaiException(nguoiNhanId);
        }

        var tinNhan = new TinNhan
        {
            NguoiGuiId = nguoiGuiId,
            NguoiNhanId = nguoiNhanId,
            LoaiTinNhan = loai,
            NoiDungTinNhan = noiDungTinNhan ?? string.Empty,
            DuongDanFile = duongDanFile,
            TenFileGoc = tenFileGoc,
            KichThuocFile = kichThuocFile,
            LoaiFile = loaiFile,
        };

        await _khoTinNhan.ThemMoiAsync(tinNhan);
        return AnhXaDto(tinNhan);
    }

    public async Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong)
    {
        var lichSu = await _khoTinNhan.LayLichSuTheoNguoiDungAsync(nguoiHienTaiId, nguoiKiaId, truocId, soLuong);
        return lichSu.Select(AnhXaDto).ToList();
    }

    public Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId) =>
        _khoTinNhan.DanhDauDaDocAsync(nguoiGuiId, nguoiHienTaiId);

    private static TinNhanDto AnhXaDto(TinNhan t) => new(
        t.Id, t.NguoiGuiId, t.NguoiNhanId, t.LoaiTinNhan.ToString(), t.NoiDungTinNhan,
        t.DuongDanFile, t.TenFileGoc, t.KichThuocFile, t.LoaiFile, t.DaDoc, t.ThoiGianTao);
}
