using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public enum ActivityVisualizationUpdateOutcome
    {
        Applied = 0,
        AcceptedNoChange = 1,
        StaleRevision = 2,
        PreviewUnavailable = 3,
        InvalidSelection = 4,
        RequiredMembersMissing = 5,
        PlanningFailed = 6,
        ApplyFailed = 7,
        Queued = 8,
        Superseded = 9
    }

    public sealed class ActivityVisualizationUpdateResult
    {
        internal ActivityVisualizationUpdateResult(
            ActivityVisualizationUpdateOutcome outcome,
            long revision,
            bool visibilityChanged,
            IEnumerable<ModelElementId> missingRequiredMembers = null,
            IEnumerable<ModelElementId> missingOptionalMembers = null,
            string failureCode = null)
        {
            Outcome = outcome;
            Revision = revision;
            VisibilityChanged = visibilityChanged;
            MissingRequiredMembers = Copy(missingRequiredMembers);
            MissingOptionalMembers = Copy(missingOptionalMembers);
            FailureCode = failureCode;
        }

        public ActivityVisualizationUpdateOutcome Outcome { get; }

        public long Revision { get; }

        public bool VisibilityChanged { get; }

        public IReadOnlyList<ModelElementId> MissingRequiredMembers { get; }

        public IReadOnlyList<ModelElementId> MissingOptionalMembers { get; }

        public string FailureCode { get; }

        public bool IsAccepted => Outcome != ActivityVisualizationUpdateOutcome.StaleRevision;

        public bool IsSuccessful => Outcome == ActivityVisualizationUpdateOutcome.Applied
            || Outcome == ActivityVisualizationUpdateOutcome.AcceptedNoChange
            || Outcome == ActivityVisualizationUpdateOutcome.Queued
            || Outcome == ActivityVisualizationUpdateOutcome.Superseded;

        private static ReadOnlyCollection<ModelElementId> Copy(IEnumerable<ModelElementId> identifiers)
        {
            List<ModelElementId> copy = identifiers != null
                ? new List<ModelElementId>(identifiers)
                : new List<ModelElementId>();
            copy.Sort();
            return copy.AsReadOnly();
        }
    }
}
