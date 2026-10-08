// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_ModelTest;

public class when_testing_without_mcp_state : given.a_model
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_not_compile_an_unimported_sibling_when_testing_a_file(bool persisted)
    {
        if (persisted)
        {
            Persist(CreateWorkspace());
        }

        File.WriteAllText(Path.Combine(Root, "unimported.play"), "module Broken\n  feature Invalid\n    slice StateChange Invalid\n      event Invalid\n        missingType");
        ModelTest.Run([Path.Combine(Root, "application.play"), "--filter", "M.F.Register.Correct", "--format", "json"], Output, Error).ShouldEqual(0);
        using var report = JsonDocument.Parse(Output.ToString());
        report.RootElement.GetProperty("discovered").GetInt32().ShouldEqual(2);
        report.RootElement.GetProperty("selected").GetInt32().ShouldEqual(1);
        Error.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_ignore_stale_identities_after_hand_edits_and_file_renames(bool singleFile)
    {
        var persisted = CreateWorkspace();
        var previousId = persisted.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Specification && assignment.Address.Parts[^1].Key == "Correct").Id.ToString();
        var renamed = Path.Combine(Root, "renamed.play");
        File.Move(Path.Combine(Root, "application.play"), renamed);
        File.WriteAllText(renamed, File.ReadAllText(renamed).Replace("specification Correct", "specification Renamed", StringComparison.Ordinal));
        var target = singleFile ? renamed : Root;
        string[] arguments = [target, "--filter", "M.F.Register.Renamed", "--format", "json"];
        using var expected = new StringWriter();
        ModelTest.Run(arguments, expected, Error).ShouldEqual(0);

        Persist(persisted);
        var statePath = Path.Combine(Root, ".screenplay", McpState.FileName);
        var state = File.ReadAllText(statePath);
        ModelTest.Run(arguments, Output, Error).ShouldEqual(0);
        Output.ToString().ShouldEqual(expected.ToString());
        File.ReadAllText(statePath).ShouldEqual(state);
        using var report = JsonDocument.Parse(Output.ToString());
        report.RootElement.GetProperty("results")[0].GetProperty("semanticId").GetString().ShouldNotEqual(previousId);
        Error.ToString().ShouldBeEmpty();
    }

    ScreenplayWorkspace CreateWorkspace()
    {
        var name = new DirectoryInfo(Root).Name;
        var identity = ApplicationIdentity.Create(name);
        var document = WorkspaceDocument.Create("durable-document", PortablePlayPath.Parse("application.play"), File.ReadAllBytes(Path.Combine(Root, "application.play")));

        return ScreenplayWorkspace.Create(identity, name, [document], SemanticIdentityCatalog.Empty(identity));
    }

    void Persist(ScreenplayWorkspace workspace)
    {
        Directory.CreateDirectory(Path.Combine(Root, ".screenplay"));
        File.WriteAllBytes(Path.Combine(Root, ".screenplay", McpState.FileName), McpState.Serialize(workspace));
    }
}
