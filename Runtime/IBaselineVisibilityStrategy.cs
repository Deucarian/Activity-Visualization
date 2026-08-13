namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Defines the visibility state restored when Activity selection is cleared.
    /// </summary>
    public interface IBaselineVisibilityStrategy
    {
        VisibilityPlan CreatePlan(long revision, IModelElementIndex modelIndex);
    }
}
