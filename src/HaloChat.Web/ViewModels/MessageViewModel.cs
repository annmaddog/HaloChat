namespace HaloChat.Web.ViewModels;

public class MessageViewModel
{
    public string Content { get; set; } = default!;
    public bool IsMine { get; set; }
    public DateTime SentAtUtc { get; set; }
}
