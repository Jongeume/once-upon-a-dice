namespace OUD.Unity.Tutorial
{
    /// <summary>
    /// 타이틀 화면에서 BattleScene으로 넘어갈 때 "이번 진입이 강제 튜토리얼인지"를
    /// 전달하는 휘발성(런타임 전용) 플래그.
    ///
    /// 영구 완료 상태(<see cref="TutorialState"/>, PlayerPrefs)와 분리되어 있어,
    /// 튜토리얼 재생 중 종료해도 완료 기록이 훼손되지 않는다.
    ///
    /// - New Game 버튼  → <see cref="ForceTutorial"/> = false (완료면 일반, 미완료면 튜토리얼)
    /// - Tutorial 버튼  → <see cref="ForceTutorial"/> = true  (완료 여부 무관 항상 튜토리얼)
    ///
    /// static 필드이므로 씬 로드 간에는 유지되고, 앱 재시작 시 기본값(false)으로 초기화된다.
    /// 기본값 false = New Game 동작이라 직접 BattleScene을 열어도 안전하다.
    /// </summary>
    public static class TutorialEntry
    {
        public static bool ForceTutorial;
    }
}
