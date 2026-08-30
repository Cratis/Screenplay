// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

sealed class WorkspaceSemanticAddressComparer : IComparer<SemanticAddress>
{
    internal static WorkspaceSemanticAddressComparer Instance { get; } = new();

    public int Compare(SemanticAddress? x, SemanticAddress? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        var kind = ((int)x.Kind).CompareTo((int)y.Kind);
        if (kind != 0)
        {
            return kind;
        }

        var count = Math.Min(x.Parts.Length, y.Parts.Length);
        for (var index = 0; index < count; index++)
        {
            var partKind = ((int)x.Parts[index].Kind).CompareTo((int)y.Parts[index].Kind);
            if (partKind != 0)
            {
                return partKind;
            }

            var key = StringComparer.Ordinal.Compare(x.Parts[index].Key, y.Parts[index].Key);
            if (key != 0)
            {
                return key;
            }
        }

        return x.Parts.Length.CompareTo(y.Parts.Length);
    }
}
