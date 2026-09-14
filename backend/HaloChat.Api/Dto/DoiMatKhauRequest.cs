using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record DoiMatKhauRequest(
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ.")]
    string MatKhauCu,
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
    string MatKhauMoi);
