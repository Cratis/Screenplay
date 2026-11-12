// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions.for_ExtensionContractResolver;

public class when_resolving_a_locked_extension : Specification
{
    static readonly SystemExtensionSyntax Reference = new("cratis/stripe", "2", SourceLocation.Start);

    ExtensionFixture _restored;
    ExtensionResolution _resolution;

    void Establish() => _restored = ExtensionFixture.Locked("cratis/stripe", "2", ExtensionContractSample.Stripe);

    void Because() => _resolution = _restored.Resolver().Resolve([Reference], _restored.Lock);

    void Destroy() => _restored.Dispose();

    [Fact] void should_report_nothing() => _resolution.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_contract() => _resolution.Contracts["cratis/stripe"].Name.ShouldEqual("Stripe");

    [Fact]
    void should_give_the_system_the_contracts_abilities_and_qualified_triggers()
    {
        var application = new ApplicationSyntax([], [], [], [], SourceLocation.Start) { Systems = [new SystemSyntax("Payments", null, SourceLocation.Start) { Extension = Reference }] };
        var applied = _resolution.Apply(application);
        applied.Systems.Single().Abilities.Select(ability => ability.Name).ShouldEqual(["CreatePaymentIntent"]);
        applied.Triggers!.Select(trigger => trigger.Name).ShouldEqual(["Payments.PaymentSucceeded"]);
        application.Systems.Single().Abilities.ShouldBeEmpty();
    }

    [Fact] void should_report_a_reference_missing_from_the_lock() => _restored.Resolver().Resolve([new("cratis/other", null, SourceLocation.Start)], _restored.Lock).Diagnostics.Select(d => d.Code).ShouldEqual([DiagnosticCodes.ExtensionNotLocked]);
    [Fact] void should_report_a_changed_range_as_stale() => _restored.Resolver().Resolve([Reference with { Version = "3" }], _restored.Lock).Diagnostics.Select(d => d.Code).ShouldEqual([DiagnosticCodes.ExtensionLockStale]);
    [Fact] void should_warn_instead_of_failing_in_an_editor() => _restored.Resolver(ExtensionResolutionMode.Editor).Resolve([new("cratis/other", null, SourceLocation.Start)], _restored.Lock).Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
