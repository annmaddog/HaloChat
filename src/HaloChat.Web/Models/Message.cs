using System.ComponentModel.DataAnnotations;

namespace HaloChat.Web.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = default!;

    [Required]
    public string ReceiverId { get; set; } = default!;

    [Required]
    public byte[] CipherText { get; set; } = default!;

    public byte[]? Iv { get; set; }

    public byte[]? Tag { get; set; }

    [Required]
    public string Algorithm { get; set; } = "none";

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
