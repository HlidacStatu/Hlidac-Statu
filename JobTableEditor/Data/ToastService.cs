using System;
using System.Web;
using System.Threading.Tasks;
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

                _ = Task.Run(async () => await DismissInFiveSeconds()).ConfigureAwait(false);
            }

            public string Id { get; } = Guid.NewGuid().ToString();
            public DateTime Created { get; } = DateTime.Now;
            public string Title { get; init; }
            public MarkupString Message { get; init; }
            public ToastLevel ToastLevel { get; init; }
            public bool IsDismissed { get; private set; }
            
            public Func<Task> NotifyDismissed { get; set; }

            public async Task Dismiss()
            {
                IsDismissed = true;
                if(NotifyDismissed != null)
                    await NotifyDismissed();
            }
            
            private async Task DismissInFiveSeconds()
            {
                await Task.Delay(5000).ConfigureAwait(false);
                await Dismiss();
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
        
        public Task CreateInfoToast(string title, string message) => CreateToast(ToastLevel.Info, title, message);
        public Task CreateInfoToast(string title, MarkupString message) => CreateToast(ToastLevel.Info, title, message);
        public Task CreateSuccessToast(string title, string message) => CreateToast(ToastLevel.Success, title, message);
        public Task CreateSuccessToast(string title, MarkupString message) => CreateToast(ToastLevel.Success, title, message);
        public Task CreateWarningToast(string title, string message) => CreateToast(ToastLevel.Warning, title, message);
        public Task CreateWarningToast(string title, MarkupString message) => CreateToast(ToastLevel.Warning, title, message);
        public Task CreateErrorToast(string title, string message) => CreateToast(ToastLevel.Error, title, message);
        public Task CreateErrorToast(string title, MarkupString message) => CreateToast(ToastLevel.Error, title, message);

        private Task CreateToast(ToastLevel toastLevel, string title, string message) =>
            CreateToast(toastLevel, title, new MarkupString(HttpUtility.HtmlEncode(message)));

        private async Task CreateToast(ToastLevel toastLevel, string title, MarkupString message)
        {
            var toastMessage = new ToastMessage(title, message, toastLevel);

            await NotifyChange(toastMessage);
        }
        
        

        private async Task NotifyChange(ToastMessage toastMessage)
        {
            if (OnChange != null)
                await OnChange(toastMessage);
        }

    }
}