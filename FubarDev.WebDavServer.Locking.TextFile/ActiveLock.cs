using System;
using System.Xml.Linq;

namespace FubarDev.WebDavServer.Locking.TextFile
{
    public class ActiveLock : IActiveLock
    {
        public string Path { get; set; }

        public string Href { get; set; }

        public bool Recursive { get; set; }

        public string AccessType { get; set; }

        public string ShareMode { get; set; }

        public TimeSpan Timeout { get; set; }

        public string StateToken { get; set; }

        public DateTime Issued { get; set; }

        public DateTime? LastRefresh { get; set; }

        public DateTime Expiration { get; set; }

        public XElement Owner { get; set; }

        public XElement GetOwner()
        {
            return Owner;
        }
    }
}
