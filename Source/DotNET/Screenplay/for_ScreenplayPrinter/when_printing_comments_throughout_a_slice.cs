// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_comments_throughout_a_slice : given.a_printer
{
    const string Source =
        """
        // @public
        module Sales // @owner Sales
          feature Orders
            // @derived
            slice StateChange Place
              // explain the command
              command Place // command annotation
                id Uuid // property annotation
              event Placed
                id Uuid
              specification Placing
                when Place
                  id = "web" // source annotation
              // at the end of the slice
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_keep_every_comment() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_keep_annotations() => _roundtrip.Printed.ShouldContain("// @public");
    [Fact] void should_keep_trailing_comments() => _roundtrip.Printed.ShouldContain("id = \"web\" // source annotation");
    [Fact] void should_keep_block_end_comments() => _roundtrip.Printed.ShouldContain("// at the end of the slice");
    [Fact] void should_not_put_comments_in_typed_json() => SyntaxJson.Serialize(_roundtrip.Original!.Value!).GetRawText().ShouldNotContain("sourceComments");
}
