// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_repeated_behavior_uses : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        behavior Pick
          on click
            when item.missing == "open"
              notify info "Open"
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

    [Fact] void should_report_the_repeated_finding_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownActionSubjectField).ShouldEqual(1);
    [Fact] void should_report_it_at_the_first_site() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownActionSubjectField).Location.Line.ShouldEqual(3);
}
