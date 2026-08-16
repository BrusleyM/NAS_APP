using UnityEngine;

namespace NAS.Core.Models
{
    /// <summary>
    /// One car in the catalog. Populated at runtime from the backend vehicle
    /// catalog API (see Core/Vehicles/VehicleCatalogApi.cs), or hand-authored
    /// as a design-time asset under Resources/Cars for local fixtures/testing.
    ///
    /// NOTE: the display name is "carName", not "name" — ScriptableObject
    /// already has a built-in "name" (the asset's file name), so a second
    /// field avoids confusing the two.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCarData", menuName = "NAS/Car Data")]
    public class CarData : ScriptableObject
    {
        [Tooltip("The backend vehicle_model row ID. 0 for hand-authored local fixtures that don't correspond to a real DB row.")]
        public int id;

        public string carName;
        public int year;
        public string category;

        [Tooltip("Must match one of the filter values used by the type dropdown (Sedan, SUV, Hatchback, Van, etc).")]
        public string type;

        public float retailPrice;

        [Tooltip("Optional. Shown on the car card if assigned; card falls back to a blank image slot if left empty.")]
        public Texture2D image;

        [Tooltip("Remote thumbnail URL for API-sourced cars, downloaded lazily and cached at runtime (see RemoteTextureLoader). Ignored if 'image' is already assigned.")]
        public string imageUrl;
    }
}
