using System;

namespace Deucarian.ActivityVisualization
{
    public readonly struct VisibilityAssignment : IEquatable<VisibilityAssignment>
    {
        public VisibilityAssignment(ModelElementId elementId, bool isVisible)
        {
            if (!elementId.IsValid)
            {
                throw new ArgumentException("A valid model element identifier is required.", nameof(elementId));
            }

            ElementId = elementId;
            IsVisible = isVisible;
        }

        public ModelElementId ElementId { get; }

        public bool IsVisible { get; }

        public bool Equals(VisibilityAssignment other)
        {
            return ElementId.Equals(other.ElementId) && IsVisible == other.IsVisible;
        }

        public override bool Equals(object obj)
        {
            return obj is VisibilityAssignment other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ElementId.GetHashCode() * 397) ^ IsVisible.GetHashCode();
            }
        }
    }
}
