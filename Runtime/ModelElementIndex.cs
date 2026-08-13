using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public sealed class ModelElementIndex : IModelElementIndex
    {
        private readonly HashSet<ModelElementId> identifiers;
        private readonly ReadOnlyCollection<ModelElementId> elementIds;

        public ModelElementIndex(IEnumerable<ModelElementId> elementIds)
        {
            if (elementIds == null)
            {
                throw new ArgumentNullException(nameof(elementIds));
            }

            identifiers = new HashSet<ModelElementId>();
            foreach (ModelElementId elementId in elementIds)
            {
                if (!elementId.IsValid)
                {
                    throw new ArgumentException("The model index cannot contain an invalid identifier.", nameof(elementIds));
                }

                identifiers.Add(elementId);
            }

            List<ModelElementId> sorted = new List<ModelElementId>(identifiers);
            sorted.Sort();
            this.elementIds = sorted.AsReadOnly();
        }

        public IReadOnlyList<ModelElementId> ElementIds => elementIds;

        public bool Contains(ModelElementId elementId)
        {
            return identifiers.Contains(elementId);
        }
    }
}
