using System;
using System.Collections.Generic;
using Deucarian.Diagnostics;
using Deucarian.Logging;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Authoritative Activity preview, selection, revision, and applied visibility state.
    /// </summary>
    public sealed partial class ActivityVisualizationStateOwner :
        IActivityVisualizationCommands,
        IReadOnlyActivityVisualizationState,
        IDisposable
    {
        private static readonly DLog Log = DLog.For("ActivityVisualization.State");

        private readonly object syncRoot = new object();
        private readonly IModelElementIndex modelIndex;
        private readonly IModelVisibilityController visibilityController;
        private readonly IActivityVisibilityPlanner planner;
        private readonly IBaselineVisibilityStrategy baselineStrategy;
        private readonly DiagnosticProviderRegistration diagnosticsRegistration;
        private readonly List<ActivityVisualizationStateChangedEventArgs> deferredEvents =
            new List<ActivityVisualizationStateChangedEventArgs>();

        private long latestRevision = -1;
        private ActivityPreviewSnapshot preview;
        private ActivitySelection selection;
        private VisibilityPlan activePlan;
        private ActivityVisualizationUpdateOutcome lastOutcome = ActivityVisualizationUpdateOutcome.AcceptedNoChange;
        private bool disposed;
        private int staleUpdateCount;
        private int invalidSelectionCount;
        private int planningFailureCount;
        private int applyFailureCount;
        private int optionalMissingCount;
        private PendingVisibilityCandidate pendingCandidate;
        private bool isApplying;
        private bool isPublishing;

        public ActivityVisualizationStateOwner(
            IModelElementIndex modelIndex,
            IModelVisibilityController visibilityController,
            IActivityVisibilityPlanner planner,
            IBaselineVisibilityStrategy baselineStrategy)
        {
            this.modelIndex = modelIndex ?? throw new ArgumentNullException(nameof(modelIndex));
            this.visibilityController = visibilityController ?? throw new ArgumentNullException(nameof(visibilityController));
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.baselineStrategy = baselineStrategy ?? throw new ArgumentNullException(nameof(baselineStrategy));

            VisibilityPlan initialPlan = this.baselineStrategy.CreatePlan(-1, this.modelIndex);
            RequireCompletePlan(initialPlan);
            VisibilityApplyResult initialApply = this.visibilityController.Apply(initialPlan);
            if (!initialApply.Succeeded)
            {
                throw new InvalidOperationException(
                    "Initial visibility baseline failed with code: " + (initialApply.FailureCode ?? "unspecified"));
            }

            activePlan = initialPlan;
            diagnosticsRegistration = DiagnosticProviderRegistry.Register(
                new ActivityVisualizationDiagnosticsProvider(this));
        }

        public event EventHandler<ActivityVisualizationStateChangedEventArgs> StateChanged;

        public long LatestRevision { get { lock (syncRoot) { return latestRevision; } } }

        public ActivityPreviewSnapshot Preview { get { lock (syncRoot) { return preview; } } }

        public ActivitySelection Selection { get { lock (syncRoot) { return selection; } } }

        public VisibilityPlan ActivePlan { get { lock (syncRoot) { return activePlan; } } }

        public ActivityVisualizationUpdateOutcome LastOutcome { get { lock (syncRoot) { return lastOutcome; } } }

        public bool IsDisposed { get { lock (syncRoot) { return disposed; } } }

        public ActivityVisualizationUpdateResult ReplacePreview(ActivityPreviewSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            ActivityVisualizationStateChangedEventArgs eventArgs;
            ActivityVisualizationUpdateResult result;
            lock (syncRoot)
            {
                EnsureNotDisposed();
                if (TryRejectStale(snapshot.Revision, out result))
                {
                    return result;
                }

                ActivitySelection previousSelection = selection;
                bool previewChanged = preview == null || !preview.IsVisibilityEquivalentTo(snapshot);
                preview = snapshot;
                latestRevision = snapshot.Revision;

                result = selection == null
                    ? CreateAcceptedNoChange(snapshot.Revision)
                    : PlanAndApply(selection, snapshot.Revision, false);
                QueueAuthoritativeReconciliationIfRequired(result, snapshot.Revision);
                RecordOutcome(result);
                eventArgs = new ActivityVisualizationStateChangedEventArgs(
                    result,
                    previousSelection,
                    selection,
                    previewChanged);
                EnqueueEvent(eventArgs);
            }

            PublishDeferredEvents();
            return result;
        }

        public ActivityVisualizationUpdateResult Select(ActivitySelection requestedSelection, long revision)
        {
            if (requestedSelection == null)
            {
                throw new ArgumentNullException(nameof(requestedSelection));
            }

            ActivityVisualizationStateChangedEventArgs eventArgs;
            ActivityVisualizationUpdateResult result;
            lock (syncRoot)
            {
                EnsureNotDisposed();
                if (TryRejectStale(revision, out result))
                {
                    return result;
                }

                ActivitySelection previousSelection = selection;
                latestRevision = revision;
                if (preview == null)
                {
                    result = NewResult(ActivityVisualizationUpdateOutcome.PreviewUnavailable, revision, false);
                }
                else
                {
                    result = PlanAndApply(requestedSelection, revision, true);
                }

                QueueAuthoritativeReconciliationIfRequired(result, revision);
                RecordOutcome(result);
                eventArgs = new ActivityVisualizationStateChangedEventArgs(
                    result,
                    previousSelection,
                    selection,
                    false);
                EnqueueEvent(eventArgs);
            }

            PublishDeferredEvents();
            return result;
        }

        public ActivityVisualizationUpdateResult Clear(long revision)
        {
            ActivityVisualizationStateChangedEventArgs eventArgs;
            ActivityVisualizationUpdateResult result;
            lock (syncRoot)
            {
                EnsureNotDisposed();
                if (TryRejectStale(revision, out result))
                {
                    return result;
                }

                ActivitySelection previousSelection = selection;
                latestRevision = revision;
                VisibilityPlan baselinePlan;
                try
                {
                    baselinePlan = baselineStrategy.CreatePlan(revision, modelIndex);
                    RequireCompletePlan(baselinePlan);
                }
                catch (Exception exception)
                {
                    planningFailureCount++;
                    Log.Error("Baseline planning failed; exceptionType=" + exception.GetType().Name);
                    result = NewResult(ActivityVisualizationUpdateOutcome.PlanningFailed, revision, false, "baseline_plan");
                    QueueAuthoritativeReconciliationIfRequired(result, revision);
                    RecordOutcome(result);
                    eventArgs = new ActivityVisualizationStateChangedEventArgs(result, previousSelection, selection, false);
                    EnqueueEvent(eventArgs);
                    goto PublishResult;
                }

                result = ApplyCandidate(baselinePlan, null, revision);
                QueueAuthoritativeReconciliationIfRequired(result, revision);
                RecordOutcome(result);
                eventArgs = new ActivityVisualizationStateChangedEventArgs(result, previousSelection, selection, false);
                EnqueueEvent(eventArgs);
            }

        PublishResult:
            PublishDeferredEvents();
            return result;
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                StateChanged = null;
                pendingCandidate = null;
                deferredEvents.Clear();
            }

            diagnosticsRegistration.Dispose();
        }

        internal ActivityVisualizationDiagnosticSnapshot CaptureDiagnosticSnapshot()
        {
            lock (syncRoot)
            {
                return new ActivityVisualizationDiagnosticSnapshot(
                    disposed,
                    latestRevision,
                    selection != null,
                    lastOutcome,
                    staleUpdateCount,
                    invalidSelectionCount,
                    planningFailureCount,
                    applyFailureCount,
                    optionalMissingCount);
            }
        }

        private ActivityVisualizationUpdateResult PlanAndApply(
            ActivitySelection requestedSelection,
            long revision,
            bool replaceSelection)
        {
            VisibilityPlanningResult planning;
            try
            {
                planning = planner.CreateSelectionPlan(revision, preview, requestedSelection, modelIndex);
            }
            catch (Exception exception)
            {
                planningFailureCount++;
                Log.Error("Selection planning failed; exceptionType=" + exception.GetType().Name);
                return NewResult(ActivityVisualizationUpdateOutcome.PlanningFailed, revision, false, "selection_plan");
            }

            optionalMissingCount += planning.MissingOptionalMembers.Count;
            if (planning.MissingOptionalMembers.Count > 0)
            {
                Log.Warning("Selection plan omitted optional model members; count=" + planning.MissingOptionalMembers.Count);
            }

            if (planning.Outcome == VisibilityPlanningOutcome.InvalidSelection)
            {
                invalidSelectionCount++;
                Log.Warning("Selection was not present in the current normalized preview.");
                return NewResult(ActivityVisualizationUpdateOutcome.InvalidSelection, revision, false);
            }

            if (planning.Outcome == VisibilityPlanningOutcome.RequiredMembersMissing)
            {
                planningFailureCount++;
                Log.Warning("Selection plan is missing required model members; count=" + planning.MissingRequiredMembers.Count);
                return new ActivityVisualizationUpdateResult(
                    ActivityVisualizationUpdateOutcome.RequiredMembersMissing,
                    revision,
                    false,
                    planning.MissingRequiredMembers,
                    planning.MissingOptionalMembers);
            }

            try
            {
                RequireCompletePlan(planning.Plan);
            }
            catch (Exception exception)
            {
                planningFailureCount++;
                Log.Error("Selection planner returned an incomplete plan; exceptionType=" + exception.GetType().Name);
                return NewResult(ActivityVisualizationUpdateOutcome.PlanningFailed, revision, false, "incomplete_plan");
            }

            ActivitySelection acceptedSelection = replaceSelection ? requestedSelection : selection;
            ActivityVisualizationUpdateResult applied = ApplyCandidate(
                planning.Plan,
                acceptedSelection,
                revision);
            if (planning.MissingOptionalMembers.Count == 0)
            {
                return applied;
            }

            return new ActivityVisualizationUpdateResult(
                applied.Outcome,
                applied.Revision,
                applied.VisibilityChanged,
                applied.MissingRequiredMembers,
                planning.MissingOptionalMembers,
                applied.FailureCode);
        }

        private bool TryRejectStale(long revision, out ActivityVisualizationUpdateResult result)
        {
            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), "Revision cannot be negative.");
            }

            if (revision > latestRevision)
            {
                result = null;
                return false;
            }

            staleUpdateCount++;
            result = NewResult(ActivityVisualizationUpdateOutcome.StaleRevision, revision, false);
            lastOutcome = result.Outcome;
            return true;
        }

        private void RequireCompletePlan(VisibilityPlan plan)
        {
            if (plan == null || plan.Assignments.Count != modelIndex.ElementIds.Count)
            {
                throw new InvalidOperationException("Visibility plans must assign every indexed model element exactly once.");
            }

            for (int index = 0; index < modelIndex.ElementIds.Count; index++)
            {
                if (plan.Assignments[index].ElementId != modelIndex.ElementIds[index])
                {
                    throw new InvalidOperationException("Visibility plan identifiers must exactly match the model index.");
                }
            }
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ActivityVisualizationStateOwner));
            }
        }

        private static ActivityVisualizationUpdateResult CreateAcceptedNoChange(long revision)
        {
            return NewResult(ActivityVisualizationUpdateOutcome.AcceptedNoChange, revision, false);
        }

        private static ActivityVisualizationUpdateResult NewResult(
            ActivityVisualizationUpdateOutcome outcome,
            long revision,
            bool visibilityChanged,
            string failureCode = null)
        {
            return new ActivityVisualizationUpdateResult(outcome, revision, visibilityChanged, null, null, failureCode);
        }

        private void EnqueueEvent(ActivityVisualizationStateChangedEventArgs eventArgs)
        {
            int position = deferredEvents.Count;
            while (position > 0
                && deferredEvents[position - 1].Result.Revision > eventArgs.Result.Revision)
            {
                position--;
            }

            deferredEvents.Insert(position, eventArgs);
        }

        private void PublishDeferredEvents()
        {
            while (true)
            {
                EventHandler<ActivityVisualizationStateChangedEventArgs> handlers;
                ActivityVisualizationStateChangedEventArgs eventArgs;
                lock (syncRoot)
                {
                    if (isApplying || isPublishing || deferredEvents.Count == 0)
                    {
                        return;
                    }

                    isPublishing = true;
                    handlers = StateChanged;
                    eventArgs = deferredEvents[0];
                    deferredEvents.RemoveAt(0);
                }

                try
                {
                    PublishToHandlers(handlers, eventArgs);
                }
                finally
                {
                    lock (syncRoot)
                    {
                        isPublishing = false;
                    }
                }
            }
        }

    }
}
