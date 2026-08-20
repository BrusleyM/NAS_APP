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
    /// Reads which renderers count as "body paint" from CarComponents
    /// (CarComponents.PaintableBodyRenderers) instead of guessing from
    /// material names - this used to be a material-name heuristic
    /// ("contains 'paint'", excluding "trim") because these were
    /// Sketchfab-sourced models with inconsistent material naming per car.
    /// The Blender mesh-regrouping pass replaced that inconsistency with a
    /// real per-car component structure, so this now trusts that structure
    /// directly. Cars without a CarComponents (or without any paintable
    /// renderers on it) silently keep their original color instead of
    /// guessing wrong - this never throws or blocks anything else in the
    /// scene.
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

            var components = _placedInstance.GetComponent<CarComponents>();
            if (components == null)
            {
                Debug.LogWarning("CarPaintController: no CarComponents on the placed car - color not applied.");
                return;
            }

            var applied = 0;
            foreach (var renderer in components.PaintableBodyRenderers)
            {
                foreach (var material in renderer.materials)
                {
                    // glTFast's runtime-generated Shader Graph materials
                    // (Shader Graphs/glTF-pbrMetallicRoughness and its
                    // variants) don't expose Unity's usual "_Color"/
                    // "_BaseColor" - material.color is a no-op on them, no
                    // error, no warning, it just silently does nothing. The
                    // property they actually expose is "baseColorFactor",
                    // matching the glTF spec's own field name.
                    if (material.HasProperty(BaseColorProperty))
                        material.SetColor(BaseColorProperty, color);
                    else if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", color);
                    else if (material.HasProperty("_Color"))
                        material.color = color;
                    else
                        continue;
                    applied++;
                }
            }

            if (applied == 0)
                Debug.LogWarning("CarPaintController: CarComponents found no paintable body renderers on this car - color not applied.");
        }

        private static readonly int BaseColorProperty = Shader.PropertyToID("baseColorFactor");
    }
}
