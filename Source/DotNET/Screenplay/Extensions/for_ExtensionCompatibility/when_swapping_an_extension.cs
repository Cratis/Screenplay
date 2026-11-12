// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionCompatibility;

public class when_swapping_an_extension : Specification
{
    ExtensionContract _required;

    static ExtensionContract Parse(string text) => ExtensionContractParser.Parse(text).Value!;

    void Establish() => _required = Parse(ExtensionContractSample.Stripe);

    [Fact] void should_accept_a_contract_that_offers_the_same_members() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe)).ShouldBeEmpty();
    [Fact] void should_accept_a_contract_that_offers_more() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe + "\n  trigger Refunded\n    reason String")).ShouldBeEmpty();
    [Fact] void should_report_a_missing_ability() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe.Replace("CreatePaymentIntent idempotent", "Charge idempotent").Replace("invokes CreatePaymentIntent", "invokes Charge"))).ShouldContain("ability 'CreatePaymentIntent' is not offered");
    [Fact] void should_report_a_changed_input() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe.Replace("orderId String", "orderId Guid"))).ShouldContain("ability 'CreatePaymentIntent' has different inputs or returns");
    [Fact] void should_report_a_missing_trigger() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe.Replace("trigger PaymentSucceeded", "trigger Other"))).ShouldContain("trigger 'PaymentSucceeded' is not offered");
    [Fact] void should_report_a_missing_component() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe.Replace("component PaymentForm", "component Other"))).ShouldContain("component 'PaymentForm' is not offered");
    [Fact] void should_report_a_changed_component_property() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe.Replace("property amount Money", "property amount String"))).ShouldContain("component 'PaymentForm' has different properties");
    [Fact] void should_report_a_missing_signal() => ExtensionCompatibility.Mismatches(_required, Parse(ExtensionContractSample.Stripe.Replace("raises succeeded", "raises completed"))).ShouldContain("component 'PaymentForm' does not raise 'succeeded'");
}
