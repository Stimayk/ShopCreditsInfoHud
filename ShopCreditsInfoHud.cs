using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using ShopAPI;

namespace ShopCreditsInfoHud
{
    public class ShopCreditsInfoHud : BasePlugin
    {
        public override string ModuleName => "[SHOP] Credits Info Hud";
        public override string ModuleVersion => "v1.0";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Information about credits in hud";

        private IShopApi? _shopApi;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _shopApi = IShopApi.Capability.Get();
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        private void OnTick()
        {
            var players = Utilities.GetPlayers().Where(IsValidPlayer);
            foreach (var player in players)
            {
                UpdatePlayerCreditsHud(player);
            }
        }

        private void UpdatePlayerCreditsHud(CCSPlayerController player)
        {
            const PlayerButtons ButtonFlag = (PlayerButtons)8589934592;
            if ((player.Buttons & ButtonFlag) != 0)
            {
                var credits = _shopApi?.GetClientCredits(player) ?? 0;
                player.PrintToCenter(Localizer["Credits", credits]);
            }
        }

        private bool IsValidPlayer(CCSPlayerController player)
        {
            return player.IsValid
                   && player.PlayerPawn?.IsValid == true
                   && !player.IsBot
                   && !player.IsHLTV
                   && player.Connected == PlayerConnectedState.PlayerConnected;
        }
    }
}