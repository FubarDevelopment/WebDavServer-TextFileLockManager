using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FubarDev.WebDavServer.Locking.TextFile
{
    public class TextFileLockManager : LockManagerBase
    {
        public TextFileLockManager(ILockCleanupTask cleanupTask, ISystemClock systemClock, ILogger logger, IOptions<TextFileLockManagerOptions> options)
            : base(cleanupTask, systemClock, logger, options.Value)
        {
        }

        protected override Task<ILockManagerTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
