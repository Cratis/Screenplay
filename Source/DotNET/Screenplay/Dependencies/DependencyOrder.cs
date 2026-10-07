// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Presentation-only proposed child order; never applied to syntax, identities or executable bytes.
/// </summary>
/// <param name="Container">The container, including the application root.</param>
/// <param name="Children">Suggested immediate child order.</param>
/// <param name="Changed">Whether the order differs from authored order.</param>
public sealed record DependencyContainerOrder(DependencyNode Container, IReadOnlyList<DependencyNode> Children, bool Changed);

/// <summary>
/// A story-order suggestion for all containers and its depth-first slice traversal.
/// </summary>
/// <param name="Containers">Per-container child suggestions.</param>
/// <param name="Slices">Depth-first story traversal of slices.</param>
public sealed record DependencyOrder(IReadOnlyList<DependencyContainerOrder> Containers, IReadOnlyList<DependencyNode> Slices);
