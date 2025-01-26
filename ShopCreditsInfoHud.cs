using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using ShopAPI;
using System.Drawing;

namespace ShopCreditsInfoHud
{
    public class ShopCreditsInfoHud : BasePlugin
    {
        public override string ModuleName => "[SHOP] Credits Info Hud";
        public override string ModuleVersion => "v2.0.0";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Information about credits in hud";

        private IShopApi? _shopApi;
        private static readonly Dictionary<ulong, List<CPointWorldText>> PlayerHudTexts = [];

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _shopApi = IShopApi.Capability.Get();
            if (_shopApi != null)
            {
                _shopApi.CreditsAddPost += OnCreditsAdded;
                _shopApi.CreditsTakePost += OnCreditsTaken;
            }

            _ = AddTimer(1.0f, OnHudUpdate, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
        }

        public override void Unload(bool hotReload)
        {
            if (_shopApi != null)
            {
                _shopApi.CreditsAddPost -= OnCreditsAdded;
                _shopApi.CreditsTakePost -= OnCreditsTaken;
            }
            CleanupExistingHud();
        }

        private void OnCreditsAdded(CCSPlayerController player, int newCredits, IShopApi.WhoChangeCredits who)
        {
            var totalCredits = _shopApi?.GetClientCredits(player) ?? 0;

            if (PlayerHudTexts.TryGetValue(player.SteamID, out List<CPointWorldText>? value))
            {
                if (!value[0].IsValid)
                {
                    CleanupExistingHud();
                    _ = CreateHud(player, Localizer["AddCredits", newCredits, totalCredits], color: Color.Lime);
                }
                else
                {
                    value[0].AcceptInput("SetMessage", value[0], value[0], Localizer["AddCredits", newCredits, totalCredits]);
                }
            }
            else
            {
                CleanupExistingHud();
                _ = CreateHud(player, Localizer["AddCredits", newCredits, totalCredits], color: Color.Lime);
            }
        }

        private void OnCreditsTaken(CCSPlayerController player, int newCredits, IShopApi.WhoChangeCredits who)
        {
            var totalCredits = _shopApi?.GetClientCredits(player) ?? 0;

            if (PlayerHudTexts.TryGetValue(player.SteamID, out List<CPointWorldText>? value))
            {
                if (!value[0].IsValid)
                {
                    CleanupExistingHud();
                    _ = CreateHud(player, Localizer["TakeCredits", newCredits, totalCredits], color: Color.Red);
                }
                else
                {
                    value[0].AcceptInput("SetMessage", value[0], value[0], Localizer["TakeCredits", newCredits, totalCredits]);
                }
            }
            else
            {
                CleanupExistingHud();
                _ = CreateHud(player, Localizer["TakeCredits", newCredits, totalCredits], color: Color.Red);
            }
        }

        private void OnHudUpdate()
        {
            foreach (var player in Utilities.GetPlayers().Where(IsValidPlayer))
            {
                var credits = _shopApi?.GetClientCredits(player) ?? 0;
                UpdatePlayerHud(player, Localizer["Credits", credits], Color.Lime);
            }
        }

        private static void UpdatePlayerHud(CCSPlayerController player, string message, Color color)
        {
            if (PlayerHudTexts.TryGetValue(player.SteamID, out var hudTexts))
            {
                if (!hudTexts[0].IsValid)
                {
                    CleanupExistingHud(player.SteamID);
                    _ = CreateHud(player, message, color: color);
                }
                else
                {
                    hudTexts[0].AcceptInput("SetMessage", hudTexts[0], hudTexts[0], message);
                }
            }
            else
            {
                CleanupExistingHud(player.SteamID);
                _ = CreateHud(player, message, color: color);
            }
        }

        private static bool IsValidPlayer(CCSPlayerController player)
        {
            return player.IsValid && !player.IsBot && !player.IsHLTV &&
            player.Connected == PlayerConnectedState.PlayerConnected &&
            player.PawnIsAlive && player.PlayerPawn.IsValid;
        }

        public static CPointWorldText CreateHud(CCSPlayerController player, string text, int size = 30, Color? color = null, string font = "", float shiftX = -1.5f, float shiftY = -4.7f)
        {
            CCSPlayerPawn pawn = player?.PlayerPawn.Value!;

            var handle = new CHandle<CCSGOViewModel>(pawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4);
            if (!handle.IsValid)
            {
                CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
                viewmodel.DispatchSpawn();
                handle.Raw = viewmodel.EntityHandle.Raw;
                Utilities.SetStateChanged(pawn, "CCSPlayerPawnBase", "m_pViewModelServices");
            }

            CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
            worldText.MessageText = text;
            worldText.Enabled = true;
            worldText.FontSize = size;
            worldText.Fullbright = true;
            worldText.Color = color ?? Color.Aquamarine;
            worldText.WorldUnitsPerPx = 0.01f;
            worldText.FontName = font;
            worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
            worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_TOP;
            worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

            QAngle eyeAngles = pawn.EyeAngles;
            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);

            Vector eyePosition = new();
            eyePosition += forward * 7;
            eyePosition += right * shiftX;
            eyePosition += up * shiftY;
            QAngle angles = new()
            {
                Y = eyeAngles.Y + 270,
                Z = 90 - eyeAngles.X,
                X = 0
            };

            worldText.DispatchSpawn();
            worldText.Teleport(pawn.AbsOrigin! + eyePosition + new Vector(0, 0, pawn.ViewOffset.Z), angles, null);
            Server.NextFrame(() =>
            {
                worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
            });

            if (!PlayerHudTexts.TryGetValue(player!.SteamID, out List<CPointWorldText>? value))
            {
                value = ([]);
                PlayerHudTexts[player.SteamID] = value;
            }

            value.Add(worldText);

            return worldText;
        }

        private static void CleanupExistingHud(ulong? steamId = null)
        {
            if (steamId.HasValue)
            {
                if (PlayerHudTexts.TryGetValue(steamId.Value, out var textEntities))
                {
                    _ = textEntities.RemoveAll(textEntity =>
                    {
                        if (textEntity.IsValid)
                        {
                            textEntity.Remove();
                            return true;
                        }
                        return false;
                    });

                    if (textEntities.Count == 0)
                    {
                        _ = PlayerHudTexts.Remove(steamId.Value);
                    }
                }
            }
            else
            {
                foreach (var key in PlayerHudTexts.Keys.ToList())
                {
                    _ = PlayerHudTexts[key].RemoveAll(textEntity =>
                    {
                        if (textEntity.IsValid)
                        {
                            textEntity.Remove();
                            return true;
                        }
                        return false;
                    });

                    if (PlayerHudTexts[key].Count == 0)
                    {
                        _ = PlayerHudTexts.Remove(key);
                    }
                }
            }
        }
    }
}
