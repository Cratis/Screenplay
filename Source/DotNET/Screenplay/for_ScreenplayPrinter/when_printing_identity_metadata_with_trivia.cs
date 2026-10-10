// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_identity_metadata_with_trivia : given.a_printer
{
    const string Source =
        """"
        // Caller metadata
        identity // one shape
          description "Caller data" // intent
          department String optional from claim "department" // token
          // A derived flag
          partner Bool
            // Opaque source
            ```csharp
            return true;
            ```
          external String
            file Identity/External.cs // implementation
        """";
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_preserve_the_authored_comments() => _roundtrip.Printed.ShouldEqual(Source + "\n");
    [Fact] void should_keep_a_stable_printed_form() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_keep_the_typed_tree_unchanged() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
}
