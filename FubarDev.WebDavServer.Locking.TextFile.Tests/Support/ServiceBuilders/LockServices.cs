using System;
using System.IO;
using System.Security.Principal;

using FubarDev.WebDavServer.FileSystem;
using FubarDev.WebDavServer.FileSystem.InMemory;
using FubarDev.WebDavServer.Props.Dead;
using FubarDev.WebDavServer.Props.Store;
using FubarDev.WebDavServer.Props.Store.InMemory;
using FubarDev.WebDavServer.Utils.UAParser;

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

            var princial = new GenericPrincipal(new GenericIdentity("anonymous", "anonymous"), new string[0]);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ILockCleanupTask, LockCleanupTask>();
            serviceCollection.AddTransient<PathTraversalEngine>();
            serviceCollection.AddScoped(sp =>
            {
                var fsf = sp.GetRequiredService<IFileSystemFactory>();
                return fsf.CreateFileSystem(princial);
            });
            serviceCollection.AddScoped(sp =>
            {
                var psf = sp.GetRequiredService<IPropertyStoreFactory>();
                var fs = sp.GetRequiredService<IFileSystem>();
                return psf.Create(fs);
            });
            serviceCollection.AddTransient<IDeadPropertyFactory, DeadPropertyFactory>();
            serviceCollection.AddSingleton<ISystemClock, SystemClock>();

            serviceCollection.Configure<TextFileLockManagerOptions>(opt =>
            {
                opt.Rounding = new DefaultLockTimeRounding(DefaultLockTimeRoundingMode.OneHundredMilliseconds);
                opt.LockFileName = _tempFileName;
            });

            serviceCollection.AddScoped<ILockManager, TextFileLockManager>();
            serviceCollection.AddScoped<IFileSystemFactory, InMemoryFileSystemFactory>();
            serviceCollection.AddScoped<IPropertyStoreFactory, InMemoryPropertyStoreFactory>();

            serviceCollection.AddScoped<IWebDavContext>(sp => new Context(princial));

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

        private class Context : IWebDavContext
        {
            public Context(IPrincipal principal)
            {
                User = User;
            }

            public string RequestProtocol { get; } = "HTTP/1.1";
            public Uri RelativeRequestUrl { get; } = new Uri("/", UriKind.Relative);
            public Uri AbsoluteRequestUrl { get; } = new Uri("http://localhost/");
            public Uri BaseUrl { get; } = new Uri("http://localhost/");
            public Uri RootUrl { get; } = new Uri("http://localhost/");
            public IUAParserOutput DetectedClient { get; } = Parser.GetDefault().Parse("Test");

            public IWebDavRequestHeaders RequestHeaders => throw new NotSupportedException();

            public IPrincipal User { get; }
            public IWebDavDispatcher Dispatcher => throw new NotSupportedException();
        }
    }
}
