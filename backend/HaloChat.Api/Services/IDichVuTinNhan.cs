using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuTinNhan
{
    Task<TinNhanDto> GuiTinNhanAsync(
        string nguoiGuiId, string? nguoiNhanId, string? nhomId, string loaiTinNhan, string noiDungTinNhan,
        string? duongDanFile, string? tenFileGoc, long? kichThuocFile, string? loaiFile, string? traLoiId);

    Task<List<TinNhanDto>> LayLichSuAsync(string nguoiHienTaiId, string nguoiKiaId, string? truocId, int soLuong);

    Task<List<TinNhanDto>> LayLichSuNhomAsync(string nguoiHienTaiId, string nhomId, string? truocId, int soLuong);

    Task DanhDauDaDocAsync(string nguoiHienTaiId, string nguoiGuiId);

    Task DanhDauDaDocNhomAsync(string nguoiHienTaiId, string nhomId);

    Task<List<HoiThoaiTomTatDto>> LayDanhSachHoiThoaiAsync(string nguoiDungId);

    Task<TinNhanDto> ThuHoiAsync(string idHienTai, string tinNhanId);
    Task<TinNhanDto> GhimAsync(string idHienTai, string tinNhanId);
    Task<TinNhanDto> BoGhimAsync(string idHienTai, string tinNhanId);
    Task AnAsync(string idHienTai, string tinNhanId);
    Task<List<TinNhanDto>> LayTinDaGhimTheoNguoiDungAsync(string idHienTai, string doiTacId);
    Task<List<TinNhanDto>> LayTinDaGhimTheoNhomAsync(string idHienTai, string nhomId);
    Task<List<TinNhanDto>> LayMediaTheoNguoiDungAsync(string idHienTai, string doiTacId);
    Task<List<TinNhanDto>> LayMediaTheoNhomAsync(string idHienTai, string nhomId);
    Task<List<TinNhanDto>> TimKiemTheoNguoiDungAsync(string idHienTai, string doiTacId, string tuKhoa);
    Task<List<TinNhanDto>> TimKiemTheoNhomAsync(string idHienTai, string nhomId, string tuKhoa);

    Task<TinNhanDto> ThaCamXucAsync(string idHienTai, string tinNhanId, string loaiCamXuc);
    Task<TinNhanDto> BoCamXucAsync(string idHienTai, string tinNhanId);
}
