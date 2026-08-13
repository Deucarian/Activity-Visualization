using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Deucarian.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.ActivityVisualization.Tests
{
    public sealed class ActivityVisualizationStateOwnerTests
    {
        [Test]
        public void SelectionReplacementAndClearApplyExpectedFullStates()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two", "three"))
            {
                owner.ReplacePreview(new ActivityPreviewSnapshot(
                    0,
                    new[]
                    {
                        new ActivityVisibilityDefinition("a", new[] { ActivityVisualizationTestData.Member("one") }),
                        new ActivityVisibilityDefinition("b", new[] { ActivityVisualizationTestData.Member("two") })
                    }));

                ActivityVisualizationUpdateResult first = owner.Select(ActivitySelection.ForActivity("a"), 1);
                ActivityVisualizationUpdateResult replacement = owner.Select(ActivitySelection.ForActivity("b"), 2);
                ActivityVisualizationUpdateResult cleared = owner.Clear(3);

                Assert.That(first.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.Applied));
                Assert.That(replacement.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.Applied));
                Assert.That(ActivityVisualizationTestData.IsVisible(controller.AppliedPlans[2], "one"), Is.False);
                Assert.That(ActivityVisualizationTestData.IsVisible(controller.AppliedPlans[2], "two"), Is.True);
                Assert.That(cleared.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.Applied));
                Assert.That(controller.AppliedPlans.Last().Assignments.All(item => item.IsVisible), Is.True);
                Assert.That(owner.Selection, Is.Null);
            }
        }

        [Test]
        public void RepeatedEquivalentPlanIsIdempotent()
        {
            ModelElementId one = ActivityVisualizationTestData.Element("one");
            ModelElementId two = ActivityVisualizationTestData.Element("two");
            StatefulVisibilityController controller = new StatefulVisibilityController(new[] { one, two });
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                owner.Select(ActivitySelection.ForActivity("activity-a"), 1);
                int callsAfterFirstSelection = controller.AppliedPlans.Count;

                ActivityVisualizationUpdateResult result = owner.Select(
                    ActivitySelection.ForActivity("activity-a"),
                    2);

                Assert.That(result.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.AcceptedNoChange));
                Assert.That(controller.AppliedPlans.Count, Is.EqualTo(callsAfterFirstSelection + 1));
                Assert.That(owner.ActivePlan.Revision, Is.EqualTo(2));
            }
        }

        [Test]
        public void RepeatedEquivalentPlanRepairsExternalVisibilityDrift()
        {
            ModelElementId one = ActivityVisualizationTestData.Element("one");
            ModelElementId two = ActivityVisualizationTestData.Element("two");
            StatefulVisibilityController controller = new StatefulVisibilityController(new[] { one, two });
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                owner.Select(ActivitySelection.ForActivity("activity-a"), 1);
                controller.SetExternalVisibility(one, false);
                controller.SetExternalVisibility(two, true);

                ActivityVisualizationUpdateResult result = owner.Select(
                    ActivitySelection.ForActivity("activity-a"),
                    2);

                Assert.That(result.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.Applied));
                Assert.That(result.VisibilityChanged, Is.True);
                Assert.That(controller.IsVisible(one), Is.True);
                Assert.That(controller.IsVisible(two), Is.False);
                Assert.That(owner.ActivePlan.Revision, Is.EqualTo(2));
            }
        }

        [Test]
        public void ReentrantNewerSelectionSupersedesApplyingSelection()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(new ActivityPreviewSnapshot(
                    0,
                    new[]
                    {
                        new ActivityVisibilityDefinition(
                            "activity-a",
                            new[] { ActivityVisualizationTestData.Member("one") }),
                        new ActivityVisibilityDefinition(
                            "activity-b",
                            new[] { ActivityVisualizationTestData.Member("two") })
                    }));
                ActivityVisualizationUpdateResult reentrant = null;
                List<long> publishedRevisions = new List<long>();
                List<ActivityVisualizationUpdateOutcome> publishedOutcomes =
                    new List<ActivityVisualizationUpdateOutcome>();
                owner.StateChanged += (_, args) =>
                {
                    publishedRevisions.Add(args.Result.Revision);
                    publishedOutcomes.Add(args.Result.Outcome);
                };
                bool triggered = false;
                controller.Applying = plan =>
                {
                    if (!triggered && plan.Revision == 1)
                    {
                        triggered = true;
                        reentrant = owner.Select(ActivitySelection.ForActivity("activity-b"), 2);
                    }
                };

                ActivityVisualizationUpdateResult original = owner.Select(
                    ActivitySelection.ForActivity("activity-a"),
                    1);

                Assert.That(reentrant, Is.Not.Null);
                Assert.That(reentrant.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.Queued));
                Assert.That(original.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.Superseded));
                Assert.That(owner.LatestRevision, Is.EqualTo(2));
                Assert.That(owner.Selection.ActivityId, Is.EqualTo("activity-b"));
                Assert.That(owner.ActivePlan.Revision, Is.EqualTo(2));
                Assert.That(ActivityVisualizationTestData.IsVisible(owner.ActivePlan, "one"), Is.False);
                Assert.That(ActivityVisualizationTestData.IsVisible(owner.ActivePlan, "two"), Is.True);
                Assert.That(controller.AppliedPlans.Select(plan => plan.Revision), Is.EqualTo(new[] { -1L, 1L, 2L }));
                Assert.That(publishedRevisions, Is.EqualTo(new[] { 1L, 2L, 2L }));
                Assert.That(
                    publishedOutcomes,
                    Is.EqualTo(new[]
                    {
                        ActivityVisualizationUpdateOutcome.Superseded,
                        ActivityVisualizationUpdateOutcome.Queued,
                        ActivityVisualizationUpdateOutcome.Applied
                    }));
            }
        }

        [Test]
        public void ReentrantRapidSelectionsCoalesceToNewestCandidate()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two", "three"))
            {
                owner.ReplacePreview(new ActivityPreviewSnapshot(
                    0,
                    new[]
                    {
                        new ActivityVisibilityDefinition("a", new[] { ActivityVisualizationTestData.Member("one") }),
                        new ActivityVisibilityDefinition("b", new[] { ActivityVisualizationTestData.Member("two") }),
                        new ActivityVisibilityDefinition("c", new[] { ActivityVisualizationTestData.Member("three") })
                    }));
                bool triggered = false;
                controller.Applying = plan =>
                {
                    if (!triggered && plan.Revision == 1)
                    {
                        triggered = true;
                        owner.Select(ActivitySelection.ForActivity("b"), 2);
                        owner.Select(ActivitySelection.ForActivity("c"), 3);
                    }
                };

                owner.Select(ActivitySelection.ForActivity("a"), 1);

                Assert.That(owner.LatestRevision, Is.EqualTo(3));
                Assert.That(owner.Selection.ActivityId, Is.EqualTo("c"));
                Assert.That(owner.ActivePlan.Revision, Is.EqualTo(3));
                Assert.That(controller.AppliedPlans.Select(plan => plan.Revision), Is.EqualTo(new[] { -1L, 1L, 3L }));
            }
        }

        [Test]
        public void VisibilityEquivalentPreviewReconcilesWithoutChangingTargets()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                owner.Select(ActivitySelection.ForActivity("activity-a"), 1);
                int calls = controller.AppliedPlans.Count;

                ActivityVisualizationUpdateResult result = owner.ReplacePreview(
                    ActivityVisualizationTestData.Snapshot(
                        2,
                        "activity-a",
                        new[] { ActivityVisualizationTestData.Member("one") }));

                Assert.That(result.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.AcceptedNoChange));
                Assert.That(controller.AppliedPlans.Count, Is.EqualTo(calls + 1));
            }
        }

        [Test]
        public void StalePreviewCannotRestoreOldVisibility()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                owner.Select(ActivitySelection.ForActivity("activity-a"), 1);
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    5,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("two") }));
                int calls = controller.AppliedPlans.Count;

                ActivityVisualizationUpdateResult stale = owner.ReplacePreview(
                    ActivityVisualizationTestData.Snapshot(
                        4,
                        "activity-a",
                        new[] { ActivityVisualizationTestData.Member("one") }));

                Assert.That(stale.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.StaleRevision));
                Assert.That(controller.AppliedPlans.Count, Is.EqualTo(calls));
                Assert.That(ActivityVisualizationTestData.IsVisible(owner.ActivePlan, "two"), Is.True);
                Assert.That(owner.LastOutcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.StaleRevision));
            }
        }

        [Test]
        public void OptionalMissingMembersAreReturnedWithoutBlockingApply()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[]
                    {
                        ActivityVisualizationTestData.Member("one"),
                        ActivityVisualizationTestData.Member(
                            "optional",
                            ModelElementRequirement.Optional)
                    }));

                ActivityVisualizationUpdateResult result = owner.Select(
                    ActivitySelection.ForActivity("activity-a"),
                    1);

                Assert.That(result.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.AcceptedNoChange));
                Assert.That(result.MissingOptionalMembers, Has.Count.EqualTo(1));
                Assert.That(ActivityVisualizationTestData.IsVisible(owner.ActivePlan, "one"), Is.True);
            }
        }

        [Test]
        public void InvalidSelectionPreservesLastValidState()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                owner.Select(ActivitySelection.ForActivity("activity-a"), 1);
                VisibilityPlan validPlan = owner.ActivePlan;
                ActivitySelection validSelection = owner.Selection;
                int calls = controller.AppliedPlans.Count;

                ActivityVisualizationUpdateResult invalid = owner.Select(
                    ActivitySelection.ForActivity("unknown"),
                    2);

                Assert.That(invalid.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.InvalidSelection));
                Assert.That(owner.ActivePlan, Is.SameAs(validPlan));
                Assert.That(owner.Selection, Is.SameAs(validSelection));
                Assert.That(controller.AppliedPlans.Count, Is.EqualTo(calls));
            }
        }

        [Test]
        public void MissingRequiredMemberPreservesLastValidState()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                owner.Select(ActivitySelection.ForActivity("activity-a"), 1);
                VisibilityPlan validPlan = owner.ActivePlan;

                ActivityVisualizationUpdateResult result = owner.ReplacePreview(
                    ActivityVisualizationTestData.Snapshot(
                        2,
                        "activity-a",
                        new[] { ActivityVisualizationTestData.Member("missing") }));

                Assert.That(result.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.RequiredMembersMissing));
                Assert.That(owner.ActivePlan, Is.SameAs(validPlan));
                Assert.That(owner.Selection.ActivityId, Is.EqualTo("activity-a"));
            }
        }

        [Test]
        public void ApplyFailureDoesNotCommitCandidateState()
        {
            RecordingVisibilityController controller = new RecordingVisibilityController();
            using (ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one", "two"))
            {
                owner.ReplacePreview(ActivityVisualizationTestData.Snapshot(
                    0,
                    "activity-a",
                    new[] { ActivityVisualizationTestData.Member("one") }));
                VisibilityPlan baseline = owner.ActivePlan;
                controller.NextResult = VisibilityApplyResult.Failed("test_failure");
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("Visibility application failed; code=test_failure"));

                ActivityVisualizationUpdateResult result = owner.Select(
                    ActivitySelection.ForActivity("activity-a"),
                    1);

                Assert.That(result.Outcome, Is.EqualTo(ActivityVisualizationUpdateOutcome.ApplyFailed));
                Assert.That(result.FailureCode, Is.EqualTo("test_failure"));
                Assert.That(owner.ActivePlan, Is.SameAs(baseline));
                Assert.That(owner.Selection, Is.Null);
            }
        }

        [Test]
        public void RuntimeAssemblyHasNoCameraOrNavigationDependency()
        {
            string[] references = typeof(ActivityVisualizationStateOwner)
                .Assembly
                .GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ToArray();

            Assert.That(references.Any(name =>
                name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Navigation", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void DisposeUnregistersDiagnosticsAndIsIdempotent()
        {
            int before = DiagnosticProviderRegistry.SnapshotProviders().Count;
            RecordingVisibilityController controller = new RecordingVisibilityController();
            ActivityVisualizationStateOwner owner =
                ActivityVisualizationTestData.CreateOwner(controller, "one");

            Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before + 1));

            owner.Dispose();
            owner.Dispose();

            Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before));
            Assert.Throws<ObjectDisposedException>(() => owner.Clear(0));
        }
    }
}
