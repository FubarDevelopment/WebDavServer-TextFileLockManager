using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.WebDavServer.Locking.TextFile.Tests.Support.ServiceBuilders;
using FubarDev.WebDavServer.Model.Headers;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace FubarDev.WebDavServer.Locking.TextFile.Tests
{
    public class TextFileLockManagerTests : IDisposable
    {
        private readonly LockServices _services = new LockServices();

        private readonly IServiceScope _scope;

        public TextFileLockManagerTests()
        {
            _scope = _services.ServiceProvider.CreateScope();
        }

        protected IServiceProvider ServiceProvider => _scope.ServiceProvider;

        [Fact]
        public async Task TestGetEmptyLocksAsync()
        {
            var lockManager = ServiceProvider.GetService<ILockManager>();
            var locks =
                (await lockManager
                     .GetLocksAsync(CancellationToken.None)
                     .ConfigureAwait(false))
                .ToList();
            Assert.Equal(0, locks.Count);
        }

        [Fact]
        public async Task TestAddLockAsync()
        {
            var ct = CancellationToken.None;
            var lockManager = ServiceProvider.GetService<ILockManager>();

            var l = new Lock(
                new Uri(string.Empty, UriKind.Relative),
                new Uri("http://localhost/"),
                true,
                null,
                LockAccessType.Write,
                LockShareMode.Exclusive,
                TimeoutHeader.Infinite);
            var lr = await lockManager.LockAsync(l, ct);
            Assert.True(lr.ConflictingLocks?.IsEmpty ?? true);

            var locks =
                (await lockManager
                     .GetLocksAsync(ct)
                     .ConfigureAwait(false))
                .ToList();
            Assert.Equal(1, locks.Count);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _services.Dispose();
        }
    }
}
