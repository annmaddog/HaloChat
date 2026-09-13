using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record ThemThanhVienRequest([Required(ErrorMessage = "Vui lòng chọn người để thêm.")] string ThanhVienId);
