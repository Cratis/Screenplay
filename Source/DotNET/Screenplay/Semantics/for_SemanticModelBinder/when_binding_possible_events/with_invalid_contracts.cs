// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class with_invalid_contracts : given.a_possible_events_binder
{
    [Theory]
    [InlineData("")]
    [InlineData("            Missing\n")]
    [InlineData("            Created for missing\n")]
    [InlineData("            Created for ids\n")]
    [InlineData("            Created for one of id\n")]
    [InlineData("            Created for maybe\n")]
    [InlineData("            Created for one of maybeIds\n")]
    void should_refuse_invalid_entries(string entries)
    {
        var source = Prefix + Properties + "        maybe Uuid optional\n        maybeIds Uuid[] optional\n        handler\n          may produce\n" + entries + "          implementation";
        var result = BindPossible(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventSet);
    }

    [Fact]
    void should_require_an_identifier_for_an_omitted_destination() =>
        BindPossible(Source.Replace("id Uuid identifier", "id Uuid", StringComparison.Ordinal)).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventSet);

    [Theory]
    [InlineData("receipt Id generated", "Created for receipt")]
    [InlineData("receipt Id[] generated", "Created for one of receipt")]
    void should_refuse_non_identifier_generated_destinations(string property, string entry)
    {
        var source = "concept Id : Uuid\n" + Prefix + Properties + "        " + property + "\n        handler\n          may produce\n            " + entry + "\n          implementation";
        BindPossible(source).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventSet);
    }

    [Fact]
    void should_refuse_duplicate_resolved_event_identities_in_programmatic_syntax()
    {
        var result = BindPossible(Source, transform: syntax => TransformCommands(syntax, command => command with
        {
            Handler = command.Handler! with
            {
                PossibleEvents = command.Handler!.PossibleEvents! with
                {
                    Events = [new("Created", "id", false, null, [], command.Location), new("Created", "other", false, null, [], command.Location)]
                }
            }
        }));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventSet);
    }

    [Fact]
    void should_refuse_mixed_programmatic_contracts()
    {
        var result = BindPossible(Source, transform: syntax => TransformCommands(syntax, command => command with
        {
            Produces = [new ProducesSyntax("Created", null, [], command.Location)]
        }));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventSet);
    }

    [Fact]
    void should_refuse_an_expression_disguised_as_a_property_in_programmatic_syntax()
    {
        var result = BindPossible(Source, transform: syntax => TransformCommands(syntax, command => command with
        {
            Handler = command.Handler! with { PossibleEvents = command.Handler!.PossibleEvents! with { Events = [new("Created", "cmd.id", false, null, [], command.Location)] } }
        }));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidPossibleEventSet);
    }
}
