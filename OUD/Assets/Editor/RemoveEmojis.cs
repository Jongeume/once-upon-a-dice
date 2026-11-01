// RemoveEmojis.cs — 씬 내 모든 TMP 텍스트에서 이모지 제거
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text.RegularExpressions;

public static class RemoveEmojis
{
    // 이모지 유니코드 범위 패턴
    private static readonly Regex EmojiPattern = new Regex(
        @"[\u2600-\u27BF]|[\uD83C-\uDBFF][\uDC00-\uDFFF]|[\u2300-\u23FF]|" +
        @"[\u2B00-\u2BFF]|[\u3000-\u303F]|\u00A9|\u00AE|[\u2000-\u206F]",
        RegexOptions.Compiled);

    // 알려진 이모지 직접 치환 테이블
    private static readonly System.Collections.Generic.Dictionary<string, string> KnownEmojis =
        new System.Collections.Generic.Dictionary<string, string>
    {
        { "📋 기술 목록",  "기술 목록"  },
        { "🔄 리롤 2/2",  "리롤 2/2"  },
        { "✅ 기술 사용",  "기술 사용"  },
        { "↩ 뒤로가기",   "뒤로가기"   },
        { "⚔  전투 승리  ⚔", "전투 승리" },
        { "💀  전투 패배", "전투 패배"  },
        { "🔄 리롤 ",     "리롤 "      },
    };

    [MenuItem("OUD/Remove Emojis from Scene")]
    public static void Execute()
    {
        int count = 0;
        var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var t in texts)
        {
            if (EditorUtility.IsPersistent(t)) continue;
            string original = t.text;
            string cleaned  = original;

            foreach (var kv in KnownEmojis)
                cleaned = cleaned.Replace(kv.Key, kv.Value);

            cleaned = EmojiPattern.Replace(cleaned, "").Trim();

            if (cleaned != original)
            {
                t.text = cleaned;
                EditorUtility.SetDirty(t);
                count++;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[RemoveEmojis] " + count + "개 TMP 텍스트 이모지 제거 완료");
    }
}
