// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// A presentation node; addresses of model nodes use dotted declaration addresses, contexts use context:.
/// </summary>
/// <param name="Kind">The level: application, module, feature, slice or context.</param>
/// <param name="Address">The declaration address or prefixed outside-context address.</param>
/// <param name="Scope">The full hierarchy segments including this node.</param>
/// <param name="Rank">The authored traversal index, or deterministic syntax-order fallback.</param>
public sealed record DependencyNode(string Kind, string Address, IReadOnlyList<string> Scope, int Rank)
{
    internal string Key => Kind + ":" + Address;
}
