// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_identifying_authored_specification_steps : given.a_semantic_binder
{
    const string Source = "module M\n  feature Outer\n    feature Inner\n      slice StateChange S\n        command C\n          amount Int\n          produces E\n            amount = amount\n        event E\n          amount Int\n        example Input : C\n        specification Authored\n          parameter count Int\n          case Small count = 1\n          case Large count = 2\n          given clock \"2026-10-16T08:00:00Z\"\n          given caller\n            authenticated\n          then readmodel View\n            id = \"key\"\n          when Input\n          then E amount = case.count\n";
    Diagnostic[] _diagnostics;

    void Because() => _diagnostics = [.. Bind(Source).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationProperty)];

    [Fact] void should_report_each_expanded_case() => _diagnostics.Length.ShouldEqual(2);
    [Fact] void should_keep_the_authored_name_and_nested_scope() => _diagnostics.Select(diagnostic => diagnostic.Arguments["specification"]).ShouldEqual(Enumerable.Repeat(DiagnosticArgument.Declaration("specification", ["M", "Outer", "Inner", "S", "Authored"]), 2));
    [Fact] void should_count_clock_caller_and_readmodel_clauses_before_the_action() => _diagnostics.Select(diagnostic => diagnostic.Arguments["stepIndex"]).ShouldEqual(Enumerable.Repeat(DiagnosticArgument.Index(3), 2));
    [Fact] void should_keep_case_names_separate_from_the_declaration() => _diagnostics.Select(diagnostic => diagnostic.Arguments["case"]).ShouldEqual(DiagnosticArgument.Text("Small"), DiagnosticArgument.Text("Large"));
    [Fact] void should_name_the_used_example() => _diagnostics.Select(diagnostic => diagnostic.Arguments["example"]).ShouldEqual(Enumerable.Repeat(DiagnosticArgument.Text("Input"), 2));
}
