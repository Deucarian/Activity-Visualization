using NUnit.Framework;

namespace Deucarian.ActivityVisualization.Tests
{
    public sealed class ActivityVisibilityPlannerTests
    {
        private readonly ActivityVisibilityPlanner planner = new ActivityVisibilityPlanner();

        [Test]
        public void ActivitySelectionCreatesFullReplacementPlan()
        {
            ModelElementIndex index = new ModelElementIndex(new[]
            {
                ActivityVisualizationTestData.Element("c"),
                ActivityVisualizationTestData.Element("a"),
                ActivityVisualizationTestData.Element("b")
            });
            ActivityPreviewSnapshot snapshot = ActivityVisualizationTestData.Snapshot(
                0,
                "activity-a",
                new[]
                {
                    ActivityVisualizationTestData.Member("c"),
                    ActivityVisualizationTestData.Member("a")
                });

            VisibilityPlanningResult result = planner.CreateSelectionPlan(
                1,
                snapshot,
                ActivitySelection.ForActivity("activity-a"),
                index);

            Assert.That(result.Outcome, Is.EqualTo(VisibilityPlanningOutcome.Planned));
            Assert.That(result.Plan.Assignments.Count, Is.EqualTo(3));
            Assert.That(result.Plan.Assignments[0].ElementId.Value, Is.EqualTo("a"));
            Assert.That(ActivityVisualizationTestData.IsVisible(result.Plan, "a"), Is.True);
            Assert.That(ActivityVisualizationTestData.IsVisible(result.Plan, "b"), Is.False);
            Assert.That(ActivityVisualizationTestData.IsVisible(result.Plan, "c"), Is.True);
        }

        [Test]
        public void IdentifiersAreCanonicalizedBeforeMembershipAndIndexDeduplication()
        {
            ModelElementIndex index = new ModelElementIndex(new[]
            {
                new ModelElementId(" test ", " one "),
                new ModelElementId("test", "one")
            });
            ActivityVisibilityDefinition definition = new ActivityVisibilityDefinition(
                " activity-a ",
                new[]
                {
                    new ModelElementMember(
                        new ModelElementId(" test ", " one "),
                        ModelElementRequirement.Optional),
                    new ModelElementMember(
                        new ModelElementId("test", "one"),
                        ModelElementRequirement.Required)
                });

            Assert.That(index.ElementIds, Has.Count.EqualTo(1));
            Assert.That(index.Contains(new ModelElementId("test", "one")), Is.True);
            Assert.That(definition.ActivityId, Is.EqualTo("activity-a"));
            Assert.That(definition.Members, Has.Count.EqualTo(1));
            Assert.That(definition.Members[0].ElementId, Is.EqualTo(new ModelElementId("test", "one")));
            Assert.That(definition.Members[0].Requirement, Is.EqualTo(ModelElementRequirement.Required));
        }

        [Test]
        public void MissingOptionalMemberProducesPlanAndDiagnosticData()
        {
            ModelElementIndex index = new ModelElementIndex(new[] { ActivityVisualizationTestData.Element("present") });
            ActivityPreviewSnapshot snapshot = ActivityVisualizationTestData.Snapshot(
                0,
                "activity-a",
                new[]
                {
                    ActivityVisualizationTestData.Member("present"),
                    ActivityVisualizationTestData.Member("optional", ModelElementRequirement.Optional)
                });

            VisibilityPlanningResult result = planner.CreateSelectionPlan(
                1,
                snapshot,
                ActivitySelection.ForActivity("activity-a"),
                index);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.MissingOptionalMembers, Has.Count.EqualTo(1));
            Assert.That(result.Plan, Is.Not.Null);
        }

        [Test]
        public void MissingRequiredMemberRejectsWholePlan()
        {
            ModelElementIndex index = new ModelElementIndex(new[] { ActivityVisualizationTestData.Element("present") });
            ActivityPreviewSnapshot snapshot = ActivityVisualizationTestData.Snapshot(
                0,
                "activity-a",
                new[] { ActivityVisualizationTestData.Member("required-but-missing") });

            VisibilityPlanningResult result = planner.CreateSelectionPlan(
                1,
                snapshot,
                ActivitySelection.ForActivity("activity-a"),
                index);

            Assert.That(result.Outcome, Is.EqualTo(VisibilityPlanningOutcome.RequiredMembersMissing));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.MissingRequiredMembers, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnknownActivityIsAnInvalidSelection()
        {
            ModelElementIndex index = new ModelElementIndex(new[] { ActivityVisualizationTestData.Element("one") });
            ActivityPreviewSnapshot snapshot = ActivityVisualizationTestData.Snapshot(
                0,
                "activity-a",
                new[] { ActivityVisualizationTestData.Member("one") });

            Assert.That(
                planner.CreateSelectionPlan(1, snapshot, ActivitySelection.ForActivity("missing"), index).Outcome,
                Is.EqualTo(VisibilityPlanningOutcome.InvalidSelection));
        }

        [Test]
        public void NullMembershipCollectionIsRejectedRatherThanTreatedAsEmpty()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new ActivityVisibilityDefinition("activity-a", null));
        }

        [Test]
        public void DuplicateMembershipIsCanonicalAndRequiredWins()
        {
            ActivityVisibilityDefinition definition = new ActivityVisibilityDefinition(
                "activity-a",
                new[]
                {
                    ActivityVisualizationTestData.Member("one", ModelElementRequirement.Optional),
                    ActivityVisualizationTestData.Member("one", ModelElementRequirement.Required)
                });

            Assert.That(definition.Members, Has.Count.EqualTo(1));
            Assert.That(definition.Members[0].Requirement, Is.EqualTo(ModelElementRequirement.Required));
        }
    }
}
