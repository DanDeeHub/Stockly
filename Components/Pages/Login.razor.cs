using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using MudBlazor;
using Stockly.Dtos.Users;
using Stockly.Interfaces;

namespace Stockly.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject] private IApiService ApiService { get; set; } = null!;
    [Inject] private ProtectedLocalStorage LocalStorage { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IUserStateService UserState  { get; set; } = null!;
    
    protected UserRequestDto LoginModel { get; set; } = new();
    protected bool IsLoading { get; set; }
    private string ErrorMessage { get; set; } = string.Empty;
    private MudForm _form = null!;
    protected bool IsValid;
    protected string[] Errors = [];
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && OperatingSystem.IsBrowser())
        {
            var token = await LocalStorage.GetAsync<string>("authToken");
            if (token.Success)
            {
                await UserState.LoadUserStateAsync();
                Navigation.NavigateTo("/", forceLoad: true); 
            }
            else
                Navigation.NavigateTo("/Login");
        }
    }

    protected async Task HandleLogin()
    {
        await _form.Validate();
        if (!IsValid)
        {
            ErrorMessage = "Please fix validation errors";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        StateHasChanged();

        var (success, token) = await ApiService.AuthenticateAsync(LoginModel);
    
        if (!success || string.IsNullOrEmpty(token))
            ErrorMessage = "Invalid username or password";
        else
        {
            await UserState.LoadUserStateAsync();
            await InvokeAsync(() => Navigation.NavigateTo("/", forceLoad: true));
        }

        IsLoading = false;
        StateHasChanged();
    }
}