// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Dependencies;

internal enum DependencyDeclarationStatus
{
    Used,
    Provisional,
    Unused,
    Invalid
}

internal enum DependencyCoverageStatus
{
    Declared,
    Provisional,
    Undeclared
}

internal sealed record DependencyDeclaration(DependsOnSyntax Syntax, string? Resolved, DependencyDeclarationStatus Status);
internal sealed record DeclaredDependencyEdge(DependencyEvidence Evidence, DependencyCoverageStatus Status, IReadOnlyList<DependsOnSyntax> CoveringDeclarations);
internal sealed record CheckedDependencyContainer(DependencyNode Container, SourceLocation Location, IReadOnlyList<DependencyDeclaration> Declarations, IReadOnlyList<DeclaredDependencyEdge> Edges);
internal sealed record DeclaredDependencyReport(IReadOnlyList<CheckedDependencyContainer> Containers, IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// Checks each opted-in container's own inventory against explicit reference evidence, never executable identity.
/// </summary>
internal static class DeclaredDependencies
{
    static readonly string[] _countedKinds = ["usesFactsFrom", "reactsTo", "decidesFrom", "asks", "shows"];

    internal static DeclaredDependencyReport For(ApplicationSyntax application)
    {
        var owners = new Dictionary<string, (SourceLocation Location, List<DependsOnSyntax> Dependencies)>(StringComparer.Ordinal);
        void Inventory(string[] scope, SourceLocation location, IEnumerable<DependsOnSyntax> dependencies)
        {
            var address = string.Join('.', scope);
            if (!owners.TryGetValue(address, out var owner)) owners[address] = owner = (location, []);
            owner.Dependencies.AddRange(dependencies);
        }
        void Feature(FeatureSyntax feature, string[] parent)
        {
            string[] scope = [.. parent, feature.Name];
            Inventory(scope, feature.Location, feature.DependsOn);
            foreach (var child in feature.Features) Feature(child, scope);
        }
        foreach (var module in application.Modules)
        {
            Inventory([module.Name], module.Location, module.DependsOn);
            foreach (var feature in module.Features) Feature(feature, [module.Name]);
        }
        if (!owners.Values.Any(owner => owner.Dependencies.Count > 0)) return new([], []);

        var graph = DependencyGraph.For(application);
        var containers = graph.Nodes.Where(node => node.Kind == "module" || node.Kind == "feature").ToArray();
        Declaration[] targets = [.. containers.Select(node => new Declaration(node.Scope[^1], new([.. node.Scope.Take(node.Scope.Count - 1)])))];
        var checkedContainers = new List<CheckedDependencyContainer>();
        var findings = new List<Diagnostic>();
        foreach (var container in containers.Where(node => owners[node.Address].Dependencies.Count > 0))
        {
            var owner = owners[container.Address];
            var declarations = owner.Dependencies.Select(syntax =>
            {
                var resolution = DeclaredDependencyTargets.Resolve(syntax.Target, new(container.Scope), targets);
                var target = resolution.Resolved is { } resolved ? string.Join('.', resolved.Scope.Segments.Append(resolved.Name)) : null;
                var valid = target is not null && !Contains(container.Address, target) && !Contains(target, container.Address);
                return new DependencyDeclaration(syntax, target, valid ? DependencyDeclarationStatus.Unused : DependencyDeclarationStatus.Invalid);
            }).ToArray();
            var edges = new List<DeclaredDependencyEdge>();
            foreach (var evidence in Ordered(graph.Edges.SelectMany(edge => edge.Evidence).Where(item => _countedKinds.Contains(item.Kind, StringComparer.Ordinal) && Contains(container.Address, item.Consumer.Address))))
            {
                // A producer in this container or its proper ancestor cannot be named as a dependency.
                var candidates = new[] { evidence.Producer }.Concat(evidence.Alternatives).Where(producer =>
                {
                    var producerContainer = string.Join('.', producer.Scope.Take(producer.Scope.Count - 1));
                    return !Contains(container.Address, producer.Address) && !Contains(producerContainer, container.Address);
                }).ToArray();
                if (candidates.Length == 0) continue;
                var covering = declarations.Select((declaration, index) => (Declaration: declaration, Index: index))
                    .Where(item => item.Declaration.Status != DependencyDeclarationStatus.Invalid && candidates.Any(candidate => Contains(item.Declaration.Resolved!, candidate.Address))).ToArray();
                foreach (var item in covering)
                {
                    var status = evidence.Ambiguous && declarations[item.Index].Status != DependencyDeclarationStatus.Used ? DependencyDeclarationStatus.Provisional : DependencyDeclarationStatus.Used;
                    declarations[item.Index] = declarations[item.Index] with { Status = status };
                }
                var coverage = covering.Length > 0 ? DependencyCoverageStatus.Declared : DependencyCoverageStatus.Undeclared;
                edges.Add(new(evidence, evidence.Ambiguous ? DependencyCoverageStatus.Provisional : coverage, [.. covering.Select(item => item.Declaration.Syntax)]));
            }
            foreach (var bucket in edges.Where(edge => edge.Status == DependencyCoverageStatus.Undeclared).GroupBy(edge => edge.Evidence.Producer.Scope[0], StringComparer.Ordinal))
            {
                var evidence = bucket.Select(edge => edge.Evidence).ToArray();
                var prefix = evidence[0].Producer.Scope.Take(evidence[0].Producer.Scope.Count - 1).ToArray();
                foreach (var item in evidence.Skip(1)) prefix = [.. prefix.TakeWhile((segment, index) => index < item.Producer.Scope.Count - 1 && segment == item.Producer.Scope[index])];
                var target = string.Join('.', prefix);
                var suggestedTargets = Contains(container.Address, target) || Contains(target, container.Address)
                    ? evidence.Select(item => Enumerable.Range(1, item.Producer.Scope.Count - 1).Select(length => string.Join('.', item.Producer.Scope.Take(length)))
                        .First(candidate => !Contains(container.Address, candidate) && !Contains(candidate, container.Address))).Distinct(StringComparer.Ordinal).ToArray()
                    : [target];
                var details = string.Join("; ", evidence.Select(item => $"{item.Kind} {item.Role} '{item.Name}' at {item.Location.Path ?? string.Empty}:{item.Location.Line}:{item.Location.Column}"));
                findings.Add(new(DiagnosticSeverity.Warning, DiagnosticCodes.UndeclaredDependency, $"Container '{container.Address}' depends on '{string.Join("', '", suggestedTargets)}' without declaring {(suggestedTargets.Length == 1 ? "it" : "them")} - evidence: {details}", owner.Location));
            }
            foreach (var declaration in declarations)
            {
                if (declaration.Status == DependencyDeclarationStatus.Unused) findings.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.UnusedDependencyDeclaration, $"Dependency '{declaration.Syntax.Target}' on '{container.Address}' is not used by any counted explicit reference", declaration.Syntax.Location));
                if (declaration.Status == DependencyDeclarationStatus.Invalid) continue;
                var reciprocal = owners[declaration.Resolved!].Dependencies.Exists(dependency =>
                {
                    var resolution = DeclaredDependencyTargets.Resolve(dependency.Target, new(declaration.Resolved!.Split('.')), targets);
                    return resolution.Resolved is { } resolved && string.Join('.', resolved.Scope.Segments.Append(resolved.Name)) == container.Address;
                });
                if (reciprocal) findings.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.MutualDependencyDeclarations, $"Containers '{container.Address}' and '{declaration.Resolved}' declare each other", declaration.Syntax.Location));
            }
            checkedContainers.Add(new(container, owner.Location, declarations, edges));
        }

        return new(checkedContainers, findings);
    }

    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        foreach (var diagnostic in For(application).Diagnostics) context.Add(diagnostic);
    }

    static bool Contains(string container, string address) => address == container || address.StartsWith(container + ".", StringComparison.Ordinal);
    static IOrderedEnumerable<DependencyEvidence> Ordered(IEnumerable<DependencyEvidence> evidence) => evidence.OrderBy(item => item.Location.Path, StringComparer.Ordinal)
        .ThenBy(item => item.Location.Line).ThenBy(item => item.Location.Column).ThenBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.Consumer.Address, StringComparer.Ordinal).ThenBy(item => item.Producer.Address, StringComparer.Ordinal).ThenBy(item => item.Role, StringComparer.Ordinal).ThenBy(item => item.Name, StringComparer.Ordinal);
}
