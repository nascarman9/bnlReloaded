using System.Collections.Concurrent;

namespace BNLReloadedServer.ServerTypes;

public abstract class Updater
{
    private readonly BlockingCollection<Action> _updateActions = new();

    private bool BlockEnqueuing { get; set; }

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

            try
            {
                updateAction?.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        _updateActions.Dispose();
    }
    
    public bool EnqueueAction(Action func)
    {
        if (BlockEnqueuing) return false;
        _updateActions.Add(func);
        return true;
    }

    public void Stop()
    {
        _updateActions.CompleteAdding();
        BlockEnqueuing = true;
    }
}