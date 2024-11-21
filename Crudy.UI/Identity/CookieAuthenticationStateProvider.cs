using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Crudy.UI.Identity;

 public class CookieAuthenticationStateProvider : AuthenticationStateProvider, IAccountManagement
    {
        private readonly JsonSerializerOptions jsonSerializerOptions =
            new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _httpClient;
        
        private bool _authenticated = false;
        
        private readonly ClaimsPrincipal Unauthenticated =
            new(new ClaimsIdentity());
        
        public CookieAuthenticationStateProvider(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
            _httpClient = httpClientFactory.CreateClient("Auth");
        }

        public async Task<FormResult> RegisterAsync(string email, string password)
        {
            string[] defaultDetail = ["An unknown error prevented registration from succeeding."];
 
            try
            {
                var result = await _httpClient.PostAsJsonAsync(
                    "/api/user/register", new
                    {
                        email,
                        password
                    });
 
                if (result.IsSuccessStatusCode)
                {
                    return new FormResult { Succeeded = true };
                }
 
                var details = await result.Content.ReadAsStringAsync();
                var problemDetails = JsonDocument.Parse(details);
                var errors = new List<string>();
                var errorList = problemDetails.RootElement.GetProperty("errors");
 
                foreach (var errorEntry in errorList.EnumerateObject())
                {
                    if (errorEntry.Value.ValueKind == JsonValueKind.String)
                    {
                        errors.Add(errorEntry.Value.GetString()!);
                    }
                    else if (errorEntry.Value.ValueKind == JsonValueKind.Array)
                    {
                        errors.AddRange(
                            errorEntry.Value.EnumerateArray().Select(
                                e => e.GetString() ?? string.Empty)
                            .Where(e => !string.IsNullOrEmpty(e)));
                    }
                }
 
                return new FormResult
                {
                    Succeeded = false,
                    ErrorList = problemDetails == null ? defaultDetail : [.. errors]
                };
            }
            catch { }
 
            return new FormResult
            {
                Succeeded = false,
                ErrorList = defaultDetail
            };
        }
        
        public async Task<FormResult> LoginAsync(string email, string password)
        {
            try
            {
                var result = await _httpClient.PostAsJsonAsync(
                    "/api/user/login", new
                    {
                        email,
                        password
                    });
 
                if (result.IsSuccessStatusCode)
                {
                    string token = await result.Content.ReadAsStringAsync();
                    await _localStorage.SetItemAsync("token",token );
                    
                    NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
 
                    return new FormResult { Succeeded = true };
                }
            }
            catch { }
 
            return new FormResult
            {
                Succeeded = false,
                ErrorList = ["Invalid email and/or password."]
            };
        }
        
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            _authenticated = false;
 
            var user = Unauthenticated;
 
            try
            {
                var token = await _localStorage.GetItemAsync<string?>("token");

                if (token is not null)
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    var userResponse = await _httpClient.GetAsync("/api/user/user-info");

                    userResponse.EnsureSuccessStatusCode();

                    var userJson = await userResponse.Content.ReadAsStringAsync();
                    var userInfo = JsonSerializer.Deserialize<UserInfo>(userJson, jsonSerializerOptions);

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
            }
            catch { }
 
            return new AuthenticationState(user);
        }
 
        public async Task LogoutAsync()
        {
            const string Empty = "{}";
            var emptyContent = new StringContent(Empty, Encoding.UTF8, "application/json");
            await _httpClient.PostAsync("Logout", emptyContent);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
 
        public async Task<bool> CheckAuthenticatedAsync()
        {
            await GetAuthenticationStateAsync();
            return _authenticated;
        }
    }
