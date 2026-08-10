using UnityEngine;

namespace NAS.Core.Models
{
    /// <summary>
    /// One car in the catalog. Lives as an asset under Resources/Cars, so
    /// CarSelectionScreenController can load the whole set at runtime via
    /// Resources.LoadAll<CarData/>("Cars") instead of a hardcoded list.
    ///
    /// NOTE: the display name is "carName", not "name" — ScriptableObject
    /// already has a built-in "name" (the asset's file name), so a second
    /// field avoids confusing the two.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCarData", menuName = "NAS/Car Data")]
    public class CarData : ScriptableObject
    {
        public string carName;
        public string category;

        [Tooltip("Must match one of the filter values used by the type dropdown (Sedan, SUV, Hatchback, Van, etc).")]
        public string type;

        public float retailPrice;

        [Tooltip("Optional. Shown on the car card if assigned; card falls back to a blank image slot if left empty.")]
        public Texture2D image;
    }
}
