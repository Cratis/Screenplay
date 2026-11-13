// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_Storylines;

public class when_a_storyline_step_names_nothing : given.a_storyline_model
{
    const string Storyline =
        """
        storyline ReturnAnItem
          command RequestReturn
          event ReturnApproved
          query RequestReturn
        """;

    CompilationResult<ApplicationSyntax> _result;
    Diagnostic[] _unknown;

    void Establish() => _result = new ScreenplayCompiler().Compile(Model + Storyline);

    void Because() => _unknown = [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownStorylineElement)];

    [Fact] void should_report_each_step_that_names_nothing() => _unknown.Length.ShouldEqual(2);
    [Fact] void should_name_the_missing_event() => _unknown[0].Message.ShouldContain("event 'ReturnApproved'");
    [Fact] void should_resolve_by_kind_so_a_command_name_is_not_a_query() => _unknown[1].Message.ShouldContain("query 'RequestReturn'");
    [Fact] void should_report_it_as_a_warning() => _unknown.ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    [Fact] void should_still_compile() => _result.Success.ShouldBeTrue();
}
