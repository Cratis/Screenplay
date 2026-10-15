// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_own_capture_reactions;

public class with_child_and_nested_appends : Specification
{
    ApplicationSyntax _application;
    ParserContext _context;

    void Establish()
    {
        _application = new ScreenplayCompiler().Parse("""
            module M
              feature F
                slice Translate Import
                  event First
                  event Second
                  capture Feed
                    children items identified by id
                      append First
                    nested detail
                      append Second
                      append First
                  reaction FollowUp
                    when First
                    when Second
            """).Value!;
        _context = ParserContext.ForDiagnostics();
    }

    void Because() => PublicEventUsageValidator.Validate(_application, _context);

    [Fact] void should_report_one_warning() => _context.Diagnostics.Count.ShouldEqual(1);
    [Fact] void should_report_the_own_capture_code() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DirectionlessTranslationReactsToOwnCapture);
    [Fact] void should_remain_a_warning() => _context.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_locate_the_reaction() => _context.Diagnostics.Single().Location.Line.ShouldEqual(12);
    [Fact] void should_list_each_matching_event_once_and_explain_the_split() => _context.Diagnostics.Single().Message.ShouldEqual("Reaction 'FollowUp' reacts to events appended by a capture in the same directionless Translate slice (First, Second); keep the capture in this Translate slice and move the reaction to an Automation slice.");
}
