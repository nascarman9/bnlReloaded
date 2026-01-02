using System.Collections.Concurrent;
using System.Text;
using System.Timers;
using BNLReloadedServer.BaseTypes;
using BNLReloadedServer.Database;
using BNLReloadedServer.Servers;
using BNLReloadedServer.Service;
using Moserware.Skills;
using NetCoreServer;
using NumberPartitioning;
using Timer = System.Timers.Timer;

namespace BNLReloadedServer.ServerTypes;

public class Matchmaker(AsyncTaskTcpServer server)
{
    private const double MaxSecondWaitTimeWithFullLobby = 60;
    private const double MaxSecondWaitTimeTillShorthandedGame = 120;
    private const double MaxSecondWaitTimeTillForcedInLobby = 420;
    private const double MinimumMatchQuality = 0.3;
    private const double SquadBoost = 1.07;
    private const uint MaxThresholdOfShorthandedness = 4;
    private const double QueueCheckSeconds = 30;
    private const uint NumBalChecks = 8;
    private const int AbortDelay = 2000;
    private const bool ShowQueueMessages = true;

    private enum PopStatus
    {
        None,
        Match,
        Backfill,
        PopFailed
    }

    private class QueueData
    {
        public Key GameModeKey { get; init; }
        public List<PlayerQueueData> Players { get; set; } = [];
        public ConcurrentDictionary<uint, bool> DoBackfilling { get; } = new();
        public required SessionSender Sender { get; init; }
        public required SessionSender MatchSender1 { get; init; }
        public required SessionSender MatchSender2 { get; init; }
        public required IServiceMatchmaker ServiceMatchmaker { get; init; }
        public required IServiceMatchmaker PopServiceMatchmaker1 { get; init; }
        public required IServiceMatchmaker PopServiceMatchmaker2 { get; init; }
        public ConcurrentDictionary<uint, bool> AcceptVotes1 { get; } = new();
        public ConcurrentDictionary<uint, bool> AcceptVotes2 { get; } = new();
        public List<PlayerQueueData>? Team1 { get; set; }
        public List<PlayerQueueData>? Team2 { get; set; }
        public DateTimeOffset LastJoinTime { get; set; }
        public PopStatus IsPop { get; set; }
        public BackfillInfo? ActiveBackfillInfo { get; set; }
        public CancellationTokenSource? LoopCanceler { get; set; }
        public Task? QueueLoop { get; set; }
        public Timer? QueueTimer { get; set; }
        public ulong? ConfTime { get; set; }
        
        public CardGameMode GameModeCard => Databases.Catalogue.GetCard<CardGameMode>(GameModeKey);

        public bool EnoughForPop()
        {
            var maxPlayers = GameModeCard.PlayersPerTeam * 2;
            return (DateTimeOffset.Now - LastJoinTime).TotalSeconds > MaxSecondWaitTimeTillShorthandedGame &&
                GameModeKey != CatalogueHelper.ModeRanked.Key &&
                Databases.RegionServerDatabase.GetActiveGamesCount(GameModeKey) <= 0
                    ? Players.Count >= maxPlayers - MaxThresholdOfShorthandedness
                    : Players.Count >= maxPlayers;
        }
    }
    
    private record QueueGrouping(List<PlayerQueueData> Players, double RatingMean, DateTimeOffset MinJoinTime);
    
    private record BackfillInfo(Dictionary<uint, Rating> Team1, Dictionary<uint, Rating> Team2, string GameInstanceId);
    
    private readonly ConcurrentDictionary<Key, QueueData> _queues = new();

    public int GetQueueCount(Key gameModeKey)
    {
        return _queues.TryGetValue(gameModeKey, out var queue) ? queue.Players.Count : 0;
    }

    public Dictionary<Key, List<PlayerQueueData>> GetQueues()
    {
        return _queues.ToDictionary(queue => queue.Key, queue => queue.Value.Players.ToList());
    }

    private void StartQueue(Key gameModeKey)
    {
        var sender = new SessionSender(server);
        var matchSender = new SessionSender(server);
        var matchSender2 = new SessionSender(server);
        var queue = new QueueData
        {
            GameModeKey = gameModeKey,
            Sender = sender,
            MatchSender1 = matchSender,
            MatchSender2 = matchSender2,
            ServiceMatchmaker = new ServiceMatchmaker(sender),
            PopServiceMatchmaker1 = new ServiceMatchmaker(matchSender),
            PopServiceMatchmaker2 = new ServiceMatchmaker(matchSender2),
            LastJoinTime = DateTimeOffset.Now
        };
        _queues[gameModeKey] = queue;
        queue.QueueLoop = RunQueueCheck(queue);
    }

