// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.when_opening_a_model_folder_with_a_code_folder_root;

public class and_the_folder_holds_other_files : given.a_code_folder_root
{
    Exception? _failure;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(ProjectPath, "docs"));
        File.WriteAllText(Path.Combine(ProjectPath, "docs", "README.md"), "# Petclinic\n");
    }

    void Because() => _failure = Open(Path.Combine(ProjectPath, "docs"));

    [Fact] void should_refuse_the_root_change() => (_failure as McpFailure)!.FailureKind.ShouldEqual("RootChangeRefused");
    [Fact] void should_keep_the_configured_root() => ReferenceEquals(Workspaces.ReadRoot(), Configured).ShouldBeTrue();
}
