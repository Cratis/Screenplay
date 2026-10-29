// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

sealed class AdmissibleSecurity(ApplicationSyntax syntax, AdmissibleSyntax index)
{
    readonly AdmissibleSecuritySyntax _security = new();
    readonly HashSet<string> _policies = new(StringComparer.Ordinal);
    readonly HashSet<string> _reads = new(StringComparer.Ordinal);

    internal bool TryPlan(IReadOnlyList<Diagnostic> errors, AdmissibleExclusions plan)
    {
        _security.VisitApplication(syntax);
        foreach (var error in errors)
        {
            if (!TryAttribute(error, plan)) return false;
        }
        if (syntax.CommandReadMode is not null && !CloseReads(errors, plan)) return false;
        return SafeAfterPruning(index.Remove(plan.Removed));
    }

    static string? DeclaredPolicy(DiagnosticArgument argument)
    {
        var value = argument.Value;
        if (value.ValueKind != JsonValueKind.Object) return null;
        if (!value.TryGetProperty("kind", out var kind)) return null;
        if (kind.GetString() != "policy") return null;
        return value.TryGetProperty("path", out var path) ? string.Join('/', path.EnumerateArray().Select(part => part.GetString())) : string.Empty;
    }

    static bool RemovedOwner(AdmissibleSecuritySyntax.ViewReference reference, AdmissibleExclusions plan) =>
        reference.Slice is { } slice && plan.Removed.Contains(slice);

    bool TryAttribute(Diagnostic error, AdmissibleExclusions plan)
    {
        var policies = PoliciesFor(error).Distinct(StringComparer.Ordinal).ToArray();
        if (policies.Length == 0) return index.Find(error) is { } unit && plan.Add(unit.Syntax, [error], null);
        foreach (var policy in policies)
        {
            if (!ClosePolicy(policy, error, plan)) return false;
        }
        return true;
    }

    bool ClosePolicy(string name, Diagnostic diagnostic, AdmissibleExclusions plan)
    {
        var declarations = syntax.Policies.Where(policy => policy.Name == name).ToArray();
        if (declarations.Length != 1) return false;
        if (_security.Authorizations.Exists(use => !AdmissibleSecuritySyntax.Known(use.Syntax.Requirement))) return false;
        var reason = $"Excluded policy '{name}' and all declarations protected by it.";
        if (!plan.Add(declarations[0], [diagnostic], reason)) return false;
        _policies.Add(name);
        foreach (var use in _security.Authorizations.Where(use => use.Syntax.References().Any(reference => reference.Name == name)))
        {
            if (!CloseAuthorization(use, declarations[0], (diagnostic, reason), plan)) return false;
        }
        return true;
    }

    bool CloseAuthorization(AdmissibleSecuritySyntax.Authorization use, PolicySyntax policy, (Diagnostic Diagnostic, string Reason) evidence, AdmissibleExclusions plan)
    {
        if (use.Slices is null) return false;
        if (use.Syntax.References().Any(reference => syntax.Policies.Count(policy => policy.Name == reference.Name) != 1)) return false;
        foreach (var slice in use.Slices)
        {
            if (!plan.Add(slice, [evidence.Diagnostic], evidence.Reason, forcedBy: [policy])) return false;
        }
        plan.Removed.Add(use.Syntax);
        return true;
    }

    bool CloseReads(IReadOnlyList<Diagnostic> errors, AdmissibleExclusions plan)
    {
        while (true)
        {
            var affected = _security.Reads.Where(read => AffectedRead(read, plan)).Select(read => read.Name)
                .Where(name => !_reads.Contains(name)).Distinct(StringComparer.Ordinal).ToArray();
            if (affected.Length == 0) return true;
            if (!CloseReadBatch(affected, errors, plan)) return false;
        }
    }

    bool CloseReadBatch(string[] names, IReadOnlyList<Diagnostic> errors, AdmissibleExclusions plan)
    {
        if (!_security.CertainViewReferences) return false;
        if (_security.References.Exists(reference => reference.Name.Contains('.', StringComparison.Ordinal))) return false;
        return names.All(name => CloseRead(name, errors, plan));
    }

    bool AffectedRead(AdmissibleSecuritySyntax.ViewReference read, AdmissibleExclusions plan) =>
        RemovedOwner(read, plan) || _security.Views.Exists(view => view.Name == read.Name && RemovedOwner(view, plan));

    bool CloseRead(string name, IReadOnlyList<Diagnostic> errors, AdmissibleExclusions plan)
    {
        var views = _security.Views.Where(view => view.Name == name).ToArray();
        if (views.Length != 1 || views[0].Slice is null) return false;
        var reason = $"Excluded protected read '{name}' and every reader of its read model.";
        var references = _security.References.Where(reference => reference.Name == name).Concat(views).ToArray();
        var triggers = ReadTriggers(name, plan);
        foreach (var reference in references)
        {
            if (reference.Slice is null || !plan.Add(reference.Slice, errors, reason, forcedBy: triggers)) return false;
        }
        _reads.Add(name);
        return true;
    }

    SliceSyntax[] ReadTriggers(string name, AdmissibleExclusions plan) =>
        [.. _security.Reads.Concat(_security.Views).Where(reference => reference.Name == name)
            .Where(reference => RemovedOwner(reference, plan)).Select(reference => reference.Slice!).Distinct()];

    IEnumerable<string> PoliciesFor(Diagnostic diagnostic)
    {
        if (index.Find(diagnostic)?.Syntax is PolicySyntax policy) yield return policy.Name;
        foreach (var reference in _security.Authorizations.Where(use => use.Owns(diagnostic.Location)).SelectMany(use => use.Syntax.References()))
        {
            yield return reference.Name;
        }
        if (diagnostic.Arguments.TryGetValue("policy", out var named))
        {
            yield return named.Value.ValueKind == JsonValueKind.String ? named.Value.GetString()! : DeclaredPolicy(named) ?? string.Empty;
        }
        foreach (var name in diagnostic.Arguments.Values.Select(DeclaredPolicy).OfType<string>()) yield return name;
    }

    bool SafeAfterPruning(ApplicationSyntax pruned)
    {
        var remaining = new AdmissibleSecuritySyntax();
        remaining.VisitApplication(pruned);
        if (RetainsExcludedPolicy(pruned, remaining)) return false;
        return !remaining.References.Concat(remaining.Views).Any(reference => _reads.Contains(reference.Name));
    }

    bool RetainsExcludedPolicy(ApplicationSyntax pruned, AdmissibleSecuritySyntax remaining) =>
        (_policies.Count > 0 && remaining.Authorizations.Exists(ReferencesExcludedPolicy)) ||
        (pruned.Personas ?? []).Any(persona => persona.Policies.Any(_policies.Contains));

    bool ReferencesExcludedPolicy(AdmissibleSecuritySyntax.Authorization use) =>
        !AdmissibleSecuritySyntax.Known(use.Syntax.Requirement) || use.Syntax.References().Any(reference => _policies.Contains(reference.Name));
}
