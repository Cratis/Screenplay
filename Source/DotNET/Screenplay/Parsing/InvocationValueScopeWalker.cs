// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reports <c>$result</c>, <c>$outcome</c>, <c>$failure</c> and <c>$trigger</c> where no matching branch or workflow node is in scope.
/// </summary>
/// <param name="_context">The <see cref="ParserContext"/> to report to.</param>
internal sealed class InvocationValueScopeWalker(ParserContext _context) : ScreenplaySyntaxWalker
{
    string? _branch;
    bool _inWorkflow;

    public override void VisitInvocationBranch(InvocationBranchSyntax syntax)
    {
        var previous = _branch;
        _branch = syntax.Selector;
        base.VisitInvocationBranch(syntax);
        _branch = previous;
    }

    public override void VisitWorkflowNode(WorkflowNodeSyntax syntax)
    {
        var previous = _inWorkflow;
        _inWorkflow = true;
        base.VisitWorkflowNode(syntax);
        _inWorkflow = previous;
    }

    public override void VisitInvocationValueExpression(InvocationValueExpressionSyntax syntax)
    {
        base.VisitInvocationValueExpression(syntax);
        var inScope = syntax.Source switch
        {
            "result" => _branch == "returned",
            "failure" => _branch == "failed",
            "outcome" => _branch is not null and not ("returned" or "failed"),
            _ => _inWorkflow
        };
        if (inScope) return;
        var where = syntax.Source switch
        {
            "result" => "only in an 'on returned' branch",
            "failure" => "only in an 'on failed' branch",
            "outcome" => "only in an 'on <Outcome>' branch",
            _ => "only in a workflow node"
        };
        _context.Error(DiagnosticCodes.InvocationValueOutsideBranch, $"'${syntax.Source}' is available {where}.", syntax.Location);
    }
}
