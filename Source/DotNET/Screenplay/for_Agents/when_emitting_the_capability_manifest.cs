// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Agents;

namespace Cratis.Screenplay.for_Agents;

public class when_emitting_the_capability_manifest : given.a_manifest_model
{
    AgentCapabilityManifestEmission _emission;
    byte[] _bytes;

    void Because()
    {
        var compilation = Bind(File.ReadAllText(DocumentationFile("capability-manifest-v1.example.play")));
        Assert.True(compilation.Success, string.Join('\n', compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _emission = AgentCapabilityManifest.From(compilation);
        _bytes = AgentCapabilityManifestJson.Write(_emission.Manifest!);
    }

    [Fact] void should_emit_the_version() => _emission.Manifest!.Contract.ShouldEqual("screenplay-capability-manifest/1");
    [Fact] void should_use_personal_tokens() => _emission.Manifest!.TokenKind.ShouldEqual("personal");
    [Fact] void should_emit_only_the_two_listed_operations() => _emission.Manifest!.Operations.Count.ShouldEqual(2);
    [Fact] void should_have_no_refusal() => _emission.Refusal.ShouldEqual(AgentCapabilityManifestRefusal.None);
    [Fact] void should_not_require_approval_yet() => _emission.Manifest!.Operations.All(operation => !operation.ApprovalRequired).ShouldBeTrue();
    [Fact] void should_pin_the_shared_golden() => _bytes.ShouldEqual(File.ReadAllBytes(DocumentationFile("capability-manifest-v1.example.json")));

    [Fact]
    void should_describe_the_nested_query_exactly()
    {
        var operation = _emission.Manifest!.Operations.Single(operation => operation.Kind == "query");
        operation.Operation.ShouldEqual("Sales.Invoices.History.Listing.All");
        operation.Module.ShouldEqual("Sales");
        operation.Features.SequenceEqual(["Invoices", "History"]).ShouldBeTrue();
        operation.Slice.ShouldEqual("Listing");
        operation.Name.ShouldEqual("All");
        operation.Scope.ShouldEqual("read");
        operation.Description.ShouldEqual("List invoices");
    }

    [Fact]
    void should_describe_the_command_exactly()
    {
        var operation = _emission.Manifest!.Operations.Single(operation => operation.Kind == "command");
        operation.Operation.ShouldEqual("Sales.Invoices.Creating.Create");
        operation.Scope.ShouldEqual("write");
        operation.Description.ShouldEqual("Create an invoice");
    }

    [Fact]
    void should_have_exactly_the_schema_properties()
    {
        using var schema = JsonDocument.Parse(File.ReadAllBytes(DocumentationFile("capability-manifest-v1.schema.json")));
        using var manifest = JsonDocument.Parse(_bytes);
        Properties(manifest.RootElement).ShouldEqual(Properties(schema.RootElement.GetProperty("properties")));
        foreach (var operation in manifest.RootElement.GetProperty("operations").EnumerateArray())
        {
            Properties(operation).ShouldEqual(Properties(schema.RootElement.GetProperty("$defs").GetProperty("operation").GetProperty("properties")));
        }
    }

    static string[] Properties(JsonElement value) => value.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
}
