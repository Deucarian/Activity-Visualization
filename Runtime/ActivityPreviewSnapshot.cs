using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// A complete normalized preview containing visibility-relevant data only.
    /// </summary>
    public sealed class ActivityPreviewSnapshot
    {
        private readonly ReadOnlyCollection<ActivityVisibilityDefinition> activities;
        private readonly Dictionary<string, ActivityVisibilityDefinition> activitiesById;

        public ActivityPreviewSnapshot(long revision, IEnumerable<ActivityVisibilityDefinition> activities)
        {
            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), "Revision cannot be negative.");
            }

            Revision = revision;
            activitiesById = new Dictionary<string, ActivityVisibilityDefinition>(StringComparer.Ordinal);
            if (activities != null)
            {
                foreach (ActivityVisibilityDefinition activity in activities)
                {
                    if (activity == null)
                    {
                        throw new ArgumentException("Activity definitions cannot contain null.", nameof(activities));
                    }

                    if (activitiesById.ContainsKey(activity.ActivityId))
                    {
                        throw new ArgumentException("Duplicate Activity identifier: " + activity.ActivityId, nameof(activities));
                    }

                    activitiesById.Add(activity.ActivityId, activity);
                }
            }

            List<ActivityVisibilityDefinition> sorted = new List<ActivityVisibilityDefinition>(activitiesById.Values);
            sorted.Sort((left, right) => string.Compare(left.ActivityId, right.ActivityId, StringComparison.Ordinal));
            this.activities = sorted.AsReadOnly();
        }

        public long Revision { get; }

        public IReadOnlyList<ActivityVisibilityDefinition> Activities => activities;

        public bool TryGetActivity(string activityId, out ActivityVisibilityDefinition activity)
        {
            if (activityId == null)
            {
                activity = null;
                return false;
            }

            return activitiesById.TryGetValue(activityId, out activity);
        }

        public bool IsVisibilityEquivalentTo(ActivityPreviewSnapshot other)
        {
            if (other == null || activities.Count != other.activities.Count)
            {
                return false;
            }

            for (int index = 0; index < activities.Count; index++)
            {
                if (!activities[index].IsVisibilityEquivalentTo(other.activities[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
