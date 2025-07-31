using Microsoft.AspNetCore.Components;
using Stockly.Interfaces;

namespace Stockly.Components.Pages;

public partial class AuthorizedPageBase : ComponentBase
{
    [Inject] private IUserStateService UserState { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    protected void GoToHome() => 
        Navigation.NavigateTo(UserState.IsAuthenticated ? "/" : "/login");
}