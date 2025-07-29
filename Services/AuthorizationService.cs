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

            Console.WriteLine($"Checking access for page '{pageName}', User role: '{_userState.Role}'");
            
            var result = pageName.ToLower() switch
            {
                "home" => true, // Everyone can access home
                "products" => true, // Everyone can access products
                "adminview" => _userState.Role == "admin",
                "openinginventory" => _userState.Role == "opening" || _userState.Role == "admin",
                "closinginventory" => _userState.Role == "closing" || _userState.Role == "admin",
                "login" => true, // Everyone can access login
                _ => false // Default deny
            };
            
            Console.WriteLine($"Access result for '{pageName}': {result}");
            return result;
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