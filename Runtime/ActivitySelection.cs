using System;

namespace Deucarian.ActivityVisualization
{
    public enum ActivitySelectionKind
    {
        Activity = 0,
        Step = 1
    }

    /// <summary>
    /// Stable domain selection. It intentionally contains no camera or Unity object data.
    /// </summary>
    public sealed class ActivitySelection : IEquatable<ActivitySelection>
    {
        private ActivitySelection(ActivitySelectionKind kind, string activityId, string stepId)
        {
            Kind = kind;
            ActivityId = RequireId(activityId, nameof(activityId));
            StepId = kind == ActivitySelectionKind.Step ? RequireId(stepId, nameof(stepId)) : null;
        }

        public ActivitySelectionKind Kind { get; }

        public string ActivityId { get; }

        public string StepId { get; }

        public static ActivitySelection ForActivity(string activityId)
        {
            return new ActivitySelection(ActivitySelectionKind.Activity, activityId, null);
        }

        public static ActivitySelection ForStep(string activityId, string stepId)
        {
            return new ActivitySelection(ActivitySelectionKind.Step, activityId, stepId);
        }

        public bool Equals(ActivitySelection other)
        {
            return other != null
                && Kind == other.Kind
                && string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal)
                && string.Equals(StepId, other.StepId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as ActivitySelection);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ActivityId);
                hash = (hash * 397) ^ (StepId != null ? StringComparer.Ordinal.GetHashCode(StepId) : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return Kind == ActivitySelectionKind.Activity
                ? "activity:" + ActivityId
                : "activity:" + ActivityId + "/step:" + StepId;
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable identifier is required.", parameterName);
            }

            return value.Trim();
        }
    }
}
