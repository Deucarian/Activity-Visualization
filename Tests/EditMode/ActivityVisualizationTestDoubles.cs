using System.Collections.Generic;

namespace Deucarian.ActivityVisualization.Tests
{
    internal sealed class RecordingVisibilityController : IModelVisibilityController
    {
        public readonly List<VisibilityPlan> AppliedPlans = new List<VisibilityPlan>();

        public VisibilityApplyResult NextResult = VisibilityApplyResult.Completed();

        public System.Action<VisibilityPlan> Applying { get; set; }

        public VisibilityApplyResult Apply(VisibilityPlan plan)
        {
            AppliedPlans.Add(plan);
            Applying?.Invoke(plan);
            return NextResult;
        }
    }

    internal sealed class StatefulVisibilityController : IModelVisibilityController
    {
        private readonly Dictionary<ModelElementId, bool> visibility =
            new Dictionary<ModelElementId, bool>();

        public readonly List<VisibilityPlan> AppliedPlans = new List<VisibilityPlan>();

        public StatefulVisibilityController(IEnumerable<ModelElementId> elementIds)
        {
            foreach (ModelElementId elementId in elementIds)
            {
                visibility.Add(elementId, false);
            }
        }

        public bool IsVisible(ModelElementId elementId) => visibility[elementId];

        public void SetExternalVisibility(ModelElementId elementId, bool isVisible)
        {
            visibility[elementId] = isVisible;
        }

        public VisibilityApplyResult Apply(VisibilityPlan plan)
        {
            AppliedPlans.Add(plan);
            bool changed = false;
            foreach (VisibilityAssignment assignment in plan.Assignments)
            {
                if (visibility[assignment.ElementId] != assignment.IsVisible)
                {
                    visibility[assignment.ElementId] = assignment.IsVisible;
                    changed = true;
                }
            }

            return VisibilityApplyResult.Completed(changed);
        }
    }

    internal static class ActivityVisualizationTestData
    {
        public static ModelElementId Element(string value)
        {
            return new ModelElementId("test", value);
        }

        public static ModelElementMember Member(
            string value,
            ModelElementRequirement requirement = ModelElementRequirement.Required)
        {
            return new ModelElementMember(Element(value), requirement);
        }

        public static ActivityPreviewSnapshot Snapshot(
            long revision,
            string activityId,
            IEnumerable<ModelElementMember> activityMembers,
            string stepId = null,
            IEnumerable<ModelElementMember> stepMembers = null)
        {
            ActivityStepVisibilityDefinition[] steps = stepId == null
                ? null
                : new[] { new ActivityStepVisibilityDefinition(stepId, stepMembers) };
            return new ActivityPreviewSnapshot(
                revision,
                new[] { new ActivityVisibilityDefinition(activityId, activityMembers, steps) });
        }

        public static ActivityVisualizationStateOwner CreateOwner(
            RecordingVisibilityController controller,
            params string[] elementIds)
        {
            List<ModelElementId> identifiers = new List<ModelElementId>();
            foreach (string elementId in elementIds)
            {
                identifiers.Add(Element(elementId));
            }

            return new ActivityVisualizationStateOwner(
                new ModelElementIndex(identifiers),
                controller,
                new ActivityVisibilityPlanner(),
                new ShowAllBaselineVisibilityStrategy());
        }

        public static ActivityVisualizationStateOwner CreateOwner(
            IModelVisibilityController controller,
            params string[] elementIds)
        {
            List<ModelElementId> identifiers = new List<ModelElementId>();
            foreach (string elementId in elementIds)
            {
                identifiers.Add(Element(elementId));
            }

            return new ActivityVisualizationStateOwner(
                new ModelElementIndex(identifiers),
                controller,
                new ActivityVisibilityPlanner(),
                new ShowAllBaselineVisibilityStrategy());
        }

        public static bool IsVisible(VisibilityPlan plan, string elementId)
        {
            ModelElementId expected = Element(elementId);
            foreach (VisibilityAssignment assignment in plan.Assignments)
            {
                if (assignment.ElementId == expected)
                {
                    return assignment.IsVisible;
                }
            }

            throw new KeyNotFoundException(elementId);
        }
    }
}
