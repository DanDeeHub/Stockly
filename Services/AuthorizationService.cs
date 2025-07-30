using Stockly.Services;

namespace Stockly.Services
{
    public class AuthorizationService
    {
        private readonly UserStateService _userState;

        public AuthorizationService(UserStateService userState)
        {
            _userState = userState;
        }

        public bool CanAccessPage(string pageName)
        {
            if (!_userState.IsAuthenticated)
                return false;

            return pageName.ToLower() switch
            {
                "home" => true, // Everyone can access home
                "products" => _userState.Role == "admin", // Only Admin can access products
                "inventory" => true, // Everyone can access inventory
                "dashboard" => true, // Everyone can access dashboard
                "adminview" => _userState.Role == "admin",
                "openinginventory" => _userState.Role == "opening" || _userState.Role == "admin",
                "closinginventory" => _userState.Role == "closing" || _userState.Role == "admin",
                "login" => true, // Everyone can access login
                _ => false // Default deny
            };
        }

        public bool HasRole(string role)
        {
            return _userState.IsAuthenticated && _userState.Role == role;
        }

        public bool HasAnyRole(params string[] roles)
        {
            return _userState.IsAuthenticated && roles.Contains(_userState.Role);
        }
    }
} 