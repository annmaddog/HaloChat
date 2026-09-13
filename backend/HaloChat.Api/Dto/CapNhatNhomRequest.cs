using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record CapNhatNhomRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên nhóm.")]
    [MinLength(2, ErrorMessage = "Tên nhóm phải có ít nhất 2 ký tự.")]
    string TenNhom,
    string? MoTa,
    string? DuongDanAnhDaiDien);
