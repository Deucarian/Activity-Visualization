using System;
using System.Collections.Generic;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Pure mapping from normalized Activity membership to a deterministic full visibility plan.
    /// </summary>
    public sealed class ActivityVisibilityPlanner : IActivityVisibilityPlanner
    {
        public VisibilityPlanningResult CreateSelectionPlan(
            long revision,
            ActivityPreviewSnapshot snapshot,
            ActivitySelection selection,
            IModelElementIndex modelIndex)
        {
            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision));
            }

            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (selection == null)
            {
                throw new ArgumentNullException(nameof(selection));
            }

            if (modelIndex == null)
            {
                throw new ArgumentNullException(nameof(modelIndex));
            }

            if (!snapshot.TryGetActivity(selection.ActivityId, out ActivityVisibilityDefinition activity))
            {
                return VisibilityPlanningResult.InvalidSelection();
            }

            IReadOnlyList<ModelElementMember> members;
            if (selection.Kind == ActivitySelectionKind.Activity)
            {
                members = activity.Members;
            }
            else if (!activity.TryGetStep(selection.StepId, out ActivityStepVisibilityDefinition step))
            {
                return VisibilityPlanningResult.InvalidSelection();
            }
            else
            {
                members = step.Members;
            }

            HashSet<ModelElementId> visibleIdentifiers = new HashSet<ModelElementId>();
            List<ModelElementId> missingRequired = new List<ModelElementId>();
            List<ModelElementId> missingOptional = new List<ModelElementId>();

            foreach (ModelElementMember member in members)
            {
                if (modelIndex.Contains(member.ElementId))
                {
                    visibleIdentifiers.Add(member.ElementId);
                }
                else if (member.Requirement == ModelElementRequirement.Required)
                {
                    missingRequired.Add(member.ElementId);
                }
                else
                {
                    missingOptional.Add(member.ElementId);
                }
            }

            if (missingRequired.Count > 0)
            {
                return VisibilityPlanningResult.RequiredMembersMissing(missingRequired, missingOptional);
            }

            List<VisibilityAssignment> assignments = new List<VisibilityAssignment>(modelIndex.ElementIds.Count);
            foreach (ModelElementId elementId in modelIndex.ElementIds)
            {
                assignments.Add(new VisibilityAssignment(elementId, visibleIdentifiers.Contains(elementId)));
            }

            VisibilityPlan plan = new VisibilityPlan(
                revision,
                VisibilityPlanKind.Selection,
                assignments,
                selection);
            return VisibilityPlanningResult.Planned(plan, missingOptional);
        }
    }
}
