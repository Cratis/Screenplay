// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_SlicePlacementParser;

public class when_validating_placements : Specification
{
    [Theory]
    [InlineData("shows")]
    [InlineData("shows event")]
    [InlineData("shows command AddBook")]
    [InlineData("shows event BookAdded extra")]
    public void should_refuse_a_malformed_placement(string line) =>
        Validate("ReviewBooks", line).ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSlicePlacement && diagnostic.Severity == DiagnosticSeverity.Error);

    [Theory]
    [InlineData("shows event BookRemoved")]
    [InlineData("shows readmodel Shelf")]
    public void should_warn_about_an_element_nobody_declares(string line) =>
        Validate("ReviewBooks", line).ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownPlacedElement && diagnostic.Severity == DiagnosticSeverity.Warning);

    [Fact] void should_warn_about_showing_an_element_the_slice_declares() =>
        Validate("AddBook", "shows event BookAdded").ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantSlicePlacement);

    [Fact] void should_warn_about_showing_an_element_twice() =>
        Validate("ReviewBooks", "shows event BookAdded\n      shows event BookAdded").Count(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantSlicePlacement).ShouldEqual(1);

    static IEnumerable<Diagnostic> Validate(string slice, string lines)
    {
        var addBook = slice == "AddBook" ? $"\n      {lines}" : string.Empty;
        var reviewBooks = slice == "ReviewBooks" ? $"\n      {lines}" : string.Empty;
        return new ScreenplayCompiler().Compile($"""
            module Lending
              feature Books
                slice StateChange AddBook{addBook}
                  event BookAdded
                    title String

                slice StateView ReviewBooks{reviewBooks}
            """).Diagnostics;
    }
}
