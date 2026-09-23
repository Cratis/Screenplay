// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_interleaved_declarations : given.a_printer
{
    const string Source =
        """
        module Sales
          feature First
            slice StateChange Place
              event Placed
                id Uuid
              specification Placing
                file Specs/Placing.cs
              command Place
                id Uuid identifier
                produces Placed
                  id = id
              constraint UniquePlace
                unique event Placed
          form Edit for Place
            field id
          feature Second
            slice StateView List
              query Find => Uuid[]
              screen List
                table Find
                  column id
              query Other => Uuid[]
          screen template Shell
            content
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_parse_without_errors() => _roundtrip.Original!.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_reparse_without_errors() => _roundtrip.Reparsed.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_keep_interleaved_slice_members() => Positions("event Placed", "specification Placing", "command Place", "constraint UniquePlace");
    [Fact] void should_keep_module_members() => Positions("feature First", "form Edit", "feature Second", "screen template Shell");
    [Fact] void should_keep_feature_members() => Positions("query Find", "screen List", "query Other");
    [Fact] void should_be_stable() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    void Positions(params string[] declarations)
    {
        var positions = declarations.Select(declaration => _roundtrip.Printed.IndexOf(declaration, StringComparison.Ordinal)).ToArray();
        positions.All(position => position >= 0).ShouldBeTrue();
        positions.SequenceEqual(positions.Order()).ShouldBeTrue();
    }
}
