using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;

using System.Text;

namespace Macrodox {
    public class Main : BasePlugin {
        public override string ModuleName => "Macrodox";
        public override string ModuleVersion => "6.6.6";
        public override string ModuleAuthor => "eboyfriends";

        private Dictionary<CCSPlayerController, PlayerJumpData> PlayerJumps = new();

        public override void Load(bool hotReload) {
            Logger.LogInformation("We are loading Macrodox!");
            
            RegisterListener<Listeners.OnTick>(() => OnTickListener());

            AddCommand("macrodox", "Show player's jump data", CommandMacrodox);
            AddCommand("macrodoxall", "Show all players' jump data", CommandMacrodoxAll);
        }

        public override void Unload(bool hotReload) {
            Logger.LogInformation("We are unloading Macrodox!");
        }

        private void OnTickListener() {
            foreach (CCSPlayerController player in Utilities.GetPlayers()) {
                if (!PlayerJumps.TryGetValue(player, out PlayerJumpData? value)) {
                    value = new PlayerJumpData();
                    PlayerJumps[player] = value;
                }

                PlayerJumpData jumpData = value;
                PlayerButtons buttons = player.Buttons;

                bool? isOnGround = player.PlayerPawn.Value?.GroundEntity.IsValid;

                /*
                    theres one slight issue but shouldnt be MUCH of a problem, isJumping is only checking if they clicked space. 
                    therefor, if they rebind it to something else it wont register it. You'd think that EventPlayerJump would 
                    include jumps while their in the air but it does not.
                */
                
                bool isJumping = buttons.HasFlag(PlayerButtons.Jump);

                if ((bool)isOnGround) {
                    if (jumpData.IsInAir && jumpData.AirJumps > 0) {
                        jumpData.AddJump(jumpData.AirJumps);
                        jumpData.AirJumps = 0;
                    }
                    jumpData.IsInAir = false;
                    jumpData.WasJumping = false;
                } else {
                    if (!jumpData.IsInAir) {
                        jumpData.IsInAir = true;
                    }
                    
                    if (isJumping && !jumpData.WasJumping) {
                        jumpData.AirJumps++;
                    }
                    jumpData.WasJumping = isJumping;
                }
            }
        }

        private void CommandMacrodox(CCSPlayerController? player, CommandInfo command) {
            if (command.ArgCount < 2) {
                command.ReplyToCommand("Usage: /macrodox <playername>");
                return;
            }

            string targetName = command.ArgByIndex(1);
            CCSPlayerController? targetPlayer = Utilities.GetPlayers().FirstOrDefault(p => p.PlayerName.Equals(targetName, StringComparison.OrdinalIgnoreCase));

            if (targetPlayer == null) {
                command.ReplyToCommand($"Player '{targetName}' not found.");
                return;
            }

            if (!PlayerJumps.TryGetValue(targetPlayer, out PlayerJumpData? jumpData)) {
                command.ReplyToCommand($"No jump data available for {targetName}.");
                return;
            }

            string jumpHistory = string.Join(", ", jumpData.LastJumps.Select(j => j.ToString()));
            command.ReplyToCommand($"{targetName}'s last 10 jumps: {jumpHistory}");
        }
    
        private void CommandMacrodoxAll(CCSPlayerController? player, CommandInfo command) {
            StringBuilder consoleOutput = new StringBuilder();
            consoleOutput.AppendLine("Jump data for all players:");

            foreach (var playerEntry in PlayerJumps) {
                string playerName = playerEntry.Key.PlayerName;
                PlayerJumpData jumpData = playerEntry.Value;
                string jumpHistory = string.Join(", ", jumpData.LastJumps.Select(j => j.ToString()));
                consoleOutput.AppendLine($"{playerName}: {jumpHistory}");
            }

            if (player != null) {
                player.PrintToConsole(consoleOutput.ToString());
                player.PrintToChat("Jump data for all players has been sent to the console.");
            }
        }
    }

    public class PlayerJumpData {
        public bool IsInAir { get; set; } = false;
        public int AirJumps { get; set; } = 0;
        public bool WasJumping { get; set; } = false;
        public Queue<int> LastJumps { get; } = new Queue<int>(10);

        public void AddJump(int jumpCount) {
            if (LastJumps.Count >= 10) {
                LastJumps.Dequeue();
            }
            LastJumps.Enqueue(jumpCount);
        }
    }
}