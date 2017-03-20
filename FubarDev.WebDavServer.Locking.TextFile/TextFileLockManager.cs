using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

namespace FubarDev.WebDavServer.Locking.TextFile
{
    public class TextFileLockManager : LockManagerBase
    {
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _sem = new SemaphoreSlim(1);

        private readonly string _lockFileName;

        public TextFileLockManager(ILockCleanupTask cleanupTask, ISystemClock systemClock, ILogger logger, IOptions<TextFileLockManagerOptions> options)
            : base(cleanupTask, systemClock, logger, options.Value)
        {
            _logger = logger;
            _lockFileName = options.Value.LockFileName;

            AddLocksToCleanupTask();
        }

        protected override Task<ILockManagerTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            _sem.Wait(cancellationToken);

            return Task.FromResult<ILockManagerTransaction>(new TextFileTransaction(_lockFileName, _sem));
        }

        private void AddLocksToCleanupTask()
        {
            if (!File.Exists(_lockFileName))
            {
                // Load all active locks and add them to the cleanup task.
                // This ensures that locks still do expire.
                try
                {
                    var file = File.ReadAllText(_lockFileName);
                    var activeLocks = JsonConvert.DeserializeObject<ICollection<ActiveLock>>(file);
                    foreach (var activeLock in activeLocks)
                    {
                        LockCleanupTask.Add(this, activeLock);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(0, ex, "Failed to load the lock file: {0}", ex.Message);
                }
            }
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
