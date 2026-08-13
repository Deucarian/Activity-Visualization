using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.ActivityVisualization.Samples.Basic
{
    public sealed class ActivityVisibilitySample : MonoBehaviour
    {
        [SerializeField] private GameObject elementA;
        [SerializeField] private GameObject elementB;
        [SerializeField] private GameObject elementC;

        private ActivityVisualizationStateOwner stateOwner;
        private long revision = -1;

        private void Awake()
        {
            Dictionary<ModelElementId, GameObject> targets = new Dictionary<ModelElementId, GameObject>
            {
                { Element("a"), elementA },
                { Element("b"), elementB },
                { Element("c"), elementC }
            };
            SampleModelAdapter model = new SampleModelAdapter(targets);
            stateOwner = new ActivityVisualizationStateOwner(
                model,
                model,
                new ActivityVisibilityPlanner(),
                new ShowAllBaselineVisibilityStrategy());
            stateOwner.ReplacePreview(CreatePreview(NextRevision()));
        }

        [ContextMenu("Select Activity")]
        public void SelectActivity()
        {
            stateOwner.Select(ActivitySelection.ForActivity("installation"), NextRevision());
        }

        [ContextMenu("Select First Step")]
        public void SelectFirstStep()
        {
            stateOwner.Select(ActivitySelection.ForStep("installation", "fasteners"), NextRevision());
        }

        [ContextMenu("Clear Selection")]
        public void ClearSelection()
        {
            stateOwner.Clear(NextRevision());
        }

        private void OnDestroy()
        {
            stateOwner?.Dispose();
            stateOwner = null;
        }

        private long NextRevision()
        {
            revision++;
            return revision;
        }

        private static ActivityPreviewSnapshot CreatePreview(long previewRevision)
        {
            return new ActivityPreviewSnapshot(
                previewRevision,
                new[]
                {
                    new ActivityVisibilityDefinition(
                        "installation",
                        new[] { Required("a"), Required("b") },
                        new[]
                        {
                            new ActivityStepVisibilityDefinition(
                                "fasteners",
                                new[] { Required("b") })
                        })
                });
        }

        private static ModelElementId Element(string value) => new ModelElementId("sample", value);

        private static ModelElementMember Required(string value) => new ModelElementMember(Element(value));

        private sealed class SampleModelAdapter : IModelElementIndex, IModelVisibilityController
        {
            private readonly Dictionary<ModelElementId, GameObject> targets;
            private readonly ModelElementIndex index;

            public SampleModelAdapter(Dictionary<ModelElementId, GameObject> targets)
            {
                this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
                index = new ModelElementIndex(targets.Keys);
            }

            public IReadOnlyList<ModelElementId> ElementIds => index.ElementIds;

            public bool Contains(ModelElementId elementId) => index.Contains(elementId);

            public VisibilityApplyResult Apply(VisibilityPlan plan)
            {
                foreach (VisibilityAssignment assignment in plan.Assignments)
                {
                    if (!targets.TryGetValue(assignment.ElementId, out GameObject target) || target == null)
                    {
                        return VisibilityApplyResult.Failed("sample_target_missing");
                    }
                }

                bool visibilityChanged = false;
                foreach (VisibilityAssignment assignment in plan.Assignments)
                {
                    GameObject target = targets[assignment.ElementId];
                    if (target.activeSelf != assignment.IsVisible)
                    {
                        target.SetActive(assignment.IsVisible);
                        visibilityChanged = true;
                    }
                }

                return VisibilityApplyResult.Completed(visibilityChanged);
            }
        }
    }
}