    private void StopQueue(Key gameModeKey)
    {
        _queues.Remove(gameModeKey, out var queue);
        try
        {
            queue?.LoopCanceler?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        queue?.QueueLoop = null;
        queue?.ConfTime = null;
        queue?.AcceptVotes1.Clear();
        queue?.AcceptVotes2.Clear();
        queue?.MatchSender1.UnsubscribeAll();
        queue?.MatchSender2.UnsubscribeAll();
        queue?.Sender.UnsubscribeAll();
        queue?.IsPop = PopStatus.None;
        queue?.Players.Clear();
    }

    public void AddPlayer(Key gameModeKey, uint playerId, Guid guid, Rating rating, ulong? squadId, IServiceMatchmaker matchmakerService)
    {
        if (!_queues.ContainsKey(gameModeKey))
        {
            StartQueue(gameModeKey);
        }
        
        if (!_queues.TryGetValue(gameModeKey, out var queue)) return;
        queue.Players.RemoveAll(p => p.PlayerId == playerId);
        queue.Players.Add(new PlayerQueueData(playerId, guid, rating, DateTimeOffset.Now, squadId));
        queue.LastJoinTime = DateTimeOffset.Now;
        queue.Sender.Subscribe(guid);
        matchmakerService.SendMatchmakerUpdate(new MatchmakerUpdate
        {
            State = new MatchmakerState
            {
                State = MatchmakerStateType.InQueue,
                QueueGameMode = gameModeKey
            }
        });
        
        SendQueueUpdate(queue.ServiceMatchmaker, new MatchmakerUpdate
        {
            PlayersInQueue = queue.Players.Count
        });
        
        QueueCheck(queue);
    }

    public void RemovePlayer(uint playerId, IServiceMatchmaker? matchmakerService)
    {
        foreach (var queue in _queues.Values.Where(x => x.Players.Any(p => p.PlayerId == playerId)).ToList())
        {
            var player = queue.Players.First(p => p.PlayerId == playerId);
            queue.Sender.Unsubscribe(player.PlayerGuid);
            queue.MatchSender1.Unsubscribe(player.PlayerGuid);
            queue.MatchSender2.Unsubscribe(player.PlayerGuid);
            queue.Players.Remove(player);
            queue.DoBackfilling.TryRemove(playerId, out _);
            queue.ServiceMatchmaker.SendQueueLeft(playerId);
            
            matchmakerService?.SendMatchmakerUpdate(new MatchmakerUpdate
            {
                State = new MatchmakerState
                {
                    State = MatchmakerStateType.None
                }
            });
            
            SendQueueUpdate(queue.ServiceMatchmaker, new MatchmakerUpdate
            {
                PlayersInQueue = queue.Players.Count
            });
            

            if (queue.Players.Count == 0)
            {
                StopQueue(queue.GameModeKey);
            }
        }
    }

    public void RemoveSquad(ulong squadId, List<IServiceMatchmaker> serviceMatchmakers)
    {
        foreach (var queue in _queues.Values.Where(x => x.Players.Any(p => p.SquadId == squadId)).ToList())
        {
            var players = queue.Players.Where(p => p.SquadId == squadId).ToList();
            players.ForEach(p =>
            {
                queue.Sender.Unsubscribe(p.PlayerGuid);
                queue.MatchSender1.Unsubscribe(p.PlayerGuid);
                queue.MatchSender2.Unsubscribe(p.PlayerGuid);
                queue.Players.Remove(p);
                queue.DoBackfilling.TryRemove(p.PlayerId, out _);
                queue.ServiceMatchmaker.SendQueueLeft(p.PlayerId);
            });
            
            serviceMatchmakers.ForEach(m => m.SendMatchmakerUpdate(new MatchmakerUpdate
            {
                State = new MatchmakerState
                {
                    State = MatchmakerStateType.None
                }
            }));
            
            SendQueueUpdate(queue.ServiceMatchmaker, new MatchmakerUpdate
            {
                PlayersInQueue = queue.Players.Count
            });
            
            if (queue.Players.Count == 0)
            {
                StopQueue(queue.GameModeKey);
            }
        }
    }

    public void SetDoBackfilling(uint playerId, bool value)
    {
        foreach (var queue in _queues.Values.Where(x => x.Players.Any(p => p.PlayerId == playerId)).ToList())
        {
            queue.DoBackfilling[playerId] = value;
        }
    }

    public void ForceStartGame(uint playerId)
    {
        foreach (var queue in _queues.Values.Where(x => x.Players.Any(p => p.PlayerId == playerId)).ToList())
        {
            if (queue.IsPop is not PopStatus.None)
            {
                continue;
            }

            if (queue.Players.Count < 1)
                return;

            queue.IsPop = PopStatus.Match;
            var balance = queue.Players.Count == 1 ? [[queue.Players.First()], []] : DoQueueBalance(queue, true);

            if (balance == null)
            {
                queue.IsPop = PopStatus.None;
                continue;
            }
            
            queue.Team1 = balance[0];
            queue.Team2 = balance[1];
            foreach (var player in balance[0].Union(balance[1]))
            {
                queue.MatchSender1.Subscribe(player.PlayerGuid);
            }
                
            SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
            {
                State = new MatchmakerState
                {
                    State = MatchmakerStateType.None
                }
            });
                
            foreach (var p in queue.Team1.Union(queue.Team2))
            {
                RemovePlayer(p.PlayerId, null);
            }
                        
            queue.AcceptVotes1.Clear();
            queue.MatchSender1.UnsubscribeAll();
            Databases.RegionServerDatabase.StartGameFromMatchmaker(queue.GameModeCard, queue.Team1.ToList(),
                queue.Team2.ToList());
            queue.Team1.Clear();
            queue.Team2.Clear();
            queue.Team1 = null;
            queue.Team2 = null;
                        
            queue.IsPop = PopStatus.None;
        }
    }

