using System;
using System.IO;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FubarDev.WebDavServer.Locking.TextFile.Tests.Support.ServiceBuilders
{
    public class LockServices : IDisposable
    {
        private readonly string _tempFileName;

        public LockServices()
        {
            _tempFileName = Path.GetTempFileName();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.AddScoped<ISystemClock, TestSystemClock>();
            serviceCollection.Configure<TextFileLockManagerOptions>(opt =>
            {
                opt.Rounding = new DefaultLockTimeRounding(DefaultLockTimeRoundingMode.OneHundredMilliseconds);
                opt.LockFileName = _tempFileName;
            });
            serviceCollection.AddTransient<ILockCleanupTask, LockCleanupTask>();
            serviceCollection.AddTransient<ILockManager, TextFileLockManager>();
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var loggerFactory = ServiceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddDebug(LogLevel.Trace);
        }

        public IServiceProvider ServiceProvider { get; }

        public string LockFileName => _tempFileName;

        public void Dispose()
        {
            File.Delete(_tempFileName);
        }
    }
}
