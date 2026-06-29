using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NekoLegends
{
    public class AnimeCelShaderV3DemoBtns : MonoBehaviour
    {
        [SerializeField] private List<MeshRenderer> targets = new();

        [Header("Animation Settings")]
        [SerializeField, Min(0.01f)] private float dissolveDuration = 1f; 
        [SerializeField] private Ease easeType = Ease.EaseInOut;

        private enum Ease { Linear, EaseIn, EaseOut, EaseInOut }

        // Prevent overlapping same-property coroutines per target
        private readonly Dictionary<string, Coroutine> activeCoroutines = new();

        // Cache original outline enabled state per renderer
        private readonly Dictionary<MeshRenderer, bool[]> outlineCache = new();

        /* ---------- PUBLIC BUTTON METHODS (single int param) ---------- */

        public void ToggleEdgeDissolve(int targetIndex) =>
            StartCoroutineWrapper(targetIndex, "_dissolve_edge");

        public void ToggleSecondTexDissolve(int targetIndex) =>
            StartCoroutineWrapper(targetIndex, "_BuffTex_switch_dissolve");

        public void ToggleMatcapDissolve(int targetIndex) =>
            StartCoroutineWrapper(targetIndex, "_special_buff_dissolve");

        /* ---------- INTERNALS ---------- */

        private void StartCoroutineWrapper(int index, string propName)
        {
            string key = $"{index}_{propName}";
            if (activeCoroutines.TryGetValue(key, out var existing))
            {
                StopCoroutine(existing);
                activeCoroutines.Remove(key);
            }

            var coro = AnimateDissolve(index, propName);
            activeCoroutines[key] = StartCoroutine(coro);
        }

        private IEnumerator AnimateDissolve(int index, string propName)
        {
            if (index < 0 || index >= targets.Count) yield break;
            var mr = targets[index];
            if (mr == null) yield break;

            string key = $"{index}_{propName}";
            var mats = mr.materials; // accessing this instantiates if needed

            // Ensure global dissolve toggle is on if the shader has it
            foreach (var m in mats)
                if (m.HasProperty("_use_dissolve"))
                    SetToggleKeyword(m, "_use_dissolve", "_USE_DISSOLVE_ON", true);

            bool isEdge = propName == "_dissolve_edge";

            // Cache original outline state (only once) and immediately disable outlines for edge dissolve
            if (isEdge)
            {
                if (!outlineCache.ContainsKey(mr))
                {
                    bool[] orig = new bool[mats.Length];
                    for (int i = 0; i < mats.Length; ++i)
                    {
                        if (mats[i].HasProperty("_use_outlines"))
                        {
                            float f = mats[i].GetFloat("_use_outlines");
                            orig[i] = f > 0.5f;
                        }
                        else
                        {
                            orig[i] = false;
                        }
                    }
                    outlineCache[mr] = orig;
                }

                // disable outlines regardless of animation direction (but remember original)
                for (int i = 0; i < mats.Length; ++i)
                {
                    if (mats[i].HasProperty("_use_outlines"))
                    {
                        SetToggleKeyword(mats[i], "_use_outlines", "_USE_OUTLINES_ON", false);
                    }
                }
            }

            // Determine current toggle state (majority rule)
            float start;
            {
                int have = 0, onCount = 0;
                foreach (var m in mats)
                {
                    if (m.HasProperty(propName))
                    {
                        have++;
                        if (m.GetFloat(propName) > 0.5f) onCount++;
                    }
                }
                if (have == 0)
                    start = 1f;
                else
                    start = (onCount * 2 >= have) ? 1f : 0f; // majority => treat as 1
            }

            float end = Mathf.Approximately(start, 1f) ? 0f : 1f;
            float duration = Mathf.Max(0.0001f, dissolveDuration);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float k = EaseEval(t, easeType);
                float val = Mathf.Lerp(start, end, k);
                foreach (var m in mats)
                    if (m.HasProperty(propName))
                        m.SetFloat(propName, val);
                yield return null;
            }

            // Snap to final
            foreach (var m in mats)
                if (m.HasProperty(propName))
                    m.SetFloat(propName, end);

            // If edge dissolve ended back at "1" (i.e., toggled to the non-dissolved state), restore outlines if they were originally on
            if (isEdge && Mathf.Approximately(end, 1f))
            {
                if (outlineCache.TryGetValue(mr, out var original))
                {
                    for (int i = 0; i < mats.Length; ++i)
                    {
                        if (original[i] && mats[i].HasProperty("_use_outlines"))
                        {
                            SetToggleKeyword(mats[i], "_use_outlines", "_USE_OUTLINES_ON", true);
                        }
                    }
                }
            }

            activeCoroutines.Remove(key);
        }

        // Helper to set toggle-like float and its corresponding keyword
        private static void SetToggleKeyword(Material m, string floatProp, string keyword, bool enabled)
        {
            m.SetFloat(floatProp, enabled ? 1f : 0f);
            if (enabled)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        private static float EaseEval(float x, Ease e)
        {
            x = Mathf.Clamp01(x);
            return e switch
            {
                Ease.Linear => x,
                Ease.EaseIn => x * x,
                Ease.EaseOut => 1f - (1f - x) * (1f - x),
                Ease.EaseInOut => x < .5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f,
                _ => x,
            };
        }
    }
}
