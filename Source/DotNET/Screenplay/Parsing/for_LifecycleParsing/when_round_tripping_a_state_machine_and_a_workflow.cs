// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_LifecycleParsing;

public class when_round_tripping_a_state_machine_and_a_workflow : Specification
{
    ApplicationSyntax _parsed = null!;
    string _printed = null!;
    string _printedAgain = null!;
    CompilationResult<ApplicationSyntax> _reparsed = null!;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _parsed = compiler.Parse(a_lifecycle_source.Source).Value!;
        _printed = new ScreenplayPrinter().Print(_parsed);
        _reparsed = compiler.Parse(_printed);
        _printedAgain = new ScreenplayPrinter().Print(_reparsed.Value!);
    }

    [Fact] void should_parse_the_state_machine() => _parsed.StateMachines.Single().States.Count().ShouldEqual(4);
    [Fact] void should_have_one_initial_state() => _parsed.StateMachines.Single().States.Count(state => state.IsInitial).ShouldEqual(1);
    [Fact] void should_keep_the_guard() => _parsed.StateMachines.Single().States.Single(state => state.Name == "Sent").Transitions.First().When.ShouldNotBeNull();
    [Fact] void should_keep_what_entering_does() => _parsed.StateMachines.Single().States.Single(state => state.Name == "Sent").Entering.Select(entry => entry.EntryKind).ShouldEqual([StateEntryKind.Send, StateEntryKind.Start]);
    [Fact] void should_parse_every_node_kind() => _parsed.Workflows.Single().Nodes.Select(node => node.NodeKind).Distinct().Count().ShouldEqual(7);
    [Fact] void should_name_the_first_trigger_implicitly() => _parsed.Workflows.Single().Nodes.First().Name.ShouldEqual("Trigger");
    [Fact] void should_keep_the_specifications() => _parsed.Workflows.Single().Specifications.Count().ShouldEqual(5);
    [Fact] void should_reparse_without_errors() => _reparsed.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _printedAgain.ShouldEqual(_printed);
    [Fact] void should_print_the_trigger_shorthand() => _printed.ShouldContain("  trigger when InvoiceLifecycle enters Overdue\n    -> ChargeLateFee");
    [Fact] void should_quote_decision_outlets() => _printed.ShouldContain("    \"chase\" -> EachLine");
}
