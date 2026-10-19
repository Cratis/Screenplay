// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ConsistencyDeclarations;

public class when_resolving_many_view_names
{
    [Theory]
    [InlineData(500)]
    [InlineData(1000)]
    void should_index_view_names_without_rescanning_read_models(int count)
    {
        var source = "module Projects\n  feature Recording\n    slice StateView Overview\n" +
            string.Join('\n', Enumerable.Range(0, count).Select(index => $"      readmodel View{index}\n        id Guid"));
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var slice = application.Modules.Single().Features.Single().Slices.Single();
        var readModels = slice.ReadModels!.ToArray();
        var visits = 0;
        IEnumerable<ReadModelSyntax> ReadModels()
        {
            visits++;
            foreach (var readModel in readModels)
            {
                yield return readModel;
            }
        }

        slice = slice with { ReadModels = ReadModels() };
        var scope = new DeclarationScope(["Projects", "Recording", "Overview"]);
        var declarations = new ConsistencyDeclarations(application, [(slice, scope)]);
        var before = visits;
        foreach (var index in Enumerable.Range(0, count))
        {
            var name = $"View{index}";
            declarations.View(name, scope)!.Name.ShouldEqual(name);
            declarations.View(name, scope)!.Name.ShouldEqual(name);
            declarations.View($"Unknown{index}", scope).ShouldBeNull();
        }

        (visits - before).ShouldEqual(1);
    }

    [Fact]
    void should_leave_a_view_declared_in_two_slices_unresolved()
    {
        const string source = "module Projects\n  feature Recording\n    slice StateView First\n      readmodel Shared\n        id Guid\n" +
            "    slice StateView Second\n      readmodel Shared\n        id Guid";
        var application = new ScreenplayCompiler().Parse(source).Value!;
        var slices = application.Modules.Single().Features.Single().Slices.ToArray();
        var declarations = new ConsistencyDeclarations(application, [
            (slices[0], new DeclarationScope(["Projects", "Recording", "First"])),
            (slices[1], new DeclarationScope(["Projects", "Recording", "Second"]))]);
        var outside = new DeclarationScope(["Projects", "Elsewhere"]);

        declarations.View("Shared", outside).ShouldBeNull();
        declarations.View("Shared", outside).ShouldBeNull();
    }
}
