// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings.for_StringsFile;

public class when_parsing_and_writing : Specification
{
    const string Source =
        """
        // English strings
        invoices.title      = "Invoices"
        invoices.registered = "Invoice {number} registered"

        invoices.actions.newInvoice = "New Invoice"
        """;

    StringsFile _file;
    string _written;
    StringsFile _reparsed;
    string _writtenAgain;

    void Because()
    {
        _file = StringsFile.Parse(Source);
        _written = _file.Write();
        _reparsed = StringsFile.Parse(_written);
        _writtenAgain = _reparsed.Write();
    }

    [Fact] void should_parse_all_entries() => _file.Entries.Count().ShouldEqual(3);
    [Fact] void should_parse_the_keys() => _file.Entries.Select(_ => _.Key).ShouldContainOnly("invoices.title", "invoices.registered", "invoices.actions.newInvoice");
    [Fact] void should_keep_placeholders_verbatim() => _file.Entries.Single(_ => _.Key == "invoices.registered").Value.ShouldEqual("Invoice {number} registered");
    [Fact] void should_round_trip_the_entries() => _reparsed.Entries.SequenceEqual(_file.Entries).ShouldBeTrue();
    [Fact] void should_write_the_same_text_on_a_second_pass() => _writtenAgain.ShouldEqual(_written);
}
