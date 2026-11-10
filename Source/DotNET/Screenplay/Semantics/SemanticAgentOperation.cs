// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>An explicitly exposed operation with a required token scope and human description.</summary>
/// <param name="Kind">Whether this is a query or command.</param>
/// <param name="Operation">The identity of the existing executable operation.</param>
/// <param name="Scope">The token scope required to call it.</param>
/// <param name="Description">The authored description.</param>
public sealed record SemanticAgentOperation(SemanticAgentOperationKind Kind, SemanticId Operation, SemanticAgentScope Scope, string Description);
