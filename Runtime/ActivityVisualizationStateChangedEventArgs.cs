using System;

namespace Deucarian.ActivityVisualization
{
    public sealed class ActivityVisualizationStateChangedEventArgs : EventArgs
    {
        internal ActivityVisualizationStateChangedEventArgs(
            ActivityVisualizationUpdateResult result,
            ActivitySelection previousSelection,
            ActivitySelection currentSelection,
            bool previewChanged)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            PreviousSelection = previousSelection;
            CurrentSelection = currentSelection;
            PreviewChanged = previewChanged;
        }

        public ActivityVisualizationUpdateResult Result { get; }

        public ActivitySelection PreviousSelection { get; }

        public ActivitySelection CurrentSelection { get; }

        public bool PreviewChanged { get; }
    }
}
