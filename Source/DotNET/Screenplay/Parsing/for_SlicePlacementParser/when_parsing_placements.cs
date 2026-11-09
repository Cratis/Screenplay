// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SlicePlacementParser;

public class when_parsing_placements : Specification
{
    const string Source = """
        module Lending
          feature Books
            slice StateChange AddBook
              event BookAdded
                title String

            slice StateView ListBooks
              projection Books => Book
                from BookAdded

            slice StateView ReviewBooks
              shows event BookAdded
              shows readmodel Book
        """;

    CompilationResult<ApplicationSyntax> _parsed = null!;
    SliceSyntax _slice = null!;

    void Because()
    {
        _parsed = new ScreenplayCompiler().Compile(Source);
        _slice = _parsed.Value!.Modules.Single().Features.Single().Slices.Last();
    }

    [Fact] void should_read_the_kinds() => _slice.Placements.Select(placement => placement.Kind).ShouldContainOnly(SlicePlacementKind.Event, SlicePlacementKind.ReadModel);
    [Fact] void should_read_the_names() => _slice.Placements.Select(placement => placement.Name).ShouldContainOnly("BookAdded", "Book");
    [Fact] void should_resolve_both_declarations() => _parsed.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPlacedElement || diagnostic.Code == DiagnosticCodes.RedundantSlicePlacement);
}
