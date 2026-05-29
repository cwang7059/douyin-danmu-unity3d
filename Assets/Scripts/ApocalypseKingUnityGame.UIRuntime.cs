using UnityEngine;

public sealed partial class ApocalypseKingUnityGame
{
    private void RefreshHud()
    {
        UIRuntime.RefreshHud();
    }

    private void ShowLoading(bool visible)
    {
        UIRuntime.ShowLoading(visible);
    }

    private void UpdateLoadingLabel()
    {
        UIRuntime.UpdateLoadingLabel();
    }

    private void SetLoadingMessage(string text)
    {
        UIRuntime.SetLoadingMessage(text);
    }

    private void ShowBanner(string text, bool urgent, float duration)
    {
        UIRuntime.ShowBanner(text, urgent, duration);
    }

    private void HideBanner()
    {
        UIRuntime.HideBanner();
    }

    private sealed class UIRuntimeSystem
    {
        private const string HideBannerMethodName = "HideBanner";

        private readonly ApocalypseKingUnityGame game;

        public UIRuntimeSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void RefreshHud()
        {
            int soldierAlive = game.CountActive(game.soldiers);
            int tankAlive = game.CountActive(game.tanks);
            int airAlive = game.CountActive(game.aircraft);
            int humanAlive = soldierAlive + tankAlive + airAlive;
            int humanTotal = ApocalypseKingUnityGame.MaxSoldierCount + ApocalypseKingUnityGame.MaxTankCount + ApocalypseKingUnityGame.MaxAircraftCount;
            int giantAlive = game.CountActive(game.giants);
            float giantHp = Mathf.Ceil(game.GetGiantHpTotal());
            float giantMax = game.GetGiantMaxHpTotal();
            float hpPct = Mathf.Clamp01(giantHp / Mathf.Max(1f, giantMax));
            float humanPct = Mathf.Clamp01(humanAlive / Mathf.Max(1f, (float)humanTotal));
            int pool = 380000 + Mathf.FloorToInt(game.battleTime * 8200f) + game.humanLosses * 2600;
            float remaining = Mathf.Max(0f, 180f - game.battleTime);
            float skillCooldown = 9f - game.battleTime % 9f;

            if (game.leftTeamLabel != null)
            {
                game.leftTeamLabel.text = $"BLUE FORCE {humanAlive}";
            }

            if (game.rightTeamLabel != null)
            {
                game.rightTeamLabel.text = giantAlive > 0 ? "MONSTER SIDE" : "MONSTER DOWN";
            }

            if (game.battlePhaseLabel != null)
            {
                game.battlePhaseLabel.text = game.ended ? "RESULT" : game.paused ? "PAUSED" : "LIVE BARRAGE WAR";
            }

            if (game.poolLabel != null)
            {
                game.poolLabel.text = $"POINT POOL {pool:N0}";
            }

            if (game.timerLabel != null)
            {
                game.timerLabel.text = FormatTime(remaining);
            }

            if (game.humanLabel != null)
            {
                game.humanLabel.text = $"Force {humanAlive}/{humanTotal}  Tanks {tankAlive}";
            }

            if (game.giantLabel != null)
            {
                game.giantLabel.text = $"Boss {giantAlive}/{ApocalypseKingUnityGame.MaxGiantCount} HP {giantHp:0}";
            }

            if (game.statusLabel != null)
            {
                game.statusLabel.text = game.paused ? "Paused" : game.ended ? "Battle over" : $"Battle {FormatTime(game.battleTime)}  Losses {game.humanLosses}";
            }

            if (game.bottomTickerLabel != null)
            {
                game.bottomTickerLabel.text = BuildTickerMessage(soldierAlive, tankAlive, airAlive, giantHp);
            }

            if (game.giftFeedLabel != null)
            {
                game.giftFeedLabel.text = $"Gift heat +{pool - 380000:N0}  Barrage combo x{1 + Mathf.FloorToInt(game.battleTime * 0.45f) % 9}";
            }

            if (game.skillCountdownLabel != null)
            {
                game.skillCountdownLabel.text = $"Barrage skill CD {Mathf.CeilToInt(skillCooldown)}s";
            }

            if (game.humanPowerFill != null)
            {
                game.humanPowerFill.fillAmount = humanPct;
                game.humanPowerFill.color = humanPct > 0.28f ? ApocalypseKingUnityGame.HumanColor : new Color(1f, 0.63f, 0.26f, 1f);
            }

            if (game.monsterPowerFill != null)
            {
                game.monsterPowerFill.fillAmount = hpPct;
                game.monsterPowerFill.color = hpPct > 0.35f ? ApocalypseKingUnityGame.GiantColor : new Color(1f, 0.82f, 0.24f, 1f);
            }

            if (game.hpFill != null && game.hpFill != game.monsterPowerFill)
            {
                game.hpFill.fillAmount = hpPct;
                game.hpFill.color = hpPct > 0.35f ? ApocalypseKingUnityGame.GiantColor : new Color(1f, 0.82f, 0.24f, 1f);
            }
        }

        public void ShowLoading(bool visible)
        {
            if (game.loadingPanel != null)
            {
                game.loadingPanel.gameObject.SetActive(visible);
            }
        }

        public void UpdateLoadingLabel()
        {
            if (game.loadingLabel == null || game.assetsReady)
            {
                return;
            }

            int dots = ((int)(game.loadingPulseTime * 3f) % 3) + 1;
            game.loadingLabel.text = $"Loading Poly Pizza 3D models{new string('.', dots)}";
        }

        public void SetLoadingMessage(string text)
        {
            if (game.loadingLabel != null)
            {
                game.loadingLabel.text = text;
            }
        }

        public void ShowBanner(string text, bool urgent, float duration)
        {
            if (game.bannerLabel == null)
            {
                return;
            }

            game.bannerLabel.gameObject.SetActive(true);
            game.bannerLabel.text = text;
            game.bannerLabel.color = urgent ? new Color(1f, 0.87f, 0.44f, 1f) : new Color(0.96f, 0.98f, 1f, 1f);
            game.CancelInvoke(HideBannerMethodName);
            game.Invoke(HideBannerMethodName, duration);
        }

        public void HideBanner()
        {
            if (game.bannerLabel != null)
            {
                game.bannerLabel.gameObject.SetActive(false);
            }
        }

        private string BuildTickerMessage(int soldierAlive, int tankAlive, int airAlive, float giantHp)
        {
            int index = Mathf.Abs(Mathf.FloorToInt(game.battleTime * 0.7f)) % 5;
            switch (index)
            {
                case 0:
                    return $"Barrage: blue force focus fire  soldiers {soldierAlive}/{ApocalypseKingUnityGame.MaxSoldierCount}";
                case 1:
                    return $"Barrage: tank line spacing stable  {tankAlive}/{ApocalypseKingUnityGame.MaxTankCount} online";
                case 2:
                    return $"Barrage: air support suppressing  helicopters {airAlive}/{ApocalypseKingUnityGame.MaxAircraftCount}";
                case 3:
                    return $"Barrage: boss HP {giantHp:0}  breach contested";
                default:
                    return $"Barrage: drag to inspect the battlefield  losses {game.humanLosses}";
            }
        }

        private static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int total = Mathf.FloorToInt(seconds);
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }
    }
}
