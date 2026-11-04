// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAttachmentContents.given;

public class an_attachment_request : Specification
{
    internal string RootPath = null!;
    internal McpRoot Root = null!;
    internal McpAttachmentContents.Request Request = null!;
    internal ImmutableArray<WorkspaceDocument> Documents;

    void Establish()
    {
        RootPath = SpecFixtureDirectory.Create("attachment-request");
        Root = new(RootPath);
        Request = new();
        Documents = [Document("application.play", Source("Handler.cs"))];
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "first");
    }

    internal static WorkspaceDocument Document(string path, string text) =>
        WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(text));

    internal static string Source(string attachment) =>
        $"module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file {attachment}";

    void Destroy() => SpecFixtureDirectory.Delete(RootPath);
}
