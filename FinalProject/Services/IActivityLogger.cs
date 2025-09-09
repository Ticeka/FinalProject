using System.Threading.Tasks;

namespace FinalProject.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(string? userId, string action, string? subjectType, string? subjectId,
                      string message, object? meta = null, string? ipHash = null, string? userAgent = null);

        // ช็อตคัตยอดฮิต
        Task RatingAddedAsync(string? userId, int beerId, string beerName, double score);
        Task CommentAddedAsync(string? userId, int beerId, string beerName, string body);
        Task FavoriteToggledAsync(string? userId, int beerId, string beerName, bool isFav);
        Task ProfileEditedAsync(string userId);
        Task AvatarChangedAsync(string userId, bool removed);
        Task AdminUserLockedAsync(string adminId, string targetUserId, string targetUserName, int days);
        Task AdminUserUnlockedAsync(string adminId, string targetUserId, string targetUserName);
        Task Admin2faToggledAsync(string adminId, string targetUserId, string targetUserName, bool enable);
        Task AdminRolesUpdatedAsync(string adminId, string targetUserId, string targetUserName, string rolesCsv);
    }
}
