// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents everything a command handler is given when it runs - the command itself and the
/// circumstances it runs under.
/// </summary>
/// <param name="Command">The command being handled, conforming to the shape the <c>command</c> declaration
/// gives it. Typically an <see cref="System.Dynamic.ExpandoObject"/> populated from the body of the request.</param>
/// <param name="Tenant">The <see cref="TenantId"/> the command is executing for.</param>
/// <param name="CausedBy">The <see cref="Contexts.CausedBy">identity</see> that caused the command to run.</param>
/// <param name="Causation">The <see cref="Contexts.Causation"/> linking the command back to what caused it.</param>
/// <param name="Occurred">When the command was received.</param>
/// <remarks>
/// This is the type an inline <c>handler csharp</c> block and any imported C# handler file compile
/// against - it is in scope as <c>context</c>. The same values are reachable declaratively from a
/// <c>produces</c> mapping through the <c>$context.</c> expressions.
/// </remarks>
public record CommandContext(
    dynamic Command,
    TenantId Tenant,
    CausedBy CausedBy,
    Causation Causation,
    DateTimeOffset Occurred);
