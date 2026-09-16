using System.ComponentModel.DataAnnotations;

namespace HaloChat.Api.Dto;

public record DoiAnhDaiDienRequest(
    [Required, MinLength(1)] string DuongDanAnhDaiDien);
