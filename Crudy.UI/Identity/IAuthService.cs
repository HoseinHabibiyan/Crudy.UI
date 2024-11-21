namespace Crudy.UI.Identity;

public interface IAuthService
{
    public Task<FormResult> Login(string email, string password);
    public Task<FormResult> Register(string email, string password);
    Task<UserInfo?> GetUserInfo();
    Task<bool> IsAuthenticated();
    Task Logout();
}