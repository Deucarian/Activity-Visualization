using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public sealed class ActivityStepVisibilityDefinition
    {
        private readonly ReadOnlyCollection<ModelElementMember> members;

        public ActivityStepVisibilityDefinition(string stepId, IEnumerable<ModelElementMember> members)
        {
            StepId = ActivityVisibilityCollection.RequireDomainId(stepId, nameof(stepId));
            this.members = ActivityVisibilityCollection.CanonicalizeMembers(members);
        }

        public string StepId { get; }

        public IReadOnlyList<ModelElementMember> Members => members;

        internal bool IsVisibilityEquivalentTo(ActivityStepVisibilityDefinition other)
        {
            return other != null
                && string.Equals(StepId, other.StepId, StringComparison.Ordinal)
                && ActivityVisibilityCollection.SequenceEqual(members, other.members);
        }
    }
}
