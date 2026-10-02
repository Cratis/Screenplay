// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_inline_events : given.a_printer
{
    const string Source =
        """
        module Projects
          feature Naming
            slice StateChange Rename
              command Rename
                projectId Uuid identifier
                name String
                produces event Renamed
                  id "PreviouslyRenamed"
                  description
                    ```markdown
                    A **new** name.
                    ```
                  documentation
                    ```markdown
                    # Renaming
                    Keeps the project's identity.
                    ```
                  tag audit
                  // The payload's sequence label
                  @sequence String = name // Keep this on the mapping
                  description String = name
              event Existing
                id "PreviouslyExisting"
                description "An existing event"
                documentation
                  ```markdown
                  More about this event.
                  ```
                name String
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_be_stable_on_the_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_keep_inline_declarations() => _roundtrip.Printed.ShouldContain("produces event Renamed");
    [Fact] void should_escape_reserved_payload_names() => _roundtrip.Printed.ShouldContain("@sequence String = name");
    [Fact] void should_keep_the_pin() => _roundtrip.Printed.ShouldContain("id \"PreviouslyRenamed\"");
    [Fact] void should_keep_markdown_documentation() => _roundtrip.Printed.ShouldContain("# Renaming");
    [Fact] void should_keep_the_leading_mapping_comment() => _roundtrip.Printed.ShouldContain("// The payload's sequence label\n          @sequence String = name");
    [Fact] void should_keep_the_trailing_mapping_comment() => _roundtrip.Printed.ShouldContain("@sequence String = name // Keep this on the mapping");

    [Fact]
    void should_print_duplicate_properties_using_each_mappings_own_type()
    {
        var source = Source.Replace("description String = name", "@sequence Uuid = projectId", StringComparison.Ordinal);
        var roundtrip = RoundTrip(source);
        roundtrip.Original!.Success.ShouldBeFalse();
        roundtrip.Printed.ShouldContain("@sequence String = name");
        roundtrip.Printed.ShouldContain("@sequence Uuid = projectId");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }
}
