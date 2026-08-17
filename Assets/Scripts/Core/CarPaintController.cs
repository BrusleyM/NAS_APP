using NAS.Core.Events;
using UnityEngine;

namespace NAS.Core
{
    /// <summary>
    /// Applies the AR Customize sheet's selected paint color to the currently
    /// placed car model. Purely event-driven - never talks to
    /// ObjectPlacerController or ArViewportController directly, just listens
    /// for CarPlacedEvent (to know which instance to paint) and
    /// PaintColorSelectedEvent (to know what color).
    ///
    /// Material matching is a best-effort heuristic, not a guarantee: these are
    /// Sketchfab-sourced models with inconsistent material naming per car (see
    /// .claude/CLAUDE.md's "Vehicle catalog" section). A material whose name
    /// contains "paint" or is exactly "body" (case-insensitive) is treated as
    /// the body paint material. Cars without a matching material silently keep
    /// their original color instead of guessing wrong - this never throws or
    /// blocks anything else in the scene.
    /// </summary>
    public class CarPaintController : MonoBehaviour
    {
        private GameObject _placedInstance;

        private void OnEnable()
        {
            EventBus.Subscribe<CarPlacedEvent>(HandleCarPlaced);
            EventBus.Subscribe<PaintColorSelectedEvent>(HandlePaintColorSelected);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CarPlacedEvent>(HandleCarPlaced);
            EventBus.Unsubscribe<PaintColorSelectedEvent>(HandlePaintColorSelected);
        }

        private void HandleCarPlaced(CarPlacedEvent evt)
        {
            _placedInstance = evt.Instance;
        }

        private void HandlePaintColorSelected(PaintColorSelectedEvent evt)
        {
            if (_placedInstance == null)
            {
                Debug.LogWarning("CarPaintController: no placed car to paint yet.");
                return;
            }

            if (!ColorUtility.TryParseHtmlString(evt.HexCode, out var color))
            {
                Debug.LogWarning($"CarPaintController: '{evt.HexCode}' is not a valid color.");
                return;
            }

            var applied = 0;
            foreach (var renderer in _placedInstance.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.materials)
                {
                    if (!IsPaintMaterial(material.name)) continue;
                    material.color = color;
                    applied++;
                }
            }

            if (applied == 0)
                Debug.LogWarning("CarPaintController: no body paint material found on this car - color not applied.");
        }

        private static bool IsPaintMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return false;

            // Unity suffixes " (Instance)" to material.name once accessed via
            // .materials (which auto-instantiates) - strip it before matching.
            var name = materialName.Replace(" (Instance)", "");

            // Verified against the BMW M5 model: alongside its real body-paint
            // material ("bM_CarPaint_Max1") it also ships
            // "bM_CarPaint_Trim_CarbonA_Max1" and
            // "bM_CarPaint_Trim_PlasticSmoothBlack_Max1" - both contain "paint"
            // but are trim pieces that should stay their own color, not follow
            // the body. Excluding "trim" catches both without excluding the
            // real paint material on any of the other cars checked.
            if (name.IndexOf("trim", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return name.IndexOf("paint", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.Equals("body", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
