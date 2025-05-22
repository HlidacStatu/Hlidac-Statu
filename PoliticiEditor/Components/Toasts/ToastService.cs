namespace PoliticiEditor.Components.Toasts;

public sealed class ToastService
{
    private readonly object _lock = new();
    private readonly List<ToastMessage> _messages = new();

    public event Action? OnMessagesChange;

    public void AddMessage(string title, string message, int durationInSeconds = 5,
        ToastMessage.ImportanceLevel importance = ToastMessage.ImportanceLevel.Info)
    {
        var toast = new ToastMessage()
        {
            Title = title,
            Message = message,
            Duration = TimeSpan.FromSeconds(durationInSeconds),
            Importance = importance
        };

        AddMessage(toast);
    }
    public void AddInfoMessage(string title, string message)
    {
        var toast = new ToastMessage()
        {
            Title = title,
            Message = message,
            Duration = TimeSpan.FromSeconds(5),
            Importance = ToastMessage.ImportanceLevel.Info
        };

        AddMessage(toast);
    }
    public void AddWarningMessage(string title, string message)
    {
        var toast = new ToastMessage()
        {
            Title = title,
            Message = message,
            Duration = TimeSpan.FromSeconds(5),
            Importance = ToastMessage.ImportanceLevel.Warning
        };

        AddMessage(toast);

        AddMessage(toast);
    }
    public void AddErrorMessage(string title, string message)
    {
        var toast = new ToastMessage()
        {
            Title = title,
            Message = message,
            Duration = TimeSpan.FromSeconds(5),
            Importance = ToastMessage.ImportanceLevel.Error
        };

        AddMessage(toast);
    }

    public void AddMessage(ToastMessage toast)
    {
        lock (_lock)
        {
            _messages.Add(toast);
        }

        _ = HandleDisplayAsync(toast); //fire and forget
        OnMessagesChange?.Invoke();
    }

    public void RemoveMessage(ToastMessage toast)
    {
        lock (_lock)
        {
            toast.IsVisible = false;
            if (_messages.Contains(toast))
            {
                _messages.Remove(toast);
            }
        }

        OnMessagesChange?.Invoke();
    }

    private async Task HandleDisplayAsync(ToastMessage toast)
    {
        await toast.DisplayAsync();
        
        lock (_lock)
        {
            if (toast.IsVisible && _messages.Contains(toast))
            {
                _messages.Remove(toast);
            }
        }
        
        OnMessagesChange?.Invoke();
        
    }

    public List<ToastMessage> GetVisibleMessages()
    {
        lock (_lock)
        {
            return _messages.Where(m => m.IsVisible).ToList();
        }
    }

    public sealed class ToastMessage
    {
        public string Message { get; set; } = "";
        public string Title { get; set; } = "";
        public ImportanceLevel Importance { get; set; } = ImportanceLevel.Info;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public DateTime Created { get; } = DateTime.Now;
        public bool IsVisible { get; internal set; } = false;


        public async Task DisplayAsync()
        {
            IsVisible = true;

            if (Duration > TimeSpan.Zero)
            {
                await Task.Delay(Duration);
                IsVisible = false;
            }
        }

        public enum ImportanceLevel
        {
            Info,
            Warning,
            Error
        }
    }
}