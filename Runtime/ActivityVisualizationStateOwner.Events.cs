using System;

namespace Deucarian.ActivityVisualization
{
    public sealed partial class ActivityVisualizationStateOwner
    {
        private ActivityVisualizationUpdateResult ApplyCandidate(
            VisibilityPlan candidate,
            ActivitySelection acceptedSelection,
            long revision)
        {
            if (isApplying)
            {
                pendingCandidate = new PendingVisibilityCandidate(candidate, acceptedSelection, revision);
                return NewResult(ActivityVisualizationUpdateOutcome.Queued, revision, false);
            }

            isApplying = true;
            try
            {
                PendingVisibilityCandidate current =
                    new PendingVisibilityCandidate(candidate, acceptedSelection, revision);
                ActivityVisualizationUpdateResult requestedResult = null;
                while (current != null)
                {
                    ActivitySelection previousSelection = selection;
                    ActivityVisualizationUpdateResult currentResult = ApplyOneCandidate(current);
                    if (current.Revision == revision)
                    {
                        requestedResult = currentResult;
                    }

                    if (pendingCandidate == null)
                    {
                        return requestedResult ?? currentResult;
                    }

                    PendingVisibilityCandidate next = pendingCandidate;
                    pendingCandidate = null;
                    if (current.Revision == revision
                        && currentResult.Outcome != ActivityVisualizationUpdateOutcome.ApplyFailed)
                    {
                        requestedResult = NewResult(
                            ActivityVisualizationUpdateOutcome.Superseded,
                            revision,
                            currentResult.VisibilityChanged);
                    }

                    current = next;
                    if (current.PublishCompletion)
                    {
                        ActivityVisualizationUpdateResult completion = ApplyOneCandidate(current);
                        RecordOutcome(completion);
                        EnqueueEvent(new ActivityVisualizationStateChangedEventArgs(
                            completion,
                            previousSelection,
                            selection,
                            false));
                        if (pendingCandidate == null)
                        {
                            return requestedResult ?? completion;
                        }

                        current = pendingCandidate;
                        pendingCandidate = null;
                    }
                }

                throw new InvalidOperationException("Visibility reconciliation ended without a result.");
            }
            finally
            {
                isApplying = false;
                pendingCandidate = null;
            }
        }

        private ActivityVisualizationUpdateResult ApplyOneCandidate(PendingVisibilityCandidate candidate)
        {
            bool assignmentsEquivalent = activePlan != null
                && activePlan.HasSameAssignments(candidate.Plan);
            VisibilityApplyResult applyResult;
            try
            {
                applyResult = visibilityController.Apply(candidate.Plan);
            }
            catch (Exception exception)
            {
                applyFailureCount++;
                Log.Error("Visibility application threw; exceptionType=" + exception.GetType().Name);
                return NewResult(
                    ActivityVisualizationUpdateOutcome.ApplyFailed,
                    candidate.Revision,
                    false,
                    "controller_exception");
            }

            if (!applyResult.Succeeded)
            {
                applyFailureCount++;
                string failureCode = string.IsNullOrWhiteSpace(applyResult.FailureCode)
                    ? "controller_rejected"
                    : applyResult.FailureCode;
                Log.Error("Visibility application failed; code=" + failureCode);
                return NewResult(
                    ActivityVisualizationUpdateOutcome.ApplyFailed,
                    candidate.Revision,
                    false,
                    failureCode);
            }

            if (disposed)
            {
                return NewResult(
                    ActivityVisualizationUpdateOutcome.ApplyFailed,
                    candidate.Revision,
                    false,
                    "owner_disposed_during_apply");
            }

            if (pendingCandidate != null && pendingCandidate.Revision > candidate.Revision)
            {
                return NewResult(
                    ActivityVisualizationUpdateOutcome.Superseded,
                    candidate.Revision,
                    applyResult.VisibilityChanged);
            }

            activePlan = candidate.Plan;
            selection = candidate.Selection;
            return NewResult(
                applyResult.VisibilityChanged || !assignmentsEquivalent
                    ? ActivityVisualizationUpdateOutcome.Applied
                    : ActivityVisualizationUpdateOutcome.AcceptedNoChange,
                candidate.Revision,
                applyResult.VisibilityChanged);
        }

        private void QueueAuthoritativeReconciliationIfRequired(
            ActivityVisualizationUpdateResult result,
            long revision)
        {
            if (!isApplying
                || result.Outcome == ActivityVisualizationUpdateOutcome.Queued
                || pendingCandidate != null && pendingCandidate.Revision >= revision)
            {
                return;
            }

            VisibilityPlan reconciliation = new VisibilityPlan(
                revision,
                activePlan.Kind,
                activePlan.Assignments,
                activePlan.Selection);
            pendingCandidate = new PendingVisibilityCandidate(
                reconciliation,
                selection,
                revision,
                false);
        }

        private void RecordOutcome(ActivityVisualizationUpdateResult result)
        {
            if (result.Outcome != ActivityVisualizationUpdateOutcome.Superseded)
            {
                lastOutcome = result.Outcome;
            }
        }

        private void PublishToHandlers(
            EventHandler<ActivityVisualizationStateChangedEventArgs> handlers,
            ActivityVisualizationStateChangedEventArgs eventArgs)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Delegate callback in handlers.GetInvocationList())
            {
                try
                {
                    ((EventHandler<ActivityVisualizationStateChangedEventArgs>)callback)(this, eventArgs);
                }
                catch (Exception exception)
                {
                    Log.Error("A state observer threw; exceptionType=" + exception.GetType().Name);
                }
            }
        }

        private sealed class PendingVisibilityCandidate
        {
            public PendingVisibilityCandidate(
                VisibilityPlan plan,
                ActivitySelection selection,
                long revision,
                bool publishCompletion = true)
            {
                Plan = plan;
                Selection = selection;
                Revision = revision;
                PublishCompletion = publishCompletion;
            }

            public VisibilityPlan Plan { get; }

            public ActivitySelection Selection { get; }

            public long Revision { get; }

            public bool PublishCompletion { get; }
        }
    }
}
