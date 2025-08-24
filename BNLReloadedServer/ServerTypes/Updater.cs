using System.Collections.Concurrent;

namespace BNLReloadedServer.ServerTypes;

public abstract class Updater
{
    private readonly BlockingCollection<Delegate> _updateActions = new();

    protected Updater()
    {
        Task.Run(RunUpdater);
    }
    
    private void RunUpdater()
    {
        while (!_updateActions.IsCompleted)
        {
            Delegate? updateAction = null;
            try
            {
                updateAction = _updateActions.Take();
            }
            catch (InvalidOperationException) { }

            updateAction?.DynamicInvoke();
        }
        
        _updateActions.Dispose();
    }
    
    public void EnqueueAction(Delegate func)
    {
        _updateActions.Add(func);
    }

    public void Stop()
    {
        _updateActions.CompleteAdding();
    }
}