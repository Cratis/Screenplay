// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets.for_SecretsFile;

public class when_parsing_an_unencrypted_value : Specification
{
    const string Source =
        """
        // A plaintext value must never appear in a secrets file
        azureAdClientId = my-plaintext-secret
        """;

    Exception _error;

    void Because() => _error = Catch.Exception(() => SecretsFile.Parse(Source));

    [Fact] void should_fail_to_parse() => _error.ShouldBeOfExactType<InvalidSecretsLine>();
    [Fact] void should_report_the_line_number() => _error.Message.ShouldContain("line 2");
}
