// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Reports an admitted command handler's execution fault, not an unsupported implementation or malformed payload.
/// </summary>
/// <param name="requirementId">The admitted handler requirement identity.</param>
/// <param name="kind">The execution fault classification.</param>
/// <param name="innerException">The original execution failure.</param>
public sealed class SemanticCommandBodyFaultException(string requirementId, SemanticCommandBodyFaultKind kind, Exception innerException)
    : Exception($"Command handler execution {kind} (requirement {requirementId}).", innerException)
{
    /// <summary>Gets the admitted handler requirement identity.</summary>
    public string RequirementId { get; } = requirementId;

    /// <summary>Gets the execution fault classification.</summary>
    public SemanticCommandBodyFaultKind Kind { get; } = kind;
}
