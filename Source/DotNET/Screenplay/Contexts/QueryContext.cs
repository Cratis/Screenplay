// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents everything a query performer is given when it runs - the arguments of the query and the
/// circumstances it runs under.
/// </summary>
/// <param name="Arguments">The arguments of the query, conforming to the <c>by</c> and <c>filter</c>
/// parameters the <c>query</c> declaration gives it. Typically an <see cref="System.Dynamic.ExpandoObject"/>
/// populated from the incoming request.</param>
/// <param name="Tenant">The <see cref="TenantId"/> the query is executing for.</param>
/// <param name="CausedBy">The <see cref="Contexts.CausedBy">identity</see> that caused the query to run.</param>
/// <param name="Causation">The <see cref="Contexts.Causation"/> linking the query back to what caused it.</param>
/// <param name="Occurred">When the query was received.</param>
/// <remarks>
/// This is the type an inline <c>performer csharp</c> block and any imported C# performer file compile
/// against - it is in scope as <c>context</c>. A query parameter declared with <c>from $context....</c>
/// is filled from the same values before the performer runs.
/// </remarks>
public record QueryContext(
    dynamic Arguments,
    TenantId Tenant,
    CausedBy CausedBy,
    Causation Causation,
    DateTimeOffset Occurred);
