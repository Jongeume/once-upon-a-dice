namespace OUD.Unity.Tutorial
{
    public enum TutorialTrigger
    {
        Auto,
        PhaseChanged,
        ButtonClicked,
        DiceKept,
        SkillSelected,
        TargetSelected,
        TurnEnded,
        EnemyShielded,
        BattleWon,
    }

    public enum GlowTarget
    {
        None,
        RollDiceButton,
        RerollButton,
        UseSkillButton,
        ExecuteButton,
        DiceEntries,
        SkillCards,
        EnemyCards,
    }

    public class TutorialStep
    {
        public string GuideText { get; }
        public TutorialTrigger Trigger { get; }
        public GlowTarget[] GlowTargets { get; }
        public float DelayBefore { get; }
        public bool IsConditional { get; }

        public TutorialStep(
            string guideText,
            TutorialTrigger trigger,
            GlowTarget[] glowTargets = null,
            float delayBefore = 0f,
            bool isConditional = false)
        {
            GuideText = guideText;
            Trigger = trigger;
            GlowTargets = glowTargets ?? System.Array.Empty<GlowTarget>();
            DelayBefore = delayBefore;
            IsConditional = isConditional;
        }
    }
}
