// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.given;

public class a_document_with_other_mapping_sites : Specification
{
    protected const string Source =
        """
        // Keep café 🍰 and the BOM
        module Shop
          feature Orders
            slice Translate Orders
              event Recorded
                note String
                other String
              command Process
                note String
              reaction React
                when Recorded
                  invokes Process
                    note  =  "old"  // reaction
              capture Capture
                append Recorded
                  when note
                    note  =  "old"  // capture
              readmodel View
                note String
              query Find => View
                by note String from "old"  // query
              projection Projection => View
                from Recorded
                  note  =  "old"  // projection
        seed
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            Recorded
              note  =  "old"  // seed
        """;

    protected WorkspaceAuthoringResult Result = null!;
    protected ScreenplayWorkspace Workspace = null!;

    void Establish()
    {
        var bytes = Bytes(Source);
        var document = WorkspaceDocument.Create("orders", PortablePlayPath.Parse("Shop/Orders.play"), bytes);
        Workspace = ScreenplayWorkspace.Create("Shop", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    }

    protected static byte[] Bytes(string text) => [.. Encoding.UTF8.Preamble, .. Encoding.UTF8.GetBytes(text.Replace("\n", "\r\n", StringComparison.Ordinal))];

    protected void ReplaceLiteral(string site)
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is LiteralExpressionSyntax literal &&
            literal.Value is string value && value == "old" && entry.Location.Line == SiteLine(site));
        Result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, ((LiteralExpressionSyntax)entry.Node) with { Value = "new" })]
        });
    }

    protected void ReplaceMapping(string site)
    {
        var entry = WorkspaceSyntaxIndex.Create(Workspace).Entries.Single(entry => entry.Node is PropertyMappingSyntax mapping &&
            mapping.Property == "note" && entry.Location.Line == SiteLine(site));
        Result = Workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, ((PropertyMappingSyntax)entry.Node) with { Property = "other" })]
        });
    }

    protected void AssertExact(string site, bool mapping = false)
    {
        Result.Accepted.ShouldBeTrue();
        var previous = site == "query" ? "by note String from \"old\"  // query" : $"note  =  \"old\"  // {site}";
        var next = mapping ? $"other = \"old\"  // {site}" : previous.Replace("\"old\"", "\"new\"", StringComparison.Ordinal);
        Result.Workspace!.Documents.Single().Bytes.AsSpan().SequenceEqual(Bytes(Source.Replace(previous, next, StringComparison.Ordinal))).ShouldBeTrue();
        Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization).ShouldBeFalse();
    }

    static int SiteLine(string site) => site switch
    {
        "reaction" => 13,
        "capture" => 17,
        "query" => 21,
        "projection" => 24,
        "seed" => 28,
        _ => 0
    };
}
