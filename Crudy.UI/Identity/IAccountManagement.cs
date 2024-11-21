namespace Crudy.UI.Identity;

public interface IAccountManagement
{
    public Task<FormResult> LoginAsync(string email, string password);
    public Task LogoutAsync();
    public Task<FormResult> RegisterAsync(string email, string password);
    public Task<bool> CheckAuthenticatedAsync();
}