using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FubarDev.WebDavServer.FileSystem;
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
            var lockManager = ServiceProvider.GetRequiredService<ILockManager>();
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
            var lockManager = ServiceProvider.GetRequiredService<ILockManager>();

            var l = new Lock(
                new Uri(string.Empty, UriKind.Relative),
                new Uri("http://localhost/"),
                true,
                null,
                LockAccessType.Write,
                LockShareMode.Exclusive,
                TimeoutHeader.Infinite);
            var lr = await lockManager.LockAsync(l, ct).ConfigureAwait(false);
            Assert.True(lr.ConflictingLocks?.IsEmpty ?? true);

            var locks =
                (await lockManager
                     .GetLocksAsync(ct)
                     .ConfigureAwait(false))
                .ToList();
            Assert.Equal(1, locks.Count);
        }

        [Fact]
        public async Task TestLockRefreshAsync()
        {
            var ct = CancellationToken.None;
            var lockManager = ServiceProvider.GetRequiredService<ILockManager>();

            var l = new Lock(
                new Uri(string.Empty, UriKind.Relative),
                new Uri("http://localhost/"),
                true,
                null,
                LockAccessType.Write,
                LockShareMode.Exclusive,
                TimeSpan.FromMinutes(1));
            var lr = await lockManager.LockAsync(l, ct).ConfigureAwait(false);
            Assert.True(lr.ConflictingLocks?.IsEmpty ?? true);
            Assert.NotNull(lr.Lock);

            var oldExpiration = lr.Lock.Expiration;

            var fs = ServiceProvider.GetRequiredService<IFileSystem>();
            var ctxt = ServiceProvider.GetRequiredService<IWebDavContext>();
            //var ifHeader = new IfHeader(new []{ new IfHeaderList()});
            var ifHeader = IfHeader.Parse($"(<{lr.Lock.StateToken}>)", EntityTagComparer.Strong, ctxt);
            var lrr = await lockManager.RefreshLockAsync(fs, ifHeader, TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);
            Assert.Null(lrr.ErrorResponse);
            Assert.NotNull(lrr.RefreshedLocks);
            Assert.Equal(1, lrr.RefreshedLocks.Count);

            var refreshedLock = lrr.RefreshedLocks.Single();
            var newExpiration = refreshedLock.Expiration;
            Assert.True(oldExpiration < newExpiration, $"The {newExpiration} must come after the {oldExpiration}");
        }

        [Fact]
        public async Task TestRemoveLockAsync()
        {
            var ct = CancellationToken.None;
            var lockManager = ServiceProvider.GetRequiredService<ILockManager>();

            var l = new Lock(
                new Uri(string.Empty, UriKind.Relative),
                new Uri("http://localhost/"),
                true,
                null,
                LockAccessType.Write,
                LockShareMode.Exclusive,
                TimeoutHeader.Infinite);
            var lr = await lockManager.LockAsync(l, ct).ConfigureAwait(false);
            Assert.True(lr.ConflictingLocks?.IsEmpty ?? true);

            var lrs = await lockManager.ReleaseAsync(string.Empty, new Uri(lr.Lock.StateToken), ct);
            Assert.Equal(LockReleaseStatus.Success, lrs);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _services.Dispose();
        }
    }
}
