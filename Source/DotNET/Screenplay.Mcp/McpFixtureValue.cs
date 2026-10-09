// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

sealed record McpFixtureValue(
    McpReadOwner Specification,
    string Role,
    int Occurrence,
    string Target,
    IEnumerable<McpReadOwner> Candidates,
    string Property,
    TypeRefSyntax? DeclaredType,
    string ExpressionKind,
    object? Value,
    SourceLocation Location,
    string Origin,
    string? Example,
    object? OverriddenValue)
{
    /// <summary>
    /// Gets the persona supplying a synthesized caller atom.
    /// </summary>
    public string? Persona { get; init; }

    /// <summary>
    /// Gets the policy supplying a synthesized caller atom.
    /// </summary>
    public string? Policy { get; init; }

    public string? Table { get; init; }
    public string? Case { get; init; }
    public string? CaseParameter { get; init; }
}
