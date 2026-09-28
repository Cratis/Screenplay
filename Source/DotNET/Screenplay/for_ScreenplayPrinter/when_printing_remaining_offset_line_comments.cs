// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_remaining_offset_line_comments : given.a_printer
{
    const string Source = """
        type Entry
          id String
          description "A value" // type description
        trigger Arrived
          id String
          description "An occurrence" // trigger description
        behavior Notify
          order 7 // behavior order
          parameter message String
          on click // interaction trigger
            execute Save
              on failure // failure continuation
                notify error "Failed"
              on success // success continuation
                open dialog Details
                  on result // result continuation
                    notify info "Done"
            where item.status == "Open" // interaction where
          description "An action" // behavior description
        module Shop
          dialog template Details
            content
          feature Orders
            slice StateChange Place
              event Saved
                id String
              command Save
                id String
                description "A command" // command description
              reducer Reduce => Entry
                on Saved
                  file Code/Reduce.cs
                  description "A rule" // rule description
                description "A reducer" // reducer description
              readmodel List
                id String
                description "A model" // readmodel description
              reaction Process
                when Arrived // reaction trigger
                  id String // trigger value
                  description "A reaction trigger" // reaction trigger description
                every 5 minutes // interval trigger
                at 08:00 // scheduled trigger
                where id == "1" // reaction where
                description "A reaction" // reaction description
              description "A slice" // slice description
            description "A feature" // feature description
          description "A module" // module description
        """;

    [Fact]
    void should_keep_comments_on_each_line_after_reordering_and_blank_line_insertion()
    {
        foreach (var source in new[] { Source, Source.Replace("\n", "\n\n", StringComparison.Ordinal) })
        {
            var roundtrip = RoundTrip(source);
            roundtrip.Original!.Diagnostics.ShouldBeEmpty();
            roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
            foreach (var line in new[]
            {
                "description \"A value\" // type description",
                "description \"An occurrence\" // trigger description",
                "order 7 // behavior order",
                "on click // interaction trigger",
                "on failure // failure continuation",
                "on success // success continuation",
                "on result // result continuation",
                "where item.status == \"Open\" // interaction where",
                "description \"An action\" // behavior description",
                "description \"A command\" // command description",
                "description \"A rule\" // rule description",
                "description \"A reducer\" // reducer description",
                "description \"A model\" // readmodel description",
                "when Arrived // reaction trigger",
                "every 5 minutes // interval trigger",
                "at 08:00 // scheduled trigger",
                "id String // trigger value",
                "description \"A reaction trigger\" // reaction trigger description",
                "where id == \"1\" // reaction where",
                "description \"A reaction\" // reaction description",
                "description \"A slice\" // slice description",
                "description \"A feature\" // feature description",
                "description \"A module\" // module description"
            })
            {
                roundtrip.Printed.Split('\n').Count(candidate => candidate.Trim() == line).ShouldEqual(1);
            }

            roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
        }
    }

    [Fact]
    void should_not_include_new_directive_anchors_in_typed_json()
    {
        var parsed = _compiler.Parse(Source);
        parsed.Diagnostics.ShouldBeEmpty();
        var application = parsed.Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var reaction = slice.Reactions.Single();
        var behavior = application.Behaviors.Single();
        var action = behavior.Bindings.Single().Actions.Single();

        foreach (var node in new SyntaxNode[]
        {
            application.Types!.Single(), application.Triggers!.Single(), behavior, behavior.Bindings.Single(),
            action, action.OnSuccess.Single(), module, feature, slice, slice.Commands.Single(),
            slice.ReadModels!.Single(), slice.Reducers!.Single(), slice.Reducers!.Single().Rules.Single(),
            reaction, reaction.Triggers.First()
        })
        {
            node.DirectiveLocations.ShouldNotBeEmpty();
            var withoutAnchors = node with { DirectiveLocations = new Dictionary<string, SourceLocation>() };
            SyntaxJson.Serialize(node).GetRawText().ShouldEqual(SyntaxJson.Serialize(withoutAnchors).GetRawText());
            SyntaxJson.StructurallyEqual(node, withoutAnchors).ShouldBeTrue();
        }
    }
}
