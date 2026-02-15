#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FanArrangeEqualGapWindow : EditorWindow
{
    GameObject target;
    float innerRadius = 1f;
    float outerRadius = 5f;
    float angleRange  = 180f;
    bool randomizeOrder = false;
    bool renameChildren = false;

    [MenuItem("Tools/Fan Arrange Children")]
    static void OpenWindow()
    {
        GetWindow<FanArrangeEqualGapWindow>("Fan Arrange (Consistent Density)").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Fan-Arrange Children (Consistent Density)", EditorStyles.boldLabel);
        target      = (GameObject)EditorGUILayout.ObjectField("Target", target, typeof(GameObject), true);
        innerRadius = EditorGUILayout.FloatField("Inner Radius", innerRadius);
        outerRadius = EditorGUILayout.FloatField("Outer Radius", outerRadius);
        angleRange  = EditorGUILayout.FloatField("Fan Angle (°)", angleRange);
        randomizeOrder = EditorGUILayout.Toggle("Randomize Order", randomizeOrder);
        renameChildren = EditorGUILayout.Toggle("Rename Children (other_1, 2, ...)", renameChildren);

        if (GUILayout.Button("Arrange Children"))
            ArrangeConsistentDensity();

        if (GUILayout.Button("Calculate Current Density"))
            CalculateCurrentDensity();
    }

    void ArrangeConsistentDensity()
    {
        if (target == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Target GameObject.", "OK");
            return;
        }

        int N = target.transform.childCount;
        if (N == 0)
        {
            EditorUtility.DisplayDialog("Info", "Target has no children to arrange.", "OK");
            return;
        }
        if (innerRadius < 0 || outerRadius <= innerRadius)
        {
            EditorUtility.DisplayDialog("Error", "Require 0 ≤ Inner < Outer radius.", "OK");
            return;
        }

        float theta = angleRange * Mathf.Deg2Rad;
        Vector3 origin = target.transform.position;
        float effectiveInnerRadius = innerRadius;

        // --- Step 1: Compute ideal ring count K via quadratic formula ---
        // Ring 0 always holds 2 items. Rings 1..K-1 must hold N-2 items.
        // For consistent density, arc spacing ≈ radial spacing = d.
        // Per-ring ideal count: count_i = r_i * theta / d  (i >= 1)
        // where r_i = effectiveInnerRadius + d * i, d = (outerRadius - effectiveInnerRadius) / (K - 1)
        // Substituting and summing: a*u^2 + b*u - c = 0, u = K-1
        int remaining = N - 2;

        int ringCount;
        float d; // spacing

        if (remaining <= 0)
        {
            // Only 1 or 2 children: single ring
            ringCount = 1;
            d = outerRadius - effectiveInnerRadius;
        }
        else
        {
            float radialSpan = outerRadius - effectiveInnerRadius;
            float a = theta * (effectiveInnerRadius + outerRadius) / (2f * radialSpan);
            float b = theta / 2f;
            float c = (float)remaining;

            float discriminant = b * b + 4f * a * c;
            float u = (-b + Mathf.Sqrt(discriminant)) / (2f * a);

            int K = Mathf.Max(2, Mathf.RoundToInt(u) + 1);
            ringCount = K;
            d = radialSpan / (K - 1);
        }

        // --- Steps 1b–3: Compute per-ring counts, adjusting inner radius so
        //     ring-0's chord distance matches the actual min chord on other rings.
        //     Ring 0 has 2 children separated by angleRange/2 degrees.
        //     Chord distance = 2 * r0 * sin(theta/4).
        //     We iterate: compute counts → find min chord on rings 1+ → if ring-0
        //     chord is shorter, bump effectiveInnerRadius and recompute. ---
        int[] counts = new int[ringCount];
        float sinHalfSep = Mathf.Sin(theta / 4f);

        for (int iter = 0; iter < 10; iter++)
        {
            d = (outerRadius - effectiveInnerRadius) / Mathf.Max(1, ringCount - 1);

            // Compute per-ring counts
            float[] fractionals = new float[ringCount];
            counts[0] = 2;
            fractionals[0] = 0f;

            for (int i = 1; i < ringCount; i++)
            {
                float r_i = effectiveInnerRadius + d * i;
                float ideal = r_i * theta / d;
                counts[i] = Mathf.Max(1, Mathf.RoundToInt(ideal));
                fractionals[i] = ideal - Mathf.Floor(ideal);
            }

            // Adjust total to exactly N
            int sum = counts.Sum();
            int diff = N - sum;

            if (diff > 0)
            {
                var indices = Enumerable.Range(1, ringCount - 1)
                    .OrderByDescending(i => fractionals[i])
                    .ToList();
                for (int j = 0; diff > 0; j++)
                {
                    counts[indices[j % indices.Count]]++;
                    diff--;
                }
            }
            else if (diff < 0)
            {
                var indices = Enumerable.Range(1, ringCount - 1)
                    .OrderBy(i => fractionals[i])
                    .ToList();
                for (int j = 0; diff < 0; j++)
                {
                    int idx = indices[j % indices.Count];
                    if (counts[idx] > 1)
                    {
                        counts[idx]--;
                        diff++;
                    }
                }
            }

            // Check ring-0 chord vs actual min chord on rings 1+
            if (ringCount <= 1 || sinHalfSep <= 1e-6f)
                break;

            float minChordOther = float.MaxValue;
            for (int i = 1; i < ringCount; i++)
            {
                float r_i = effectiveInnerRadius + d * i;
                float chord_i = 2f * r_i * Mathf.Sin(theta / (2f * counts[i]));
                if (chord_i < minChordOther) minChordOther = chord_i;
            }

            float ring0Chord = 2f * effectiveInnerRadius * sinHalfSep;

            if (ring0Chord >= minChordOther - 1e-4f)
                break; // ring-0 gap is adequate

            // Increase inner radius so ring-0 chord matches minChordOther
            effectiveInnerRadius = minChordOther / (2f * sinHalfSep);
        }

        // --- Step 4: Position items on each ring ---
        var finalPositions = new List<Vector3>();
        var finalRotations = new List<Quaternion>();
        var finalYOffsets = new List<float>();

        for (int ri = 0; ri < ringCount; ri++)
        {
            int n = counts[ri];
            float r = effectiveInnerRadius + d * ri;
            if (ringCount == 1) r = effectiveInnerRadius; // single ring case

            // Rings 0–1: y=0; ring 2+: y = 0.675 * (ri - 1)
            float yOffset = ri >= 2 ? 0.675f * (ri - 1) : 0f;

            for (int j = 0; j < n; j++)
            {
                float angle = -angleRange / 2f + angleRange * (j + 0.5f) / n;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 pos = origin + dir * r;
                finalPositions.Add(pos);
                finalRotations.Add(Quaternion.LookRotation((origin - pos).normalized));
                finalYOffsets.Add(yOffset);
            }
        }

        // Randomize sibling order in the hierarchy if requested
        Undo.RegisterCompleteObjectUndo(target.transform, "Fan-Arrange Consistent Density");
        if (randomizeOrder)
        {
            for (int i = N - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                target.transform.GetChild(j).SetSiblingIndex(i);
            }
        }

        // Apply to children
        for (int i = 0; i < N && i < finalPositions.Count; i++)
        {
            Transform child = target.transform.GetChild(i);
            Undo.RecordObject(child, "Move Child");
            var pos = finalPositions[i];
            pos.y = origin.y + finalYOffsets[i];
            child.position = pos;
            child.rotation = finalRotations[i];
            EditorUtility.SetDirty(child);
        }

        // Rename children if requested
        if (renameChildren)
        {
            for (int i = 0; i < N; i++)
            {
                Transform child = target.transform.GetChild(i);
                string newName = $"other_{i + 1}";
                Undo.RecordObject(child.gameObject, "Rename Child");
                child.gameObject.name = newName;
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        // --- Verification: compute nearest-neighbor distance stats ---
        float minDist = float.MaxValue;
        float maxDist = 0f;
        double distSum = 0;
        int distCount = 0;

        for (int i = 0; i < finalPositions.Count; i++)
        {
            float nearest = float.MaxValue;
            Vector3 pi = finalPositions[i];
            for (int j = 0; j < finalPositions.Count; j++)
            {
                if (i == j) continue;
                Vector3 pj = finalPositions[j];
                float dist = new Vector2(pi.x - pj.x, pi.z - pj.z).magnitude; // XZ distance
                if (dist < nearest) nearest = dist;
            }
            if (nearest < minDist) minDist = nearest;
            if (nearest > maxDist) maxDist = nearest;
            distSum += nearest;
            distCount++;
        }

        float avgDist = (float)(distSum / distCount);
        string ringInfo = string.Join(", ", counts.Select((c, i) => $"R{i}={c}"));
        string radiusNote = effectiveInnerRadius > innerRadius + 1e-4f
            ? $"\nInner radius adjusted: {innerRadius:F2} → {effectiveInnerRadius:F2}"
            : "";

        Debug.Log($"[FanArrange] Nearest-neighbor distances — min: {minDist:F3}, max: {maxDist:F3}, avg: {avgDist:F3}, spacing d: {d:F3}{radiusNote}");

        EditorUtility.DisplayDialog(
            "Done",
            $"Placed {N} children across {ringCount} rings (d={d:F2}):\n" +
            $"[{ringInfo}]{radiusNote}\n\n" +
            $"Nearest-neighbor distances:\n" +
            $"  min={minDist:F3}  max={maxDist:F3}  avg={avgDist:F3}",
            "OK"
        );
    }

    void CalculateCurrentDensity()
    {
        if (target == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Target GameObject.", "OK");
            return;
        }

        int totalChildren = target.transform.childCount;
        if (totalChildren == 0)
        {
            EditorUtility.DisplayDialog("Info", "Target has no children.", "OK");
            return;
        }
        if (innerRadius < 0 || outerRadius <= innerRadius)
        {
            EditorUtility.DisplayDialog("Error", "Require 0 ≤ Inner < Outer radius.", "OK");
            return;
        }

        Vector3 origin = target.transform.position;
        Vector3 forward = target.transform.forward;
        float halfAngle = angleRange / 2f;

        int insideCount = 0;
        int outsideCount = 0;

        for (int i = 0; i < totalChildren; i++)
        {
            Transform child = target.transform.GetChild(i);
            Vector3 offset = child.position - origin;
            float xzDist = new Vector2(offset.x, offset.z).magnitude;

            if (xzDist < innerRadius || xzDist > outerRadius)
            {
                outsideCount++;
                continue;
            }

            Vector3 dirXZ = new Vector3(offset.x, 0f, offset.z);
            if (dirXZ.sqrMagnitude < 0.0001f)
            {
                // Child is essentially at origin on XZ plane — treat as inside if innerRadius is 0
                if (innerRadius <= 0f)
                    insideCount++;
                else
                    outsideCount++;
                continue;
            }

            float signedAngle = Vector3.SignedAngle(forward, dirXZ, Vector3.up);
            if (Mathf.Abs(signedAngle) <= halfAngle)
                insideCount++;
            else
                outsideCount++;
        }

        float theta = angleRange * Mathf.Deg2Rad;
        float area = 0.5f * (outerRadius * outerRadius - innerRadius * innerRadius) * theta;
        float density = area > 0f ? insideCount / area : 0f;

        string msg =
            $"Total children: {totalChildren}\n" +
            $"Inside fan: {insideCount}\n" +
            $"Outside fan: {outsideCount}\n\n" +
            $"Fan area: {area:F3} sq units\n" +
            $"Density: {density:F4} children/sq unit";

        Debug.Log($"[FanArrange] Density — inside: {insideCount}, outside: {outsideCount}, area: {area:F3}, density: {density:F4}");
        EditorUtility.DisplayDialog("Current Density", msg, "OK");
    }
}
#endif
