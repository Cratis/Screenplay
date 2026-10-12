// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Explicitly adds a declaration property or a mapping on the new property's chain.
/// </summary>
/// <param name="At">The revision-bound declaration or mapping-block handle.</param>
/// <param name="Name">The property name at a declaration, otherwise null.</param>
/// <param name="Value">The mapping expression at a block, otherwise null.</param>
public sealed record WorkspacePropertyHop(WorkspaceNodeHandle At, string? Name = null, string? Value = null);
