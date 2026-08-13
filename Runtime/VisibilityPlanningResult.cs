using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public enum VisibilityPlanningOutcome
    {
        Planned = 0,
        InvalidSelection = 1,
        RequiredMembersMissing = 2
    }

    public sealed class VisibilityPlanningResult
    {
        private readonly ReadOnlyCollection<ModelElementId> missingRequiredMembers;
        private readonly ReadOnlyCollection<ModelElementId> missingOptionalMembers;

        private VisibilityPlanningResult(
            VisibilityPlanningOutcome outcome,
            VisibilityPlan plan,
            IEnumerable<ModelElementId> missingRequiredMembers,
            IEnumerable<ModelElementId> missingOptionalMembers)
        {
            Outcome = outcome;
            Plan = plan;
            this.missingRequiredMembers = CopySorted(missingRequiredMembers);
            this.missingOptionalMembers = CopySorted(missingOptionalMembers);
        }

        public VisibilityPlanningOutcome Outcome { get; }

        public VisibilityPlan Plan { get; }

        public IReadOnlyList<ModelElementId> MissingRequiredMembers => missingRequiredMembers;

        public IReadOnlyList<ModelElementId> MissingOptionalMembers => missingOptionalMembers;

        public bool Succeeded => Outcome == VisibilityPlanningOutcome.Planned;

        public static VisibilityPlanningResult Planned(
            VisibilityPlan plan,
            IEnumerable<ModelElementId> missingOptionalMembers)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            return new VisibilityPlanningResult(
                VisibilityPlanningOutcome.Planned,
                plan,
                Array.Empty<ModelElementId>(),
                missingOptionalMembers);
        }

        public static VisibilityPlanningResult InvalidSelection()
        {
            return new VisibilityPlanningResult(
                VisibilityPlanningOutcome.InvalidSelection,
                null,
                Array.Empty<ModelElementId>(),
                Array.Empty<ModelElementId>());
        }

        public static VisibilityPlanningResult RequiredMembersMissing(
            IEnumerable<ModelElementId> missingRequiredMembers,
            IEnumerable<ModelElementId> missingOptionalMembers)
        {
            return new VisibilityPlanningResult(
                VisibilityPlanningOutcome.RequiredMembersMissing,
                null,
                missingRequiredMembers,
                missingOptionalMembers);
        }

        private static ReadOnlyCollection<ModelElementId> CopySorted(IEnumerable<ModelElementId> identifiers)
        {
            List<ModelElementId> copy = identifiers != null
                ? new List<ModelElementId>(identifiers)
                : new List<ModelElementId>();
            copy.Sort();
            return copy.AsReadOnly();
        }
    }
}
