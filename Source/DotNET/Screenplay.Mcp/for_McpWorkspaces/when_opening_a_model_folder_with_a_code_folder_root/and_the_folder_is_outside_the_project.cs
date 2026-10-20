// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.when_opening_a_model_folder_with_a_code_folder_root;

public class and_the_folder_is_outside_the_project : given.a_code_folder_root
{
    Exception? _failure;

    void Because() => _failure = Open(Path.Combine(FixturePath, "Screenplay"));

    [Fact] void should_refuse_the_root_change() => (_failure as McpFailure)!.FailureKind.ShouldEqual("RootChangeRefused");
    [Fact] void should_keep_the_configured_root() => ReferenceEquals(Workspaces.ReadRoot(), Configured).ShouldBeTrue();
}
