// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_scaling_inline_destinations : given.a_semantic_binder
{
    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    void should_resolve_the_command_identifier_without_rescanning_properties_for_each_production(int count)
    {
        var source = "module Projects\n  feature Recording\n    slice StateChange Record\n      command Record\n        recordId Uuid identifier\n" +
            string.Join('\n', Enumerable.Range(0, count).Select(index => $"        value{index} String")) + "\n" +
            string.Join('\n', Enumerable.Range(0, count).Select(index => $"        produces event Recorded{index}"));
        var syntax = new ScreenplayCompiler().Parse(source).Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var visits = 0;
        IEnumerable<PropertySyntax> Properties()
        {
            foreach (var property in command.Properties)
            {
                visits++;
                yield return property;
            }
        }

        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command with { Properties = Properties() }] }] }] }] };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string StableKey = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "application.play", source);
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeTrue();
        (visits <= (count + 1) * 4).ShouldBeTrue();
        (visits >= count + 1).ShouldBeTrue();
    }
}
