using System.Collections.Generic;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Read-only set of model elements that can receive visibility state.
    /// </summary>
    public interface IModelElementIndex
    {
        IReadOnlyList<ModelElementId> ElementIds { get; }

        bool Contains(ModelElementId elementId);
    }
}
