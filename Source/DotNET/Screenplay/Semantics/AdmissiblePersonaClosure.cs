// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissiblePersonaClosure(ApplicationSyntax syntax)
{
    readonly HashSet<string> _removed = new(StringComparer.Ordinal);

    internal bool Close(PolicySyntax policy, Diagnostic diagnostic, AdmissibleExclusions plan, AdmissibleSecuritySyntax security)
    {
        var personas = (syntax.Personas ?? []).Where(persona => persona.Policies.Contains(policy.Name, StringComparer.Ordinal)).ToArray();
        if (personas.Any(persona => syntax.Personas!.Count(candidate => candidate.Name == persona.Name) != 1)) return false;
        var names = personas.Select(persona => persona.Name).ToHashSet(StringComparer.Ordinal);
        var invitations = (syntax.Onboarding?.Invitations ?? []).Where(invitation => invitation.By.Concat(invitation.Grant).Any(names.Contains)).ToArray();
        var removals = personas.Cast<SyntaxNode>().Concat(invitations).OrderBy(node => node.Location.Path, StringComparer.Ordinal)
            .ThenBy(node => node.Location.Line).ThenBy(node => node.Location.Column).Select(Describe);
        var reason = $"Excluded policy '{policy.Name}' and all declarations protected by it.";
        if (personas.Length > 0) reason += " Removed whole " + string.Join("; ", removals) + ".";
        if (!plan.Add(policy, [diagnostic], reason)) return false;
        foreach (var caller in security.PersonaCallers.Where(caller => names.Contains(caller.Persona)))
        {
            if (!plan.Add(caller.Owner, [diagnostic], reason, forcedBy: [policy])) return false;
        }
        plan.Removed.UnionWith(personas);
        plan.Removed.UnionWith(invitations);
        _removed.UnionWith(names);
        return true;
    }

    internal bool IsClosed(ApplicationSyntax pruned, AdmissibleSecuritySyntax remaining)
    {
        var personas = (pruned.Personas ?? []).ToArray();
        if (personas.Any(persona => _removed.Contains(persona.Name))) return false;
        var references = remaining.PersonaCallers.Select(caller => caller.Persona)
            .Concat((pruned.Onboarding?.Invitations ?? []).SelectMany(invitation => invitation.By.Concat(invitation.Grant)));
        return references.All(name => !_removed.Contains(name) && personas.Count(persona => persona.Name == name) == 1);
    }

    static string Describe(SyntaxNode node) => node switch
    {
        PersonaSyntax persona => $"persona '{persona.Name}' at {At(persona.Location)}",
        OnboardingInvitationSyntax invitation => $"{invitation.Kind} invitation at {At(invitation.Location)}",
        _ => throw new InvalidOperationException("Unknown persona closure node.")
    };

    static string At(SourceLocation location) => $"{location.Path ?? "<source>"}:{location.Line}:{location.Column}";
}
