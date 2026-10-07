// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// A mutually dependent group at one level or among a container's immediate children.
/// </summary>
/// <param name="Container">The parent for a sibling group; null for a level-wide group.</param>
/// <param name="Members">Members in authored order.</param>
internal sealed record DependencyGroup(DependencyNode? Container, IReadOnlyList<DependencyNode> Members);
