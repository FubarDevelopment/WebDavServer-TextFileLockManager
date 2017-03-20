using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace FubarDev.WebDavServer.Locking.TextFile
{
    public class TextFileLockManager : LockManagerBase
    {
        public TextFileLockManager(ILockCleanupTask cleanupTask, ISystemClock systemClock, ILogger logger, ILockManagerOptions options = null)
            : base(cleanupTask, systemClock, logger, options)
        {
        }

        protected override Task<ILockManagerTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
