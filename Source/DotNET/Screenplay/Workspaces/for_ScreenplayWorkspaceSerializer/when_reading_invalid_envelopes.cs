// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspaceSerializer;

public class when_reading_invalid_envelopes : given.a_transport_workspace
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    void should_reject_unknown_versions(int version) => Reject(root => root["schemaVersion"] = version);

    [Theory]
    [InlineData("schema")]
    [InlineData("schemaVersion")]
    [InlineData("applicationIdentity")]
    [InlineData("applicationName")]
    [InlineData("revision")]
    [InlineData("identityCatalog")]
    [InlineData("documents")]
    void should_reject_missing_root_members(string member) => Reject(root => root.Remove(member));

    [Theory]
    [InlineData("id")]
    [InlineData("stableKey")]
    [InlineData("path")]
    [InlineData("bytes")]
    void should_reject_missing_document_members(string member) => Reject(root => root["documents"]![0]!.AsObject().Remove(member));

    [Theory]
    [InlineData("../escape.play")]
    [InlineData("/absolute.play")]
    [InlineData("C:/absolute.play")]
    [InlineData("\\\\server\\share\\source.play")]
    [InlineData("folder/../escape.play")]
    [InlineData("folder//source.play")]
    [InlineData("CON.play")]
    [InlineData("source.txt")]
    void should_reject_unsafe_paths(string path) => Reject(root => root["documents"]![0]!["path"] = path);

    [Theory]
    [InlineData("!")]
    [InlineData("a")]
    [InlineData("/w==")]
    [InlineData("/v8=")]
    void should_reject_invalid_base64_or_source_encoding(string bytes) => Reject(root => root["documents"]![0]!["bytes"] = bytes);

    [Theory]
    [InlineData("id")]
    [InlineData("stableKey")]
    [InlineData("path")]
    void should_reject_duplicate_document_assignments(string member) => Reject(root => root["documents"]![1]![member] = root["documents"]![0]![member]!.GetValue<string>());

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{}{}")]
    void should_reject_malformed_or_incomplete_json(string json) => RejectBytes(Encoding.UTF8.GetBytes(json));

    [Fact] void should_reject_an_unknown_schema() => Reject(root => root["schema"] = "other.workspace");
    [Fact] void should_reject_unknown_root_members() => Reject(root => root["secret"] = "not admitted");
    [Fact] void should_reject_unknown_document_members() => Reject(root => root["documents"]![0]!["encoding"] = "utf16");
    [Fact] void should_reject_unknown_catalog_members() => Reject(root => root["identityCatalog"]!["extra"] = true);
    [Fact] void should_reject_an_empty_document_set() => Reject(root => root["documents"] = new JsonArray());
    [Fact] void should_reject_a_wrong_document_container() => Reject(root => root["documents"] = new JsonObject());
    [Fact] void should_reject_null_source_bytes() => Reject(root => root["documents"]![0]!["bytes"] = null);
    [Fact] void should_reject_a_path_as_a_stable_key() => Reject(root => root["documents"]![0]!["stableKey"] = "folder/key");
    [Fact] void should_reject_portable_path_case_collisions() => Reject(root => root["documents"]![1]!["path"] = root["documents"]![0]!["path"]!.GetValue<string>().ToUpperInvariant().Replace(".PLAY", ".play", StringComparison.Ordinal));
    [Fact] void should_reject_a_document_catalog_identity_mismatch() => Reject(root => root["documents"]![0]!["id"] = DocumentId.Create("unassigned").ToString());
    [Fact] void should_reject_an_application_catalog_mismatch() => Reject(root => root["applicationIdentity"] = ApplicationIdentity.Create("another-application").ToString());
    [Fact] void should_reject_a_catalog_revision_mismatch() => Reject(root => root["identityCatalog"]!["revision"] = SemanticIdentityCatalog.Empty(StableApplicationIdentity).Revision.ToString());
    [Fact] void should_reject_a_workspace_revision_mismatch() => Reject(root => root["revision"] = WorkspaceRevision.Compute("tampered"u8).ToString());
    [Fact] void should_reject_changed_source_with_a_stale_revision() => Reject(root => root["documents"]![0]!["bytes"] = Convert.ToBase64String([.. Workspace.Documents[0].Bytes, (byte)'\n']));
    [Fact] void should_reject_a_changed_valid_path_with_a_stale_revision() => Reject(root => root["documents"]![0]!["path"] = "relocated.play");
    [Fact] void should_reject_noncanonical_base64() => Reject(root => root["documents"]![0]!["bytes"] = " " + root["documents"]![0]!["bytes"]!.GetValue<string>());
    [Fact]
    void should_reject_noncanonical_path_separators()
    {
        var document = Workspace.Documents[0];
        var relocatedDocument = WorkspaceDocument.Create(document.Id, document.StableKey, PortablePlayPath.Parse("folder/source.play"), document.Bytes.AsSpan());
        var relocated = ScreenplayWorkspace.Create(StableApplicationIdentity, Workspace.ApplicationName, Workspace.Documents.SetItem(0, relocatedDocument), Workspace.IdentityCatalog);
        var canonical = ScreenplayWorkspaceSerializer.Serialize(relocated);
        ScreenplayWorkspaceSerializer.Deserialize(canonical).Revision.ShouldEqual(relocated.Revision);
        var text = Encoding.UTF8.GetString(canonical);
        var changed = text.Replace("\"path\":\"folder/source.play\"", "\"path\":\"folder\\\\source.play\"", StringComparison.Ordinal);
        changed.ShouldNotEqual(text);

        var failure = Catch.Exception(() => ScreenplayWorkspaceSerializer.Deserialize(Encoding.UTF8.GetBytes(changed)));

        failure.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
        failure.Message.ShouldEqual("Workspace JSON is valid but not canonical.");
    }
    [Fact] void should_reject_duplicate_root_members() => RejectText(text => text.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal));
    [Fact] void should_reject_duplicate_document_members() => RejectText(text => text.Replace("\"stableKey\":", "\"stableKey\":\"duplicate\",\"stableKey\":", StringComparison.Ordinal));
    [Fact] void should_reject_duplicate_catalog_members() => RejectText(text => text.Replace("\"eventContracts\":", "\"eventContracts\":[],\"eventContracts\":", StringComparison.Ordinal));
    [Fact] void should_reject_transport_whitespace() => RejectText(text => text + "\n");
    [Fact] void should_reject_trailing_data() => RejectText(text => text + "{}");
    [Fact] void should_reject_a_transport_bom() => RejectBytes([0xef, 0xbb, 0xbf, .. ScreenplayWorkspaceSerializer.Serialize(Workspace)]);
    [Fact] void should_reject_invalid_transport_utf8() => RejectBytes([.. "{\"schema\":\""u8.ToArray(), 0xff, .. "\"}"u8.ToArray()]);

    void Reject(Action<JsonObject> change)
    {
        var root = JsonNode.Parse(ScreenplayWorkspaceSerializer.Serialize(Workspace))!.AsObject();
        change(root);
        RejectBytes(Encoding.UTF8.GetBytes(root.ToJsonString()));
    }

    void RejectText(Func<string, string> change) => RejectBytes(Encoding.UTF8.GetBytes(change(Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(Workspace)))));

    static void RejectBytes(byte[] bytes) => Catch.Exception(() => ScreenplayWorkspaceSerializer.Deserialize(bytes)).ShouldBeOfExactType<InvalidScreenplayWorkspace>();
}
