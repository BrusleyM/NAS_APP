using NAS.Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace NAS.UI.Components
{
    /// <summary>
    /// Binds one cloned CarCard.uxml instance to a CarData entry.
    /// </summary>
    public class CarCardView
    {
        private readonly VisualElement _root;
        private readonly Image _thumbnail;
        private readonly Label _nameLabel;
        private readonly Label _typeLabel;

        public VisualElement Root => _root;
        public int BoundCarIndex { get; private set; } = -1;

        public CarCardView(VisualTreeAsset template)
        {
            _root = template.CloneTree();
            _root.AddToClassList("car-card");

            _thumbnail = _root.Q<Image>("thumbnail");
            _nameLabel = _root.Q<Label>("name");
            _typeLabel = _root.Q<Label>("type");
        }

public void SetCar(CarData car, int carIndex)
        {
            BoundCarIndex = carIndex;
            _root.RemoveFromClassList("car-card--hidden");
            _root.style.visibility = Visibility.Visible;

            if (_nameLabel != null)
                _nameLabel.text = car.carName;

            if (_typeLabel != null)
                _typeLabel.text = car.category;

            if (_thumbnail != null)
                _thumbnail.image = car.image;
        }

public void SetEmpty()
        {
            BoundCarIndex = -1;
            _root.AddToClassList("car-card--hidden");
            // visibility:hidden (not display:none) so this slot still reserves its
            // layout space — with 3 symmetric slots, justify-content:center on the
            // row naturally centers the middle one. display:none would remove this
            // slot from layout entirely at a boundary (no prev/next car), leaving
            // only 2 children, which breaks that symmetry and shows half of each
            // remaining card instead of one centered card.
            _root.style.visibility = Visibility.Hidden;
        }

        public void SetSelected(bool selected)
        {
            if (selected)
                _root.AddToClassList("car-card--selected");
            else
                _root.RemoveFromClassList("car-card--selected");
        }
    }
}
