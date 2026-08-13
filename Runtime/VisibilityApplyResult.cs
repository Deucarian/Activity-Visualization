using System;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Structured result from a model-specific batch visibility adapter.
    /// </summary>
    public readonly struct VisibilityApplyResult
    {
        private VisibilityApplyResult(bool succeeded, bool visibilityChanged, string failureCode)
        {
            Succeeded = succeeded;
            VisibilityChanged = visibilityChanged;
            FailureCode = failureCode;
        }

        public bool Succeeded { get; }

        /// <summary>
        /// True when the adapter changed at least one target while reconciling the full plan.
        /// </summary>
        public bool VisibilityChanged { get; }

        public string FailureCode { get; }

        public static VisibilityApplyResult Completed(bool visibilityChanged = false)
        {
            return new VisibilityApplyResult(true, visibilityChanged, null);
        }

        public static VisibilityApplyResult Failed(string failureCode)
        {
            if (string.IsNullOrWhiteSpace(failureCode))
            {
                throw new ArgumentException("A non-sensitive failure code is required.", nameof(failureCode));
            }

            return new VisibilityApplyResult(false, false, failureCode.Trim());
        }
    }
}
