namespace Stockly.Services
{
    public class DrawerService
    {
        private bool _isOpen = false;
        
        public DrawerService()
        {
            _isOpen = false; // Ensure it starts closed
        }
        
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                _isOpen = value;
                OnChange?.Invoke();
            }
        }

        public event Action? OnChange;

        public void Toggle()
        {
            IsOpen = !IsOpen;
        }

        public void Open()
        {
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }
    }
} 