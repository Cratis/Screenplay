// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_adding_a_member_to_a_parsed_slice : given.a_printer
{
    const string Source =
        """
        module Sales
          feature Orders
            slice StateView List
              event Placed
              specification Existing
                file Specs/Existing.cs
              query Find => Uuid[]
              event Cancelled
        """;

    string _printed;
    string _typedPrinted;

    void Because()
    {
        var parsed = _compiler.Compile(Source).Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var edited = slice with
        {
            Events = [.. slice.Events, new EventSyntax("Added", [], SourceLocation.Start)],
            Queries = [.. slice.Queries, new QuerySyntax("Other", new TypeRefSyntax("Uuid", true, false, SourceLocation.Start), null, [], null, SourceLocation.Start)]
        };
        _printed = _printer.Print(parsed with { Modules = [module with { Features = [feature with { Slices = [edited] }] }] });
        _typedPrinted = _printer.Print((ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed)));
    }

    [Fact] void should_keep_original_members_in_order() => Positions(_printed, "event Placed", "specification Existing", "query Find", "event Cancelled");
    [Fact] void should_put_new_events_after_the_last_event() => Positions(_printed, "event Cancelled", "event Added");
    [Fact] void should_put_new_queries_after_the_last_query() => Positions(_printed, "query Find", "query Other", "event Cancelled");
    [Fact] void should_print_typed_json_in_canonical_kind_order() => Positions(_typedPrinted, "event Placed", "event Cancelled", "query Find", "specification Existing");

    static void Positions(string printed, params string[] declarations)
    {
        var positions = declarations.Select(declaration => printed.IndexOf(declaration, StringComparison.Ordinal)).ToArray();
        positions.All(position => position >= 0).ShouldBeTrue();
        positions.SequenceEqual(positions.Order()).ShouldBeTrue();
    }
}
