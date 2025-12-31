using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PokedexApi.Services;
using PokedexApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace PokedexApi.Hubs
{
    [Authorize]
    public class BattleHub : Hub
    {
        private readonly IBattleService _battleService;
        private readonly ITeamService _teamService;

        private static readonly ConcurrentDictionary<string, BattleState> ActiveBattles = new();
        private static readonly ConcurrentQueue<MatchmakingPlayer> MatchmakingQueue = new();
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        public BattleHub(IBattleService battleService, ITeamService teamService)
        {
            _battleService = battleService;
            _teamService = teamService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections[userId] = Context.ConnectionId;
                Console.WriteLine($"User {userId} connected with connection ID: {Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                UserConnections.TryRemove(userId, out _);
                RemoveFromMatchmaking(userId);
                await HandleBattleDisconnect(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinMatchmaking(int teamId)
        {
            try
            {
                var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var username = Context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Player";

                var team = await _teamService.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    await Clients.Caller.SendAsync("Error", "Team not found");
                    return;
                }

                var rating = await _battleService.GetOrCreateUserRatingAsync(userId);

                List<PokemonBuild> pokemonTeam;
                try
                {
                    pokemonTeam = JsonSerializer.Deserialize<List<PokemonBuild>>(team.TeamData);
                    if (pokemonTeam == null || pokemonTeam.Count == 0)
                    {
                        await Clients.Caller.SendAsync("Error", "Invalid team data");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing team: {ex.Message}");
                    await Clients.Caller.SendAsync("Error", "Failed to load team data");
                    return;
                }

                var player = new MatchmakingPlayer
                {
                    UserId = userId,
                    Username = username,
                    TeamId = teamId,
                    Team = pokemonTeam,
                    ConnectionId = Context.ConnectionId,
                    Rating = rating.Rating,
                    JoinedAt = DateTime.UtcNow
                };

                MatchmakingQueue.Enqueue(player);
                Console.WriteLine($"Player {username} joined matchmaking queue");

                await TryMatchPlayers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JoinMatchmaking error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public Task LeaveMatchmaking()
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                RemoveFromMatchmaking(userId);
            }
            return Task.CompletedTask;
        }

        public async Task<string> CreatePrivateBattle(int teamId)
        {
            try
            {
                var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var username = Context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Player";

                var team = await _teamService.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    await Clients.Caller.SendAsync("Error", "Team not found");
                    return null;
                }

                var session = await _battleService.CreateBattleSessionAsync(
                    userId, Context.ConnectionId, 0, "", teamId, 0);

                var pokemonTeam = JsonSerializer.Deserialize<List<PokemonBuild>>(team.TeamData);

                var battleState = new BattleState
                {
                    BattleId = session.BattleId,
                    Player1 = new BattlePlayer
                    {
                        UserId = userId,
                        Username = username,
                        ConnectionId = Context.ConnectionId,
                        Team = ConvertTeamToBattlePokemon(pokemonTeam),
                        CurrentPokemonIndex = 0
                    },
                    Player2 = null,
                    CurrentTurn = BattleTurn.Player1,
                    TurnNumber = 1,
                    BattleLog = new List<BattleLogEntry>(),
                    Status = BattleStatus.Waiting,
                    StartedAt = DateTime.UtcNow
                };

                ActiveBattles[session.BattleId] = battleState;
                Console.WriteLine($"Private battle created: {session.BattleId}");
                return session.BattleId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreatePrivateBattle error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
                return null;
            }
        }

        public async Task JoinPrivateBattle(string battleId, int teamId)
        {
            try
            {
                if (!ActiveBattles.TryGetValue(battleId, out var battle))
                {
                    await Clients.Caller.SendAsync("Error", "Battle not found");
                    return;
                }

                if (battle.Status != BattleStatus.Waiting)
                {
                    await Clients.Caller.SendAsync("Error", "Battle already started");
                    return;
                }

                var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var username = Context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Player";

                var team = await _teamService.GetTeamByIdAsync(teamId);
                if (team == null)
                {
                    await Clients.Caller.SendAsync("Error", "Team not found");
                    return;
                }

                var pokemonTeam = JsonSerializer.Deserialize<List<PokemonBuild>>(team.TeamData);

                battle.Player2 = new BattlePlayer
                {
                    UserId = userId,
                    Username = username,
                    ConnectionId = Context.ConnectionId,
                    Team = ConvertTeamToBattlePokemon(pokemonTeam),
                    CurrentPokemonIndex = 0
                };

                battle.Status = BattleStatus.Active;
                battle.BattleLog.Add(new BattleLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Message = $"Battle started between {battle.Player1.Username} and {battle.Player2.Username}!",
                    Type = LogType.Turn
                });

                await Clients.Client(battle.Player1.ConnectionId).SendAsync("BattleJoined", battle);
                await Clients.Client(battle.Player2.ConnectionId).SendAsync("BattleJoined", battle);

                Console.WriteLine($"Player {username} joined battle {battleId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JoinPrivateBattle error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public async Task PerformAction(string battleId, BattleAction action)
        {
            try
            {
                if (!ActiveBattles.TryGetValue(battleId, out var battle))
                {
                    await Clients.Caller.SendAsync("Error", "Battle not found");
                    return;
                }

                if (battle.Status != BattleStatus.Active)
                {
                    await Clients.Caller.SendAsync("Error", "Battle is not active");
                    return;
                }

                var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var isPlayer1 = battle.Player1.UserId == userId;
                if ((isPlayer1 && battle.CurrentTurn != BattleTurn.Player1) ||
                    (!isPlayer1 && battle.CurrentTurn != BattleTurn.Player2))
                {
                    await Clients.Caller.SendAsync("Error", "Not your turn");
                    return;
                }

                var currentPlayer = isPlayer1 ? battle.Player1 : battle.Player2;
                var opponent = isPlayer1 ? battle.Player2 : battle.Player1;

                switch (action.Type)
                {
                    case ActionType.Move:
                        ProcessMoveAction(battle, currentPlayer, opponent, action.MoveIndex);
                        break;
                    case ActionType.Switch:
                        ProcessSwitchAction(battle, currentPlayer, action.SwitchToIndex);
                        break;
                    case ActionType.Forfeit:
                        battle.Status = BattleStatus.Finished;
                        battle.Winner = isPlayer1 ? BattleTurn.Player2 : BattleTurn.Player1;
                        battle.BattleLog.Add(new BattleLogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Message = $"{currentPlayer.Username} forfeited the battle!",
                            Type = LogType.Win
                        });
                        break;
                }

                var player1Alive = battle.Player1.Team.Any(p => p.Status != PokemonStatus.Fainted);
                var player2Alive = battle.Player2.Team.Any(p => p.Status != PokemonStatus.Fainted);

                if (!player1Alive || !player2Alive)
                {
                    battle.Status = BattleStatus.Finished;
                    battle.Winner = player1Alive ? BattleTurn.Player1 : player2Alive ? BattleTurn.Player2 : BattleTurn.Draw;
                    battle.EndedAt = DateTime.UtcNow;

                    var winnerName = battle.Winner == BattleTurn.Player1 ? battle.Player1.Username :
                                     battle.Winner == BattleTurn.Player2 ? battle.Player2.Username : "No one";

                    battle.BattleLog.Add(new BattleLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Message = $"{winnerName} wins the battle!",
                        Type = LogType.Win
                    });

                    await SaveBattleResult(battle);
                    await Clients.Client(battle.Player1.ConnectionId).SendAsync("BattleFinished", battle);
                    await Clients.Client(battle.Player2.ConnectionId).SendAsync("BattleFinished", battle);

                    ActiveBattles.TryRemove(battleId, out _);
                    await _battleService.DeleteBattleSessionAsync(battleId);
                }
                else
                {
                    battle.CurrentTurn = battle.CurrentTurn == BattleTurn.Player1 ? BattleTurn.Player2 : BattleTurn.Player1;
                    battle.TurnNumber++;

                    await Clients.Client(battle.Player1.ConnectionId).SendAsync("BattleUpdated", battle);
                    await Clients.Client(battle.Player2.ConnectionId).SendAsync("BattleUpdated", battle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PerformAction error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        private async Task TryMatchPlayers()
        {
            if (MatchmakingQueue.Count < 2) return;

            if (MatchmakingQueue.TryDequeue(out var player1) &&
                MatchmakingQueue.TryDequeue(out var player2))
            {
                try
                {
                    var battleId = GenerateBattleId();

                    var battleState = new BattleState
                    {
                        BattleId = battleId,
                        Player1 = new BattlePlayer
                        {
                            UserId = player1.UserId,
                            Username = player1.Username,
                            ConnectionId = player1.ConnectionId,
                            Team = ConvertTeamToBattlePokemon(player1.Team),
                            CurrentPokemonIndex = 0
                        },
                        Player2 = new BattlePlayer
                        {
                            UserId = player2.UserId,
                            Username = player2.Username,
                            ConnectionId = player2.ConnectionId,
                            Team = ConvertTeamToBattlePokemon(player2.Team),
                            CurrentPokemonIndex = 0
                        },
                        CurrentTurn = BattleTurn.Player1,
                        TurnNumber = 1,
                        BattleLog = new List<BattleLogEntry>
                        {
                            new BattleLogEntry
                            {
                                Timestamp = DateTime.UtcNow,
                                Message = $"Battle started between {player1.Username} and {player2.Username}!",
                                Type = LogType.Turn
                            }
                        },
                        Status = BattleStatus.Active,
                        StartedAt = DateTime.UtcNow
                    };

                    ActiveBattles[battleId] = battleState;

                    await Clients.Client(player1.ConnectionId).SendAsync("BattleJoined", battleState);
                    await Clients.Client(player2.ConnectionId).SendAsync("BattleJoined", battleState);

                    Console.WriteLine($"Match created: {player1.Username} vs {player2.Username}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TryMatchPlayers error: {ex.Message}");
                    MatchmakingQueue.Enqueue(player1);
                    MatchmakingQueue.Enqueue(player2);
                }
            }
        }

        private void RemoveFromMatchmaking(string userId)
        {
            var tempQueue = new ConcurrentQueue<MatchmakingPlayer>();
            while (MatchmakingQueue.TryDequeue(out var player))
            {
                if (player.UserId.ToString() != userId)
                {
                    tempQueue.Enqueue(player);
                }
            }

            while (tempQueue.TryDequeue(out var player))
            {
                MatchmakingQueue.Enqueue(player);
            }
        }

        private async Task HandleBattleDisconnect(string userId)
        {
            foreach (var battle in ActiveBattles.Values)
            {
                if (battle.Player1.UserId.ToString() == userId || battle.Player2?.UserId.ToString() == userId)
                {
                    if (battle.Status == BattleStatus.Active)
                    {
                        var opponentConnectionId = battle.Player1.UserId.ToString() == userId
                            ? battle.Player2.ConnectionId
                            : battle.Player1.ConnectionId;

                        await Clients.Client(opponentConnectionId).SendAsync("OpponentDisconnected");
                    }

                    ActiveBattles.TryRemove(battle.BattleId, out _);
                    await _battleService.DeleteBattleSessionAsync(battle.BattleId);
                }
            }
        }

        private void ProcessMoveAction(BattleState battle, BattlePlayer attacker, BattlePlayer defender, int moveIndex)
        {
            var attackerPokemon = attacker.Team[attacker.CurrentPokemonIndex];
            var defenderPokemon = defender.Team[defender.CurrentPokemonIndex];

            if (moveIndex >= attackerPokemon.Moves.Count)
            {
                battle.BattleLog.Add(new BattleLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Message = "Invalid move!",
                    Type = LogType.Info
                });
                return;
            }

            var move = attackerPokemon.Moves[moveIndex];

            battle.BattleLog.Add(new BattleLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = $"{attackerPokemon.Nickname ?? attackerPokemon.PokemonName} used {FormatName(move.Name)}!",
                Type = LogType.Info
            });

            var damage = CalculateDamage(attackerPokemon, defenderPokemon, move);

            if (damage > 0)
            {
                defenderPokemon.CurrentHp = Math.Max(0, defenderPokemon.CurrentHp - damage);

                battle.BattleLog.Add(new BattleLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Message = $"Dealt {damage} damage to {defenderPokemon.Nickname ?? defenderPokemon.PokemonName}!",
                    Type = LogType.Damage
                });

                if (defenderPokemon.CurrentHp == 0)
                {
                    defenderPokemon.Status = PokemonStatus.Fainted;
                    battle.BattleLog.Add(new BattleLogEntry
                    {
                        Timestamp = DateTime.UtcNow,
                        Message = $"{defenderPokemon.Nickname ?? defenderPokemon.PokemonName} fainted!",
                        Type = LogType.Info
                    });
                }
            }
        }

        private void ProcessSwitchAction(BattleState battle, BattlePlayer player, int switchToIndex)
        {
            if (switchToIndex >= player.Team.Count || switchToIndex < 0)
            {
                battle.BattleLog.Add(new BattleLogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Message = "Invalid switch!",
                    Type = LogType.Info
                });
                return;
            }

            var oldPokemon = player.Team[player.CurrentPokemonIndex];
            player.CurrentPokemonIndex = switchToIndex;
            var newPokemon = player.Team[switchToIndex];

            battle.BattleLog.Add(new BattleLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Message = $"{player.Username} switched to {newPokemon.Nickname ?? newPokemon.PokemonName}!",
                Type = LogType.Switch
            });
        }

        private int CalculateDamage(BattlePokemon attacker, BattlePokemon defender, MoveData move)
        {
            var power = move.Power ?? 50;
            var level = attacker.Level;

            var isPhysical = move.DamageClass == "physical";
            var attackStat = isPhysical ? attacker.Stats.Attack : attacker.Stats.SpecialAttack;
            var defenseStat = isPhysical ? defender.Stats.Defense : defender.Stats.SpecialDefense;

            var damage = (((2.0 * level / 5.0 + 2.0) * power * (attackStat / (double)defenseStat)) / 50.0) + 2.0;

            var random = new Random();
            damage *= 0.85 + random.NextDouble() * 0.15;

            if (attacker.Types != null && attacker.Types.Contains(move.Type))
            {
                damage *= 1.5;
            }

            return (int)Math.Floor(damage);
        }

        private List<BattlePokemon> ConvertTeamToBattlePokemon(List<PokemonBuild> team)
        {
            return team.Select(p => new BattlePokemon
            {
                PokemonName = ExtractPokemonName(p.Pokemon),
                Nickname = p.Nickname,
                Level = p.Level,
                Types = ExtractTypes(p.Pokemon),
                CurrentHp = CalculateMaxHP(p),
                MaxHp = CalculateMaxHP(p),
                Status = PokemonStatus.Normal,
                Stats = new BattlePokemonStats
                {
                    Hp = CalculateStat("hp", p),
                    Attack = CalculateStat("attack", p),
                    Defense = CalculateStat("defense", p),
                    SpecialAttack = CalculateStat("special-attack", p),
                    SpecialDefense = CalculateStat("special-defense", p),
                    Speed = CalculateStat("speed", p)
                },
                Moves = ConvertMoves(p.Moves),
                Ability = p.Ability ?? "Unknown",
                Item = ExtractItemName(p.Item)
            }).ToList();
        }

        private string ExtractPokemonName(JsonElement? pokemon)
        {
            if (pokemon == null) return "Unknown";

            try
            {
                if (pokemon.Value.TryGetProperty("name", out var name))
                {
                    return name.GetString() ?? "Unknown";
                }
            }
            catch { }

            return "Unknown";
        }

        private string ExtractItemName(JsonElement? item)
        {
            if (item == null) return null;

            try
            {
                if (item.Value.TryGetProperty("name", out var name))
                {
                    return name.GetString();
                }
            }
            catch { }

            return null;
        }

        private List<string> ExtractTypes(JsonElement? pokemon)
        {
            var types = new List<string>();
            if (pokemon == null) return types;

            try
            {
                if (pokemon.Value.TryGetProperty("types", out var typesArray))
                {
                    foreach (var typeElement in typesArray.EnumerateArray())
                    {
                        if (typeElement.TryGetProperty("type", out var typeObj) &&
                            typeObj.TryGetProperty("name", out var typeName))
                        {
                            types.Add(typeName.GetString());
                        }
                    }
                }
            }
            catch { }

            return types;
        }

        private List<MoveData> ConvertMoves(List<JsonElement> moves)
        {
            var moveList = new List<MoveData>();

            foreach (var move in moves ?? new List<JsonElement>())
            {
                try
                {
                    var moveData = new MoveData
                    {
                        Name = move.TryGetProperty("name", out var name) ? name.GetString() : "tackle",
                        Power = move.TryGetProperty("power", out var power) ? power.GetInt32() : 50,
                        Type = move.TryGetProperty("type", out var typeObj) &&
                               typeObj.TryGetProperty("name", out var typeName)
                               ? typeName.GetString() : "normal",
                        DamageClass = move.TryGetProperty("damage_class", out var damageObj) &&
                                     damageObj.TryGetProperty("name", out var damageClass)
                                     ? damageClass.GetString() : "physical"
                    };
                    moveList.Add(moveData);
                }
                catch
                {
                    moveList.Add(new MoveData
                    {
                        Name = "tackle",
                        Power = 40,
                        Type = "normal",
                        DamageClass = "physical"
                    });
                }
            }

            return moveList;
        }

        private int CalculateMaxHP(PokemonBuild pokemon)
        {
            int baseStat = GetBaseStat(pokemon.Pokemon, "hp");
            var iv = pokemon.Ivs?.Hp ?? 31;
            var ev = pokemon.Evs?.Hp ?? 0;
            var level = pokemon.Level;

            return (int)Math.Floor(((2.0 * baseStat + iv + Math.Floor(ev / 4.0)) * level) / 100.0) + level + 10;
        }

        private int CalculateStat(string statName, PokemonBuild pokemon)
        {
            int baseStat = GetBaseStat(pokemon.Pokemon, statName);
            var iv = GetIV(pokemon, statName);
            var ev = GetEV(pokemon, statName);
            var level = pokemon.Level;

            return (int)Math.Floor(((2.0 * baseStat + iv + Math.Floor(ev / 4.0)) * level) / 100.0) + 5;
        }

        private int GetBaseStat(JsonElement? pokemon, string statName)
        {
            if (pokemon == null) return 50;

            try
            {
                if (pokemon.Value.TryGetProperty("stats", out var stats))
                {
                    foreach (var stat in stats.EnumerateArray())
                    {
                        if (stat.TryGetProperty("stat", out var statObj) &&
                            statObj.TryGetProperty("name", out var name) &&
                            name.GetString() == statName &&
                            stat.TryGetProperty("base_stat", out var baseStat))
                        {
                            return baseStat.GetInt32();
                        }
                    }
                }
            }
            catch { }

            return 50;
        }

        private int GetIV(PokemonBuild pokemon, string statName)
        {
            if (pokemon.Ivs == null) return 31;

            return statName switch
            {
                "hp" => pokemon.Ivs.Hp,
                "attack" => pokemon.Ivs.Attack,
                "defense" => pokemon.Ivs.Defense,
                "special-attack" => pokemon.Ivs.SpecialAttack,
                "special-defense" => pokemon.Ivs.SpecialDefense,
                "speed" => pokemon.Ivs.Speed,
                _ => 31
            };
        }

        private int GetEV(PokemonBuild pokemon, string statName)
        {
            if (pokemon.Evs == null) return 0;

            return statName switch
            {
                "hp" => pokemon.Evs.Hp,
                "attack" => pokemon.Evs.Attack,
                "defense" => pokemon.Evs.Defense,
                "special-attack" => pokemon.Evs.SpecialAttack,
                "special-defense" => pokemon.Evs.SpecialDefense,
                "speed" => pokemon.Evs.Speed,
                _ => 0
            };
        }

        private async Task SaveBattleResult(BattleState battle)
        {
            var winnerId = battle.Winner == BattleTurn.Player1 ? battle.Player1.UserId :
                          battle.Winner == BattleTurn.Player2 ? battle.Player2.UserId : (int?)null;

            var request = new SaveBattleRequest
            {
                Player1Id = battle.Player1.UserId,
                Player2Id = battle.Player2.UserId,
                WinnerId = winnerId,
                Player1TeamId = 0,
                Player2TeamId = 0,
                TotalTurns = battle.TurnNumber,
                BattleLog = JsonSerializer.Serialize(battle.BattleLog),
                StartedAt = battle.StartedAt,
                EndedAt = battle.EndedAt ?? DateTime.UtcNow
            };

            await _battleService.SaveBattleAsync(request);
        }

        private string GenerateBattleId()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string FormatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            return string.Join(" ", name.Split('-')
                .Select(word => char.ToUpper(word[0]) + word.Substring(1)));
        }
    }

    public class MatchmakingPlayer
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int TeamId { get; set; }
        public List<PokemonBuild> Team { get; set; }
        public string ConnectionId { get; set; }
        public int Rating { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class BattleState
    {
        public string BattleId { get; set; }
        public BattlePlayer Player1 { get; set; }
        public BattlePlayer Player2 { get; set; }
        public BattleTurn CurrentTurn { get; set; }
        public int TurnNumber { get; set; }
        public List<BattleLogEntry> BattleLog { get; set; }
        public BattleStatus Status { get; set; }
        public BattleTurn Winner { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class BattlePlayer
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public List<BattlePokemon> Team { get; set; }
        public int CurrentPokemonIndex { get; set; }
    }

    public class BattlePokemon
    {
        public string PokemonName { get; set; }
        public string Nickname { get; set; }
        public int Level { get; set; }
        public List<string> Types { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public PokemonStatus Status { get; set; }
        public BattlePokemonStats Stats { get; set; }
        public List<MoveData> Moves { get; set; }
        public string Ability { get; set; }
        public string Item { get; set; }
    }

    public class MoveData
    {
        public string Name { get; set; }
        public int? Power { get; set; }
        public string Type { get; set; }
        public string DamageClass { get; set; }
    }

    public class BattlePokemonStats
    {
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
    }

    public class BattleLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public LogType Type { get; set; }
    }

    public class BattleAction
    {
        public ActionType Type { get; set; }
        public int MoveIndex { get; set; }
        public int SwitchToIndex { get; set; }
    }

    public enum BattleStatus
    {
        Waiting,
        Active,
        Finished
    }

    public enum BattleTurn
    {
        Player1,
        Player2,
        Draw
    }

    public enum PokemonStatus
    {
        Normal,
        Paralyzed,
        Burned,
        Frozen,
        Poisoned,
        Asleep,
        Fainted
    }

    public enum ActionType
    {
        Move,
        Switch,
        Forfeit
    }

    public enum LogType
    {
        Info,
        Damage,
        Heal,
        Status,
        Switch,
        Turn,
        Win
    }

    public class PokemonBuild
    {
        public JsonElement? Pokemon { get; set; }
        public string Nickname { get; set; }
        public int Level { get; set; }
        public string Nature { get; set; }
        public string Ability { get; set; }
        public JsonElement? Item { get; set; }
        public List<JsonElement> Moves { get; set; }
        public PokemonEVs Evs { get; set; }
        public PokemonIVs Ivs { get; set; }
    }

    public class PokemonEVs
    {
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
    }

    public class PokemonIVs
    {
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
    }
}