using HaloChat.Api.Dto;

namespace HaloChat.Api.Services;

public interface IDichVuNhom
{
    Task<NhomDto> TaoNhomAsync(string nguoiTaoId, string tenNhom, string? moTa, string? duongDanAnhDaiDien, List<string> thanhVienIds);
    Task<List<NhomDto>> LayDanhSachAsync(string nguoiDungId);
    Task<NhomDto> LayChiTietAsync(string nguoiDungId, string nhomId);
    Task<NhomDto> CapNhatAsync(string nguoiDungId, string nhomId, string tenNhom, string? moTa, string? duongDanAnhDaiDien);
}
