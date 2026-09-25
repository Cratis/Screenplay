// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_exporting_the_executable_model : given.a_connection
{
    void Establish() => Initialize();

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    void should_round_trip_exact_canonical_bytes_for_each_supported_model(int version)
    {
        var source = version switch
        {
            1 => "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      command RegisterProject",
            2 => Source + "\n      specification RegistersProject\n        when RegisterProject\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"\n        then ProjectRegistered\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"",
            _ => Source.Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          return true;\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal)
        };
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString();
        var first = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 17 });
        first.GetProperty("available").GetBoolean().ShouldBeTrue();
        first.GetProperty("schema").GetString().ShouldEqual("cratis.screenplay.esm");
        first.GetProperty("schemaVersion").GetInt32().ShouldEqual(version);
        first.GetProperty("languageVersion").GetString().ShouldEqual($"{version}.0");
        first.GetProperty("semanticVersion").GetString().ShouldEqual($"{version}.0");
        var modelRevision = first.GetProperty("modelRevision").GetString();
        var manifestRevision = first.GetProperty("attachmentManifestRevision").GetString();
        using var bytes = new MemoryStream();
        var page = first.GetProperty("page");
        while (true)
        {
            var chunk = page.GetProperty("bytesBase64").GetBytesFromBase64();
            chunk.Length.ShouldEqual(page.GetProperty("byteCount").GetInt32());
            page.GetProperty("offset").GetInt32().ShouldEqual((int)bytes.Length);
            bytes.Write(chunk);
            if (page.GetProperty("nextOffset").ValueKind == JsonValueKind.Null) break;
            page = Content("read-workspace", new
            {
                expectedRevision = revision, view = "executable-model",
                offset = page.GetProperty("nextOffset").GetInt32(), limit = 17,
                expectedModelRevision = modelRevision, expectedAttachmentManifestRevision = manifestRevision
            }).GetProperty("page");
        }

        bytes.Length.ShouldEqual(first.GetProperty("totalBytes").GetInt32());
        var model = SemanticModelSerializer.Deserialize(bytes.ToArray());
        model.Revision.ToString().ShouldEqual(modelRevision);
        bytes.ToArray().SequenceEqual(SemanticModelSerializer.Serialize(model)).ShouldBeTrue();
        bytes.ToArray().SequenceEqual(SemanticModelSerializer.Serialize(Workspace().Compilation.Value!.Model)).ShouldBeTrue();
        Content("read-workspace", new { expectedRevision = revision, view = "implementation-requirements" })
            .GetProperty("attachmentManifestRevision").GetString().ShouldEqual(manifestRevision);
    }

    [Fact]
    void should_reject_unpinned_and_stale_continuations_without_a_page()
    {
        var revision = Content("open-workspace", new { }).GetProperty("revision").GetString();
        var first = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 1 });
        var model = first.GetProperty("modelRevision").GetString();
        var manifest = first.GetProperty("attachmentManifestRevision").GetString();
        Refused(new { expectedRevision = "stale", view = "executable-model", offset = 1, expectedModelRevision = model, expectedAttachmentManifestRevision = manifest }, "StaleRevision");
        Refused(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedAttachmentManifestRevision = manifest }, "expectedModelRevision");
        Refused(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = model }, "expectedAttachmentManifestRevision");
        Refused(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = "stale", expectedAttachmentManifestRevision = manifest }, "StaleRevision");
        Refused(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = model, expectedAttachmentManifestRevision = "stale" }, "StaleRevision");
    }

    [Fact]
    void should_refuse_attachment_body_changes_in_both_views_without_changing_workspace_or_model()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("concept ProjectName : String", "concept ProjectName : String\n  validate\n    rule Check\n      file Handler.cs", StringComparison.Ordinal).Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          return true;\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal));
        var attachment = Path.Combine(RootPath, "Handler.cs");
        File.WriteAllText(attachment, "first");
        var revision = Content("open-workspace", new { }).GetProperty("revision").GetString();
        var first = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 1 });
        if (!first.GetProperty("available").GetBoolean()) throw new InvalidOperationException(Content("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" }).GetRawText());
        var model = first.GetProperty("modelRevision").GetString();
        var manifest = first.GetProperty("attachmentManifestRevision").GetString();
        var requirements = Content("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", limit = 1 });
        requirements.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(1);
        requirements.GetProperty("attachmentManifestRevision").GetString().ShouldEqual(manifest);
        File.WriteAllText(attachment, "second");
        var changed = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 1 });
        changed.GetProperty("modelRevision").GetString().ShouldEqual(model);
        changed.GetProperty("workspace").GetProperty("revision").GetString().ShouldEqual(revision);
        changed.GetProperty("attachmentManifestRevision").GetString().ShouldNotEqual(manifest);
        Refused(new { expectedRevision = revision, view = "executable-model", offset = 1, expectedModelRevision = model, expectedAttachmentManifestRevision = manifest }, "StaleRevision");
        Refused(new { expectedRevision = revision, view = "implementation-requirements", offset = 1, expectedAttachmentManifestRevision = manifest }, "StaleRevision");
        Refused(new { expectedRevision = revision, view = "implementation-requirements", offset = 1 }, "expectedAttachmentManifestRevision");
        var next = Content("read-workspace", new { expectedRevision = revision, view = "implementation-requirements", offset = 1, expectedAttachmentManifestRevision = changed.GetProperty("attachmentManifestRevision").GetString() });
        next.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    }

    [Fact]
    void should_never_export_a_last_good_model_after_compilation_fails()
    {
        var opened = Content("open-workspace", new { });
        _ = Content("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "executable-model" });
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        authorize MissingPolicy");
        opened = Content("open-workspace", new { });
        var failed = Content("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "executable-model" });
        failed.GetProperty("available").GetBoolean().ShouldBeFalse();
        failed.GetProperty("executableDiagnosticsView").GetString().ShouldEqual("executable-diagnostics");
        failed.GetProperty("executableDiagnosticsCount").GetInt32().ShouldBeGreaterThan(0);
        failed.TryGetProperty("page", out _).ShouldBeFalse();
        failed.TryGetProperty("modelRevision", out _).ShouldBeFalse();
    }

    [Fact]
    void should_advertise_view_dependent_limits_and_keep_the_largest_page_under_the_cap()
    {
        var largeSource = new StringBuilder("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event ProjectRegistered\n");
        for (var index = 0; index < 1100; index++)
        {
            largeSource.Append("        property").Append(index).Append(" String\n");
        }

        File.WriteAllText(Path.Combine(RootPath, "application.play"), largeSource.ToString());
        var revision = Content("open-workspace", new { }).GetProperty("revision").GetString();
        var schema = CallSchema();
        schema.GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(192 * 1024);
        schema.GetProperty("then").GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(192 * 1024);
        schema.GetProperty("else").GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(200);
        var page = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 192 * 1024 });
        if (!page.GetProperty("available").GetBoolean()) throw new InvalidOperationException(Content("read-workspace", new { expectedRevision = revision, view = "executable-diagnostics" }).GetRawText());
        page.GetProperty("page").GetProperty("byteCount").GetInt32().ShouldEqual(192 * 1024);
        Encoding.UTF8.GetByteCount(page.GetRawText()).ShouldBeLessThan(McpJson.MaximumStructuredResponseBytes);
        Refused(new { expectedRevision = revision, view = "executable-model", limit = (192 * 1024) + 1 }, "'limit'");
        Refused(new { expectedRevision = revision, view = "documents", limit = 201 }, "'limit'");
        Content("read-workspace", new { expectedRevision = revision, view = "documents", limit = 200 }).GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    }

    JsonElement CallSchema()
    {
        using var response = JsonDocument.Parse(Connection.Handle("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""));
        return response.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "read-workspace").GetProperty("inputSchema").Clone();
    }

    JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");

    void Refused(object arguments, string reason)
    {
        var response = Call("read-workspace", arguments);
        if (response.TryGetProperty("error", out var error))
        {
            error.GetProperty("message").GetString()!.ShouldContain(reason);
            return;
        }

        var result = response.GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
        var content = result.GetProperty("structuredContent");
        content.GetProperty("message").GetString()!.ShouldContain(reason);
        content.TryGetProperty("page", out _).ShouldBeFalse();
    }
}
