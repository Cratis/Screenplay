// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_named_guarded_bindings;

public class and_a_behavior_has_multiple_uses : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        behavior Pick
          on click
            when item.missing == "open"
              notify info "Open"
            when item.missing == "open"
              notify info "Still open"
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                status String
              query ItemDetails => Item
              screen First
                data Item via query ItemDetails
                uses Pick
              screen Second
                data Item via query ItemDetails
                uses Pick
        """);

    [Fact] void should_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_check_shadowing_once_at_the_declaration() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableActionAlternative).ShouldEqual(1);
    [Fact] void should_report_each_unknown_condition_field_only_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownActionSubjectField).ShouldEqual(2);
    [Fact] void should_have_no_duplicate_diagnostics() => _result.Diagnostics.Count().ShouldEqual(_result.Diagnostics.Distinct().Count());
    [Fact] void should_describe_interaction_shadowing() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableActionAlternative).Message.ShouldEqual("This 'when' alternative is shadowed by earlier alternatives in this interaction");
}
