// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reporting_identity_migration_failures : given.an_authoring_connection
{
    JsonElement _opened;
    JsonElement[] _semantics;
    JsonElement[] _events;
    JsonElement _missing;
    JsonElement _stale;
    string _beforeSource;

    void Establish()
    {
        Initialize();
        _opened = Open();
        Apply(_opened, Result("propose-source", Arguments(Source)));
        _opened = Open();
        var revision = _opened.GetProperty("revision").GetString();
        _semantics = [.. Page("semantics", revision).EnumerateArray().Select(item => item.GetProperty("address"))];
        _events = [.. Page("eventContracts", revision).EnumerateArray().Select(item => item.GetProperty("address"))];
        _beforeSource = File.ReadAllText(Path.Combine(RootPath, "application.play"));
    }

    void Because()
    {
        _missing = Refused(Arguments("concept ProjectId : Uuid\nconcept ProjectName : String\n"));
        var arguments = Arguments(Source);
        arguments["semanticRenames"] = _semantics.Select(address => new { previousAddress = address, currentAddress = address }).ToArray();
        arguments["eventRenames"] = _events.Select(address => new { previousAddress = address, currentAddress = address }).ToArray();
        _stale = Refused(arguments);
    }

    [Fact] void should_reject_missing_retirements() => _missing.GetProperty("conflicts")[0].GetProperty("kind").GetString().ShouldEqual("InvalidIdentityMigration");
    [Fact] void should_return_every_missing_semantic_retirement() => Addresses(_missing, "retiredSemanticAddresses").ShouldContainOnly(_semantics.Where(address => address.GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("key").GetString() == "Projects" && part.GetProperty("kind").GetString() == "Module")).Select(address => address.GetRawText()));
    [Fact] void should_return_every_missing_event_retirement() => Addresses(_missing, "retiredEventAddresses").ShouldContainOnly(_events.Select(address => address.GetRawText()));
    [Fact] void should_offer_event_renames_as_an_alternative_to_retirement() => Addresses(_missing, "eventRenames").ShouldContainOnly(_events.Select(address => address.GetRawText()));
    [Fact] void should_report_every_stale_semantic_rename_in_the_schema_address_shape() => Addresses(_stale, "semanticRenames").ShouldContainOnly(_semantics.Select(address => address.GetRawText()));
    [Fact] void should_report_every_stale_event_rename_in_the_schema_address_shape() => Addresses(_stale, "eventRenames").ShouldContainOnly(_events.Select(address => address.GetRawText()));
    [Fact] void should_not_retain_a_refused_proposal() => _missing.TryGetProperty("proposalId", out _).ShouldBeFalse();
    [Fact] void should_not_write_the_refused_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(_beforeSource);

    Dictionary<string, object?> Arguments(string source) => new()
    {
        ["expectedRevision"] = _opened.GetProperty("revision").GetString(),
        ["expectedCatalogRevision"] = _opened.GetProperty("catalogRevision").GetString(),
        ["formatting"] = "CanonicalizeTouchedDocuments",
        ["documents"] = new[] { new { operation = "replace-document", documentId = Page("documents", _opened.GetProperty("revision").GetString())[0].GetProperty("documentId").GetString(), source } }
    };

    JsonElement Refused(object arguments)
    {
        var result = Call("propose-source", arguments).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
        return result.GetProperty("structuredContent");
    }

    static IEnumerable<string> Addresses(JsonElement failure, string argument) =>
        failure.GetProperty("identityMigrationIssues").EnumerateArray()
            .Where(issue => issue.GetProperty("arguments").EnumerateArray().Any(value => value.GetString() == argument))
            .Select(issue => issue.GetProperty("address").GetRawText());
}
