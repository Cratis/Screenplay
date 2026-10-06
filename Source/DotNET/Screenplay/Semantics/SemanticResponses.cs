// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a command response contract over complete post-generation command values.
/// </summary>
public abstract record SemanticCommandResponse;

/// <summary>
/// Represents a scalar response from one property of the command.
/// </summary>
/// <param name="Source">The command property identity.</param>
/// <param name="Type">The exact source type, including optionality.</param>
public sealed record SemanticScalarCommandResponse(SemanticId Source, SemanticTypeReference Type) : SemanticCommandResponse;

/// <summary>
/// Represents a named response whose field order is part of the contract.
/// </summary>
/// <param name="Fields">The non-empty fields in authored order.</param>
public sealed record SemanticRecordCommandResponse(ImmutableArray<SemanticCommandResponseField> Fields) : SemanticCommandResponse;

/// <summary>
/// Represents one externally named response field, without a catalog identity.
/// </summary>
/// <param name="Name">The unique field name.</param>
/// <param name="Type">The exact source type, including optionality.</param>
/// <param name="Source">The command property identity.</param>
public sealed record SemanticCommandResponseField(string Name, SemanticTypeReference Type, SemanticId Source);

/// <summary>
/// Represents a return assertion, distinct from the absence of an assertion.
/// </summary>
public abstract record SemanticSpecificationResponse;

/// <summary>
/// Represents an expected scalar response, including semantic null.
/// </summary>
/// <param name="Value">The expected value; never CLR null.</param>
public sealed record SemanticScalarSpecificationResponse(SemanticValue Value) : SemanticSpecificationResponse;

/// <summary>
/// Represents a non-empty subset of named response fields to assert.
/// </summary>
/// <param name="Fields">The unique known fields; serialized in ordinal name order.</param>
public sealed record SemanticRecordSpecificationResponse(ImmutableArray<SemanticSpecificationResponseField> Fields) : SemanticSpecificationResponse;

/// <summary>
/// Represents one expected response field, including semantic null.
/// </summary>
/// <param name="Name">The response field name.</param>
/// <param name="Value">The expected value; never CLR null.</param>
public sealed record SemanticSpecificationResponseField(string Name, SemanticValue Value);
