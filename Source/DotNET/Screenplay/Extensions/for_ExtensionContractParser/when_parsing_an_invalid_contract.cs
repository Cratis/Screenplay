// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Extensions.for_ExtensionContractParser;

public class when_parsing_an_invalid_contract
{
    static string[] Codes(string source) => [.. ExtensionContractParser.Parse(source).Diagnostics.Select(diagnostic => diagnostic.Code)];

    static string With(string line) => ExtensionContractSample.Stripe.Replace("  frontend \"frontend/index.js\"", "  frontend \"frontend/index.js\"\n" + line);

    [Fact] void should_report_a_missing_display_name() => Codes(ExtensionContractSample.Stripe.Replace("  display name \"Stripe payments\"\n", string.Empty)).ShouldEqual([DiagnosticCodes.MissingExtensionManifestField]);
    [Fact] void should_report_a_missing_description() => Codes(ExtensionContractSample.Stripe.Replace("  description \"Card payments with Stripe\"\n", string.Empty)).ShouldEqual([DiagnosticCodes.MissingExtensionManifestField]);
    [Fact] void should_report_a_missing_icon() => Codes(ExtensionContractSample.Stripe.Replace("  icon \"assets/icon.svg\"\n", string.Empty)).ShouldEqual([DiagnosticCodes.MissingExtensionManifestField]);
    [Fact] void should_report_a_component_without_a_placeholder() => Codes(ExtensionContractSample.Stripe.Replace("    placeholder \"placeholders/payment-form.html\"\n", string.Empty)).ShouldEqual([DiagnosticCodes.ComponentWithoutPlaceholder]);
    [Fact] void should_report_an_unknown_component_kind() => Codes(ExtensionContractSample.Stripe.Replace("kind react", "kind vue")).ShouldEqual([DiagnosticCodes.UnknownComponentKind]);
    [Fact] void should_report_an_unknown_platform() => Codes(ExtensionContractSample.Stripe.Replace("    platform web", "    platform toaster")).ShouldEqual([DiagnosticCodes.UnknownComponentPlatform]);
    [Fact] void should_report_a_setting_that_is_public_and_secret() => Codes(With("  setting both public secret")).ShouldEqual([DiagnosticCodes.SettingBothPublicAndSecret]);
    [Fact] void should_report_a_component_invoking_an_undeclared_ability() => Codes(ExtensionContractSample.Stripe.Replace("invokes CreatePaymentIntent", "invokes Refund")).ShouldEqual([DiagnosticCodes.ComponentInvokesUnknownAbility]);
    [Fact] void should_report_a_duplicate_ability() => Codes(With("  ability CreatePaymentIntent\n    amount Money")).ShouldEqual([DiagnosticCodes.DuplicateExtensionMember]);
    [Fact] void should_report_an_unknown_line() => Codes(With("  surprise 1")).ShouldEqual([DiagnosticCodes.InvalidExtensionContract]);
    [Fact] void should_report_a_file_without_an_extension() => Codes("setting x").ShouldEqual([DiagnosticCodes.InvalidExtensionContract]);
}