    private static (PlayerQueueData? Team1Backfill, PlayerQueueData? Team2Backfill) DoBackfillBalance(QueueData queue, double minQuality,
        BackfillInfo backfillInfo)
    {
        var validPlayers = queue.Players
            .Where(p => p.SquadId is null && queue.DoBackfilling.GetValueOrDefault(p.PlayerId)).ToList();
        if (validPlayers.Count == 0)
        {
            return (null, null);
        }

        var team1Needs = queue.GameModeCard.PlayersPerTeam - backfillInfo.Team1.Values.Count;
        var team2Needs = queue.GameModeCard.PlayersPerTeam - backfillInfo.Team2.Values.Count;

        var checkTeam1 = true;
        var checkTeam2 = true;

        if (team1Needs <= 0 && team2Needs <= 0)
        {
            return (null, null);
        }
        
        if (team1Needs > team2Needs)
        {
            checkTeam2 = false;
        }
        else if (team1Needs < team2Needs)
        {
            checkTeam1 = false;
        }

        var (p1, p2, quality) = validPlayers.AsParallel().Select(player =>
        {
            var otherPlayers = validPlayers.Except([player]).ToList();
            PlayerQueueData? otherPlayer;
            double quality;
            switch (checkTeam1, checkTeam2)
            {
                case (true, true):
                    (otherPlayer, quality) = otherPlayers.DefaultIfEmpty()
                        .Select(p =>
                        {
                            if (p is null)
                                return (null, 0);
                            var team1 = backfillInfo.Team1.ToDictionary(k => new Player<uint>(k.Key), v => v.Value);
                            team1.Add(new Player<uint>(player.PlayerId), player.Rating);
                            var team2 = backfillInfo.Team2.ToDictionary(k => new Player<uint>(k.Key), v => v.Value);
                            team2.Add(new Player<uint>(p.PlayerId), p.Rating);
                            return (p,
                                TrueSkillCalculator.CalculateMatchQuality(Databases.DefaultGameInfo, [team1, team2]));
                        })
                        .MaxBy(r => r.Item2);
                    break;

                case (true, false):
                {
                    var team1 = backfillInfo.Team1.ToDictionary(k => new Player<uint>(k.Key), v => v.Value);
                    team1.Add(new Player<uint>(player.PlayerId), player.Rating);
                    var team2 = backfillInfo.Team2.ToDictionary(k => new Player<uint>(k.Key), v => v.Value);
                    (otherPlayer, quality) = (null,
                        TrueSkillCalculator.CalculateMatchQuality(Databases.DefaultGameInfo, [team1, team2]));
                    break;
                }

                case (false, true):
                {
                    var team1 = backfillInfo.Team1.ToDictionary(k => new Player<uint>(k.Key), v => v.Value);
                    var team2 = backfillInfo.Team2.ToDictionary(k => new Player<uint>(k.Key), v => v.Value);
                    team2.Add(new Player<uint>(player.PlayerId), player.Rating);
                    (otherPlayer, quality) = (null,
                        TrueSkillCalculator.CalculateMatchQuality(Databases.DefaultGameInfo, [team1, team2]));
                    break;
                }

                default:
                    (otherPlayer, quality) = (null, 0);
                    break;
            }
            
            return (player, otherPlayer, quality);
        }).MaxBy(q => q.quality);

        if (quality >= minQuality)
        {
            return (checkTeam1, checkTeam2) switch
            {
                (true, true) => (p1, p2),
                (true, false) => (p1, null),
                (false, true) => (null, p1),
                _ => (null, null)
            };
        }

        return (null, null);
    }

