// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Supplies event occurrence time and the caller's audit identity for v2 command execution.
/// </summary>
/// <param name="Occurred">The occurrence time recorded with the event.</param>
/// <param name="Subject">The identity subject.</param>
/// <param name="Name">The identity name.</param>
/// <param name="UserName">The identity user name.</param>
public sealed record SemanticCommandOccurrence(DateTimeOffset Occurred, string Subject, string Name, string UserName)
{
    /// <summary>
    /// Gets whether this occurrence supplies time only, without a caller audit identity.
    /// </summary>
    public bool IsTimeOnly { get; init; }

    /// <summary>
    /// Gets the audit identity recorded on the facts produced under this occurrence. An occurrence that supplies time only has no
    /// caller, so the reference evaluator attributes its facts to the fixed <c>specification</c> identity.
    /// </summary>
    public SemanticCausedBy CausedBy => IsTimeOnly ? SemanticScenario.SpecificationIdentity : new(Subject, Name, UserName);
}
