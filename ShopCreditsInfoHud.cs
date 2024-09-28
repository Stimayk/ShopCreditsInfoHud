using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using ShopAPI;

namespace ShopCreditsInfoHud
{
    public class ShopCreditsInfoHud : BasePlugin
    {
        public override string ModuleName => "[SHOP] Credits Info Hud";
        public override string ModuleVersion => "v1.1.0";
        public override string ModuleAuthor => "E!N";
        public override string ModuleDescription => "Information about credits in hud";

        private IShopApi? _shopApi;

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _shopApi = IShopApi.Capability.Get();
            if (_shopApi != null)
            {
                RegisterListener<Listeners.OnTick>(OnTick);

                _shopApi.CreditsAddPost += OnCreditsAdded;
                _shopApi.CreditsTakePost += OnCreditsTaken;
            }
        }

        public override void Unload(bool hotReload)
        {
            if (_shopApi != null)
            {
                RemoveListener<Listeners.OnTick>(OnTick);
                _shopApi.CreditsAddPost -= OnCreditsAdded;
                _shopApi.CreditsTakePost -= OnCreditsTaken;
            }
        }

        private void OnTick()
        {
            var players = Utilities.GetPlayers().Where(IsValidPlayer);
            foreach (var player in players)
            {
                UpdatePlayerCreditsHud(player);
            }
        }

        private void OnCreditsAdded(CCSPlayerController player, int newCredits, IShopApi.WhoChangeCredits byWho)
        {
            var totalCredits = _shopApi?.GetClientCredits(player) ?? 0;

            player.PrintToCenter(Localizer["AddCredits", newCredits, totalCredits]);
        }

        private void OnCreditsTaken(CCSPlayerController player, int newCredits, IShopApi.WhoChangeCredits byWho)
        {
            var totalCredits = _shopApi?.GetClientCredits(player) ?? 0;

            player.PrintToCenter(Localizer["TakeCredits", newCredits, totalCredits]);
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