using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;

namespace OUD.Editor
{
    public static class DiceSceneSetup
    {
        [MenuItem("OUD/Setup Dice Scene Structure")]
        public static void Execute()
        {
            // 1. Find DiceArea and its children
            var diceArea = GameObject.Find("DiceArea");
            if (diceArea == null)
            {
                // Try searching in inactive objects
                var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (var t in allTransforms)
                {
                    if (t.name == "DiceArea" && t.gameObject.scene.isLoaded)
                    {
                        diceArea = t.gameObject;
                        break;
                    }
                }
            }

            if (diceArea == null)
            {
                Debug.LogError("[DiceSetup] DiceArea not found in scene!");
                return;
            }

            Debug.Log("[DiceSetup] Found DiceArea: " + diceArea.name);

            // 2. Load rolling frame sprites (DiceSpread_0 ~ DiceSpread_111)
            var allSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Dice/DiceSpread.png")
                .OfType<Sprite>()
                .OrderBy(s =>
                {
                    string numStr = s.name.Replace("DiceSpread_", "");
                    int.TryParse(numStr, out int num);
                    return num;
                })
                .ToArray();

            Debug.Log($"[DiceSetup] Loaded {allSprites.Length} rolling frame sprites");

            if (allSprites.Length == 0)
            {
                Debug.LogError("[DiceSetup] No sprites found in DiceSpread.png! Check import settings.");
                return;
            }

            // 3. Load result sprites (1.png ~ 6.png)
            var resultSprites = new Sprite[6];
            for (int i = 1; i <= 6; i++)
            {
                string path = $"Assets/Art/Dice/Results/{i}.png";
                resultSprites[i - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (resultSprites[i - 1] == null)
                    Debug.LogWarning($"[DiceSetup] Result sprite not found: {path}");
            }

            Debug.Log($"[DiceSetup] Loaded {resultSprites.Count(s => s != null)}/6 result sprites");

            // 4. Process each Dice1~5
            for (int i = 1; i <= 5; i++)
            {
                string diceName = $"Dice{i}";
                Transform diceTransform = null;

                // Search in DiceArea children (including inactive)
                foreach (Transform child in diceArea.transform)
                {
                    if (child.name == diceName)
                    {
                        diceTransform = child;
                        break;
                    }
                }

                if (diceTransform == null)
                {
                    Debug.LogError($"[DiceSetup] {diceName} not found under DiceArea!");
                    continue;
                }

                var diceGO = diceTransform.gameObject;
                Debug.Log($"[DiceSetup] Processing {diceName}...");

                // 4a. Delete "Value" child (TMP)
                Transform valueChild = diceTransform.Find("Value");
                if (valueChild != null)
                {
                    Debug.Log($"[DiceSetup]   Deleting Value (TMP) child from {diceName}");
                    Undo.DestroyObjectImmediate(valueChild.gameObject);
                }

                // 4b. Remove duplicate DiceEntryView components
                var diceEntryViews = diceGO.GetComponents<OUD.Unity.Battle.View.DiceEntryView>();
                if (diceEntryViews.Length > 1)
                {
                    Debug.Log($"[DiceSetup]   Removing {diceEntryViews.Length - 1} duplicate DiceEntryView(s) from {diceName}");
                    for (int d = 1; d < diceEntryViews.Length; d++)
                    {
                        Undo.DestroyObjectImmediate(diceEntryViews[d]);
                    }
                }

                // 4c. Create or find "DiceImage" child with Image component
                Transform diceImageTransform = diceTransform.Find("DiceImage");
                Image diceImageComponent;

                if (diceImageTransform == null)
                {
                    var diceImageGO = new GameObject("DiceImage");
                    Undo.RegisterCreatedObjectUndo(diceImageGO, "Create DiceImage");
                    diceImageGO.transform.SetParent(diceTransform, false);

                    // Add RectTransform (required for UI)
                    var rect = diceImageGO.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    diceImageComponent = diceImageGO.AddComponent<Image>();
                    diceImageComponent.preserveAspect = true;
                    diceImageComponent.raycastTarget = false;

                    Debug.Log($"[DiceSetup]   Created DiceImage child for {diceName}");
                }
                else
                {
                    diceImageComponent = diceImageTransform.GetComponent<Image>();
                    if (diceImageComponent == null)
                        diceImageComponent = diceImageTransform.gameObject.AddComponent<Image>();
                    Debug.Log($"[DiceSetup]   DiceImage already exists for {diceName}");
                }

                // Set initial sprite to result 1
                if (resultSprites[0] != null)
                    diceImageComponent.sprite = resultSprites[0];

                // 4d. Wire DiceEntryView SerializeFields via SerializedObject
                var entryView = diceGO.GetComponent<OUD.Unity.Battle.View.DiceEntryView>();
                if (entryView == null)
                {
                    Debug.LogError($"[DiceSetup]   No DiceEntryView found on {diceName}!");
                    continue;
                }

                var so = new SerializedObject(entryView);

                // Wire _diceImage
                var diceImageProp = so.FindProperty("_diceImage");
                if (diceImageProp != null)
                {
                    diceImageProp.objectReferenceValue = diceImageComponent;
                    Debug.Log($"[DiceSetup]   Wired _diceImage for {diceName}");
                }

                // Wire _background (the Image on the dice itself)
                var bgProp = so.FindProperty("_background");
                if (bgProp != null)
                {
                    var bgImage = diceGO.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgProp.objectReferenceValue = bgImage;
                        Debug.Log($"[DiceSetup]   Wired _background for {diceName}");
                    }
                }

                // Wire _button
                var btnProp = so.FindProperty("_button");
                if (btnProp != null)
                {
                    var btn = diceGO.GetComponent<Button>();
                    if (btn != null)
                    {
                        btnProp.objectReferenceValue = btn;
                        Debug.Log($"[DiceSetup]   Wired _button for {diceName}");
                    }
                }

                // Wire _rollingFrames
                var rollingProp = so.FindProperty("_rollingFrames");
                if (rollingProp != null)
                {
                    rollingProp.arraySize = allSprites.Length;
                    for (int s = 0; s < allSprites.Length; s++)
                    {
                        rollingProp.GetArrayElementAtIndex(s).objectReferenceValue = allSprites[s];
                    }
                    Debug.Log($"[DiceSetup]   Wired _rollingFrames ({allSprites.Length} sprites) for {diceName}");
                }

                // Wire _resultSprites
                var resultProp = so.FindProperty("_resultSprites");
                if (resultProp != null)
                {
                    resultProp.arraySize = 6;
                    for (int r = 0; r < 6; r++)
                    {
                        resultProp.GetArrayElementAtIndex(r).objectReferenceValue = resultSprites[r];
                    }
                    Debug.Log($"[DiceSetup]   Wired _resultSprites (6 sprites) for {diceName}");
                }

                so.ApplyModifiedProperties();
            }

            // 5. Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[DiceSetup] === COMPLETE === Scene structure updated and saved.");
        }
    }
}
