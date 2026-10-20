// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.when_comparing_a_model_folder_with_a_code_folder_root;

public class and_the_folder_is_inside_the_root : given.a_code_folder_root
{
    Exception? _failure;

    // An empty folder would be readable if admitted, so a refusal comes from the admission rule.
    void Establish() => Directory.CreateDirectory(Path.Combine(CodePath, "model"));

    void Because() => _failure = Catch.Exception(() => Workspaces.ReadComparisonPath(Path.Combine(CodePath, "model")));

    [Fact] void should_refuse_it() => _failure.ShouldBeOfExactType<McpFailure>();
}
