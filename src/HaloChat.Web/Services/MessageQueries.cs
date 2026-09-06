using System.Linq.Expressions;
using HaloChat.Web.Models;

namespace HaloChat.Web.Services;

public static class MessageQueries
{
    public static Expression<Func<Message, bool>> BetweenUsers(string userAId, string userBId)
        => m => (m.SenderId == userAId && m.ReceiverId == userBId) ||
                (m.SenderId == userBId && m.ReceiverId == userAId);
}
