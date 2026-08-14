using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public sealed class ActivityVisibilityDefinition
    {
        private readonly ReadOnlyCollection<ModelElementMember> members;

        public ActivityVisibilityDefinition(
            string activityId,
            IEnumerable<ModelElementMember> members)
        {
            ActivityId = ActivityVisibilityCollection.RequireDomainId(activityId, nameof(activityId));
            this.members = ActivityVisibilityCollection.CanonicalizeMembers(members);
        }

        public string ActivityId { get; }

        public IReadOnlyList<ModelElementMember> Members => members;

        internal bool IsVisibilityEquivalentTo(ActivityVisibilityDefinition other)
        {
            return other != null
                && string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal)
                && ActivityVisibilityCollection.SequenceEqual(members, other.members);
        }
    }
}
