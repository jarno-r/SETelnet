using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TelnetTestServer
{    public class UsingSynchronizationContext : IDisposable
    {
        private SynchronizationContext oldContext;

        public UsingSynchronizationContext(SynchronizationContext newContext)
        {
            oldContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(newContext);
        }

        public void Dispose()
        {
            SynchronizationContext.SetSynchronizationContext(oldContext);
        }
    }
}
