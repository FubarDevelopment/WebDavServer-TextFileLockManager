﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

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

        public TextFileLockManager(ILockCleanupTask cleanupTask, ISystemClock systemClock, ILogger<TextFileLockManager> logger, IOptions<TextFileLockManagerOptions> options)
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
                return;

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

        private class TextFileTransaction : ILockManagerTransaction
        {
            private static readonly IMapper _mapper = new MapperConfiguration(ConfigureMapper).CreateMapper();

            private readonly string _lockFileName;

            private readonly SemaphoreSlim _semaphore;

            private readonly IDictionary<string, ActiveLock> _locks;

            public TextFileTransaction(string lockFileName, SemaphoreSlim semaphore)
            {
                _lockFileName = lockFileName;
                _semaphore = semaphore;
                _locks = Load(lockFileName);
            }

            public Task<IReadOnlyCollection<IActiveLock>> GetActiveLocksAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult<IReadOnlyCollection<IActiveLock>>(_locks.Values.Cast<IActiveLock>().ToList());
            }

            public Task<bool> AddAsync(IActiveLock activeLock, CancellationToken cancellationToken)
            {
                var isNew = !_locks.ContainsKey(activeLock.StateToken);
                _locks[activeLock.StateToken] = ToActiveLock(activeLock);
                return Task.FromResult(isNew);
            }

            public Task<bool> UpdateAsync(IActiveLock activeLock, CancellationToken cancellationToken)
            {
                var isUpdating = _locks.ContainsKey(activeLock.StateToken);
                _locks[activeLock.StateToken] = ToActiveLock(activeLock);
                return Task.FromResult(isUpdating);
            }

            public Task<bool> RemoveAsync(string stateToken, CancellationToken cancellationToken)
            {
                var exists = _locks.Remove(stateToken);
                return Task.FromResult(exists);
            }

            public Task<IActiveLock> GetAsync(string stateToken, CancellationToken cancellationToken)
            {
                ActiveLock activeLock;
                if (!_locks.TryGetValue(stateToken, out activeLock))
                    return null;
                return Task.FromResult<IActiveLock>(activeLock);
            }

            public Task CommitAsync(CancellationToken cancellationToken)
            {
                var json = JsonConvert.SerializeObject(_locks.Values);
                File.WriteAllText(_lockFileName, json);
                return Task.FromResult(0);
            }

            public void Dispose()
            {
                _semaphore.Release();
            }

            private static IDictionary<string, ActiveLock> Load(string lockFileName)
            {
                try
                {
                    if (File.Exists(lockFileName))
                    {
                        var text = File.ReadAllText(lockFileName);
                        var locks = JsonConvert.DeserializeObject<List<ActiveLock>>(text);
                        return locks.ToDictionary(x => x.StateToken, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // Ignore errors and just return an empty dictionary
                }

                return new Dictionary<string, ActiveLock>(StringComparer.OrdinalIgnoreCase);
            }

            private static ActiveLock ToActiveLock(IActiveLock activeLock)
            {
                if (activeLock is ActiveLock l)
                    return l;
                return _mapper.Map<ActiveLock>(activeLock);
            }

            private static void ConfigureMapper(IMapperConfigurationExpression cfg)
            {
                cfg.CreateMap<IActiveLock, ActiveLock>()
                   .ForMember(x => x.Owner, opt => opt.MapFrom(x => x.GetOwner()));
            }
        }
    }
}
