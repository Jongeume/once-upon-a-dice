using UnityEngine;

namespace OUD.Unity.Tutorial
{
    public static class TutorialState
    {
        private const string KEY = "TutorialCompleted";

        public static bool IsCompleted
            => PlayerPrefs.GetInt(KEY, 0) == 1;

        public static void SetCompleted()
        {
            PlayerPrefs.SetInt(KEY, 1);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.SetInt(KEY, 0);
            PlayerPrefs.Save();
        }
    }
}
