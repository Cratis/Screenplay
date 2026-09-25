// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_reaction_trigger_reads : given.a_compiler
{
    const string Prefix =
        """
        trigger Signal
          key Uuid
          @reads String

        module Orders
          feature Handling
            slice StateView Status
              readmodel Status
                value String
            slice StateChange Place
              event Placed
                key Uuid
                reads String
            slice Automation Handle
              reaction Handle
        """;

    CompilationResult<ApplicationSyntax> CompileTrigger(string trigger) => _compiler.Compile(Prefix + "\n" + trigger);

    [Fact] void should_parse_a_named_trigger_read_with_alias_and_key()
    {
        var result = CompileTrigger("        when Signal\n          key\n          reads Status as current by key");
        result.Diagnostics.ShouldBeEmpty();
        var read = result.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single().Triggers.Single().Reads!.Single();
        read.ReadModel.ShouldEqual("Status");
        read.Alias.ShouldEqual("current");
        read.By.ShouldEqual("key");
    }

    [Fact] void should_parse_an_event_read_with_alias_and_key()
    {
        var result = CompileTrigger("        when Placed\n          key\n          reads Status as current by key");
        result.Diagnostics.ShouldBeEmpty();
        var read = result.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single().Triggers.Single().Reads!.Single();
        read.ReadModel.ShouldEqual("Status");
        read.Alias.ShouldEqual("current");
        read.By.ShouldEqual("key");
    }

    [Theory]
    [InlineData("every 15 minutes")]
    [InlineData("at 08:00")]
    void should_read_a_whole_view_under_a_clock(string source) =>
        CompileTrigger($"        {source}\n          reads Status").Diagnostics.ShouldBeEmpty();

    [Theory]
    [InlineData("every 15 minutes")]
    [InlineData("at 08:00")]
    void should_reject_a_clock_read_by_key(string source)
    {
        var diagnostic = CompileTrigger($"        {source}\n          reads Status by key").Diagnostics
            .Single(d => d.Code == DiagnosticCodes.ClockTriggerReadsKey);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact] void should_report_an_unknown_trigger_value()
    {
        var diagnostic = CompileTrigger("        when Signal\n          key\n          reads Status by missing").Diagnostics
            .Single(d => d.Code == DiagnosticCodes.UnknownReactionReadsKey);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    [Fact] void should_report_an_unknown_view() =>
        CompileTrigger("        when Signal\n          reads Missing").Diagnostics
            .Count(d => d.Code == DiagnosticCodes.UnknownReadModel).ShouldEqual(1);

    [Fact] void should_require_aliases_on_every_repeated_view() =>
        CompileTrigger("        when Signal\n          reads Status\n          reads Status as other").Diagnostics
            .Count(d => d.Code == DiagnosticCodes.MissingReadsAlias).ShouldEqual(1);

    [Fact] void should_reject_duplicate_aliases() =>
        CompileTrigger("        when Signal\n          reads Status as same\n          reads Status as same").Diagnostics
            .Count(d => d.Code == DiagnosticCodes.DuplicateReadsAlias).ShouldEqual(1);

    [Fact] void should_report_an_alias_that_conflicts_with_a_trigger_value() =>
        CompileTrigger("        when Signal\n          key\n          reads Status as key by key").Diagnostics
            .Count(d => d.Code == DiagnosticCodes.ReadsAliasConflictsWithProperty).ShouldEqual(1);

    [Fact] void should_report_a_malformed_read_instead_of_dropping_it() =>
        CompileTrigger("        when Signal\n          reads Status by").Diagnostics
            .Count(d => d.Code == DiagnosticCodes.InvalidReadsDeclaration).ShouldEqual(1);

    [Fact] void should_suggest_escaping_a_bare_reads_value()
    {
        var result = CompileTrigger("        when Signal\n          reads");
        var diagnostic = result.Diagnostics.Single(d => d.Code == DiagnosticCodes.InvalidReadsDeclaration);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostic.Message.ShouldContain("@reads");
        result.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single().Triggers.Single().Data.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Uuid")]
    [InlineData("String")]
    void should_suggest_escaping_a_typed_reads_value(string primitive)
    {
        var result = CompileTrigger($"        when Signal\n          reads {primitive}");
        var diagnostic = result.Diagnostics.Single(d => d.Code == DiagnosticCodes.AmbiguousReactionReadsValue);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Message.ShouldContain($"@reads {primitive}");
        var trigger = result.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single().Triggers.Single();
        trigger.Data.ShouldBeEmpty();
        trigger.Reads!.Single().ReadModel.ShouldEqual(primitive);
    }

    [Fact] void should_not_treat_a_view_read_as_a_trigger_value()
    {
        var trigger = CompileTrigger("        when Signal\n          reads Status").Value!.Modules.Single().Features.Single()
            .Slices.Last().Reactions.Single().Triggers.Single();
        trigger.Reads!.Single().ReadModel.ShouldEqual("Status");
        trigger.Data.ShouldBeEmpty();
    }

    [Fact] void should_keep_an_escaped_reads_value() =>
        CompileTrigger("        when Signal\n          @reads String").Value!.Modules.Single().Features.Single().Slices.Last()
            .Reactions.Single().Triggers.Single().Data.Single().Name.ShouldEqual("reads");

    [Fact] void should_walk_reaction_reads()
    {
        var walker = new ReadsWalker();
        walker.VisitApplication(CompileTrigger("        when Signal\n          reads Status").Value!);
        walker.Names.ShouldContain("Status");
    }

    sealed class ReadsWalker : ScreenplaySyntaxWalker
    {
        public List<string> Names { get; } = [];

        public override void VisitReads(ReadsSyntax syntax) => Names.Add(syntax.ReadModel);
    }
}
