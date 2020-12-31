using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TelnetPlugin
{
    public class MySynchronizationContext : SynchronizationContext
    {
        private static Lazy<MySynchronizationContext> lazyInstance = new Lazy<MySynchronizationContext>(() => new MySynchronizationContext());
        public static MySynchronizationContext Instance => lazyInstance.Value;

        private Queue<(SendOrPostCallback, object)> queue = new Queue<(SendOrPostCallback, object)>();

        private MySynchronizationContext()
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

        private void DoRun()
        {
            while (queue.Count > 0)
            {
                var (d, s) = queue.Dequeue();
                d(s);
            }
        }

        public static void Run()
        {
            Instance.DoRun();
        }
    }
}
