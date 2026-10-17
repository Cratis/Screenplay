// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class with_different_keys : given.a_compiler
{
    const string Source =
        """
        concept LinkId : String
        module Demo
          feature Links
            slice StateChange ReviseLink
              command ReviseLink
                linkId LinkId identifier
                supersedesLinkId LinkId
                produces event LinkRevised
                  linkId LinkId = linkId
                  supersedesLinkId LinkId = supersedesLinkId
            slice StateView Links
              readmodel LinksView
                linkId LinkId
                supersededByLinkId LinkId optional
              query GetLink => LinksView optional
                by linkId LinkId
              projection Links => LinksView
                no automap
                from LinkRevised key linkId
                  linkId = linkId
                  clear supersededByLinkId
                from LinkRevised key supersedesLinkId
                  linkId = supersedesLinkId
                  supersededByLinkId = linkId
        """;

    CompilationResult<ApplicationSyntax> _result;

    Diagnostic Duplicate => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateEventInProjectionScope);

    void Because() => _result = _compiler.Parse(Source);

    [Fact] void should_fail_ordinary_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_error() => Duplicate.Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_locate_the_second_from_line() => Duplicate.Location.Line.ShouldEqual(22);
    [Fact] void should_locate_the_second_from_column() => Duplicate.Location.Column.ShouldEqual(9);
    [Fact] void should_name_the_event_projection_and_scope() => Duplicate.Message.ShouldEqual("Event 'LinkRevised' is used more than once in projection 'Links' at the root level (Chronicle allows one use of an event type per projection level)");
}
