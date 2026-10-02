// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceSyntaxIndex;

public class when_indexing_many_event_properties
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    void should_count_standalone_generations_once_per_slice(int count)
    {
        var source = "module Projects\n  feature Recording\n    slice StateChange Record\n" +
            string.Join('\n', Enumerable.Range(1, count).Select(generation => $"      event Recorded generation {generation}\n        value String"));
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var visits = 0;
        IEnumerable<EventSyntax> Events()
        {
            foreach (var declaration in slice.Events)
            {
                visits++;
                yield return declaration;
            }
        }

        var countedSlice = slice with { Events = Events() };
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [countedSlice] }] }] };
        var identity = ApplicationIdentity.Create("Projects");
        var entries = WorkspaceSyntaxIndex.ForSyntax(syntax, SemanticIdentityCatalog.Empty(identity));
        visits.ShouldEqual(count * 2);
        var properties = entries.Where(entry => entry.Node is PropertySyntax).ToArray();
        properties.Length.ShouldEqual(count);
        var eventAddress = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(identity, "Projects", ["Recording"], "Record"), "Recorded");
        foreach (var (property, index) in properties.Select((property, index) => (property, index)))
        {
            property.Address.ShouldEqual(SemanticAddress.ForEventProperty(eventAddress, new((uint)index + 1), "value"));
        }
    }
}
