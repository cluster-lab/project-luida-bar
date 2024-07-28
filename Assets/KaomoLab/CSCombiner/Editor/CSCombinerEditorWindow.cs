using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.KaomoLab.CSCombiner.Editor
{

    [CustomEditor(typeof(CSCombiner))]
    public class CSCombinerEditorWindow
         : EditorWindow
    {
        [MenuItem("Window/かおもラボ/CSCombiner")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<CSCombinerEditorWindow>(false, "CSCombiner");
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "現在のシーンと、プロジェクト内のプレハブのスクリプトを更新します。", MessageType.Info
            );
            if (GUILayout.Button("全更新"))
            {
                CSCombiner.CombineAll();
            }
        }
    }
}
