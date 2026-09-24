// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_tagged_fence_with_comments : given.a_printer
{
    const string Source =
        """
        // This is a Screenplay comment.
        policy CanApprove
          ```csharp
          // This belongs to the embedded code.
          return true;
          ```
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_embedded_comment_in_the_code() => _roundtrip.Reparsed.Value!.Policies.Single().Code!.Code.ShouldContain("// This belongs to the embedded code.");
    [Fact] void should_not_capture_embedded_comment_as_a_source_comment() => _roundtrip.Original!.Value!.Policies.Single().SourceComments.Select(_ => _.Text).ShouldNotContain("// This belongs to the embedded code.");
    [Fact] void should_print_embedded_comment_once() => _roundtrip.Printed.Split("// This belongs to the embedded code.").Length.ShouldEqual(2);
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
}
