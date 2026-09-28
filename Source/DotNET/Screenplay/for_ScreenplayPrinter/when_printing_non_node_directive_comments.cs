// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_non_node_directive_comments : given.a_printer
{
    const string Source = """
        policy Member
          require authenticated
        persona Clerk
          policy Member // policy note
          description "A clerk" // persona description note
        module Shop
          feature Orders
            slice StateChange Place
              event OrderPlaced
                id String // property note
              // constraint lead
              constraint UniqueOrder // constraint header note
                message "Already placed" // constraint message note
                released by OrderRemoved // first release note
                ignore casing // casing note
                released by OrderCancelled // second release note
                unique id on OrderPlaced // unique rule note
            slice StateView Board
              query List => Order[]
                scoped to identity // scope note
                by id String // by note
                description "Find orders" // query description note
        """;

    [Fact]
    void should_anchor_every_directive_through_canonical_reordering()
    {
        var roundtrip = RoundTrip(Source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
        var printed = roundtrip.Printed;
        printed.ShouldContain("description \"A clerk\" // persona description note\n  policy Member // policy note");
        printed.ShouldContain("id String // property note\n\n      // constraint lead\n      constraint UniqueOrder // constraint header note");
        printed.ShouldContain("unique id on OrderPlaced // unique rule note\n        released by OrderRemoved // first release note\n        released by OrderCancelled // second release note\n        ignore casing // casing note\n        message \"Already placed\" // constraint message note");
        printed.ShouldContain("description \"Find orders\" // query description note\n        by id String // by note\n        scoped to identity // scope note");
        roundtrip.PrintedAgain.ShouldEqual(printed);
    }

    [Fact]
    void should_keep_a_comment_on_an_additional_unique_rule()
    {
        const string source = """
            module Shop
              feature Orders
                slice StateChange Place
                  event OrderPlaced
                    id String
                  event OrderImported
                    id String
                  constraint UniqueOrder // header note
                    unique id on OrderPlaced // first rule note
                    unique id on OrderImported // second rule note
                    ignore casing // casing note
            """;
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Diagnostics.ShouldBeEmpty();
        roundtrip.Printed.ShouldContain("constraint UniqueOrder // header note\n        unique id on OrderPlaced // first rule note\n        unique id on OrderImported // second rule note\n        ignore casing // casing note");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }

    [Fact]
    void should_keep_comments_with_a_typed_edit_and_replacement()
    {
        var parsed = _compiler.Parse(Source);
        parsed.Diagnostics.ShouldBeEmpty();
        var original = parsed.Value!;
        var module = original.Modules.Single();
        var feature = module.Features.Single();
        var place = feature.Slices.First();
        var board = feature.Slices.Last();
        var constraint = place.Constraints.Single();
        var query = board.Queries.Single();
        var edited = original with
        {
            Personas = [original.Personas!.Single() with { Description = "An editor" }],
            Modules = [module with
            {
                Features = [feature with
                {
                    Slices =
                    [
                        place with { Constraints = [constraint with { Message = "An existing order", ReleasedBy = ["OrderRemoved", "OrderArchived"] }] },
                        board with { Queries = [query with { Scope = "global", Description = "Find all orders" }] }
                    ]
                }]
            }]
        };

        var printed = _printer.Print(edited);
        printed.ShouldContain("description \"An editor\" // persona description note");
        printed.ShouldContain("constraint UniqueOrder // constraint header note");
        printed.ShouldContain("released by OrderRemoved // first release note\n        released by OrderArchived // second release note");
        printed.ShouldContain("message \"An existing order\" // constraint message note");
        printed.ShouldContain("by id String // by note");
        printed.ShouldContain("scoped to global // scope note");
        printed.ShouldContain("description \"Find all orders\" // query description note");
        _compiler.Parse(printed).Diagnostics.ShouldBeEmpty();
        _printer.Print(_compiler.Parse(printed).Value!).ShouldEqual(printed);

        foreach (var node in new SyntaxNode[] { original.Personas!.Single(), constraint, query })
        {
            var relocated = node with { DirectiveLocations = new Dictionary<string, SourceLocation>() };
            SyntaxJson.Serialize(node).GetRawText().ShouldEqual(SyntaxJson.Serialize(relocated).GetRawText());
            SyntaxJson.StructurallyEqual(node, relocated).ShouldBeTrue();
            SyntaxSchema.For(node.GetType().Name).GetProperty("properties").TryGetProperty("directiveLocations", out _).ShouldBeFalse();
        }
    }
}
