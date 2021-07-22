using System;

namespace HlidacStatu.Lib.OCR.Api
{
    public partial class Client
    {
        public class ProgressWithName
        {
            public decimal Value { get; set; } = 0;
            public string Name { get; set; } = "";

            public event EventHandler<ProgressWithName> ProgressChanged;

            private DateTime _lastSetProgress = DateTime.MinValue;
            private TimeSpan _setProgressMinInterval = TimeSpan.FromSeconds(5);
            public void SetProgress(decimal progress, string progressName)
            {
                if (progress < 0)
                    progress = 0;
                if (progress > 1)
                    progress = 1;
                Value = progress;
                Name = progressName;

                if ((DateTime.Now - _lastSetProgress) > _setProgressMinInterval)
                {
                    _lastSetProgress = DateTime.Now;
                    //ProgressChanged?.Invoke(this, this);
                    OnProgressChanged(this);
                }
            }

            protected virtual void OnProgressChanged(ProgressWithName e)
            {
                EventHandler<ProgressWithName> handler = ProgressChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }

        }
    }
}