// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_module_named_by_depends_on;

public class and_the_rename_would_capture_a_target : Specification
{
    WorkspaceAuthoringResult _result;

    void Because() => _result = with_proven_targets.Rename("module Runs\nmodule Payroll\n  feature Handover\n    depends on Runs\n  feature Execution\n", "Execution", "Runs");

    [Fact] void should_refuse_a_new_sibling_shadowing_the_original_module() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_binding_change() => string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)).ShouldContain("reference");
}
