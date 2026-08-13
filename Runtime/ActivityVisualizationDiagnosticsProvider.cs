using Deucarian.Diagnostics;

namespace Deucarian.ActivityVisualization
{
    internal sealed class ActivityVisualizationDiagnosticsProvider : IDiagnosticProvider
    {
        private readonly ActivityVisualizationStateOwner owner;

        public ActivityVisualizationDiagnosticsProvider(ActivityVisualizationStateOwner owner)
        {
            this.owner = owner;
        }

        public string ProviderId => "activity_visualization";

        public string DisplayName => "Activity Visualization";

        public void Collect(DiagnosticReportBuilder builder)
        {
            ActivityVisualizationDiagnosticSnapshot snapshot = owner.CaptureDiagnosticSnapshot();
            DiagnosticSeverity severity = snapshot.ApplyFailureCount > 0
                || snapshot.PlanningFailureCount > 0
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Info;

            builder.AddSection(ProviderId, DisplayName)
                .AddItem("disposed", "Disposed", snapshot.IsDisposed.ToString(), DiagnosticSeverity.Info)
                .AddItem("latest_revision", "Latest Revision", snapshot.LatestRevision.ToString(), DiagnosticSeverity.Info)
                .AddItem("selection", "Has Active Selection", snapshot.HasSelection.ToString(), DiagnosticSeverity.Info)
                .AddItem("last_outcome", "Last Outcome", snapshot.LastOutcome.ToString(), severity)
                .AddItem("stale_updates", "Stale Updates", snapshot.StaleUpdateCount.ToString(), DiagnosticSeverity.Info)
                .AddItem("invalid_selections", "Invalid Selections", snapshot.InvalidSelectionCount.ToString(), DiagnosticSeverity.Info)
                .AddItem("planning_failures", "Planning Failures", snapshot.PlanningFailureCount.ToString(), severity)
                .AddItem("apply_failures", "Apply Failures", snapshot.ApplyFailureCount.ToString(), severity)
                .AddItem("optional_members_missing", "Optional Members Missing", snapshot.OptionalMissingCount.ToString(), DiagnosticSeverity.Info);
        }
    }

    internal readonly struct ActivityVisualizationDiagnosticSnapshot
    {
        public ActivityVisualizationDiagnosticSnapshot(
            bool isDisposed,
            long latestRevision,
            bool hasSelection,
            ActivityVisualizationUpdateOutcome lastOutcome,
            int staleUpdateCount,
            int invalidSelectionCount,
            int planningFailureCount,
            int applyFailureCount,
            int optionalMissingCount)
        {
            IsDisposed = isDisposed;
            LatestRevision = latestRevision;
            HasSelection = hasSelection;
            LastOutcome = lastOutcome;
            StaleUpdateCount = staleUpdateCount;
            InvalidSelectionCount = invalidSelectionCount;
            PlanningFailureCount = planningFailureCount;
            ApplyFailureCount = applyFailureCount;
            OptionalMissingCount = optionalMissingCount;
        }

        public bool IsDisposed { get; }
        public long LatestRevision { get; }
        public bool HasSelection { get; }
        public ActivityVisualizationUpdateOutcome LastOutcome { get; }
        public int StaleUpdateCount { get; }
        public int InvalidSelectionCount { get; }
        public int PlanningFailureCount { get; }
        public int ApplyFailureCount { get; }
        public int OptionalMissingCount { get; }
    }
}
