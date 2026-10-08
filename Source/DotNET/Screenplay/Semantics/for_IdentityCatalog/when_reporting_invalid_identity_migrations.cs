// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_reporting_invalid_identity_migrations : Specification
{
    SemanticAddress _event;
    SemanticAddress _property;
    InvalidSemanticContract _missing;
    InvalidSemanticContract _stale;
    InvalidSemanticContract _retirement;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        _event = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject"), "ProjectRegistered");
        _property = SemanticAddress.ForProperty(_event, "name");
        var previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [new(_event, SemanticId.Create(_event), SemanticIdentityOrigin.Persisted), new(_property, SemanticId.Create(_property), SemanticIdentityOrigin.Persisted)],
            [new(_event, EventContractId.CreateLegacy(application, _event.Name), EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        _missing = (InvalidSemanticContract)Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(previous, previous.Revision, [], [], [], [], [], []));
        _stale = (InvalidSemanticContract)Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(previous, previous.Revision, [], [_event, _property], [_event], [], [new(_event, _event), new(_property, _property)], [new(_event, _event)]));
        _retirement = (InvalidSemanticContract)Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(previous, previous.Revision, [], [_event, _property], [_event], [], [], [], [], [_event, _property], [_event]));
    }

    [Fact] void should_report_every_missing_assignment() => _missing.IdentityMigrationIssues.Length.ShouldEqual(3);
    [Fact] void should_name_the_semantic_rename_or_retirement_arrays() => Arguments(_missing, _property).Single().ShouldContainOnly("semanticRenames", "retiredSemanticAddresses");
    [Fact] void should_name_both_event_assignment_migrations() => Arguments(_missing, _event).SelectMany(arguments => arguments).ShouldContainOnly("semanticRenames", "retiredSemanticAddresses", "eventRenames", "retiredEventAddresses");
    [Fact] void should_report_every_stale_rename_address() => _stale.IdentityMigrationIssues.Select(issue => issue.Address).ShouldContainOnly(_event, _property, _event);
    [Fact] void should_name_both_stale_rename_arrays() => _stale.IdentityMigrationIssues.SelectMany(issue => issue.Arguments).Distinct().ShouldContainOnly("semanticRenames", "eventRenames");
    [Fact] void should_report_every_invalid_retirement() => _retirement.IdentityMigrationIssues.Length.ShouldEqual(3);
    [Fact] void should_name_both_retirement_arrays() => _retirement.IdentityMigrationIssues.SelectMany(issue => issue.Arguments).Distinct().ShouldContainOnly("retiredSemanticAddresses", "retiredEventAddresses");
    [Fact] void should_include_the_mcp_kind_in_the_message() => _missing.Message.ShouldContain("\"kind\":\"EventContract\"");
    [Fact]
    void should_include_typed_address_parts_in_the_message()
    {
        using var details = JsonDocument.Parse(_missing.Message.Split("Identity migration issues: ")[1]);
        details.RootElement.EnumerateArray().Single(issue => issue.GetProperty("address").GetProperty("kind").GetString() == "Property")
            .GetProperty("address").GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("kind").GetString() == "OwnerKind" && part.GetProperty("key").GetString() == "8").ShouldBeTrue();
    }

    static IEnumerable<ImmutableArray<string>> Arguments(InvalidSemanticContract failure, SemanticAddress address) =>
        failure.IdentityMigrationIssues.Where(issue => issue.Address.Equals(address)).Select(issue => issue.Arguments);
}
