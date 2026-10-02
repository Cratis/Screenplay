// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ConsistencyDeclarations;

public class when_resolving_many_inline_event_names
{
    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    [InlineData(2000)]
    void should_index_event_and_declared_names_without_rescanning_productions(int count)
    {
        var source = "module Projects\n  feature Recording\n    slice StateChange Record\n      command Record\n" +
            string.Join('\n', Enumerable.Range(0, count).Select(index => $"        produces event Recorded{index}"));
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var slice = application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        var visits = 0;
        IEnumerable<ProducesSyntax> Productions()
        {
            foreach (var production in command.Produces)
            {
                visits++;
                yield return production;
            }
        }

        slice = slice with { Commands = [command with { Produces = Productions() }] };
        var scope = new DeclarationScope(["Projects", "Recording", "Record"]);
        var declarations = new ConsistencyDeclarations(application, [(slice, scope)]);
        foreach (var index in Enumerable.Range(0, count))
        {
            var name = $"Recorded{index}";
            declarations.Event(name, scope)!.Name.ShouldEqual(name);
            declarations.Event($"Record.{name}", scope)!.Name.ShouldEqual(name);
            declarations.Declares(name).ShouldBeTrue();
            declarations.Declares($"Unknown{index}").ShouldBeFalse();
        }

        visits.ShouldEqual(count * 2);
    }
}
