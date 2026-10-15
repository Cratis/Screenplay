// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_protected_reads;

public class with_routed_producers : given.a_protected_read_model
{
    const string Routes = "eventsource Account\n  identifier String\n  stream Notes\n";

    [Fact]
    void should_admit_a_production_route_unrelated_to_the_read_view()
    {
        var source = Routes + Source.Replace("event Removed", "event Removed\n      event Other\n        value String", StringComparison.Ordinal)
            .Replace("produces Recorded", "produces Other\n          stream Account.Notes", StringComparison.Ordinal);
        var result = Bind(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Command(result.Value!.Model).Produces.Single().Route.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_routes_on_producers_of_handled_events(bool commandRoute)
    {
        var source = Routes + (commandRoute
            ? Source.Replace("reads View by id", "stream Account.Notes\n        reads View by id", StringComparison.Ordinal)
            : Source.Replace("produces Recorded", "produces Recorded\n          stream Account.Notes", StringComparison.Ordinal));
        Refusal(Bind(source), SemanticDecisionReadRefusal.NamedSourceOrStream);
    }

    [Fact]
    void should_refuse_a_routed_reaction_production_supplied_through_the_ast()
    {
        const string source = Routes + Source + "\n" + """
                slice Automation Follow
                  event Started
                  reaction Record
                    when Started
                      produces Recorded
                        for "one"
                        value = "current"
            """;
        var result = BindReactionSyntax(source, reaction => reaction with
        {
            Triggers = [.. reaction.Triggers.Select(trigger => trigger with
            {
                Produces = [.. trigger.Produces!.Select(produced => produced with { Stream = new("Account", "Notes", produced.Location) })]
            })]
        });
        Refusal(result, SemanticDecisionReadRefusal.NamedSourceOrStream);
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
    }
}
