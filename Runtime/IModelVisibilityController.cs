namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Applies a full replacement plan. Implementations should validate all targets before mutating any target,
    /// skip targets already in the requested state, and report whether reconciliation changed visibility.
    /// </summary>
    public interface IModelVisibilityController
    {
        VisibilityApplyResult Apply(VisibilityPlan plan);
    }
}
