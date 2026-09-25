// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model.given;

public class an_export : for_McpConnection.given.a_connection
{
    protected JsonElement Content(string tool, object arguments) => Call(tool, arguments).GetProperty("result").GetProperty("structuredContent");

    protected JsonElement ReadSchema()
    {
        using var response = JsonDocument.Parse(Connection.Handle("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}"""));
        return response.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "read-workspace").GetProperty("inputSchema").Clone();
    }

    protected string Refusal(object arguments)
    {
        var response = Call("read-workspace", arguments);
        if (response.TryGetProperty("error", out var error)) return error.GetProperty("message").GetString()!;
        var result = response.GetProperty("result");
        if (!result.GetProperty("isError").GetBoolean()) return "No refusal";
        var content = result.GetProperty("structuredContent");
        if (content.TryGetProperty("page", out _)) return "Refusal contained a page";
        return content.GetProperty("message").GetString()!;
    }

    protected void SetupVersion(int version)
    {
        var source = version switch
        {
            1 => "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      command RegisterProject",
            2 => Source + "\n      specification RegistersProject\n        when RegisterProject\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"\n        then ProjectRegistered\n          for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          projectId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"\n          name = \"Screenplay\"",
            3 => Source.Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          return true;\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal),
            4 => "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n      event Registered generation 2\n        current String\n",
            _ => throw new ArgumentOutOfRangeException(nameof(version))
        };
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
    }

    protected (JsonElement First, byte[] Bytes, bool Aligned) ExportPages()
    {
        var revision = Content("open-workspace", new { applicationName = "Projects" }).GetProperty("revision").GetString();
        var first = Content("read-workspace", new { expectedRevision = revision, view = "executable-model", limit = 17 });
        var modelRevision = first.GetProperty("modelRevision").GetString();
        var manifestRevision = first.GetProperty("attachmentManifestRevision").GetString();
        using var bytes = new MemoryStream();
        var page = first.GetProperty("page");
        var aligned = true;
        while (true)
        {
            var chunk = page.GetProperty("bytesBase64").GetBytesFromBase64();
            aligned &= chunk.Length == page.GetProperty("byteCount").GetInt32() && bytes.Length == page.GetProperty("offset").GetInt32();
            bytes.Write(chunk);
            if (page.GetProperty("nextOffset").ValueKind == JsonValueKind.Null) break;
            page = Content("read-workspace", new
            {
                expectedRevision = revision, view = "executable-model",
                offset = page.GetProperty("nextOffset").GetInt32(), limit = 17,
                expectedModelRevision = modelRevision, expectedAttachmentManifestRevision = manifestRevision
            }).GetProperty("page");
        }

        return (first, bytes.ToArray(), aligned);
    }

    protected bool StrictRoundTrip(byte[] bytes)
    {
        var model = SemanticModelSerializer.Deserialize(bytes);
        return bytes.SequenceEqual(SemanticModelSerializer.Serialize(model)) &&
            bytes.SequenceEqual(SemanticModelSerializer.Serialize(Workspace().Compilation.Value!.Model));
    }
}
