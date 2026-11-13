// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// The HTTP API surface of an executable model: every command and query with its route, method, payload and
/// authorization, in model order.
/// </summary>
/// <param name="Model">The executable model the description was derived from.</param>
/// <param name="Conventions">The route conventions applied.</param>
/// <param name="Operations">The operations, commands and queries in the order their slices declare them.</param>
public sealed record ApiDescription(ExecutableSemanticModel Model, ApiConventions Conventions, ImmutableArray<ApiOperation> Operations)
{
    /// <summary>
    /// Describes the HTTP API of an executable model.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <param name="conventions">The route conventions; the Cratis renderer's when omitted.</param>
    /// <returns>The API description.</returns>
    public static ApiDescription For(ExecutableSemanticModel model, ApiConventions? conventions = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        conventions ??= ApiConventions.Cratis;
        var readModels = ApiSlices.All(model.Application)
            .SelectMany(located => located.Slice.ReadModels)
            .GroupBy(readModel => readModel.Id)
            .ToDictionary(group => group.Key, group => group.First());
        var operations = ImmutableArray.CreateBuilder<ApiOperation>();
        foreach (var (path, slice) in ApiSlices.All(model.Application))
        {
            var name = string.Join('.', path);
            var includeCommandName = conventions.IncludeCommandNameInRoute || slice.Commands.Length > 1;
            foreach (var command in slice.Commands)
            {
                operations.Add(ApiOperationFactory.Command(model.Application, conventions, path, name, command, includeCommandName));
            }

            foreach (var query in slice.Queries)
            {
                operations.Add(ApiOperationFactory.Query(model.Application, conventions, path, name, query, readModels.GetValueOrDefault(query.ReadModel)));
            }
        }

        return new(model, conventions, operations.ToImmutable());
    }
}
