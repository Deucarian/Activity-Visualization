using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Deucarian.ActivityVisualization
{
    internal static class ActivityVisibilityCollection
    {
        public static string RequireDomainId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable identifier is required.", parameterName);
            }

            return value.Trim();
        }

        public static ReadOnlyCollection<ModelElementMember> CanonicalizeMembers(
            IEnumerable<ModelElementMember> members)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            SortedDictionary<ModelElementId, ModelElementRequirement> requirements =
                new SortedDictionary<ModelElementId, ModelElementRequirement>();

            foreach (ModelElementMember member in members)
            {
                if (!member.ElementId.IsValid)
                {
                    throw new ArgumentException("Membership contains an invalid model element identifier.", nameof(members));
                }

                ModelElementId canonicalId = new ModelElementId(
                    member.ElementId.Scheme,
                    member.ElementId.Value);
                if (!requirements.TryGetValue(canonicalId, out ModelElementRequirement current)
                    || member.Requirement > current)
                {
                    requirements[canonicalId] = member.Requirement;
                }
            }

            List<ModelElementMember> canonical = new List<ModelElementMember>(requirements.Count);
            foreach (KeyValuePair<ModelElementId, ModelElementRequirement> pair in requirements)
            {
                canonical.Add(new ModelElementMember(pair.Key, pair.Value));
            }

            return canonical.AsReadOnly();
        }

        public static bool SequenceEqual(
            IReadOnlyList<ModelElementMember> left,
            IReadOnlyList<ModelElementMember> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int index = 0; index < left.Count; index++)
            {
                if (!left[index].Equals(right[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
