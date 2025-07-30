using Stockly.Services;

namespace Stockly.Services
{
    public class UserStateService
    {
        private User? _currentUser;
        private SecureStorageService? _secureStorage;

        public UserStateService(SecureStorageService secureStorage)
        {
            _secureStorage = secureStorage;
        }

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

        // Event to notify when reminders are updated
        public event Action? OnReminderUpdated;
        
        public void NotifyReminderUpdated()
        {
            OnReminderUpdated?.Invoke();
        }

        public event Action? OnChange;

        public void Login(User user)
        {
            CurrentUser = user;
        }

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            
            // Clear stored credentials on logout
            if (_secureStorage != null)
            {
                await _secureStorage.ClearAllStorageAsync();
            }
        }

        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
} 