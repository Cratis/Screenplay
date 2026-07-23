// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets.for_SecretsFile;

public class when_parsing_and_writing : Specification
{
    const string Source =
        """
        // Azure AD credentials
        azureAdClientId     = enc:v1:q83vEjRWeJASNFZ4kKvN7w==
        azureAdClientSecret = enc:v1:ASNFZ4mrze8SNFZ4q83vEg==

        // GitHub credentials
        githubClientId = enc:v1:Vnhry83vASNFVnhrEjRWeA==
        """;

    SecretsFile _file;
    string _written;
    SecretsFile _reparsed;
    string _writtenAgain;

    void Because()
    {
        _file = SecretsFile.Parse(Source);
        _written = _file.Write();
        _reparsed = SecretsFile.Parse(_written);
        _writtenAgain = _reparsed.Write();
    }

    [Fact] void should_parse_all_entries() => _file.Entries.Count().ShouldEqual(3);
    [Fact] void should_parse_the_names() => _file.Entries.Select(_ => _.Name).ShouldContainOnly("azureAdClientId", "azureAdClientSecret", "githubClientId");
    [Fact] void should_parse_the_encrypted_values() => _file.Entries.First().EncryptedValue.ShouldEqual("enc:v1:q83vEjRWeJASNFZ4kKvN7w==");
    [Fact] void should_round_trip_the_entries() => _reparsed.Entries.SequenceEqual(_file.Entries).ShouldBeTrue();
    [Fact] void should_write_the_same_text_on_a_second_pass() => _writtenAgain.ShouldEqual(_written);
}
