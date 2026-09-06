namespace HaloChat.Web.ViewModels;

public class ConversationViewModel
{
    public string OtherUserId { get; set; } = default!;
    public string OtherUserDisplayName { get; set; } = default!;
    public List<MessageViewModel> Messages { get; set; } = new();
}
