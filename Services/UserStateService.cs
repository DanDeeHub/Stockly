using Stockly.Services;

namespace Stockly.Services
{
    public class UserStateService
    {
        private User? _currentUser;

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnChange?.Invoke();
            }
        }

        public string Username => _currentUser?.Username ?? string.Empty;
        public bool IsAuthenticated => _currentUser != null;
        public string Role => _currentUser?.Role ?? string.Empty;

        public event Action? OnChange;

                            public void Login(User user)
                    {
                        CurrentUser = user;
                        Console.WriteLine($"User logged in: {user.Username}, Role: '{user.Role}'");
                    }

        public void Logout()
        {
            CurrentUser = null;
        }

        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
} 