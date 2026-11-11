// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_configuring_systems : given.a_compiler
{
    const string Source = """
        secret StripeSecretKey
          description "The live Stripe API key"

        system Payments
          description "How shoppers pay"
          extension "ghcr.io/cratis/extension-stripe" version 1
          setting secretKey from secret StripeSecretKey
          setting statementDescriptor = "ACME \"COMMERCE\""
          setting retries = 3
          setting capture = true
          activation on demand idle after 1 minute
        """;

    ApplicationSyntax _application;
    SystemSyntax _system;
    string _printed;

    string[] Codes(string source) => [.. _compiler.Compile(source).Diagnostics.Select(diagnostic => diagnostic.Code)];

    void Because()
    {
        var parsed = _compiler.Compile(Source);
        _application = parsed.Value!;
        _system = _application.Systems.Single();
        _printed = new ScreenplayPrinter().Print(_application);
    }

    [Fact] void should_report_nothing() => _compiler.Compile(Source).Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_secret() => _application.Secrets.Select(secret => (secret.Name, secret.Description)).ShouldEqual([("StripeSecretKey", "The live Stripe API key")]);
    [Fact] void should_read_the_extension() => (_system.Extension!.Reference, _system.Extension.Version).ShouldEqual(("ghcr.io/cratis/extension-stripe", "1"));
    [Fact] void should_read_the_settings_in_order() => _system.Settings.Select(setting => setting.Name).ShouldEqual(["secretKey", "statementDescriptor", "retries", "capture"]);
    [Fact] void should_read_the_secret_mapping() => _system.Settings.First().Secret.ShouldEqual("StripeSecretKey");
    [Fact] void should_unescape_a_string_literal() => _system.Settings.ElementAt(1).Value.ShouldEqual("ACME \"COMMERCE\"");
    [Fact] void should_kind_the_literals() => _system.Settings.Select(setting => setting.ValueKind).ShouldEqual([null, "string", "number", "boolean"]);
    [Fact] void should_read_the_activation() => (_system.Activation!.Always, _system.Activation.IdleAmount, _system.Activation.IdleUnit).ShouldEqual((false, 1, "minute"));
    [Fact] void should_print_the_source_back() => _printed.TrimEnd().ShouldEqual(Source.TrimEnd().Replace("on demand idle after 1 minute", "on demand idle after 1 minute"));
    [Fact] void should_round_trip() => _compiler.Compile(_printed).Diagnostics.ShouldBeEmpty();
    [Fact] void should_accept_activation_always() => Codes("system S\n  activation always").ShouldBeEmpty();
    [Fact] void should_accept_on_demand_without_an_idle_timeout() => Codes("system S\n  activation on demand").ShouldBeEmpty();
    [Fact] void should_pluralize_the_idle_unit_when_printing() => new ScreenplayPrinter().Print(_compiler.Compile("system S\n  activation on demand idle after 10 minutes").Value!).ShouldContain("idle after 10 minutes");
    [Fact] void should_report_an_unknown_secret() => Codes("system S\n  setting key from secret Missing").ShouldContain(DiagnosticCodes.UnknownSecret);
    [Fact] void should_report_an_unused_secret() => Codes("secret Lonely").ShouldContain(DiagnosticCodes.UnusedSecret);
    [Fact] void should_report_a_duplicate_secret() => Codes("secret A\nsecret a\nsystem S\n  setting k from secret A").ShouldContain(DiagnosticCodes.DuplicateSecret);
    [Fact] void should_report_a_duplicate_mapping() => Codes("system S\n  setting k = 1\n  setting k = 2").ShouldContain(DiagnosticCodes.DuplicateSystemSetting);
    [Fact] void should_report_a_duplicate_mapping_across_secret_and_literal() => Codes("secret A\nsystem S\n  setting k from secret A\n  setting k = 2").ShouldContain(DiagnosticCodes.DuplicateSystemSetting);
    [Fact] void should_report_a_malformed_secret() => Codes("secret").ShouldContain(DiagnosticCodes.InvalidSecretDeclaration);
    [Fact] void should_report_a_secret_with_a_foreign_child() => Codes("secret A\n  value \"x\"\nsystem S\n  setting k from secret A").ShouldContain(DiagnosticCodes.InvalidSecretDeclaration);
    [Fact] void should_report_a_malformed_setting() => Codes("system S\n  setting k").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
    [Fact] void should_refuse_a_bare_word_as_a_literal() => Codes("system S\n  setting k = hello").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
    [Fact] void should_report_a_malformed_extension() => Codes("system S\n  extension nothing").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
    [Fact] void should_report_a_second_extension() => Codes("system S\n  extension \"a\"\n  extension \"b\"").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
    [Fact] void should_report_a_second_activation() => Codes("system S\n  activation always\n  activation on demand").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
    [Fact] void should_report_a_zero_idle_timeout() => Codes("system S\n  activation on demand idle after 0 minutes").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
    [Fact] void should_report_an_indented_child_of_a_line() => Codes("system S\n  setting k = 1\n    more").ShouldContain(DiagnosticCodes.InvalidSystemDeclaration);
}
