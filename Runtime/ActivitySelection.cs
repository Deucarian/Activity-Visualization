using System;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Stable domain selection. It intentionally contains no camera or Unity object data.
    /// </summary>
    public sealed class ActivitySelection : IEquatable<ActivitySelection>
    {
        private ActivitySelection(string activityId)
        {
            ActivityId = RequireId(activityId, nameof(activityId));
        }

        public string ActivityId { get; }

        public static ActivitySelection ForActivity(string activityId)
        {
            return new ActivitySelection(activityId);
        }

        public bool Equals(ActivitySelection other)
        {
            return other != null
                && string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as ActivitySelection);

        public override int GetHashCode()
        {
            unchecked
            {
                return StringComparer.Ordinal.GetHashCode(ActivityId);
            }
        }

        public override string ToString()
        {
            return "activity:" + ActivityId;
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
