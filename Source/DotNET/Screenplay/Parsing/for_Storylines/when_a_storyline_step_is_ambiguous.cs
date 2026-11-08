// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_Storylines;

public class when_a_storyline_step_is_ambiguous : given.a_storyline_model
{
    const string Storyline =
        """
        storyline ReturnAnItem
          slice RequestReturn
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Establish() => _result = new ScreenplayCompiler().Compile(Model + Storyline);

    [Fact] void should_report_the_ambiguous_reference() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.AmbiguousReference]);
    [Fact] void should_name_where_the_candidates_sit() => _result.Diagnostics.Single().Message.ShouldContain("Returns.Requests, Returns.Refunds");
}
