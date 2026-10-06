// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents a command response computed from complete post-generation values, present only on acceptance.
/// </summary>
public abstract record SemanticExecutionResponse;

/// <summary>
/// Represents a scalar command response, including semantic null.
/// </summary>
/// <param name="Value">The response value.</param>
public sealed record SemanticScalarExecutionResponse(SemanticValue Value) : SemanticExecutionResponse;

/// <summary>
/// Represents a named command response in authored field order.
/// </summary>
/// <param name="Fields">The computed response fields.</param>
public sealed record SemanticRecordExecutionResponse(ImmutableArray<SemanticExecutionResponseField> Fields) : SemanticExecutionResponse;

/// <summary>
/// Represents one externally named response field.
/// </summary>
/// <param name="Name">The response field name.</param>
/// <param name="Value">The response value.</param>
public sealed record SemanticExecutionResponseField(string Name, SemanticValue Value);
