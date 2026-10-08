// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal readonly record struct SemanticPolicyTruth(bool? Value)
{
    internal static SemanticPolicyTruth Unknown => new(null);
    internal bool IsTrue => Value is true;
    internal bool IsFalse => Value is false;

    internal SemanticPolicyTruth Not() => new(!Value);
    internal SemanticPolicyTruth And(SemanticPolicyTruth other)
    {
        if (IsFalse || other.IsFalse) return new(false);

        return IsTrue && other.IsTrue ? new(true) : Unknown;
    }

    internal SemanticPolicyTruth Or(SemanticPolicyTruth other)
    {
        if (IsTrue || other.IsTrue) return new(true);

        return IsFalse && other.IsFalse ? new(false) : Unknown;
    }
}
