/*
 * 謝辞:
 * 開発にあたり、KaomoLab/CSCombiner (https://vkao.booth.pm/items/5924956) の設計を大いに参考にさせていただきました。
 * 実装自体は独自のものであり、オリジナルのコードの移植ではありません。
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClusterVR.CreatorKit.Item.Implements;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClusterMetaverseLab.Luida.Scripting
{
    [RequireComponent(typeof(ScriptableItem))]
    [DisallowMultipleComponent]
    public class LuidaScriptCombiner : MonoBehaviour
    {
        const string SourceCodePropertyName = "sourceCode";
        const string SourceCodeAssetFieldName = "sourceCodeAsset";
        const string ScriptSeparator = "\r\n";

        [SerializeField] List<JavaScriptAsset> itemScripts = new List<JavaScriptAsset>();
        [SerializeField] List<JavaScriptAsset> playerScripts = new List<JavaScriptAsset>();

        public List<JavaScriptAsset> ItemScripts => itemScripts;
        public List<JavaScriptAsset> PlayerScripts => playerScripts;

        public bool Combine()
        {
            var combined = false;
            combined |= CombineItemScripts();
            combined |= CombinePlayerScripts();
            return combined;
        }

        public void PrependScript(JavaScriptAsset clusterScript, JavaScriptAsset playerScript, bool combineNow = false)
        {
            if (clusterScript != null) itemScripts.Insert(0, clusterScript);
            if (playerScript != null) playerScripts.Insert(0, playerScript);

            if (combineNow) Combine();
        }

        public void AppendScript(JavaScriptAsset clusterScript, JavaScriptAsset playerScript, bool combineNow = false)
        {
            if (clusterScript != null) itemScripts.Add(clusterScript);
            if (playerScript != null) playerScripts.Add(playerScript);

            if (combineNow) Combine();
        }

        public void ReplaceScript(JavaScriptAsset clusterScript, int clusterScriptIndex, JavaScriptAsset playerScript, int playerScriptIndex, bool combineNow = false)
        {
            if (clusterScript != null) itemScripts[clusterScriptIndex] = clusterScript;
            if (playerScript != null) playerScripts[playerScriptIndex] = playerScript;

            if (combineNow) Combine();
        }

        public void ClearScripts(bool combineNow = false)
        {
            itemScripts.Clear();
            playerScripts.Clear();

            if (combineNow) Combine();
        }

        public void CombineScripts()
        {
            Combine();
        }

        public bool HasScriptableItem()
        {
            return GetComponent<ScriptableItem>() != null;
        }

        public bool HasPlayerScript()
        {
            return GetComponent<PlayerScript>() != null;
        }

        public bool HasScriptableItemSourceAsset()
        {
            return HasSourceCodeAsset(GetComponent<ScriptableItem>());
        }

        public bool HasPlayerScriptSourceAsset()
        {
            return HasSourceCodeAsset(GetComponent<PlayerScript>());
        }

        public static void CombineAll()
        {
            Debug.Log("[LUIDA] Combining item scripts...");
            var sceneCount = CombineActiveScene();
            var prefabCount = CombineProjectPrefabs();
            Debug.Log($"[LUIDA] Combine finished: {sceneCount} object(s) in the active scene, {prefabCount} prefab(s) updated.");
        }

        static int CombineActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            var combiners = scene.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<LuidaScriptCombiner>(true));

            var count = 0;
            foreach (var combiner in combiners)
            {
                combiner.Combine();
                count++;
            }
            return count;
        }

        static int CombineProjectPrefabs()
        {
            var updated = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var combiners = prefab.GetComponentsInChildren<LuidaScriptCombiner>(true);
                    if (combiners.Length == 0) continue;

                    var changed = false;
                    foreach (var combiner in combiners)
                    {
                        changed |= combiner.Combine();
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefab, path);
                        updated++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                }
            }
            return updated;
        }

        // sourceCodeAsset はCCK側でprivateなので、リフレクションでしか参照できない
        static bool HasSourceCodeAsset(Component component)
        {
            if (component == null) return false;
            var field = component.GetType().GetField(SourceCodeAssetFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null && field.GetValue(component) != null;
        }

        static string Concatenate(List<JavaScriptAsset> scripts)
        {
            return string.Join(ScriptSeparator, scripts.Select(s => s == null ? "" : s.text));
        }

        static void WriteSourceCode(Object target, string code)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            serialized.FindProperty(SourceCodePropertyName).stringValue = code;
            serialized.ApplyModifiedProperties();
        }

        bool CombineItemScripts()
        {
            var scriptableItem = GetComponent<ScriptableItem>();
            if (scriptableItem == null) return false;

            var code = Concatenate(itemScripts);
            // 同一なら書き込まない（prefabを不要にdirtyにしないため）
            if (scriptableItem.GetSourceCode(true) == code) return true;

            WriteSourceCode(scriptableItem, code);
            // CCKはsourceCodeを初回参照時に別フィールドへキャッシュするため、
            // SerializedObject経由で書き込んだ後はrefresh付きで読み直さないと古い内容が使われる
            scriptableItem.GetSourceCode(true);
            return true;
        }

        bool CombinePlayerScripts()
        {
            var playerScript = GetComponent<PlayerScript>();
            if (playerScript == null) return false;

            WriteSourceCode(playerScript, Concatenate(playerScripts));
            playerScript.GetSourceCode(true);
            return true;
        }
    }
}
#endif
