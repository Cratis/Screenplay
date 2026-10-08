// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Describes unresolved event consumers that cannot be attributed to the selected scope.
/// </summary>
/// <param name="ReferenceCount">The number of unresolved event references outside the reported declarations.</param>
/// <param name="Scopes">The sorted, distinct consumer scopes; an empty address means the application.</param>
public sealed record ScopedUnresolvedEventConsumers(int ReferenceCount, ImmutableArray<string> Scopes);
