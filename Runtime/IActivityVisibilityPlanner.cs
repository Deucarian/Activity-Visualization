namespace Deucarian.ActivityVisualization
{
    public interface IActivityVisibilityPlanner
    {
        VisibilityPlanningResult CreateSelectionPlan(
            long revision,
            ActivityPreviewSnapshot snapshot,
            ActivitySelection selection,
            IModelElementIndex modelIndex);
    }
}
