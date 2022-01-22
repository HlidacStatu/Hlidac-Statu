using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace HlidacStatu.JobTableEditor.Data
{
    public class ToastService 
    {
        public class ToastMessage
        {
            public string Id { get; } = Guid.NewGuid().ToString();
            public DateTime Created { get; } = DateTime.Now;
            public string Title { get; init; }
            public string Message { get; init; }
            public ToastLevel ToastLevel { get; init; }
            public bool IsDismissed { get; private set; }
            
            public void Dismiss()
            {
                IsDismissed = true;
            }
        }
        
        public enum ToastLevel
        {
            Success,
            Info,
            Warning,
            Error
        }
        
        private Timer _cleaningTimer = new Timer(60 * 1000) // once per minute
        {
            AutoReset = true,
        }; 
        
        private ConcurrentDictionary<string, ToastMessage> ToastMessages { get; } = new();
        
        public event Func<List<ToastMessage>, Task> OnChange;
        
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
            var toastMessage = new ToastMessage
            {
                Title = title,
                Message = message,
                ToastLevel = toastLevel,
            };
            
            ToastMessages.TryAdd(toastMessage.Id, toastMessage);

            await NotifyChange();
        }

        public ToastService()
        {
            _cleaningTimer.Elapsed += CleaningTimerTick;
            _cleaningTimer.Start();
        }

        private void CleaningTimerTick(object sender, ElapsedEventArgs e)
        {
            if (ToastMessages.Count == 0)
                return;
            
            var toastIdsToRemove = ToastMessages.Values.Where(t => t.IsDismissed).Select(t => t.Id).ToList();
            foreach (var toastIdToRemove in toastIdsToRemove)
            {
                ToastMessages.TryRemove(toastIdToRemove, out _);
            }
        }

        private async Task NotifyChange()
        {
            if (OnChange != null)
                await OnChange(ToastMessages.Values.OrderByDescending(t => t.Created).ToList());
        }

    }
}