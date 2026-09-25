// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_exporting_the_executable_model.given;

public class a_paged_attachment : an_export
{
    protected string Attachment = null!;
    protected string Revision = null!;
    protected string Model = null!;
    protected string Manifest = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("concept ProjectName : String", "concept ProjectName : String\n  validate\n    rule Check\n      file Handler.cs", StringComparison.Ordinal).Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          return true;\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal));
        Attachment = Path.Combine(RootPath, "Handler.cs");
        File.WriteAllText(Attachment, "first");
        Initialize();
        Revision = Content("open-workspace", new { }).GetProperty("revision").GetString()!;
        var first = Content("read-workspace", new { expectedRevision = Revision, view = "executable-model", limit = 1 });
        if (!first.GetProperty("available").GetBoolean()) throw new InvalidOperationException(Content("read-workspace", new { expectedRevision = Revision, view = "executable-diagnostics" }).GetRawText());
        Model = first.GetProperty("modelRevision").GetString()!;
        Manifest = first.GetProperty("attachmentManifestRevision").GetString()!;
        var requirements = Content("read-workspace", new { expectedRevision = Revision, view = "implementation-requirements", limit = 1 });
        if (requirements.GetProperty("page").GetProperty("nextOffset").GetInt32() != 1 || requirements.GetProperty("attachmentManifestRevision").GetString() != Manifest) throw new InvalidOperationException("Expected two requirements sharing the model attachment manifest.");
    }
}
