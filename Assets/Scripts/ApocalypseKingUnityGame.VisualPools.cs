public sealed partial class ApocalypseKingUnityGame
{
    private void PrewarmFallbackEffectViews(int count)
    {
        VisualPools.PrewarmFallbackEffectViews(count);
    }

    private void PrewarmDeathVisuals(UnitKind kind, int count)
    {
        VisualPools.PrewarmDeathVisuals(kind, count);
    }

    private sealed class VisualPoolSystem
    {
        private readonly ApocalypseKingUnityGame game;

        public VisualPoolSystem(ApocalypseKingUnityGame game)
        {
            this.game = game;
        }

        public void PrewarmFallbackEffectViews(int count)
        {
            for (int i = 0; i < count && game.effects.Count < ApocalypseKingUnityGame.MaxEffects; i++)
            {
                game.effects.Add(game.CreateEffectView());
            }
        }

        public void PrewarmDeathVisuals(UnitKind kind, int count)
        {
            for (int i = 0; i < count && game.deathVisuals.Count < ApocalypseKingUnityGame.MaxDeathVisuals; i++)
            {
                game.deathVisuals.Add(game.CreateDeathVisual(kind));
            }
        }
    }
}
