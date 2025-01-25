using Microsoft.EntityFrameworkCore;
using My2DWorldServer.Extensions;
using My2DWorldServer.Services;
using My2DWorldShared.Data;
using My2DWorldShared.DataEntities;
using My2DWorldShared.Enums;
using My2DWorldShared.Extensions;
using My2DWorldShared.Models;
using My2DWorldShared.PacketsIn;
using My2DWorldShared.PacketsOut;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace My2DWorldServer.Calls
{
    public class GameCaller : IGameCaller
    {
        private readonly UsersSessionCollection _users;
        private readonly IDbContextFactory<SqlDbContext> _dbContextFactory;
        private readonly UserSession _session;
        private readonly IGameInformer _gameInformer;

        public GameCaller(
            UsersSessionCollection users, 
            UserSession session, 
            IDbContextFactory<SqlDbContext> dbContextFactory, 
            IGameInformer gameInformer)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _gameInformer = gameInformer ?? throw new ArgumentNullException(nameof(gameInformer));
        }

        public async Task OnAuthenticate(PacketAuthenticate packet)
        {
            if (!_session.Logged)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (dbContext == null) throw new ApplicationException("Failed to create a database context.");

                    var user = await dbContext.Users
                        .FirstOrDefaultAsync(x => x.Username == packet.Username && x.Password == packet.Password);

                    if (user != null)
                    {
                        if (!_users.Sessions.Any(x => x.UserId == user.Id))
                        {
                            _session.Logged = true;
                            _session.UserId = user.Id;
                            await _gameInformer.SendAuthenticateConnection();
                        }
                        else
                        {
                            await _gameInformer.SendAuthenticateRejectConnection("User already connected.");
                            throw new ApplicationException("User already connected.");
                        }
                    }
                    else
                    {
                        await _gameInformer.SendAuthenticateRejectConnection("Invalid username or password.");
                        throw new ApplicationException("Invalid username or password.");
                    }
                }
            }
            else
            {
                await _gameInformer.SendAuthenticateRejectConnection("Already authenticated.");
                throw new ApplicationException("Already authenticated.");
            }
        }

        public async Task OnChangeServer(PacketChangeServer packet)
        {
            if (_session.Logged)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (dbContext == null) throw new ApplicationException("Failed to create a database context.");

                    var server = await dbContext.Servers.FindAsync(packet.ServerId);
                    if (server == null) throw new ApplicationException("Invalid Server Id.");

                    var user = await dbContext.Users.FindAsync(_session.UserId);
                    if (user == null) throw new ApplicationException("User not found.");

                    if (_users.Sessions.Count(y => y.ServerId == server.Id) < server.ServerMaxPlayers)
                    {
                        _session.ServerId = server.Id;
                        await _gameInformer.SendPushUserInformation();
                        await _gameInformer.SendMapChange(user.LastLocationId ?? 1, 0);
                    }
                }
            }
            else throw new ApplicationException("Not authenticated.");
        }

        public async Task OnChatMessage(PacketChatMessage packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (dbContext == null) throw new ApplicationException("Failed to create a database context.");

                    var user = await dbContext.Users.FindAsync(_session.UserId);
                    if (user == null) throw new ApplicationException("User not found.");

                    await _gameInformer.SendPushUserMessageToRoom(user, packet.Message);
                }
            }
        }

        public async Task OnTryEquipItem(PacketEquipItem packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (dbContext == null) throw new ApplicationException("Failed to create a database context.");

                    var user = await dbContext.Users.FindAsync(_session.UserId);
                    if (user == null) throw new ApplicationException("User not found.");

                    var item = user.Inventory?.FirstOrDefault(x => x.ItemId == packet.ItemId)?.Item;
                    if (item != null)
                    {
                        var itemFieldFromUser = user.GetType().GetProperty(Enum.GetName(typeof(ItemType), item.Type) ?? "");
                        if (itemFieldFromUser != null)
                        {
                            if ((itemFieldFromUser.GetValue(user) as int?) != item.Id)
                            {
                                itemFieldFromUser.SetValue(user, item.Id);
                                await dbContext.UpdateFieldsAsync(user, itemFieldFromUser.Name);
                                var playerEquip = new PacketPlayerEquipItem
                                {
                                    PlayerName = user.Username,
                                    Item = new EquipedLoadoutModel
                                    {
                                        Id = item.Id,
                                        Type = item.Type,
                                        FilePath = item.FilePath
                                    }
                                };
                                await _users.Sessions.Where(x => x.ServerId == _session.ServerId && x.MapId == _session.MapId && x.GameId == null)
                                    .ForEachAsyncCustom(x => x.WebSocket.SendAsync(playerEquip, CancellationToken.None));
                            }
                            else
                            {
                                itemFieldFromUser.SetValue(user, null);
                                await dbContext.UpdateFieldsAsync(user, itemFieldFromUser.Name);
                                var playerUnequip = new PacketPlayerUnequipItem
                                {
                                    PlayerName = user.Username,
                                    ItemType = item.Type
                                };
                                await _users.Sessions.Where(x => x.ServerId == _session.ServerId && x.MapId == _session.MapId && x.GameId == null)
                                    .ForEachAsyncCustom(x => x.WebSocket.SendAsync(playerUnequip, CancellationToken.None));
                            }
                        }
                    }
                    else throw new ApplicationException("Invalid equip item received.");
                }
            }
        }

        public async Task OnGameLoad(PacketGameLoad packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (dbContext == null) throw new ApplicationException("Failed to create a database context.");

                    var game = await dbContext.Games.FindAsync(packet.GameId);
                    if (game == null) throw new ApplicationException("Game not found.");

                    var user = await dbContext.Users.FindAsync(_session.UserId);
                    if (user == null) throw new ApplicationException("User not found.");

                    await _gameInformer.SendExitedRoomToAll(user);
                    _session.GameId = packet.GameId;
                    await _gameInformer.SendPlayerGameLoad(game);
                }
            }
        }

        public Task OnGameProgressUpdate(PacketGameProgressUpdate packet)
        {
            return Task.CompletedTask;
        }

        public async Task OnGameQuit(PacketGameQuit packet)
        {
            if (_session.Logged && _session.ServerId != null && _session.GameId != null)
            {
                _session.GameId = null;
                await _gameInformer.SendMapChange(_session.MapId ?? 1, -1);
            }
        }

        public async Task OnMapChange(PacketMapChange packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                await _gameInformer.SendMapChange(packet.MapId, packet.ExitId);
            }
        }

        public async Task OnPlayerMove(PacketPlayerMove packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                await _gameInformer.SendPlayerUpdatePosition(packet.X, packet.Y);
            }
        }

        public Task OnShopBuy(PacketShopBuy packet)
        {
            throw new NotImplementedException();
        }

        public async Task OnShopLoad(PacketShopLoad packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                await _gameInformer.SendPlayerShopBatch(packet.ShopId, packet.ShopPage, 5);
            }
        }

        public async Task OnRequestChangeServer(PacketRequestChangeServer packet)
        {
            if (_session.Logged && _session.ServerId != null)
            {
                await OnQuitServer();
                await _gameInformer.SendAuthenticateConnection();
            }
        }

        public async Task OnQuitServer()
        {
            if (_session.Logged && _session.MapId != null && _session.ServerId != null)
            {
                using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
                {
                    if (dbContext == null) throw new ApplicationException("Failed to create a database context.");

                    var user = await dbContext.Users.FindAsync(_session.UserId);
                    if (user != null)
                    {
                        user.LastLocationId = _session.MapId;
                        await dbContext.UpdateFieldsAsync(user, x => x.LastLocationId);
                    }
                    await _gameInformer.SendExitedRoomToAll(user);
                    _session.ServerId = null;
                    _session.MapId = null;
                    _session.GameId = null;
                }
            }
        }

        public async Task OnRequestInventoryBatch(PacketRequestInventoryBatch packet)
        {
            await _gameInformer.SendInventoryBatch(packet.Offset, 20);
        }
    }
}
