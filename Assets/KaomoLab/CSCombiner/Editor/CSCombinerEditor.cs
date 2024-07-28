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
    public class CSCombinerEditor
         : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var target = serializedObject.targetObject as CSCombiner;

            EditorGUILayout.HelpBox(
                "スクリプトは上から順に結合されます。", MessageType.Info
            );

            if (target.hasScriptableItem())
            {
                if (target.hasScriptableItemAsset())
                {
                    EditorGUILayout.HelpBox(
                        "ScriptableItemに.jsファイルがセットされているため、このスクリプトは使用されません。\n", MessageType.Warning
                    );
                }
                var clusterScripts = serializedObject.FindProperty("clusterScripts");
                EditorGUILayout.PropertyField(
                    clusterScripts, new GUIContent("結合するClusterScript")
                );
            }

            if(target.hasPlayerScript())
            {
                if (target.hasPlayerScriptAsset())
                {
                    EditorGUILayout.HelpBox(
                        "PlayerScriptに.jsファイルがセットされているため、このスクリプトは使用されません。\n", MessageType.Warning
                    );
                }
                var playerScripts = serializedObject.FindProperty("playerScripts");
                EditorGUILayout.PropertyField(
                    playerScripts, new GUIContent("結合するPlayerScript")
                );
            }

            EditorGUILayout.HelpBox(
                "このオブジェクトのスクリプトを更新します。", MessageType.Info
            );
            if (GUILayout.Button("更新"))
            {
                target.ClearMD5();
                target.Combine();
            }
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(
                "現在のシーンと、プロジェクト内のプレハブのスクリプトを更新します。", MessageType.Info
            );
            if (GUILayout.Button("全更新"))
            {
                CSCombiner.CombineAll();
            }

            serializedObject.ApplyModifiedProperties();

        }
    }
}
