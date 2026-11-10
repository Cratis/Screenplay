// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Agents;

/// <summary>A validated, explicitly exposed executable operation.</summary>
/// <param name="Kind">The operation kind.</param>
/// <param name="Name">The unqualified operation name.</param>
/// <param name="Path">The module, feature and slice path.</param>
/// <param name="QualifiedName">The canonical qualified name.</param>
/// <param name="Scope">The required token scope.</param>
/// <param name="Description">The authored description.</param>
/// <param name="IsObservable">Whether the query delivers a live result.</param>
public sealed record AgentCapability(AgentOperationKind Kind, string Name, IReadOnlyList<string> Path, string QualifiedName, AgentScope Scope, string Description, bool IsObservable);
