using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.JobTableEditor.Data
{
    public class ToastService 
    {
        public class ToastMessage
        {
            public ToastMessage(string title, string message, ToastLevel toastLevel)
            {
                Title = title;
                Message = message;
                ToastLevel = toastLevel;

                _ = Task.Run(async () => await DismissInFiveSeconds()).ConfigureAwait(false);
            }

            public string Id { get; } = Guid.NewGuid().ToString();
            public DateTime Created { get; } = DateTime.Now;
            public string Title { get; init; }
            public string Message { get; init; }
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
        
        public async Task CreateInfoToast(string title, string message)
        {
            await CreateToast(ToastLevel.Info, title, message);
        }
        
        public async Task CreateSuccessToast(string title, string message)
        {
            await CreateToast(ToastLevel.Success, title, message);
        }
        
        public async Task CreateWarningToast(string title, string message)
        {
            await CreateToast(ToastLevel.Warning, title, message);
        }
        
        public async Task CreateErrorToast(string title, string message)
        {
            await CreateToast(ToastLevel.Error, title, message);
        }

        private async Task CreateToast(ToastLevel toastLevel, string title, string message)
        {
            var toastMessage = new ToastMessage(title: title, message: message, toastLevel: toastLevel);

            await NotifyChange(toastMessage);
        }

        

        

        private async Task NotifyChange(ToastMessage toastMessage)
        {
            if (OnChange != null)
                await OnChange(toastMessage);
        }

    }
}