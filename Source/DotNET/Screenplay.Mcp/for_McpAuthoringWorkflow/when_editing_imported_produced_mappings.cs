// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_editing_imported_produced_mappings : given.an_authoring_connection
{
    [Theory]
    [InlineData(false, "PreserveTrivia")]
    [InlineData(true, "PreserveTrivia")]
    [InlineData(false, "CanonicalizeTouchedDocuments")]
    [InlineData(true, "CanonicalizeTouchedDocuments")]
    public void should_preview_only_the_requested_mapping_with_original_placed_handles_and_stable_identities(bool native, string formatting)
    {
        const string slice = "// café oldName\r\nslice StateChange S\r\n  command C\r\n    key String identifier\r\n    oldName String\r\n    newName String\r\n    produces E\r\n      for key\r\n      key = key\r\n      value = oldName  // keep oldName café\r\n  event E\r\n    key String\r\n    value String\r\n";
        var source = native ? "module M\r\n  feature F\r\n" + string.Join("\r\n", slice.Split("\r\n").Select(line => "    " + line)) : slice;
        byte[] bytes = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(source)];
        var root = native ? "module M\r\n  import \"imported.play\"\r\n" : "module M\r\n  feature F\r\n    import \"imported.play\"\r\n";
        var rootPath = Path.Combine(RootPath, "application.play");
        var importedPath = Path.Combine(RootPath, "imported.play");
        File.WriteAllBytes(rootPath, Encoding.UTF8.GetBytes(root));
        File.WriteAllBytes(importedPath, bytes);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var catalogRevision = opened.GetProperty("catalogRevision").GetString();
        var mappings = Result("read-ast", new { expectedRevision = revision, kind = "PropertyMappingSyntax", includeContent = true })
            .GetProperty("page").GetProperty("items");
        var mapping = mappings.EnumerateArray().Single(item => item.GetProperty("node").GetProperty("property").GetString() == "value");
        var original = (PropertyMappingSyntax)SyntaxJson.Deserialize(mapping.GetProperty("node"));
        var replacement = original with { Source = ((PathExpressionSyntax)original.Source) with { Path = "newName" } };
        var proposal = Result("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = catalogRevision,
            validation = "Executable",
            formatting,
            operations = new[] { new { operation = "replace", target = mapping.GetProperty("handle"), expected = mapping.GetProperty("node"), node = SyntaxJson.Serialize(replacement) } }
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Success.ShouldBeTrue();
        candidate.IdentityCatalog.Revision.ToString().ShouldEqual(catalogRevision);
        candidate.Documents.Single(document => document.Path.Value == "application.play").Bytes.ToArray().ShouldEqual(Encoding.UTF8.GetBytes(root));
        var printed = candidate.Documents.Single(document => document.Path.Value == "imported.play");
        if (formatting == "PreserveTrivia")
        {
            printed.Bytes.ToArray().ShouldEqual([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes(source.Replace("value = oldName  //", "value = newName  //", StringComparison.Ordinal))]);
        }
        else
        {
            printed.Text.ShouldContain("value = newName");
            printed.Text.ShouldContain("keep oldName café");
            printed.Text.Contains("module M", StringComparison.Ordinal).ShouldBeFalse();
        }

        var command = candidate.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var newSource = command.Properties.Single(property => property.Name == "newName").Id;
        var target = candidate.Compilation.Value.Model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties.Single(property => property.Name == "value").Id;
        ((SemanticResolvedExpression)command.Produces.Single().Mappings.Single(value => value.TargetProperty == target).Source).Target.ShouldEqual(newSource);
        File.ReadAllBytes(rootPath).ShouldEqual(Encoding.UTF8.GetBytes(root));
        File.ReadAllBytes(importedPath).ShouldEqual(bytes);
    }
}
