// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_PrivilegeCompleteness;

public class when_comparing_event_producers : Specification
{
    [Theory]
    [InlineData("", "", DiagnosticSeverity.Warning)]
    [InlineData("  require authenticated", "        authorize Access", DiagnosticSeverity.Warning)]
    [InlineData("  require role \"Automation\"", "        authorize Access", null)]
    [InlineData("  require role \"Automation\" and role \"Auditor\"", "        authorize Access", null)]
    [InlineData("  require role \"Automation\" or role \"User\"", "        authorize Access", DiagnosticSeverity.Warning)]
    [InlineData("  file Access.cs", "        authorize Access", DiagnosticSeverity.Information)]
    void should_report_the_gate_comparison_at_each_producer(string policy, string gate, DiagnosticSeverity? severity)
    {
        var source = (policy.Length > 0 ? $"policy Access\n{policy}\n" : string.Empty) + $"module M\n  feature F\n    slice Automation S\n      event E\n      command Producer\n{gate}\n        produces E\n          for \"one\"\n      command C\n      reaction R\n        runs as system role \"Automation\"\n        when E\n          invokes C";
        var compiled = new ScreenplayCompiler().Compile(source);
        compiled.Success.ShouldBeTrue();
        var findings = ModelCompleteness.Check(compiled, new([CompletenessCheck.Privilege]));
        findings.Length.ShouldEqual(severity is null ? 0 : 1);
        if (severity is not null)
        {
            findings.Single().Severity.ShouldEqual(severity.Value);
            findings.Single().Code.ShouldEqual(DiagnosticCodes.ReactionPrivilegeEscalation);
            findings.Single().Message.ShouldContain("command 'Producer'");
        }
    }

    [Fact]
    void should_check_every_declared_role()
    {
        var source = "policy Access\n  require role \"Automation\"\nmodule M\n  feature F\n    slice Automation S\n      event E\n      command C\n        authorize Access\n        produces E\n          for \"one\"\n      reaction R\n        runs as system role \"Automation\" and role \"Auditor\"\n        when E\n          invokes C";
        ModelCompleteness.Check(new ScreenplayCompiler().Compile(source), new([CompletenessCheck.Privilege])).Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    }

    [Fact]
    void should_count_captures_and_other_reactions_as_unprivileged_producers()
    {
        var source = "module M\n  feature F\n    slice Automation S\n      event E\n      event Input\n      command C\n      reaction Producer\n        when Input\n          produces E\n            for \"one\"\n      reaction Elevated\n        runs as system role \"Automation\"\n        when E\n          invokes C\n    slice Translate Import\n      capture Imported\n        source api\n        append E";
        var findings = ModelCompleteness.Check(new ScreenplayCompiler().Compile(source), new([CompletenessCheck.Privilege]));
        findings.Select(finding => finding.Message).Any(message => message.Contains("capture 'Imported'", StringComparison.Ordinal)).ShouldBeTrue();
        findings.Select(finding => finding.Message).Any(message => message.Contains("reaction 'Producer'", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("every 1 day")]
    [InlineData("when Startup")]
    void should_not_invent_a_producer_for_clock_or_application_triggers(string trigger)
    {
        var compilation = new ScreenplayCompiler().Compile($"module M\n  feature F\n    slice Automation S\n      command C\n      reaction R\n        runs as system role \"Automation\"\n        {trigger}\n          invokes C");
        compilation.Success.ShouldBeTrue();
        ModelCompleteness.Check(compilation, new([CompletenessCheck.Privilege])).ShouldBeEmpty();
    }

    [Fact] void should_parse_the_opt_in_name() => CompletenessChecks.TryParse("privilege", out _).ShouldBeTrue();
    [Fact] void should_parse_the_diagnostic_code() => CompletenessChecks.TryParse("PLAY0652", out _).ShouldBeTrue();
}
