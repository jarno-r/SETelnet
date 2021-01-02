using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelnetPlugin
{
    /*
     * A SynchronizationContext is needed to schedule continuations from await expressions.
     * Space Engineers doesn't seem to have a working SynchronizationContext for the main thread.
     * MySynchronizationContext is used by this plugin to run all await continuations in the main thread during Update().
     * Since Space Engineers might use its own MySynchronizationContext at some point, it is best to replace the original
     * SynchronizationContext only temporarily, when making calls to async functions. UsingSynchronizationContext is a helper struct to facilitate this.
     * 
     * I.e. whenver calling an async method from a synchronous context, wrap the call like this:
     * using(TelnetPlugin.UsingSC()) {
     *  // call to async method
     * }
     * 
     * Calls inside async methods to other async methods do not need to be wrapped like this,
     * provided that synchronous methods always do.
     */
    public class MySynchronizationContext : SynchronizationContext
    {
        public struct UsingSynchronizationContext : IDisposable
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

        public static UsingSynchronizationContext Using() { return TelnetPlugin.UsingSC(); }

        private Queue<(SendOrPostCallback, object)> queue = new Queue<(SendOrPostCallback, object)>();

        public MySynchronizationContext()
        {
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            queue.Enqueue((d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotImplementedException("Send");
        }

        public void Run()
        {
            while (queue.Count > 0)
            {
                var (d, s) = queue.Dequeue();
                d(s);
            }
        }
    }
}
