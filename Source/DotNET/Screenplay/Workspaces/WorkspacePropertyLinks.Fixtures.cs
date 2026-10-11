// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    void IndexCaseValues()
    {
        foreach (var entry in _entries.Where(entry => entry.Node is CaseValueExpressionSyntax))
        {
            var parameter = ((CaseValueExpressionSyntax)entry.Node).Parameter;
            var ancestors = Parents(entry).ToArray();
            var mapping = ancestors.FirstOrDefault(parent => parent.Node is PropertyMappingSyntax);
            var step = ancestors.FirstOrDefault(parent => parent.Node is SpecificationCommandSyntax or SpecificationEventSyntax or SpecificationReadModelSyntax or SpecificationQueryResultSyntax or SpecificationQuerySyntax or SpecificationWhenQuerySyntax);
            var table = ancestors.FirstOrDefault(parent => parent.Node is SpecificationSyntax);
            if (mapping is null || step is null || table is null) continue;
            var targets = Members(FixtureOwner(step), ((PropertyMappingSyntax)mapping.Node).Property);
            var role = FixtureRole(step);
            var rows = _entries.Where(row => row.Node is PropertyMappingSyntax value && value.Property == parameter &&
                Parents(row).Any(parent => parent.Node is SpecificationCaseSyntax) && Parents(row).Any(parent => parent.Handle == table.Handle));
            foreach (var row in rows) Add(row, role, targets, parameter, incomplete: targets.Length != 1);
        }
    }
}
