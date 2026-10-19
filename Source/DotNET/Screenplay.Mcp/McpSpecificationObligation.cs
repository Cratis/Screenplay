// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// One specification obligation of a declaration and the specifications that meet it.
/// </summary>
/// <param name="RuleId">The obligation rule, for example <c>SPEC001</c>.</param>
/// <param name="Declaration">The declaration that owes the specification.</param>
/// <param name="Location">Where the obliging construct is written.</param>
/// <param name="Subject">What within the declaration the obligation is about.</param>
/// <param name="Description">What a matching specification looks like.</param>
/// <param name="Reason">Why the obligation is informational only, when it is.</param>
/// <param name="MetBy">The specifications that meet the obligation.</param>
sealed record McpSpecificationObligation(string RuleId, McpReadOwner Declaration, SourceLocation Location, string Subject, string Description, string? Reason, McpReadOwner[] MetBy)
{
    /// <summary>
    /// Gets a value indicating whether at least one specification meets the obligation.
    /// </summary>
    public bool Met => MetBy.Length > 0;
}
