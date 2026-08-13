using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public sealed class ActivityVisibilityDefinition
    {
        private readonly ReadOnlyCollection<ModelElementMember> members;
        private readonly ReadOnlyCollection<ActivityStepVisibilityDefinition> steps;
        private readonly Dictionary<string, ActivityStepVisibilityDefinition> stepsById;

        public ActivityVisibilityDefinition(
            string activityId,
            IEnumerable<ModelElementMember> members,
            IEnumerable<ActivityStepVisibilityDefinition> steps = null)
        {
            ActivityId = ActivityVisibilityCollection.RequireDomainId(activityId, nameof(activityId));
            this.members = ActivityVisibilityCollection.CanonicalizeMembers(members);
            stepsById = new Dictionary<string, ActivityStepVisibilityDefinition>(StringComparer.Ordinal);

            if (steps != null)
            {
                foreach (ActivityStepVisibilityDefinition step in steps)
                {
                    if (step == null)
                    {
                        throw new ArgumentException("Step definitions cannot contain null.", nameof(steps));
                    }

                    if (stepsById.ContainsKey(step.StepId))
                    {
                        throw new ArgumentException("Duplicate Step identifier: " + step.StepId, nameof(steps));
                    }

                    stepsById.Add(step.StepId, step);
                }
            }

            List<ActivityStepVisibilityDefinition> sortedSteps = new List<ActivityStepVisibilityDefinition>(stepsById.Values);
            sortedSteps.Sort((left, right) => string.Compare(left.StepId, right.StepId, StringComparison.Ordinal));
            this.steps = sortedSteps.AsReadOnly();
        }

        public string ActivityId { get; }

        public IReadOnlyList<ModelElementMember> Members => members;

        public IReadOnlyList<ActivityStepVisibilityDefinition> Steps => steps;

        public bool TryGetStep(string stepId, out ActivityStepVisibilityDefinition step)
        {
            if (stepId == null)
            {
                step = null;
                return false;
            }

            return stepsById.TryGetValue(stepId, out step);
        }

        internal bool IsVisibilityEquivalentTo(ActivityVisibilityDefinition other)
        {
            if (other == null
                || !string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal)
                || !ActivityVisibilityCollection.SequenceEqual(members, other.members)
                || steps.Count != other.steps.Count)
            {
                return false;
            }

            for (int index = 0; index < steps.Count; index++)
            {
                if (!steps[index].IsVisibilityEquivalentTo(other.steps[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
