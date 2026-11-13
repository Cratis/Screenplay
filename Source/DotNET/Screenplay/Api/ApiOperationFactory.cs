// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Creates the HTTP operation of one command or query.
/// </summary>
static class ApiOperationFactory
{
    /// <summary>
    /// The query-string parameters every query returning many instances accepts for paging and sorting.
    /// </summary>
    internal static readonly ImmutableArray<ApiParameter> PagingParameters =
    [
        new("page", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), ApiParameterLocation.Query, false),
        new("pageSize", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), ApiParameterLocation.Query, false),
        new("sortBy", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), ApiParameterLocation.Query, false),
        new("sortDirection", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), ApiParameterLocation.Query, false)
    ];

    /// <summary>
    /// Creates the operation of a command.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="conventions">The route conventions.</param>
    /// <param name="path">The names leading to the declaring slice.</param>
    /// <param name="slice">The declaring slice as dotted path.</param>
    /// <param name="command">The command.</param>
    /// <param name="includeName">Whether the route ends with the command name.</param>
    /// <returns>The operation.</returns>
    internal static ApiOperation Command(
        SemanticApplication application,
        ApiConventions conventions,
        ImmutableArray<string> path,
        string slice,
        SemanticCommand command,
        bool includeName)
    {
        var route = ApiNames.Route(conventions.RoutePrefix, includeName ? path.Add(command.Name) : path);
        var parameters = command.Properties
            .Where(property => !property.IsGenerated)
            .Select(property => new ApiParameter(ApiNames.Camel(property.Name), property.Type, ApiParameterLocation.Body, !property.Type.IsOptional))
            .ToImmutableArray();
        return new(ApiOperationKind.Command, command.Id, command.Name, slice, "POST", route, parameters, ApiAuthorizationText.For(application, command.Authorization))
        {
            ValidationRoute = $"{route}/validate",
            Response = command.Response
        };
    }

    /// <summary>
    /// Creates the operation of a query.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <param name="conventions">The route conventions.</param>
    /// <param name="path">The names leading to the declaring slice.</param>
    /// <param name="slice">The declaring slice as dotted path.</param>
    /// <param name="query">The query.</param>
    /// <param name="readModel">The read model the query returns, when the model declares it.</param>
    /// <returns>The operation.</returns>
    internal static ApiOperation Query(
        SemanticApplication application,
        ApiConventions conventions,
        ImmutableArray<string> path,
        string slice,
        SemanticKeyedQuery query,
        SemanticReadModel? readModel)
    {
        var parameters = ImmutableArray.CreateBuilder<ApiParameter>();
        if (query.Argument is { } argument) parameters.Add(new(ApiNames.Camel(argument.Name), argument.Type, ApiParameterLocation.Query, true));
        parameters.AddRange(query.KeyParts.Select(part => new ApiParameter(ApiNames.Camel(part.Name), part.Type, ApiParameterLocation.Query, true)));
        parameters.AddRange(query.Filters.Select(filter => new ApiParameter(ApiNames.Camel(filter.Name), filter.Type, ApiParameterLocation.Query, false)));
        if (query.Cardinality == SemanticQueryCardinality.Many) parameters.AddRange(PagingParameters);
        var route = ApiNames.Route(conventions.RoutePrefix, path.Add(query.Name));
        return new(ApiOperationKind.Query, query.Id, query.Name, slice, "GET", route, parameters.ToImmutable(), ApiAuthorizationText.For(application, query.Authorization))
        {
            ReadModel = readModel,
            Cardinality = query.Cardinality,
            Live = query.Delivery == SemanticQueryDelivery.Live
        };
    }
}
