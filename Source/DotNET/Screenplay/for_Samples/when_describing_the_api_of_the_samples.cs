// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.for_Samples;

public class when_describing_the_api_of_the_samples : given.the_sample_workspaces
{
    readonly List<string> _missing = [];
    readonly List<string> _duplicateRoutes = [];
    readonly List<string> _undescribedLiveQueries = [];
    int _described;

    void Because()
    {
        foreach (var (name, workspace) in _workspaces)
        {
            if (workspace.Compilation.Value?.Model is not { } model) continue;
            var description = ApiDescription.For(model);
            var document = OpenApiDocument.Build(description);
            var operations = document["paths"]!.AsObject().SelectMany(path => path.Value!.AsObject().Select(method => method.Value!.AsObject())).ToArray();
            var ids = operations.Where(operation => operation["x-screenplay-kind"]!.GetValue<string>() != "validation")
                .Select(operation => operation["x-screenplay-id"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
            foreach (var declared in Declared(model.Application).Where(id => !ids.Contains(id))) _missing.Add($"{name}: {declared}");
            _duplicateRoutes.AddRange(description.Operations.GroupBy(operation => (operation.Method, operation.Route)).Where(group => group.Count() > 1).Select(group => $"{name}: {group.Key}"));
            _undescribedLiveQueries.AddRange(description.Operations.Where(operation => operation.Live)
                .Where(operation => document["paths"]![operation.Route]!["get"]!["x-screenplay-observable"] is null).Select(operation => $"{name}: {operation.Name}"));
            _described++;
        }
    }

    static IEnumerable<string> Declared(SemanticApplication application) =>
        ApiSlices.All(application).SelectMany(located => located.Slice.Commands.Select(command => command.Id.ToString()).Concat(located.Slice.Queries.Select(query => query.Id.ToString())));

    [Fact] void should_describe_at_least_ten_samples() => (_described >= 10).ShouldBeTrue();
    [Fact] void should_describe_every_command_and_query() => string.Join('\n', _missing).ShouldEqual(string.Empty);
    [Fact] void should_give_every_operation_its_own_route() => string.Join('\n', _duplicateRoutes).ShouldEqual(string.Empty);
    [Fact] void should_describe_every_observable_query() => string.Join('\n', _undescribedLiveQueries).ShouldEqual(string.Empty);
}
