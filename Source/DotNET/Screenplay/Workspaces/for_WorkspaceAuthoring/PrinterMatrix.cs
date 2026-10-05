// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

/// <summary>
/// Runs hint-free replacement scenarios and prints their accepted documents, so the printer correspondence can be
/// compared with the output of the merge-base implementation.
/// </summary>
static class PrinterMatrix
{
    const string Head = "module M\n  feature F\n    slice StateChange S\n      command C // command\n        label String\n";

    public const string TwoRules = "        validate // block A\n" +
        "          label rule Check // first\n            file Same.cs // file first\n" +
        "          label rule Check // second\n            file Same.cs // file second\n";

    public const string TwoBlocks = "        validate // block one\n" +
        "          label rule Check // one rule\n            file Same.cs // one file\n" +
        "        validate // block two\n" +
        "          label rule Check // two rule\n            file Same.cs // two file\n";

    public static readonly string[] RuleMutations = ["unchanged", "rename-first", "rename-second", "insert-third", "delete-first", "delete-second"];
    public static readonly string[] BlockMutations = ["unchanged", "append-third", "delete-first", "delete-second", "rename-first-rule"];

    public static IEnumerable<(string Name, string Output)> Run()
    {
        foreach (var formatting in new[] { WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments, WorkspaceAuthoringFormatting.PreserveTrivia })
        {
            foreach (var mutation in RuleMutations)
            {
                yield return ($"rules/{formatting}/{mutation}", Propose(TwoRules, block: true, mutation, formatting));
            }

            foreach (var mutation in BlockMutations)
            {
                yield return ($"blocks/{formatting}/{mutation}", Propose(TwoBlocks, block: false, mutation, formatting));
            }
        }
    }

    public static string Propose(string body, bool block, string mutation, WorkspaceAuthoringFormatting formatting)
    {
        var workspace = ScreenplayWorkspace.Create("M", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Head + body))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("M")));
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.First(entry => block ? entry.Node is DeclarativeValidateSyntax : entry.Node is CommandSyntax);
        var json = JsonNode.Parse(SyntaxJson.Serialize(entry.Node).GetRawText())!;
        Mutate(json, block, mutation);
        var node = SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(json));
        var result = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = formatting,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, node)]
        });
        if (!result.Accepted)
        {
            return "REFUSED";
        }

        return result.WritePlan!.Entries.Length == 0 ? "NOOP" : result.WritePlan.Entries.Single().After!.Text;
    }

    static void Mutate(JsonNode json, bool block, string mutation)
    {
        var items = block ? (JsonArray)json["rules"]! : (JsonArray)json["validations"]!;
        switch (mutation)
        {
            case "rename-first":
                Rename(items[0]!);
                break;
            case "rename-second":
                Rename(items[1]!);
                break;
            case "rename-first-rule":
                Rename(items[0]!["rules"]![0]!);
                break;
            case "insert-third":
            case "append-third":
                items.Add(Renamed(items[0]!));
                break;
            case "delete-first":
                items.RemoveAt(0);
                break;
            case "delete-second":
                items.RemoveAt(1);
                break;
        }
    }

    static JsonNode Renamed(JsonNode source)
    {
        var copy = JsonNode.Parse(source.ToJsonString())!;
        Rename(copy);
        return copy;
    }

    static void Rename(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            if (obj["kind"]?.GetValue<string>() == "ValidationRuleSyntax")
            {
                obj["value"]!["path"] = JsonValue.Create("Renamed");
            }

            foreach (var child in obj.Select(pair => pair.Value).Where(child => child is not null).ToArray())
            {
                Rename(child!);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array.Where(child => child is not null).ToArray())
            {
                Rename(child!);
            }
        }
    }
}
