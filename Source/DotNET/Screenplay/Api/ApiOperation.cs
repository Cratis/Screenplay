// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// The kind of an HTTP operation in an API description.
/// </summary>
public enum ApiOperationKind
{
    /// <summary>
    /// A command, sent as a JSON body with POST.
    /// </summary>
    Command = 0,

    /// <summary>
    /// A query, read with GET and its arguments in the query string.
    /// </summary>
    Query = 1
}

/// <summary>
/// Where an operation reads a parameter from.
/// </summary>
public enum ApiParameterLocation
{
    /// <summary>
    /// A member of the JSON request body.
    /// </summary>
    Body = 0,

    /// <summary>
    /// A query-string parameter.
    /// </summary>
    Query = 1
}

/// <summary>
/// One value a caller supplies to an operation.
/// </summary>
/// <param name="Name">The name on the wire.</param>
/// <param name="Type">The modeled type.</param>
/// <param name="Location">Where the operation reads it from.</param>
/// <param name="Required">Whether a caller must supply it.</param>
public sealed record ApiParameter(string Name, SemanticTypeReference Type, ApiParameterLocation Location, bool Required);

/// <summary>
/// Describes who may call an operation.
/// </summary>
/// <param name="Anonymous">Whether any caller may call it; the model declares no authorization for it.</param>
/// <param name="Requires">The policy expression a caller must satisfy, such as <c>Librarian or Administrator</c>, when not anonymous.</param>
/// <param name="Policies">The names of the policies the expression refers to, in first-use order.</param>
public sealed record ApiAuthorization(bool Anonymous, string? Requires, ImmutableArray<string> Policies)
{
    /// <summary>
    /// Gets the authorization of an operation open to every caller.
    /// </summary>
    public static ApiAuthorization Open { get; } = new(true, null, []);
}

/// <summary>
/// One HTTP operation of a model: a command or a query.
/// </summary>
/// <param name="Kind">Whether it is a command or a query.</param>
/// <param name="Id">The semantic identity of the command or query.</param>
/// <param name="Name">The authored command or query name.</param>
/// <param name="Slice">The declaring slice as <c>Module.Feature[.Feature...].Slice</c>.</param>
/// <param name="Method">The HTTP method.</param>
/// <param name="Route">The route, starting with a slash.</param>
/// <param name="Parameters">The values a caller supplies, in declaration order.</param>
/// <param name="Authorization">Who may call the operation.</param>
public sealed record ApiOperation(
    ApiOperationKind Kind,
    SemanticId Id,
    string Name,
    string Slice,
    string Method,
    string Route,
    ImmutableArray<ApiParameter> Parameters,
    ApiAuthorization Authorization)
{
    /// <summary>
    /// Gets the route that validates a command without running it, or null for a query.
    /// </summary>
    public string? ValidationRoute { get; init; }

    /// <summary>
    /// Gets the command's declared response, or null when it declares none.
    /// </summary>
    public SemanticCommandResponse? Response { get; init; }

    /// <summary>
    /// Gets the read model a query returns, or null for a command.
    /// </summary>
    public SemanticReadModel? ReadModel { get; init; }

    /// <summary>
    /// Gets how many read-model instances a query returns.
    /// </summary>
    public SemanticQueryCardinality Cardinality { get; init; } = SemanticQueryCardinality.Unknown;

    /// <summary>
    /// Gets whether a query is observable: a caller that opens its route as a WebSocket receives the result again on every change.
    /// </summary>
    public bool Live { get; init; }
}
