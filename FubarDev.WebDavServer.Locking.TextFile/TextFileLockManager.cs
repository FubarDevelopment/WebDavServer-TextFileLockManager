using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FubarDev.WebDavServer.Locking.TextFile
{
    public class TextFileLockManager : LockManagerBase
    {
        private readonly SemaphoreSlim _sem = new SemaphoreSlim(1);

        private readonly string _lockFileName;

        public TextFileLockManager(ILockCleanupTask cleanupTask, ISystemClock systemClock, ILogger logger, IOptions<TextFileLockManagerOptions> options)
            : base(cleanupTask, systemClock, logger, options.Value)
        {
            _lockFileName = options.Value.LockFileName;
        }

        protected override Task<ILockManagerTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            _sem.Wait(cancellationToken);
            return Task.FromResult<ILockManagerTransaction>(new TextFileTransaction(_lockFileName, _sem));
        }

        private class TextFileTransaction : ILockManagerTransaction
        {
            private readonly string _lockFileName;

            private readonly SemaphoreSlim _semaphore;

            public TextFileTransaction(string lockFileName, SemaphoreSlim semaphore)
            {
                _lockFileName = lockFileName;
                _semaphore = semaphore;
            }

            public Task<IReadOnlyCollection<IActiveLock>> GetActiveLocksAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<bool> AddAsync(IActiveLock activeLock, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<bool> UpdateAsync(IActiveLock activeLock, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<bool> RemoveAsync(string stateToken, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<IActiveLock> GetAsync(string stateToken, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task CommitAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
