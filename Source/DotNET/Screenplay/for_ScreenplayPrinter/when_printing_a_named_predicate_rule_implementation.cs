// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_named_predicate_rule_implementation : given.a_printer
{
    const string Source =
        """
        concept OrganizationNumber : String
          validate
            rule BeAValidOrganizationNumber message "Must be a valid organization number"
              csharp
                ```
                return Value.Length == 9 && Value.All(char.IsDigit);
                ```

        module Customers
          feature Approval
            slice StateChange ApproveCustomer
              command ApproveCustomer
                orgNumber String

                validate
                  orgNumber rule BeUnique
                    file Validations/BeUnique.cs
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_file_reference() => _roundtrip.Printed.ShouldContain("file Validations/BeUnique.cs");
    [Fact] void should_print_the_inline_code() => _roundtrip.Printed.ShouldContain("return Value.Length == 9 && Value.All(char.IsDigit);");
    [Fact] void should_preserve_the_command_rule_file() => CommandRule().File!.Path.ShouldEqual("Validations/BeUnique.cs");
    [Fact] void should_preserve_the_concept_rule_code() => ConceptRule().Code!.Code.ShouldContain("Value.Length == 9");

    ValidationRuleSyntax CommandRule() =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();

    ValidationRuleSyntax ConceptRule() =>
        _roundtrip.Reparsed.Value!.Concepts.Single().Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single();
}
