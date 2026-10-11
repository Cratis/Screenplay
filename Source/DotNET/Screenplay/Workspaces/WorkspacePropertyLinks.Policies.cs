// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    IEnumerable<AuthoredDeclaration> PolicyCallers(AuthoredDeclaration policy) => _entries
        .Where(entry => entry.Node is CommandSyntax && EffectivePolicies(entry).Contains(policy))
        .Select(Owner).OfType<AuthoredDeclaration>().Distinct();

    IEnumerable<AuthoredDeclaration> EffectivePolicies(WorkspaceSyntaxEntry entry)
    {
        var command = (CommandSyntax)entry.Node;
        var ancestors = Parents(entry).Reverse().ToArray();
        var module = ancestors.FirstOrDefault(parent => parent.Node is ModuleSyntax);
        if (module is null) return [];
        var effective = DeclarationOwner(module)?.Syntax as ModuleSyntax ?? (ModuleSyntax)module.Node;
        var path = ancestors.Select(parent => parent.Node).OfType<FeatureSyntax>().Select(feature => feature.Name);
        var gates = EffectiveAuthorizationScopes.For(effective, path, command.Authorize).OfType<AuthorizeSyntax>();

        return gates.SelectMany(gate => gate.References())
            .SelectMany(reference => Resolve(reference.Name, ["Policy"], entry));
    }
}
