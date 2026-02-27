using UnityEngine;

namespace NAS.Core
{
    public class CarSelectionManager : MonoBehaviour
    {
        public static CarSelectionManager Instance { get; private set; }

        [SerializeField] private GameObject _selectedCarPrefab;

        public GameObject SelectedCarPrefab
        {
            get => _selectedCarPrefab;
            set => _selectedCarPrefab = value;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}