using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JustSaying.v2
{
    public abstract class DeferredActionBuilder
    {
        private readonly Queue<Func<Task>> _buildActions = new Queue<Func<Task>>();

        public async Task ExecuteActionsAsync()
        {
            while (_buildActions.Any())
            {
                await _buildActions.Dequeue().Invoke();
            }
        }

        protected void QueueAction(Func<Task> buildAction) => _buildActions.Enqueue(buildAction);
    }
}