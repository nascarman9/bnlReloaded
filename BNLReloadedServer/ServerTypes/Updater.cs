using System.Threading.Channels;

namespace BNLReloadedServer.ServerTypes;

public abstract class Updater
{
    private readonly Channel<Action> _updateActions = Channel.CreateUnbounded<Action>();

    protected Updater() => _ = RunUpdater(_updateActions.Reader);

    private static async Task RunUpdater(ChannelReader<Action> actions)
    {
        try
        {
            await foreach (var action in actions.ReadAllAsync())
            {
                try
                {
                    action();
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public bool EnqueueAction(Action func) => _updateActions.Writer.TryWrite(func);

    public void Stop() => _updateActions.Writer.TryComplete();
}