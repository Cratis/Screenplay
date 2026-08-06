// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files.for_PlayFileWriter;

public class when_expanding_names_a_file_system_cannot_tell_apart : Specification
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange Register
              event InvoiceRegistered

            feature Register
        """;

    ApplicationSyntax _application;
    Exception _error;

    void Establish() => _application = new ScreenplayCompiler().Compile(Source).Value!;

    void Because() => _error = Catch.Exception(() => new PlayFileWriter().Expand(_application));

    [Fact] void should_refuse_to_lose_one_of_them() => _error.ShouldBeOfExactType<AmbiguousPlayFilePath>();
    [Fact] void should_name_the_path_they_both_claim() => _error.Message.ShouldContain(Path.Combine("Invoicing", "Invoices", "Register", "Register.play"));
}
