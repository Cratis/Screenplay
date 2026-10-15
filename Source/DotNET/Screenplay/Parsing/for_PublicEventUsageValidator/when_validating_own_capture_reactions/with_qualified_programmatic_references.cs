// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_own_capture_reactions;

public class with_qualified_programmatic_references : Specification
{
    ApplicationSyntax _application;
    ParserContext _context;

    void Establish()
    {
        var parsed = new ScreenplayCompiler().Parse("""
            import M.F.Facts.Recorded
            module M
              feature F
                slice StateChange Facts
                  event Recorded generation 1
                  event Recorded generation 2
                slice Translate Import
                  capture Feed
                    append Recorded
                  reaction FollowUp
                    when Recorded
            """).Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Last();
        var reaction = slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var qualified = trigger with { Source = new NamedTriggerSourceSyntax("M.F.Facts.Recorded", trigger.Location) };
        var altered = slice with { Reactions = [reaction with { Triggers = [trigger, qualified] }] };
        _application = parsed with { Modules = [module with { Features = [feature with { Slices = [feature.Slices.First(), altered] }] }] };
        _context = ParserContext.ForDiagnostics();
    }

    void Because() => PublicEventUsageValidator.Validate(_application, _context);

    [Fact] void should_match_the_resolved_declaration_once() => _context.Diagnostics.Count.ShouldEqual(1);
    [Fact] void should_report_the_own_capture_code() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DirectionlessTranslationReactsToOwnCapture);
    [Fact] void should_not_repeat_the_event_for_another_spelling() => _context.Diagnostics.Single().Message.ShouldContain("(Recorded)");
}
