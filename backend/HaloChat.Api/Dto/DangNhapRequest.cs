using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

// Lưu ý: đặt trực tiếp trên tham số của primary constructor (không dùng
// target "property:") — ASP.NET Core validation cho record yêu cầu vậy,
// nếu không sẽ ném InvalidOperationException lúc validate model.
public record DangNhapRequest(
    [Required] string TenDangNhap,
    [Required] string MatKhau);
