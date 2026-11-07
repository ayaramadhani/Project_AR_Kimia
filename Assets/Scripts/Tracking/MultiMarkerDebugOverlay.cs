using System;
using UnityEngine;
using Vuforia;
using ARKimia.Gameplay;

namespace ARKimia.Tracking
{
    public class MultiMarkerDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;

        private ElementTargetHandler[] handlers = Array.Empty<ElementTargetHandler>();
        private float nextRefreshTime;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
                return;

            handlers = FindObjectsByType<ElementTargetHandler>();
            Array.Sort(handlers, (a, b) => string.CompareOrdinal(a.name, b.name));
            nextRefreshTime = Time.unscaledTime + 2f;
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
                return;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.06f, 0.09f, 0.14f, 0.85f));
            bg.Apply();

            boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 8, 8) };
            boxStyle.normal.background = bg;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                richText = true,
                normal = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            if (!showOverlay)
                return;

            EnsureStyles();

            float scale = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 430f, 1f, 2.75f);
            Matrix4x4 prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            Rect safe = Screen.safeArea;
            float x = (safe.xMin / scale) + 10f;
            float y = ((Screen.height - safe.yMax) / scale) + 10f;
            float width = Mathf.Min(320f, (safe.width / scale) - 20f);

            int tracked = ElementCombinationManager.Instance != null
                ? ElementCombinationManager.Instance.TrackedMarkerCount
                : 0;
            bool running = VuforiaApplication.Instance != null && VuforiaApplication.Instance.IsRunning;
            int maxSim = VuforiaConfiguration.Instance != null
                ? VuforiaConfiguration.Instance.Vuforia.MaxSimultaneousImageTargets
                : 0;

            GUILayout.BeginArea(new Rect(x, y, width, (safe.height / scale) - 20f));
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label($"<b>Multi-Marker Check</b> | Vuforia: {(running ? "<color=#59FF8A>ON</color>" : "<color=#FF6B6B>OFF</color>")} (Max {maxSim})", labelStyle);
            GUILayout.Label($"Tracked: <b>{tracked} / {handlers.Length}</b>", labelStyle);

            for (int i = 0; i < handlers.Length; i++)
            {
                ElementTargetHandler h = handlers[i];
                if (h == null)
                    continue;

                string sym = h.ElementData != null ? h.ElementData.Symbol : "?";
                string col = h.IsTracked ? "#59FF8A" : "#9AA5B1";
                GUILayout.Label($"• <b>{sym}</b> ({h.name}): <color={col}>{h.CurrentStatus}</color>", labelStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = prev;
        }
    }
}
