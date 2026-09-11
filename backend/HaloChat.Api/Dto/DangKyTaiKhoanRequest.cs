using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

// Lưu ý: đặt trực tiếp trên tham số của primary constructor (không dùng
// target "property:") — ASP.NET Core validation cho record yêu cầu vậy,
// nếu không sẽ ném InvalidOperationException lúc validate model.
public record DangKyTaiKhoanRequest(
    [Required, MinLength(3)] string TenTaiKhoan,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string MatKhau);
