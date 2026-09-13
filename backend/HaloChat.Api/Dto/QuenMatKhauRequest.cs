using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record QuenMatKhauRequest(
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    string Email);
