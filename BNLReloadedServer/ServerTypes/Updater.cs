using System.Collections.Concurrent;

namespace BNLReloadedServer.ServerTypes;

public abstract class Updater
{
    private readonly BlockingCollection<Action> _updateActions = new();

    protected Updater()
    {
        Task.Run(RunUpdater);
    }
    
    private void RunUpdater()
    {
        while (!_updateActions.IsCompleted)
        {
            Action? updateAction = null;
            try
            {
                updateAction = _updateActions.Take();
            }
            catch (InvalidOperationException) { }

            updateAction?.Invoke();
        }
        
        _updateActions.Dispose();
    }
    
    public void EnqueueAction(Action func)
    {
        _updateActions.Add(func);
    }

    public void Stop()
    {
        _updateActions.CompleteAdding();
    }
}