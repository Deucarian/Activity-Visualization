using System;

namespace Deucarian.ActivityVisualization
{
    public enum ModelElementRequirement
    {
        Optional = 0,
        Required = 1
    }

    /// <summary>
    /// One normalized Activity membership reference.
    /// </summary>
    public readonly struct ModelElementMember : IEquatable<ModelElementMember>
    {
        public ModelElementMember(ModelElementId elementId, ModelElementRequirement requirement = ModelElementRequirement.Required)
        {
            if (!elementId.IsValid)
            {
                throw new ArgumentException("A valid model element identifier is required.", nameof(elementId));
            }

            ElementId = elementId;
            Requirement = requirement;
        }

        public ModelElementId ElementId { get; }

        public ModelElementRequirement Requirement { get; }

        public bool Equals(ModelElementMember other)
        {
            return ElementId.Equals(other.ElementId) && Requirement == other.Requirement;
        }

        public override bool Equals(object obj)
        {
            return obj is ModelElementMember other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ElementId.GetHashCode() * 397) ^ (int)Requirement;
            }
        }
    }
}
