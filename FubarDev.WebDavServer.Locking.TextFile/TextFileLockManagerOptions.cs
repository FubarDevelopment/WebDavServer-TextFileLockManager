namespace FubarDev.WebDavServer.Locking.TextFile
{
    public class TextFileLockManagerOptions : ILockManagerOptions
    {
        public string LockFileName { get; set; }

        public ILockTimeRounding Rounding { get; set; } = new DefaultLockTimeRounding(DefaultLockTimeRoundingMode.OneSecond);
    }
}
