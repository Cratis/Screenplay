// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler;

public class when_closing_workflow_approvals : given.persona_documents
{
    void Establish()
    {
        Source("persona Manager\n  policy Manager\n  policy Audit\n" + ApprovalSource("Manager", "Audit"));
        _normal.Success.ShouldBeFalse();
        Record();
    }

    void Because() => _result = _compiler.CompileAdmissible("Example", _documents);

    [Fact]
    void should_roll_back_instead_of_rebinding_a_removed_manager_to_a_policy()
    {
        (_result.Compilation.Value?.Model.Application.Workflows.SelectMany(workflow => workflow.Nodes)
            .Where(node => node.Approver == "Manager" && node.ApproverKind == SemanticApproverKind.Policy).ToArray() ?? []).ShouldBeEmpty();
        _result.Compilation.Success.ShouldBeFalse();
        _result.Compilation.Value.ShouldBeNull();
        _result.Compilation.Diagnostics.ShouldEqual(_normal.Diagnostics);
        _result.Exclusions.ShouldBeEmpty();
        _rounds.Count.ShouldEqual(1);
    }
}
