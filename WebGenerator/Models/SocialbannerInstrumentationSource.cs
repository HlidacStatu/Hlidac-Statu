namespace HlidacStatu.WebGenerator.Models
{
    // Copyright The OpenTelemetry Authors
    // SPDX-License-Identifier: Apache-2.0


    using System.Diagnostics;
    using System.Diagnostics.Metrics;

    /// <summary>
    /// It is recommended to use a custom type to hold references for
    /// ActivitySource and Instruments. This avoids possible type collisions
    /// with other components in the DI container.
    /// </summary>
    public sealed class SocialbannerInstrumentationSource : IDisposable
    {
        internal const string ActivitySourceName = "webgenerator";
        internal const string MeterName = "socialbanner";
        private readonly Meter meter;

        public SocialbannerInstrumentationSource()
        {
            string? version = typeof(SocialbannerInstrumentationSource).Assembly.GetName().Version?.ToString();
            this.ActivitySource = new ActivitySource(ActivitySourceName, version);
            this.meter = new Meter(MeterName, version);
            this.Requested = this.meter.CreateCounter<long>("socialbanner.requested", description: "When socialbanner was requested.");
            this.Generated = this.meter.CreateCounter<long>("socialbanner.generated", description: "When socialbanner was generated from screenshots");
            this.FromCache = this.meter.CreateCounter<long>("socialbanner.from_cache", description: "When socialbanner was loaded from cache");
        }

        public ActivitySource ActivitySource { get; }

        public Counter<long> Requested { get; }
        public Counter<long> Generated { get; }
        public Counter<long> FromCache { get; }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.meter.Dispose();
        }
    }
}
