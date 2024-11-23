using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Crudy.UI.Features.Home.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Crudy.UI.Identity;

 public class CookieAuthenticationStateProvider(ILocalStorageService localStorageService , IHttpClientFactory clientFactory , NavigationManager navigationManager) : AuthenticationStateProvider, IAuthService
    {
        private readonly JsonSerializerOptions jsonSerializerOptions =
            new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

        private readonly HttpClient _httpClient = clientFactory.CreateClient("default");
        
        private bool _authenticated = false;
        
        private readonly ClaimsPrincipal Unauthenticated = new(new ClaimsIdentity());
        

        public async Task<FormResult> Register(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/user/register", new
                {
                    email,
                    password
                });
 
            if (response.IsSuccessStatusCode)
            {
                return new FormResult { Succeeded = true };
            }
            
            var result = await response.Content.ReadFromJsonAsync<IdentityResult>();
 
            return new FormResult
            {
                Succeeded = false,
                ErrorList = [result.Detail]
            };
        }

        public async Task<UserInfo?> GetUserInfo()
        {
            try
            {
                var token = await localStorageService.GetItemAsync<string?>("token");

                if (token is null)
                    return default;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                var userResponse = await _httpClient.GetAsync("/api/user/user-info");

                userResponse.EnsureSuccessStatusCode();

                return await userResponse.Content.ReadFromJsonAsync<UserInfo>();
            }
            catch (Exception e)
            {
                // ignored
            }
            
            return default;
        }

        public async Task<bool> IsAuthenticated()
        {
            var token = await localStorageService.GetItemAsync<string?>("token");
            return token is not null;
        }

        public async Task<FormResult> Login(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/user/login", new
                {
                    email,
                    password
                });
                
            if (response.IsSuccessStatusCode)
            {
                await GetAuthenticationStateAsync();
                await localStorageService.SetItemAsync("token",await response.Content.ReadAsStringAsync() );
                    
                return new FormResult { Succeeded = true };
            }
                
            var result = await response.Content.ReadFromJsonAsync<IdentityResult>();

            return new FormResult
            {
                Succeeded = false,
                ErrorList = [result.Detail]
            };
        }
        
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            _authenticated = false;
 
            var user = Unauthenticated;
 
            var token = await localStorageService.GetItemAsync<string?>("token");

            if (token is not null)
            {
              var userInfo = await GetUserInfo();

                if (userInfo != null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userInfo.Email),
                        new(ClaimTypes.Email, userInfo.Email)
                    };

                    var id = new ClaimsIdentity(claims, nameof(CookieAuthenticationStateProvider));
                    user = new ClaimsPrincipal(id);
                    _authenticated = true;
                }
            }

            return new AuthenticationState(user);
        }
 
        public async Task<bool> CheckAuthenticatedAsync()
        {
            await GetAuthenticationStateAsync();
            return _authenticated;
        }

        public async Task Logout()
        {
            await localStorageService.RemoveItemAsync("token");
            navigationManager.Refresh();
        }
    }
