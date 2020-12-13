using AltV.Net;
using AltV.Net.Async;
using AltV.Net.Elements.Entities;
using AltV.Net.Enums;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace PlainServer
{
    public class Main : AsyncResource
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static ConcurrentStack<IColShape> ColShapes = new ConcurrentStack<IColShape>();
        public Main()
        { }
        public override void OnStart()
        {
            var cfg = new NLog.Config.LoggingConfiguration();
            var layout =
                "${longdate}|${threadid:padding=5}|${level:uppercase=true}|${logger:shortName=true}|${message}|${exception}|${all-event-properties}";
            var logconsole = new NLog.Targets.ConsoleTarget() { Name = "logconsole", Layout = layout };
            cfg.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            LogManager.Configuration = cfg;
            AltAsync.OnColShape += HandleColshape;
            AltAsync.OnPlayerConnect += HandlePlayerConnect;
            AltAsync.OnPlayerDisconnect += HandlePlayerDisconnect;

        }

        private async Task HandlePlayerDisconnect(IPlayer player, string reason)
        {
            Logger.Info("Player disconnected. Removing colshape");
            ColShapes.TryPop(out var colShape);
            await AltAsync.Do(() =>
            {
                Alt.RemoveColShape(colShape);
            });
        }

        private async Task HandleColshape(IColShape colShape, IEntity targetEntity, bool state)
        {
            lock (targetEntity)
            {
                switch (targetEntity)
                {
                    case IPlayer player when state:
                        {
                            Logger.Info("Player entered a colshape");
                            break;
                        }
                    case IPlayer player:
                        {
                            Logger.Info("Player left a colshape");
                            break;
                        }
                    case IVehicle vehicle when state:
                        {
                            Logger.Info("Vehicle entered a colshape");
                            break;
                        }
                    case IVehicle vehicle:
                        {
                            Logger.Info("Vehicle left a colshape");
                            break;
                        }
                }
            }
        }

        private async Task HandlePlayerConnect(IPlayer player, string reason)
        {
            lock (player)
            {
                Logger.Info($"Player {player.Name} connected. Creating Colshape");
                player.Model = (uint)PedModel.FreemodeMale01;
                player.Spawn(new AltV.Net.Data.Position(813, -279, 66), 1000);
            }

            await AltAsync.Do(() =>
            {
                var colshapeToAdd = Alt.CreateColShapeSphere(new AltV.Net.Data.Position(813, -279, 66), 10);
                ColShapes.Push(colshapeToAdd);
            });
            Logger.Info("Created Colshape");
        }

        private async Task HandleClientEvent(IPlayer player, string eventName, object[] args)
        {
            Logger.Info($"Player event with name '{eventName}' and arguments {string.Join(";", args)}");
        }

        public override void OnStop()
        {
        }
    }
}
