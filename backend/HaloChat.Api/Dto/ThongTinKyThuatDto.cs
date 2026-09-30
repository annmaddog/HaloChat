namespace HaloChat.Api.Dto;

/// <summary>
/// [Thông tin kỹ thuật] Số liệu ĐO TRỰC TIẾP lúc bấm xem, không lưu MongoDB — xem
/// docs/superpowers/specs/2026-09-30-halochat-chu-ky-so-design.md §8.
/// </summary>
public record ThongTinKyThuatDto(
    bool ApDungDuoc,
    bool DaMaHoa,
    string? ThuatToanMaHoa,
    int? KichThuocGocByte,
    int? KichThuocMaHoaByte,
    double? TyLePhinh,
    string? CiphertextRutGon,
    string? NonceRutGon,
    string? AuthTagRutGon,
    double? ThoiGianMaHoaMs,
    double? ThoiGianGiaiMaMs,
    bool CoChuKy,
    bool? DaXacThucChuKy,
    string? ThuatToanChuKy);
