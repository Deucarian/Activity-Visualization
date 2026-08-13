namespace Deucarian.ActivityVisualization
{
    public interface IActivityVisualizationCommands
    {
        ActivityVisualizationUpdateResult ReplacePreview(ActivityPreviewSnapshot snapshot);

        ActivityVisualizationUpdateResult Select(ActivitySelection selection, long revision);

        ActivityVisualizationUpdateResult Clear(long revision);
    }
}
