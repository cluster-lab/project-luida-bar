#if UNITY_EDITOR
using ClusterVR.CreatorKit.Item.Implements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Assets.KaomoLab.CSCombiner
{
    [RequireComponent(typeof(ScriptableItem)), DisallowMultipleComponent]
    public class CSCombiner
        : MonoBehaviour
    {

        //IPreprocessBuildWithReport.OnPreprocessBuildでビルド前に処理しようと思ったけど、
        //prefabの中まで処理できるか分からなかったから手動に変更

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            EditorApplication.playModeStateChanged += playMode =>
            {
                if(playMode == PlayModeStateChange.ExitingEditMode)
                {
                    CombineAll();
                }
            };
        }
        public static void CombineAll()
        {
            UnityEngine.Debug.Log(String.Format("[{0}]更新開始", typeof(CSCombiner).Name));
            CombineAllOfScene();
            CombineAllOfProject();
            UnityEngine.Debug.Log(String.Format("[{0}]更新終了", typeof(CSCombiner).Name));
        }
        static void CombineAllOfScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            foreach (var combiner in rootObjects.SelectMany(o => o.GetComponentsInChildren<CSCombiner>(true)))
            {
                UnityEngine.Debug.Log(String.Format("[{0}][Scene]{1}", typeof(CSCombiner).Name, combiner.name));
                if (!combiner.Combine())
                {
                    continue;
                }
                //Undoしないし、いらない？
                //EditorUtility.SetDirty(combiner.gameObject);
                //EditorSceneManager.MarkSceneDirty(scene);
            }
        }
        static void CombineAllOfProject()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", null);

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = PrefabUtility.LoadPrefabContents(path);

                var combiners = prefab.GetComponentsInChildren<CSCombiner>(true);
                if (combiners.Length == 0)
                {
                    PrefabUtility.UnloadPrefabContents(prefab);
                    continue;
                }

                UnityEngine.Debug.Log(String.Format("[{0}][Prefab]{1}", typeof(CSCombiner).Name, path));

                bool changed = false;
                foreach (var combiner in combiners)
                {
                    UnityEngine.Debug.Log(String.Format("[{0}][Prefab]{1}", typeof(CSCombiner).Name, combiner.name));
                    changed |= combiner.Combine();
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }

                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }






        [SerializeField] protected List<JavaScriptAsset> clusterScripts = new List<JavaScriptAsset>();
        [SerializeField] protected List<JavaScriptAsset> playerScripts = new List<JavaScriptAsset>();

        [SerializeField] List<string> clusterScriptMD5s = new List<string>();
        [SerializeField] List<string> playerScriptMD5s = new List<string>();

        public bool Combine()
        {
            var changed = false;
            changed |= CombineCode(canClusterScriptCombine, clusterScripts, clusterScriptMD5s, StoreClusterScript);
            changed |= CombineCode(canPlayerScriptCombine, playerScripts, playerScriptMD5s, StorePlayerScript);

            return changed;
        }
        bool canClusterScriptCombine()
        {
            //更新しても実質問題ないので更新する。
            //return hasScriptableItem() && !hasScriptableItemAsset();
            return hasScriptableItem();
        }
        bool canPlayerScriptCombine()
        {
            //更新しても実質問題ないので更新する。
            //return hasPlayerScript() && !hasPlayerScriptAsset();
            return hasPlayerScript();
        }
        bool CombineCode(
            Func<bool> hasComponent, List<JavaScriptAsset> sources, List<string> md5s, Action<string> SetCode
        )
        {
            if (!hasComponent()) return false;
            //MD5取る負荷考えたら全更新したほうが早いのでは？
            //var need = CheckNeedCombine(sources, md5s);
            var need = true;
            if (!need) return false;
            if (need)
            {
                var len = sources.Select(s => s == null ? 0 : s.text.Length).Sum();
                var sb = new StringBuilder(len + sources.Count * 2); //改行コード分
                sb.AppendJoin("\r\n", sources.Select(s => s == null ? "" : s.text));
                SetCode(sb.ToString());
            }
            return true;
        }
        bool CheckNeedCombine(List<JavaScriptAsset> sources, List<string> md5s)
        {
            var need = false;
            if (sources.Count != md5s.Count)
            {
                need = true;
            }

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var newMD5s = sources.Select(s => {
                    var bytes = s == null ? new byte[0] : Encoding.UTF8.GetBytes(s.text);
                    var hash = md5.ComputeHash(bytes);
                    var ret = BitConverter.ToString(hash);
                    return ret;
                }).ToArray();
                if (!need)
                {
                    for (var i = 0; i < newMD5s.Length; i++)
                    {
                        if (newMD5s[i] != md5s[i])
                        {
                            need = true;
                            break;
                        }
                    }
                }
                if (need)
                {
                    md5s.Clear();
                    md5s.AddRange(newMD5s);
                }
            }
            return need;
        }
        void StoreClusterScript(string code)
        {
            var scriptableItem = GetComponent<ScriptableItem>();
            if (scriptableItem == null) return;

            var prev = scriptableItem.GetSourceCode(true);
            if (prev == code) return;

            //Undoしないし、いらない？
            UpdateStringProperty(scriptableItem, "sourceCode", code);
            //var clusterScript_sourceCode = typeof(ScriptableItem).GetField("sourceCode", BindingFlags.Instance | BindingFlags.NonPublic);
            //clusterScript_sourceCode.SetValue(scriptableItem, code);

            scriptableItem.GetSourceCode(true); //内部のフラグをtrueにする
        }
        void StorePlayerScript(string code)
        {
            var playerScript = GetComponent<PlayerScript>();
            if (playerScript == null) return;

            //Undoしないし、いらない？
            UpdateStringProperty(playerScript, "sourceCode", code);
            //var playerScript_sourceCode = typeof(PlayerScript).GetField("sourceCode", BindingFlags.Instance | BindingFlags.NonPublic);
            //playerScript_sourceCode.SetValue(playerScript, code);

            playerScript.GetSourceCode(true); //内部のフラグをtrueにする
        }
        void UpdateStringProperty(UnityEngine.Object target, string property, string code)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            var p = serialized.FindProperty(property);
            p.stringValue = code;
            serialized.ApplyModifiedProperties();
            //serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public void ClearMD5()
        {
            clusterScriptMD5s.Clear();
            playerScriptMD5s.Clear();
        }

        public bool hasScriptableItem()
        {
            return GetComponent<ScriptableItem>() != null;
        }

        public bool hasPlayerScript()
        {
            return GetComponent<PlayerScript>() != null;
        }

        public bool hasScriptableItemAsset()
        {
            var scriptableItem_sourceCodeAsset = typeof(ScriptableItem).GetField("sourceCodeAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            var c = GetComponent<ScriptableItem>();
            if (c == null) return false;
            var a = scriptableItem_sourceCodeAsset.GetValue(c);
            if (a == null) return false;
            return true;
        }

        public bool hasPlayerScriptAsset()
        {
            var playerScript_sourceCodeAsset = typeof(PlayerScript).GetField("sourceCodeAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            var c = GetComponent<PlayerScript>();
            if (c == null) return false;
            var a = playerScript_sourceCodeAsset.GetValue(c);
            if (a == null) return false;
            return true;
        }
    }
}
#endif
