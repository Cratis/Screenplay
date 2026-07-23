// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings.for_StringsFile;

public class when_parsing_an_unquoted_value : Specification
{
    const string Source =
        """
        // Values must be quoted
        invoices.title = Invoices
        """;

    Exception _error;

    void Because() => _error = Catch.Exception(() => StringsFile.Parse(Source));

    [Fact] void should_fail_to_parse() => _error.ShouldBeOfExactType<InvalidStringsLine>();
    [Fact] void should_report_the_line_number() => _error.Message.ShouldContain("line 2");
}
