using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    public enum VisibilityPlanKind
    {
        Baseline = 0,
        Selection = 1
    }

    /// <summary>
    /// A deterministic full replacement visibility state for the indexed model.
    /// </summary>
    public sealed class VisibilityPlan
    {
        private readonly ReadOnlyCollection<VisibilityAssignment> assignments;

        public VisibilityPlan(
            long revision,
            VisibilityPlanKind kind,
            IEnumerable<VisibilityAssignment> assignments,
            ActivitySelection selection = null)
        {
            if (revision < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

            if (kind == VisibilityPlanKind.Selection && selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (kind == VisibilityPlanKind.Baseline && selection != null)
            {
                throw new ArgumentException("A baseline plan cannot carry a selection.", nameof(selection));
            }

            if (assignments == null)
            {
                throw new ArgumentNullException(nameof(assignments));
            }

            SortedDictionary<ModelElementId, bool> byIdentifier = new SortedDictionary<ModelElementId, bool>();
            foreach (VisibilityAssignment assignment in assignments)
            {
                if (byIdentifier.ContainsKey(assignment.ElementId))
                {
                    throw new ArgumentException(
                        "A visibility plan cannot assign an element more than once: " + assignment.ElementId,
                        nameof(assignments));
                }

                byIdentifier.Add(assignment.ElementId, assignment.IsVisible);
            }

            List<VisibilityAssignment> sorted = new List<VisibilityAssignment>(byIdentifier.Count);
            foreach (KeyValuePair<ModelElementId, bool> pair in byIdentifier)
            {
                sorted.Add(new VisibilityAssignment(pair.Key, pair.Value));
            }

            Revision = revision;
            Kind = kind;
            Selection = selection;
            this.assignments = sorted.AsReadOnly();
        }

        public long Revision { get; }

        public VisibilityPlanKind Kind { get; }

        public ActivitySelection Selection { get; }

        public IReadOnlyList<VisibilityAssignment> Assignments => assignments;

        public bool HasSameAssignments(VisibilityPlan other)
        {
            if (other == null || assignments.Count != other.assignments.Count)
            {
                return false;
            }

            for (int index = 0; index < assignments.Count; index++)
            {
                if (!assignments[index].Equals(other.assignments[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