    private static List<List<PlayerQueueData>>? DoQueueBalance(QueueData queue, bool force = false)
    {
        var minQuality = (DateTimeOffset.Now - queue.LastJoinTime).TotalSeconds > MaxSecondWaitTimeWithFullLobby || force
            ? 0
            : MinimumMatchQuality;
        
        var playersForQueue = Math.Min((queue.Players.Count >> 1) << 1, queue.GameModeCard.PlayersPerTeam * 2);
        var players = queue.Players.ToList();
        if (players.Count == 0) return null;
        
        var groupings = players.Where(p => p.SquadId is not null)
            .GroupBy(p => p.SquadId, p => p, (_, squadMembers) =>
            {
                var members = squadMembers.ToList();
                return new QueueGrouping(members, members.Sum(p => p.Rating.Mean) * SquadBoost, members.Min(p => p.JoinTime));
            })
            .ToList();
        groupings.AddRange(players.Where(p => p.SquadId is null)
            .Select(player => new QueueGrouping([player], player.Rating.Mean, player.JoinTime)));

        // Sort by rating
        groupings.Sort((x, y) => x.RatingMean.CompareTo(y.RatingMean));
        
        // Determine what players need to be in the next match
        var guaranteedPlayers = groupings.Where(g =>
            (DateTimeOffset.Now - g.MinJoinTime).TotalSeconds > MaxSecondWaitTimeTillForcedInLobby).ToList();
        
        var nonGuaranteedPlayers = groupings.Except(guaranteedPlayers).ToList();
        
        IEnumerable<ICollection<QueueGrouping>> playerPool;
        var guaranteedPlayersSum = guaranteedPlayers.Sum(p => p.Players.Count);
        var nonGuaranteedPlayersSum = nonGuaranteedPlayers.Sum(p => p.Players.Count);
        if (guaranteedPlayersSum >= playersForQueue)
        {
            if (guaranteedPlayersSum == playersForQueue)
            {
                playerPool = [guaranteedPlayers];
            }
            else
            {
                var flippedPlayers = guaranteedPlayers.ToList();
                flippedPlayers.Reverse();
                var topMatching = flippedPlayers.Aggregate(new List<QueueGrouping>(),
                    (queuePlayers, nextSquad) =>
                    {
                        if (nextSquad.Players.Count <= playersForQueue - queuePlayers.Sum(p => p.Players.Count))
                        {
                            queuePlayers.Add(nextSquad);
                        }

                        return queuePlayers;
                    },
                    res => res);
                topMatching.Reverse();
                
                var bottomMatching = guaranteedPlayers.Aggregate(new List<QueueGrouping>(),
                    (queuePlayers, nextSquad) =>
                    {
                        if (nextSquad.Players.Count <= playersForQueue - queuePlayers.Sum(p => p.Players.Count))
                        {
                            queuePlayers.Add(nextSquad);
                        }

                        return queuePlayers;
                    },
                    res => res);
                
                playerPool = [topMatching, bottomMatching];
                for (var i = 0; i < NumBalChecks; i++)
                {
                    var randPlayers = guaranteedPlayers.Shuffle().Aggregate(new List<QueueGrouping>(),
                        (queuePlayers, nextSquad) =>
                        {
                            if (nextSquad.Players.Count <= playersForQueue - queuePlayers.Sum(p => p.Players.Count))
                            {
                                queuePlayers.Add(nextSquad);
                            }

                            return queuePlayers;
                        },
                        res => res);
                    randPlayers.Sort((x, y) => x.RatingMean.CompareTo(y.RatingMean));
                    playerPool = playerPool.Append(randPlayers);
                }
            }
        }
        else
        {
            var playerDiff = playersForQueue - guaranteedPlayersSum;
            if (nonGuaranteedPlayersSum == playerDiff)
            {
                playerPool = [groupings];
            }
            else
            {
                var flippedPlayers = nonGuaranteedPlayers.ToList();
                flippedPlayers.Reverse();
                var topMatching = flippedPlayers.Aggregate(guaranteedPlayers,
                    (queuePlayers, nextSquad) =>
                    {
                        if (nextSquad.Players.Count <= playersForQueue - queuePlayers.Sum(p => p.Players.Count))
                        {
                            queuePlayers.Add(nextSquad);
                        }

                        return queuePlayers;
                    },
                    res => res);
                topMatching.Sort((x, y) => x.RatingMean.CompareTo(y.RatingMean));
                
                var bottomMatching = nonGuaranteedPlayers.Aggregate(guaranteedPlayers,
                    (queuePlayers, nextSquad) =>
                    {
                        if (nextSquad.Players.Count <= playersForQueue - queuePlayers.Sum(p => p.Players.Count))
                        {
                            queuePlayers.Add(nextSquad);
                        }

                        return queuePlayers;
                    },
                    res => res);
                bottomMatching.Sort((x, y) => x.RatingMean.CompareTo(y.RatingMean));
                
                playerPool = [topMatching, bottomMatching];
                for (var i = 0; i < NumBalChecks; i++)
                {
                    var randPlayers = nonGuaranteedPlayers.Shuffle().Aggregate(guaranteedPlayers,
                        (queuePlayers, nextSquad) =>
                        {
                            if (nextSquad.Players.Count <= playersForQueue - queuePlayers.Sum(p => p.Players.Count))
                            {
                                queuePlayers.Add(nextSquad);
                            }

                            return queuePlayers;
                        },
                        res => res);
                    randPlayers.Sort((x, y) => x.RatingMean.CompareTo(y.RatingMean));
                    playerPool = playerPool.Append(randPlayers);
                }
            }
        }

        var (possibleBalance, quality) = playerPool.AsParallel().Select(g =>
            {
                var groupArr = g.ToArray();
                var maxTeamPlayers = playersForQueue / 2;
                var partitioning = KarmarkarKarp.Heuristic(groupArr, g.Select(s => s.RatingMean).ToArray(), 2, true);
                while (partitioning.Partition[0].Sum(s => s.Players.Count) is var team1Count &&
                       partitioning.Partition[1].Sum(s => s.Players.Count) is var team2Count &&
                       team1Count != team2Count)
                {
                    if (team1Count > team2Count)
                    {
                        var worstPlayer = partitioning.Partition[0]
                            .Where(q => q.Players.Count <= maxTeamPlayers - team2Count)
                            .DefaultIfEmpty()
                            .MinBy(s => s?.RatingMean);
                        if (worstPlayer is not null)
                        {
                            partitioning.Partition[0].Remove(worstPlayer);
                            partitioning.Partition[1].Add(worstPlayer);
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        var worstPlayer = partitioning.Partition[1]
                            .Where(q => q.Players.Count <= maxTeamPlayers - team1Count)
                            .DefaultIfEmpty()
                            .MinBy(s => s?.RatingMean);
                        if (worstPlayer is not null)
                        {
                            partitioning.Partition[1].Remove(worstPlayer);
                            partitioning.Partition[0].Add(worstPlayer);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                return partitioning.Partition;
            }).OfType<List<QueueGrouping>[]>()
            .Select(g =>
            {
                var group = g.Select(t => t.SelectMany(l => l.Players).ToList()).ToList();
                return (group,
                    TrueSkillCalculator.CalculateMatchQuality(Databases.DefaultGameInfo,
                        group.Select(t =>
                            t.ToDictionary(k => new Player<PlayerQueueData>(k), v => v.Rating))));
            }).DefaultIfEmpty()
            .MaxBy(r => r.Item2);
        
        ShowQueueMessage($"Best balance for {queue.GameModeCard.Id}: {quality}");
        return possibleBalance is not null && quality >= minQuality ? possibleBalance : null;
    }

    private async Task RunQueueCheck(QueueData queue)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(QueueCheckSeconds));
        using var token = new CancellationTokenSource();
        queue.LoopCanceler = token;
        while (await timer.WaitForNextTickAsync(token.Token))
        {
            QueueCheck(queue);
        }

        queue.LoopCanceler = null;
    }

    private static void ShowQueueMessage(string message)
    {
        if (ShowQueueMessages)
        {
            Console.WriteLine(message);
        }
    }

    private void QueueCheck(QueueData queue)
    {
        queue.Players = queue.Players.DistinctBy(p => p.PlayerId).ToList();
        if (queue.IsPop is PopStatus.None)
        {
            ShowQueueMessage($"Attempting to create match for {queue.GameModeCard.Id}...");
            var minQuality = (DateTimeOffset.Now - queue.LastJoinTime).TotalSeconds > MaxSecondWaitTimeWithFullLobby
                ? 0
                : MinimumMatchQuality;
            foreach (var info in Databases.RegionServerDatabase.GetBackfillNeeded(queue.GameModeKey)
                         .Select(tuple => new BackfillInfo(tuple.team1, tuple.team2, tuple.instanceId)))
            {
                var (team1Backfill, team2Backfill) = DoBackfillBalance(queue, minQuality, info);
                if (team1Backfill is not null || team2Backfill is not null)
                {
                    queue.IsPop = PopStatus.Backfill;
                }
                else
                {
                    continue;
                }
                
                ShowQueueMessage($"Backfilling for {queue.GameModeCard.Id}: {info.GameInstanceId}");
                queue.ActiveBackfillInfo = info;
                var confirmTime = CatalogueHelper.GlobalLogic.Matchmaker.ConfirmTime;
                queue.ConfTime = (ulong)DateTimeOffset.Now.AddSeconds(confirmTime).ToUnixTimeMilliseconds();
                var matchUpdate = new MatchmakerUpdate
                {
                    State = new MatchmakerState
                    {
                        State = MatchmakerStateType.ConfirmingBackfilling,
                        ConfirmationTimeout = queue.ConfTime
                    }
                };

                if (team1Backfill is not null)
                {
                    queue.MatchSender1.Subscribe(team1Backfill.PlayerGuid);
                    queue.Team1 = [team1Backfill];
                    SendQueueUpdate(queue.PopServiceMatchmaker1, matchUpdate);
                }

                if (team2Backfill is not null)
                {
                    queue.MatchSender2.Subscribe(team2Backfill.PlayerGuid);
                    queue.Team2 = [team2Backfill];
                    SendQueueUpdate(queue.PopServiceMatchmaker2, matchUpdate);
                }
                
                queue.QueueTimer = new Timer(TimeSpan.FromSeconds(confirmTime).TotalMilliseconds);
                queue.QueueTimer.AutoReset = false;
                queue.QueueTimer.Elapsed += CreateTimerEvent(queue);
                queue.QueueTimer.Start();
                break;
            }
        }

        if (queue.IsPop is not PopStatus.None)
            return;

        if (!queue.EnoughForPop())
        {
            ShowQueueMessage($"Not enough for pop. Only {queue.Players.Count} in queue.");
            return;
        }
        
        var balancedTeams = DoQueueBalance(queue);
        if (balancedTeams is null) return;
        queue.IsPop = PopStatus.Match;

        foreach (var player in balancedTeams.SelectMany(p => p))
        {
            queue.MatchSender1.Subscribe(player.PlayerGuid);
        }
        queue.Team1 = balancedTeams[0];
        queue.Team2 = balancedTeams[1];
        var confTime = CatalogueHelper.GlobalLogic.Matchmaker.ConfirmTime;
        queue.ConfTime = (ulong)DateTimeOffset.Now.AddSeconds(confTime).ToUnixTimeMilliseconds();
        
        ShowQueueMessage($"Creating pop for {queue.GameModeCard.Id}, {queue.MatchSender1.SenderCount} in the sender");
        var team1Names = new StringBuilder();
        var team2Names = new StringBuilder();
        queue.Team1.ForEach(p => team1Names.Append(p.PlayerId + ", "));
        queue.Team2.ForEach(p => team2Names.Append(p.PlayerId + ", "));
        ShowQueueMessage($"Team1: {team1Names} : Team2: {team2Names}");
        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
        {
            State = new MatchmakerState
            {
                State = MatchmakerStateType.Confirming,
                ConfirmationTimeout = queue.ConfTime
            }
        });
        queue.QueueTimer = new Timer(TimeSpan.FromSeconds(confTime).TotalMilliseconds);
        queue.QueueTimer.AutoReset = false;
        queue.QueueTimer.Elapsed += CreateTimerEvent(queue);
        queue.QueueTimer.Start();
    }

    private async Task HandleQueueVote(QueueData queue, PlayerQueueData player, bool vote, IServiceMatchmaker? serviceMatchmaker)
    {
        switch (queue.IsPop)
        {
            case PopStatus.Match:
                queue.AcceptVotes1.TryAdd(player.PlayerId, vote);
                if (!vote)
                {
                    queue.IsPop = PopStatus.PopFailed;
                    queue.QueueTimer?.Stop();
                    queue.QueueTimer?.Dispose();
                    queue.QueueTimer = null;
                    
                    SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                    {
                        State = new MatchmakerState
                        {
                            State = MatchmakerStateType.Aborting,
                            AbortingByDecline = true
                        }
                    });
                    await Task.Delay(AbortDelay);
                    RemovePlayer(player.PlayerId, serviceMatchmaker);
                    SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                    {
                        PlayersInQueue = queue.Players.Count,
                        State = new MatchmakerState
                        {
                            State = MatchmakerStateType.InQueue
                        }
                    });
                    queue.MatchSender1.UnsubscribeAll();
                    queue.AcceptVotes1.Clear();
                    queue.ConfTime = null;
                    queue.IsPop = PopStatus.None;
                }
                else
                {
                    var acceptCount = queue.AcceptVotes1.Values.Count(v => v);
                    
                    SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                    {
                        State = new MatchmakerState
                        {
                            State = MatchmakerStateType.Confirming,
                            PlayersConfirmed = acceptCount,
                            ConfirmationTimeout = queue.ConfTime
                        }
                    });
                    
                    ShowQueueMessage($"{queue.Team1?.Count} : {queue.Team2?.Count} : {acceptCount} : {player.PlayerId}");
                    if (queue.Team1 is not null && queue.Team2 is not null &&
                        acceptCount >= queue.Team1.Count + queue.Team2.Count)
                    {
                        queue.QueueTimer?.Stop();
                        queue.QueueTimer?.Dispose();
                        queue.QueueTimer = null;
                        
                        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.None
                            }
                        });
                        
                        foreach (var p in queue.Team1.Union(queue.Team2))
                        {
                            RemovePlayer(p.PlayerId, null);
                        }
                        
                        queue.AcceptVotes1.Clear();
                        queue.MatchSender1.UnsubscribeAll();
                        Databases.RegionServerDatabase.StartGameFromMatchmaker(queue.GameModeCard, queue.Team1.ToList(),
                            queue.Team2.ToList());
                        queue.Team1.Clear();
                        queue.Team2.Clear();
                        queue.Team1 = null;
                        queue.Team2 = null;
                        queue.ConfTime = null;
                        queue.IsPop = PopStatus.None;
                    }
                }
                break;
            
            case PopStatus.Backfill:
                var inTeam1 = queue.Team1?.Any(p => p.PlayerId == player.PlayerId) is true;
                var inTeam2 = !inTeam1 && queue.Team2?.Any(p => p.PlayerId == player.PlayerId) is true;
                
                switch (inTeam1)
                {
                    case false when !inTeam2:
                        return;
                    case true:
                        queue.AcceptVotes1.TryAdd(player.PlayerId, vote);
                        break;
                    default:
                        queue.AcceptVotes2.TryAdd(player.PlayerId, vote);
                        break;
                }
                
                var matchmakerService = inTeam1 ? queue.PopServiceMatchmaker1 : queue.PopServiceMatchmaker2;
                if (!vote)
                {
                    SendQueueUpdate(matchmakerService, new MatchmakerUpdate
                    {
                        State = new MatchmakerState
                        {
                            State = MatchmakerStateType.Aborting,
                            AbortingByDecline = true
                        }
                    });
                    await Task.Delay(AbortDelay);
                    RemovePlayer(player.PlayerId, serviceMatchmaker);
                }
                else if (queue.ActiveBackfillInfo?.GameInstanceId is not null)
                {
                    RemovePlayer(player.PlayerId, serviceMatchmaker);
                    Databases.RegionServerDatabase.BackfillMatchmakerGame(player,
                        inTeam1 ? TeamType.Team1 : TeamType.Team2, queue.ActiveBackfillInfo.GameInstanceId);
                }
                
                if (inTeam1)
                {
                    queue.AcceptVotes1.Clear();
                    queue.Team1?.Clear();
                    queue.Team1 = null;
                }
                else
                {
                    queue.AcceptVotes2.Clear();
                    queue.Team2?.Clear();
                    queue.Team2 = null;
                }
                break;
            
            case PopStatus.None:
            case PopStatus.PopFailed:
            default:
                return;
        }
    }

    public void OnPopAccepted(uint playerId, bool confirm, IServiceMatchmaker? serviceMatchmaker)
    {
        foreach (var queue in _queues.Values.Where(x => x.Players.Any(p => p.PlayerId == playerId)).ToList())
        {
            var player = queue.Players.First(p => p.PlayerId == playerId);
            switch (queue.IsPop)
            {
                case PopStatus.Match:
                    if (queue.Team1 is not null && queue.Team2 is not null &&
                        (queue.Team1.Any(p => p.PlayerId == player.PlayerId) ||
                         queue.Team2.Any(p => p.PlayerId == player.PlayerId)))
                    {
                        HandleQueueVote(queue, player, confirm, serviceMatchmaker);
                    }
                    break;
                
                case PopStatus.Backfill:
                    if (queue.Team1?.Any(p => p.PlayerId == player.PlayerId) is true ||
                        queue.Team2?.Any(p => p.PlayerId == player.PlayerId) is true)
                    {
                        HandleQueueVote(queue, player, confirm, serviceMatchmaker);
                    }
                    break;
                
                case PopStatus.None:
                case PopStatus.PopFailed:
                default:
                    return;
            }
        }
    }

    private static void SendQueueUpdate(IServiceMatchmaker serviceMatchmaker, MatchmakerUpdate update) =>
        serviceMatchmaker.SendMatchmakerUpdate(update);

    private ElapsedEventHandler CreateTimerEvent(QueueData queue) =>
        (sender, args) =>
        {
            if (sender is not Timer timer) return;
            timer.Stop();
            timer.Dispose();

            switch (queue.IsPop)
            {
                case PopStatus.Match:
                    if (queue.Team1 is not null && queue.Team2 is not null &&
                        queue.AcceptVotes1.Values.Count(v => v) < queue.Team1.Count + queue.Team2.Count)
                    {
                        queue.IsPop = PopStatus.PopFailed;
                        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.Aborting,
                                AbortingByDecline = false
                            }
                        });
                        Task.Delay(AbortDelay).Wait();
                        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.InQueue
                            },
                            PlayersInQueue = queue.Players.Count
                        });
                        queue.MatchSender1.UnsubscribeAll();
                        
                        var nonAccepters = queue.Team1.Union(queue.Team2)
                            .Where(p => !queue.AcceptVotes1.ContainsKey(p.PlayerId)).ToList();
                        foreach (var nonAccepter in nonAccepters)
                        {
                            ShowQueueMessage($"{nonAccepter.PlayerId} did not accept.");
                            queue.MatchSender1.Subscribe(nonAccepter.PlayerGuid);
                        }
                        
                        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.None
                            }
                        });
                        
                        foreach (var nonAccepter in nonAccepters)
                        {
                            RemovePlayer(nonAccepter.PlayerId, null);
                        }
                        
                        queue.MatchSender1.UnsubscribeAll();
                        queue.AcceptVotes1.Clear();
                        queue.Team1?.Clear();
                        queue.Team2?.Clear();
                        queue.Team1 = null;
                        queue.Team2 = null;
                    }
                    break;
                
                case PopStatus.Backfill:
                    if (queue.AcceptVotes1.Values.Count(v => v) < queue.Team1?.Count)
                    {
                        queue.IsPop = PopStatus.PopFailed;
                        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.Aborting,
                                AbortingByDecline = false
                            }
                        });
                        Task.Delay(AbortDelay).Wait();
                        SendQueueUpdate(queue.PopServiceMatchmaker1, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.None
                            }
                        });
                        RemovePlayer(queue.Team1[0].PlayerId, null);
                        queue.AcceptVotes1.Clear();
                        queue.Team1.Clear();
                        queue.Team1 = null;
                    }
                    
                    if (queue.AcceptVotes2.Values.Count(v => v) < queue.Team2?.Count)
                    {
                        queue.IsPop = PopStatus.PopFailed;
                        SendQueueUpdate(queue.PopServiceMatchmaker2, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.Aborting,
                                AbortingByDecline = false
                            }
                        });
                        Task.Delay(AbortDelay).Wait();
                        SendQueueUpdate(queue.PopServiceMatchmaker2, new MatchmakerUpdate
                        {
                            State = new MatchmakerState
                            {
                                State = MatchmakerStateType.None
                            }
                        });
                        RemovePlayer(queue.Team2[0].PlayerId, null);
                        queue.AcceptVotes2.Clear();
                        queue.Team2.Clear();
                        queue.Team2 = null;
                    }

                    queue.ActiveBackfillInfo = null;
                    break;

                case PopStatus.None:
                case PopStatus.PopFailed:
                default:
                    return;
            }

            queue.ConfTime = null;
            queue.IsPop = PopStatus.None;
        };
}
