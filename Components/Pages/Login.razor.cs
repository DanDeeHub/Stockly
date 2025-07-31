using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Stockly.Dtos.Users;
using Stockly.Interface;
using Stockly.Services;

namespace Stockly.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IApiService ApiService { get; set; } = null!;
    [Inject] private ProtectedLocalStorage LocalStorage { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private UserStateService UserState  { get; set; } = null!;
    
    protected UserRequestDto LoginModel { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string ErrorMessage { get; set; } = string.Empty;
    protected MudForm Form;
    protected bool IsValid;
    protected string[] Errors;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var token = await LocalStorage.GetAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token.Value))
                Navigation.NavigateTo("/");
        }
        catch { /* Ignore */ }
    }

    protected async Task HandleLogin()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var success = await ApiService.LoginAsync(LoginModel);
            if (success)
                Navigation.NavigateTo("/");
            else
                ErrorMessage = "Invalid username or password";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Login failed. Please try again later.";
            await Console.Error.WriteLineAsync($"Login error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}