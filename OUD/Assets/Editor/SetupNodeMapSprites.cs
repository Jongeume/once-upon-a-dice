// SetupNodeMapSprites.cs
// 에디터 유틸리티: UI/Map 폴더 스프라이트를 NodeMapView + NodeView에 자동 매칭.
// 메뉴: OUD > Setup Node Map Sprites
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using OUD.Unity.Battle.View;

public static class SetupNodeMapSprites
{
    private const string SPRITE_PATH = "Assets/Sprites/UI/Map/";

    [MenuItem("OUD/Setup Node Map Sprites")]
    public static void Execute()
    {
        // ── 1. 스프라이트 로드 ────────────────────────────────────────────────
        Sprite combatSprite = LoadSprite("Monster_Node");
        Sprite bossSprite   = LoadSprite("Boss_Node");
        Sprite shopSprite   = LoadSprite("Store_Node");
        Sprite eliteSprite  = LoadSprite("Eilte_Node");  // 원본 파일명 오타 그대로 사용
        Sprite panelSprite  = LoadSprite("Map Panel");

        if (combatSprite == null || bossSprite == null || shopSprite == null || eliteSprite == null)
        {
            Debug.LogError("[SetupNodeMapSprites] 스프라이트 로드 실패. Sprites/UI/Map/ 폴더를 확인하세요.");
            return;
        }

        // ── 2. NodeMapView 찾기 ───────────────────────────────────────────────
        NodeMapView mapView = Object.FindFirstObjectByType<NodeMapView>(FindObjectsInactive.Include);
        if (mapView == null)
        {
            Debug.LogError("[SetupNodeMapSprites] 씬에서 NodeMapView를 찾을 수 없습니다.");
            return;
        }

        SerializedObject mapSO = new SerializedObject(mapView);

        // 노드 타입별 스프라이트 할당
        mapSO.FindProperty("_combatNodeSprite").objectReferenceValue = combatSprite;
        mapSO.FindProperty("_bossNodeSprite").objectReferenceValue   = bossSprite;
        mapSO.FindProperty("_shopNodeSprite").objectReferenceValue   = shopSprite;
        mapSO.FindProperty("_eliteNodeSprite").objectReferenceValue  = eliteSprite;

        // 맵 패널 배경 할당
        if (panelSprite != null)
        {
            SerializedProperty bgProp = mapSO.FindProperty("_panelBackground");
            if (bgProp != null && bgProp.objectReferenceValue == null)
            {
                // NodeMapView 자신 또는 부모의 Image를 패널 배경으로 사용
                Image bgImage = mapView.GetComponent<Image>();
                if (bgImage == null)
                    bgImage = mapView.transform.parent?.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgProp.objectReferenceValue = bgImage;
                    SerializedObject bgSO = new SerializedObject(bgImage);
                    bgSO.FindProperty("m_Sprite").objectReferenceValue = panelSprite;
                    bgSO.FindProperty("m_Type").intValue = (int)Image.Type.Sliced; // 9-slice
                    bgSO.ApplyModifiedProperties();
                }
            }
        }

        mapSO.ApplyModifiedProperties();

        // ── 3. 각 NodeView에 IconImage 자동 생성 ─────────────────────────────
        SerializedProperty nodesProp = mapSO.FindProperty("_nodes");
        int created = 0;

        for (int i = 0; i < nodesProp.arraySize; i++)
        {
            NodeView nv = nodesProp.GetArrayElementAtIndex(i).objectReferenceValue as NodeView;
            if (nv == null) continue;

            SerializedObject nvSO = new SerializedObject(nv);
            SerializedProperty iconProp = nvSO.FindProperty("_iconImage");

            // 이미 할당되어 있으면 스킵
            if (iconProp.objectReferenceValue != null) continue;

            // 자식에서 "NodeIcon" 이름의 Image 찾기
            Transform existing = nv.transform.Find("NodeIcon");
            Image iconImage;

            if (existing != null)
            {
                iconImage = existing.GetComponent<Image>();
            }
            else
            {
                // 새 GameObject 생성
                GameObject iconGo = new GameObject("NodeIcon");
                iconGo.transform.SetParent(nv.transform, false);
                Undo.RegisterCreatedObjectUndo(iconGo, "Create NodeIcon");

                RectTransform rt = iconGo.AddComponent<RectTransform>();
                // 프레임 중앙, 60% 크기로 배치
                rt.anchorMin = new Vector2(0.2f, 0.2f);
                rt.anchorMax = new Vector2(0.8f, 0.8f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                iconImage = iconGo.AddComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                // 기본 색상: 흰색 (스프라이트 원색 유지)
                iconImage.color = Color.white;
            }

            iconProp.objectReferenceValue = iconImage;
            nvSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(nv);
            created++;
        }

        EditorUtility.SetDirty(mapView);

        // ── 4. 기존 NodeIcon 앵커를 노드 전체 영역으로 확장 ─────────────────
        int resized = 0;
        foreach (RectTransform rt in Object.FindObjectsOfType<RectTransform>(true))
        {
            if (rt.name != "NodeIcon") continue;
            Undo.RecordObject(rt, "Expand NodeIcon");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            EditorUtility.SetDirty(rt);
            resized++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[SetupNodeMapSprites] 완료! 생성:{created} 앵커조정:{resized}개.");
    }

    private static Sprite LoadSprite(string name)
    {
        string path = SPRITE_PATH + name + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[SetupNodeMapSprites] 스프라이트 없음: {path}");
        return sprite;
    }
}
#endif
