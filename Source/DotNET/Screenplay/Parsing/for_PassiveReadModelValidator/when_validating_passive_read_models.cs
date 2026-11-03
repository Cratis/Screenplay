// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PassiveReadModelValidator;

public class when_validating_passive_read_models : Specification
{
    const string Model = "module M\n  feature F\n    slice StateChange S\n      readmodel Row\n        passive\n        id String\n";
    const string Command = "      command Decide\n        id String\n        reads Row by id\n";

    [Fact]
    void should_allow_a_command_read_without_passive_findings() => Findings(Model + Command).ShouldBeEmpty();

    [Fact]
    void should_warn_when_no_command_reads_it() => Findings(Model).Single().Code.ShouldEqual(DiagnosticCodes.PassiveReadModelNotRead);

    [Theory]
    [InlineData("Row")]
    [InlineData("Row[]")]
    [InlineData("Row optional")]
    [InlineData("observable Row[]")]
    void should_reject_queries_regardless_of_cardinality_or_observability(string type) => Findings(Model + Command + $"      query Find => {type}\n").Single().Code.ShouldEqual(DiagnosticCodes.PassiveReadModelObserved);

    [Fact]
    void should_reject_screen_data_in_a_nested_directive()
    {
        const string source = Model + Command + "      query Find => Row[]\n      screen List\n        section Rows\n          data Row[] via query Find\n";
        Findings(source).Count(diagnostic => diagnostic.Code == DiagnosticCodes.PassiveReadModelObserved).ShouldEqual(2);
    }

    [Fact]
    void should_not_count_reaction_reads_as_command_reads() => Findings(Model + "      event E\n        id String\n      reaction DecideLater\n        when E\n          reads Row by id").Single().Code.ShouldEqual(DiagnosticCodes.PassiveReadModelNotRead);

    [Fact]
    void should_allow_an_active_model_to_be_queried() => Findings(Model.Replace("        passive\n", string.Empty, StringComparison.Ordinal) + "      query Find => Row[]").ShouldBeEmpty();

    [Fact]
    void should_resolve_qualified_screen_data_in_another_slice()
    {
        const string source = Model + Command + "    slice StateView Other\n      query Find => M.F.S.Row[]\n      screen List\n        data M.F.S.Row[] via query Find";
        Findings(source).Count(diagnostic => diagnostic.Code == DiagnosticCodes.PassiveReadModelObserved).ShouldEqual(2);
    }

    [Fact]
    void should_not_infer_passivity_from_command_only_usage() => Findings((Model + Command).Replace("        passive\n", string.Empty, StringComparison.Ordinal)).ShouldBeEmpty();

    [Fact]
    void should_distinguish_equal_short_names_in_different_slices()
    {
        const string source = Model + Command + "    slice StateView Other\n      readmodel Row\n        id String\n      query Find => Row[]";
        Findings(source).ShouldBeEmpty();
    }

    [Fact]
    void should_resolve_a_qualified_command_read()
    {
        // Qualified identities are validated as syntax; the reads source grammar accepts a short name.
        var parsed = new ScreenplayCompiler().Parse(Model + "    slice StateChange Other\n      readmodel Row\n        id String\n" + Command);
        parsed.Success.ShouldBeTrue();
        var module = parsed.Value!.Modules.Single();
        var feature = module.Features.Single();
        var slices = feature.Slices.ToArray();
        var command = slices[1].Commands.Single();
        slices[1] = slices[1] with { Commands = [command with { Reads = [command.Reads!.Single() with { ReadModel = "M.F.S.Row" }] }] };
        var application = parsed.Value! with { Modules = [module with { Features = [feature with { Slices = slices }] }] };
        var context = ParserContext.ForDiagnostics();
        PassiveReadModelValidator.Validate(application, new ConsistencyDeclarations(application, [.. ScreenplayValidator.ScopedSlices(application)]), context);
        context.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    void should_not_count_a_specification_fixture_as_a_command_read() => Findings(Model + "      specification Seed\n        given readmodel Row\n          id = \"one\"").Single().Code.ShouldEqual(DiagnosticCodes.PassiveReadModelNotRead);

    [Fact]
    void should_point_query_errors_at_the_query_and_warnings_at_the_directive()
    {
        var diagnostics = Findings(Model + "      query Find => Row[]");
        diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PassiveReadModelObserved).Location.Line.ShouldEqual(7);
        diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.PassiveReadModelNotRead).Location.Line.ShouldEqual(5);
    }

    static Diagnostic[] Findings(string source)
    {
        var compiler = new ScreenplayCompiler();
        compiler.Parse(source).Success.ShouldBeTrue();
        return [.. compiler.Compile(source).Diagnostics
            .Where(diagnostic => string.Equals(diagnostic.Code, DiagnosticCodes.PassiveReadModelObserved, StringComparison.Ordinal) || string.Equals(diagnostic.Code, DiagnosticCodes.PassiveReadModelNotRead, StringComparison.Ordinal))];
    }
}
