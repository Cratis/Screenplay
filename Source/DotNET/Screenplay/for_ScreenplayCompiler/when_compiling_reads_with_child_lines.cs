// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_reads_with_child_lines : given.a_compiler
{
    const string Prefix =
        """
        module Orders
          feature Handling
            slice StateView Status
              readmodel Status
                value String
        """;

    [Fact] void should_reject_and_skip_command_reads_children()
    {
        var result = _compiler.Compile(Prefix + "\n" +
            """
                slice StateChange Place
                  command Place
                    reads Status
                      stray String
                        nested String
                    orderId Uuid
            """);
        var command = result.Value!.Modules.Single().Features.Single().Slices.Last().Commands.Single();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.ReadsWithChildren);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        command.Properties.Select(property => property.Name).ShouldContainOnly("orderId");
        command.Reads!.Single().ReadModel.ShouldEqual("Status");
    }

    [Theory]
    [InlineData("when Signal")]
    [InlineData("every 15 minutes")]
    [InlineData("at 08:00")]
    void should_reject_and_skip_reaction_reads_children(string source)
    {
        var result = _compiler.Compile(Prefix + "\n" +
            $"    slice Automation React\n      reaction React\n        {source}\n          reads Status\n            stray String\n              nested String\n          produces Done\n");
        var trigger = result.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single().Triggers.Single();
        var diagnostic = result.Diagnostics.Single(d => d.Code == DiagnosticCodes.ReadsWithChildren);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        trigger.Reads!.Single().ReadModel.ShouldEqual("Status");
        trigger.Data.ShouldBeEmpty();
        trigger.Produces!.Single().Event.ShouldEqual("Done");
        result.Diagnostics.Any(d => d.Code == DiagnosticCodes.ClockTriggerValue).ShouldBeFalse();
    }

    [Fact] void should_skip_children_of_a_bare_reaction_reads_line()
    {
        var result = _compiler.Compile(Prefix + "\n" +
            "    slice Automation React\n      reaction React\n        every 15 minutes\n          reads\n            stray String\n          reads Status\n");
        var trigger = result.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single().Triggers.Single();
        result.Diagnostics.Select(d => d.Code).ShouldContainOnly(DiagnosticCodes.InvalidReadsDeclaration, DiagnosticCodes.ReadsWithChildren);
        trigger.Reads!.Single().ReadModel.ShouldEqual("Status");
        trigger.Data.ShouldBeEmpty();
    }

    [Fact] void should_skip_children_of_an_invalid_reads_line()
    {
        var result = _compiler.Compile(Prefix + "\n" +
            """
                slice StateChange Place
                  command Place
                    reads Status by
                      stray String
                    orderId Uuid
            """);
        var command = result.Value!.Modules.Single().Features.Single().Slices.Last().Commands.Single();
        result.Diagnostics.Select(d => d.Code).ShouldContainOnly(DiagnosticCodes.InvalidReadsDeclaration, DiagnosticCodes.ReadsWithChildren);
        command.Properties.Select(property => property.Name).ShouldContainOnly("orderId");
    }
}
