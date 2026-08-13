using System;
using System.Collections.Generic;

namespace Deucarian.ActivityVisualization
{
    public sealed class ShowAllBaselineVisibilityStrategy : IBaselineVisibilityStrategy
    {
        public VisibilityPlan CreatePlan(long revision, IModelElementIndex modelIndex)
        {
            if (modelIndex == null)
            {
                throw new ArgumentNullException(nameof(modelIndex));
            }

            List<VisibilityAssignment> assignments = new List<VisibilityAssignment>(modelIndex.ElementIds.Count);
            foreach (ModelElementId elementId in modelIndex.ElementIds)
            {
                assignments.Add(new VisibilityAssignment(elementId, true));
            }

            return new VisibilityPlan(revision, VisibilityPlanKind.Baseline, assignments);
        }
    }
}
