// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceTextRuleRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        var subjects = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location && entry.Node is ValidationRuleSyntax).ToArray();
        if (subjects.Length != 1 || subjects[0].Node is not ValidationRuleSyntax { Rule: ValidationRuleKind.Matches, Value: LiteralExpressionSyntax { Value: string pattern } } rule) return [];
        if (PortableMatchPattern.Check(pattern).Replacement is not { } replacement) return [];
        var kind = replacement.Kind switch
        {
            SemanticValidationRuleKind.Equal => ValidationRuleKind.Equal,
            SemanticValidationRuleKind.NotEqual => ValidationRuleKind.NotEqual,
            SemanticValidationRuleKind.NotContains => ValidationRuleKind.NotContains,
            _ => ValidationRuleKind.EndsWith
        };
        var changed = rule with { Rule = kind, Value = new LiteralExpressionSyntax(replacement.Value, rule.Value.Location) };
        var subject = subjects[0];
        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(subject.Handle, rule, changed)])
        {
            Title = "Replace the deprecated Studio pattern with an ordinal text rule"
        };

        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }
}
