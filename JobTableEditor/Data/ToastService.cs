using System.Web;
using Microsoft.AspNetCore.Components;

namespace JobTableEditor.Data
{
    public class ToastService 
    {
        public class ToastMessage
        {
            public ToastMessage(string title, MarkupString message, ToastLevel toastLevel)
            {
                Title = title;
                Message = message;
                ToastLevel = toastLevel;

                _ = Task.Run(async () => await DismissInFiveSecondsAsync()).ConfigureAwait(false);
            }

            public string Id { get; } = Guid.NewGuid().ToString();
            public DateTime Created { get; } = DateTime.Now;
            public string Title { get; init; }
            public MarkupString Message { get; init; }
            public ToastLevel ToastLevel { get; init; }
            public bool IsDismissed { get; private set; }
            
            public Func<Task> NotifyDismissed { get; set; }

            public async Task DismissAsync()
            {
                IsDismissed = true;
                if(NotifyDismissed != null)
                    await NotifyDismissed();
            }
            
            private async Task DismissInFiveSecondsAsync()
            {
                await Task.Delay(5000).ConfigureAwait(false);
                await DismissAsync();
            }
        }
        
        public enum ToastLevel
        {
            Success,
            Info,
            Warning,
            Error
        }
        
        
        
        public event Func<ToastMessage, Task> OnChange;
        
        public Task CreateInfoToastAsync(string title, string message) => CreateToastAsync(ToastLevel.Info, title, message);
        public Task CreateInfoToastAsync(string title, MarkupString message) => CreateToastAsync(ToastLevel.Info, title, message);
        public Task CreateSuccessToastAsync(string title, string message) => CreateToastAsync(ToastLevel.Success, title, message);
        public Task CreateSuccessToastAsync(string title, MarkupString message) => CreateToastAsync(ToastLevel.Success, title, message);
        public Task CreateWarningToastAsync(string title, string message) => CreateToastAsync(ToastLevel.Warning, title, message);
        public Task CreateWarningToastAsync(string title, MarkupString message) => CreateToastAsync(ToastLevel.Warning, title, message);
        public Task CreateErrorToastAsync(string title, string message) => CreateToastAsync(ToastLevel.Error, title, message);
        public Task CreateErrorToastAsync(string title, MarkupString message) => CreateToastAsync(ToastLevel.Error, title, message);

        private Task CreateToastAsync(ToastLevel toastLevel, string title, string message) =>
            CreateToastAsync(toastLevel, title, new MarkupString(HttpUtility.HtmlEncode(message)));

        private async Task CreateToastAsync(ToastLevel toastLevel, string title, MarkupString message)
        {
            var toastMessage = new ToastMessage(title, message, toastLevel);

            await NotifyChangeAsync(toastMessage);
        }
        


        private async Task NotifyChangeAsync(ToastMessage toastMessage)
        {
            if (OnChange != null)
                await OnChange(toastMessage);
        }

    }
}