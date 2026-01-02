using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;

namespace BNLReloadedServer.Servers;

public sealed class MasterStatusHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly IMasterServerDatabase _masterDatabase;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly string _prefix;
    private Task? _listenerTask;

    public MasterStatusHttpServer(string prefix, IMasterServerDatabase masterDatabase)
    {
        _prefix = prefix.EndsWith("/") ? prefix : $"{prefix}/";
        _listener.Prefixes.Add(_prefix);
        _masterDatabase = masterDatabase;
    }

    public bool Start()
    {
        if (_listener.IsListening)
        {
            return true;
        }

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Master status HTTP server failed to start: {ex.Message}");
            return false;
        }

        _listenerTask = Task.Run(ListenAsync, _cancellation.Token);
        Console.WriteLine($"Master status HTTP server listening on {_prefix}");
        return true;
    }

    public void Stop()
    {
        _cancellation.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();

        try
        {
            _listenerTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"Master status HTTP server stopped with listener errors: {ex.Flatten().InnerException?.Message}");
        }
    }

    private async Task ListenAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (HttpListenerException ex)
            {
                if (!_cancellation.IsCancellationRequested)
                {
                    Console.WriteLine($"Master status HTTP server listener error: {ex.Message}");
                }

                break;
            }

            if (context != null)
            {
                _ = Task.Run(() => HandleRequestAsync(context), _cancellation.Token);
            }
        }
    }

    private async Task<RegionStatus> GetRegionStatusAsync(RegionInfo region, CancellationToken cancellationToken)
    {
        var status = new RegionStatus
        {
            RegionId = region.Id ?? string.Empty,
            RegionName = region.Info?.Name?.Text,
            Host = region.Host,
            Port = region.Port
        };

        if (region.Id == null)
        {
            status.Error = "region id is missing";
            return status;
        }

        var service = _masterDatabase.GetRegionServerService(region.Id);
        if (service == null)
        {
            status.Error = "region is offline";
            return status;
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(3));
            var remoteStatus = await service.RequestStatusAsync(linkedCts.Token);
            if (remoteStatus == null)
            {
                status.Error = "region returned no status";
            }
            else
            {
                status.OnlinePlayers = remoteStatus.OnlinePlayers;
                status.Queues = remoteStatus.Queues;
                status.QueuePlayers = remoteStatus.QueuePlayers;
                status.CustomGames = remoteStatus.CustomGames;
                status.ActiveGames = remoteStatus.ActiveGames;
            }
        }
        catch (OperationCanceledException)
        {
            status.Error = "region status timed out";
        }
        catch (Exception ex)
        {
            status.Error = $"region status failed: {ex.Message}";
        }

        return status;
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var regions = _masterDatabase.GetRegionServers();
            var statusTasks = regions.Select(region => GetRegionStatusAsync(region, _cancellation.Token)).ToList();
            var regionStatuses = await Task.WhenAll(statusTasks);

            var payload = JsonSerializer.Serialize(new { regions = regionStatuses });
            var buffer = Encoding.UTF8.GetBytes(payload);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, _cancellation.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Master status HTTP server request handling failed: {ex.Message}");
            if (context.Response.OutputStream.CanWrite)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        finally
        {
            try
            {
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _cancellation.Dispose();
    }
}
