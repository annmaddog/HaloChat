using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

// Lưu ý: đặt trực tiếp trên tham số của primary constructor (không dùng
// target "property:") — ASP.NET Core validation cho record yêu cầu vậy,
// nếu không sẽ ném InvalidOperationException lúc validate model.
public record DangKyTaiKhoanRequest(
    [Required(ErrorMessage = "Vui lòng nhập tên tài khoản.")]
    [MinLength(3, ErrorMessage = "Tên tài khoản phải có ít nhất 3 ký tự.")]
    string TenTaiKhoan,
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    string Email,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
    string MatKhau);
