// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_module_named_by_depends_on;

public class and_a_target_cannot_be_proven
{
    [Theory]
    [InlineData("module Consumer\n  depends on Group.Shared\nmodule A\n  feature Group\n    feature Shared\nmodule B\n  feature Group\n    feature Shared\n", "A", "Renamed")]
    [InlineData("module Consumer\n  depends on A.Missing\nmodule A\n", "A", "Renamed")]
    public void should_refuse_ambiguous_and_unresolved_targets(string source, string oldName, string newName)
    {
        var result = with_proven_targets.Rename(source, oldName, newName);
        result.Accepted.ShouldBeFalse();
        result.Conflicts.ShouldNotBeEmpty();
    }
}
