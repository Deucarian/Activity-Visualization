using System;

namespace Deucarian.ActivityVisualization
{
    public interface IReadOnlyActivityVisualizationState
    {
        event EventHandler<ActivityVisualizationStateChangedEventArgs> StateChanged;

        long LatestRevision { get; }

        ActivityPreviewSnapshot Preview { get; }

        ActivitySelection Selection { get; }

        VisibilityPlan ActivePlan { get; }

        ActivityVisualizationUpdateOutcome LastOutcome { get; }

        bool IsDisposed { get; }
    }
}
