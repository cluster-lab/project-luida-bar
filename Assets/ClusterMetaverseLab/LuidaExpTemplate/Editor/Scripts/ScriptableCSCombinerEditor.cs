// Guarded by LUIDA_HAS_CSCOMBINER (set automatically by LuidaDependencies when
// KaomoLab CSCombiner is present) so LUIDA still compiles when KaomoLab is absent.
#if LUIDA_HAS_CSCOMBINER
using Assets.KaomoLab.CSCombiner;
using Assets.KaomoLab.CSCombiner.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScriptableClusterScriptCombiner))]
public class ScriptableCSCombinerEditor : CSCombinerEditor
{

}
#endif
