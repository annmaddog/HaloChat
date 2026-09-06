using HaloChat.Security;
using HaloChat.Web.Data;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using HaloChat.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HaloChat.Web.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMessageCipher _cipher;
    private readonly IConversationKeyProvider _keyProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(
        ApplicationDbContext db,
        IMessageCipher cipher,
        IConversationKeyProvider keyProvider,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cipher = cipher;
        _keyProvider = keyProvider;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);

        var users = await _db.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                DisplayName = u.UserName ?? u.Email ?? u.Id
            })
            .ToListAsync();

        return View(users);
    }

    public async Task<IActionResult> Conversation(string id)
    {
        var currentUserId = _userManager.GetUserId(User)!;

        if (string.IsNullOrEmpty(id) || id == currentUserId)
        {
            return NotFound();
        }

        var otherUser = await _userManager.FindByIdAsync(id);
        if (otherUser is null)
        {
            return NotFound();
        }

        var messages = await _db.Messages
            .Where(MessageQueries.BetweenUsers(currentUserId, id))
            .ToListAsync();

        var key = _keyProvider.GetKey(currentUserId, id);
        var viewModel = new ConversationViewModel
        {
            OtherUserId = otherUser.Id,
            OtherUserDisplayName = otherUser.UserName ?? otherUser.Email ?? otherUser.Id,
            Messages = ChatHistoryMapper.MapToViewModels(messages, _cipher, key, currentUserId)
        };

        return View(viewModel);
    }
}
