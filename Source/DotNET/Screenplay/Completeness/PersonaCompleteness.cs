// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Completeness;

static class PersonaCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ApplicationSyntax application)
    {
        var personas = (application.Personas ?? []).Select(persona => (Persona: persona, Result: PersonaCallers.Synthesize(persona, application))).ToArray();
        if (personas.Length == 0) yield break;
        var gates = application.Modules.SelectMany(module => Gates(module.Features, module.Authorize is null ? [] : [module.Authorize])).ToArray();
        var used = gates.SelectMany(gate => gate.Gates.SelectMany(authorize => References(authorize.Requirement))).ToHashSet(StringComparer.Ordinal);
        var callers = personas.Select(persona => persona.Result.Caller).ToArray();
        foreach (var (persona, result) in personas)
        {
            foreach (var diagnostic in PersonaFindings(persona, result, used)) yield return diagnostic;
        }
        foreach (var gate in gates.Where(gate => gate.Kind != "Screen" && GateDenied(gate, callers, application)))
        {
            yield return Diagnostic.Warning(DiagnosticCodes.GateWithoutPersona, $"{gate.Kind} '{gate.Name}' is definitely denied to every declared persona's synthesized caller.", gate.Node.Location);
        }

        foreach (var diagnostic in ScreenFindings(application, gates, callers)) yield return diagnostic;
    }

    static IEnumerable<Diagnostic> PersonaFindings(PersonaSyntax persona, PersonaCallerResult result, IReadOnlySet<string> used)
    {
        if (!persona.Policies.Any(used.Contains))
        {
            yield return Diagnostic.Warning(DiagnosticCodes.PersonaWithoutGate, $"Persona '{persona.Name}' has no policy used by a command, query, or inherited screen gate.", persona.Location);
        }
        foreach (var ambiguity in result.Ambiguities)
        {
            yield return new(
                DiagnosticSeverity.Information,
                DiagnosticCodes.AmbiguousPersonaCaller,
                $"Persona '{persona.Name}' policy '{ambiguity.Policy}' has an unchosen buildable alternative '{ScreenplaySyntaxText.PolicyCondition(ambiguity.Alternative)}'; add a policy that pins it.",
                ambiguity.Location);
        }
    }

    static bool GateDenied(Gate gate, IEnumerable<SpecificationCallerSyntax?> callers, ApplicationSyntax application) =>
        gate.Gates.Count > 0 && callers.All(caller => caller is not null && Evaluate(gate.Gates, caller, application) == false);

    static IEnumerable<Diagnostic> ScreenFindings(ApplicationSyntax application, IReadOnlyList<Gate> gates, IReadOnlyList<SpecificationCallerSyntax?> callers)
    {
        var byNode = gates.ToDictionary(gate => gate.Node, gate => gate.Gates, (IEqualityComparer<SyntaxNode>)ReferenceEqualityComparer.Instance);
        var declarations = new ConsistencyDeclarations(application, [.. ScreenplayValidator.ScopedSlices(application)]);
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var screen in slice.Screens)
            {
                var bindings = Bindings(screen, scope, declarations, byNode);
                if (bindings.Length > 0 && callers.All(caller => caller is not null && ScreenDenied(bindings, byNode[screen], caller, application)))
                {
                    yield return Diagnostic.Warning(DiagnosticCodes.ScreenWithoutPersona, $"Screen '{screen.Name}' offers nothing to any declared persona: every command and query it binds is definitely denied to every synthesized caller.", screen.Location);
                }
            }
        }
    }

    static IReadOnlyList<AuthorizeSyntax>?[] Bindings(ScreenSyntax screen, DeclarationScope scope, ConsistencyDeclarations declarations, Dictionary<SyntaxNode, IReadOnlyList<AuthorizeSyntax>> gates)
    {
        var collector = new SliceReferences();
        collector.VisitScreen(screen);
        return [.. collector.References.Where(reference => reference.TargetKind == "Command" || reference.TargetKind == "Query").Select(reference =>
        {
            SyntaxNode? target = reference.TargetKind == "Command"
                ? declarations.Resolve(reference.Name, scope, slice => slice.Commands, command => command.Name)?.Node
                : declarations.Resolve(reference.Name, scope, slice => slice.Queries, query => query.Name)?.Node;
            return target is null ? null : gates[target];
        })];
    }

    static bool ScreenDenied(IEnumerable<IReadOnlyList<AuthorizeSyntax>?> bindings, IReadOnlyList<AuthorizeSyntax> inherited, SpecificationCallerSyntax caller, ApplicationSyntax application) =>
        Evaluate(inherited, caller, application) == false || bindings.All(binding => binding is not null && Evaluate([.. inherited, .. binding], caller, application) == false);

    static IEnumerable<Gate> Gates(IEnumerable<FeatureSyntax> features, IReadOnlyList<AuthorizeSyntax> inherited)
    {
        foreach (var feature in features)
        {
            var gates = feature.Authorize is null ? inherited : [.. inherited, feature.Authorize];
            foreach (var nested in Gates(feature.Features, gates)) yield return nested;
            foreach (var gate in feature.Slices.SelectMany(slice => SliceGates(slice, gates))) yield return gate;
        }
    }

    static IEnumerable<Gate> SliceGates(SliceSyntax slice, IReadOnlyList<AuthorizeSyntax> inherited)
    {
        foreach (var command in slice.Commands) yield return new("Command", command.Name, command, command.Authorize is null ? inherited : [.. inherited, command.Authorize]);
        foreach (var query in slice.Queries) yield return new("Query", query.Name, query, query.Authorize is null ? inherited : [.. inherited, query.Authorize]);

        // Screens inherit the containing module and feature gates; they have no own authorize directive.
        foreach (var screen in slice.Screens) yield return new("Screen", screen.Name, screen, inherited);
    }

    static IEnumerable<string> References(PolicyRequirementSyntax requirement) => requirement switch
    {
        PolicyReferenceSyntax reference => [reference.Name],
        LogicalPolicyRequirementSyntax logical => References(logical.Left).Concat(References(logical.Right)),
        _ => []
    };

    static bool? Evaluate(IReadOnlyList<AuthorizeSyntax> gates, SpecificationCallerSyntax caller, ApplicationSyntax application) =>
        gates.Aggregate((bool?)true, (truth, gate) => And(truth, Requirement(gate.Requirement, caller, application)));

    static bool? Requirement(PolicyRequirementSyntax requirement, SpecificationCallerSyntax caller, ApplicationSyntax application) => requirement switch
    {
        PolicyReferenceSyntax reference => application.Policies.FirstOrDefault(policy => policy.Name == reference.Name) is { Condition: { } condition, Code: null, File: null } ? Condition(condition, caller) : null,
        LogicalPolicyRequirementSyntax logical => Combine(Requirement(logical.Left, caller, application), logical.Operator, Requirement(logical.Right, caller, application)),
        _ => null
    };

    static bool? Condition(PolicyConditionSyntax condition, SpecificationCallerSyntax caller) => condition switch
    {
        AuthenticatedConditionSyntax => caller.Authenticated,
        AnonymousConditionSyntax => true,
        RoleConditionSyntax role => caller.Roles.Contains(role.Role, StringComparer.Ordinal),
        ClaimConditionSyntax claim => Claim(claim, caller),
        NotPolicyConditionSyntax not => !Condition(not.Operand, caller),
        LogicalPolicyConditionSyntax logical => Combine(Condition(logical.Left, caller), logical.Operator, Condition(logical.Right, caller)),
        _ => null
    };

    static bool? Claim(ClaimConditionSyntax claim, SpecificationCallerSyntax caller) => claim switch
    {
        { MatchesSubject: false, Matches: LiteralExpressionSyntax { Value: string value } } => caller.Claims.Any(atom => string.Equals(atom.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) && atom.Value == value),
        _ => null
    };

    static bool? Combine(bool? left, LogicalOperator op, bool? right) => op == LogicalOperator.And ? And(left, right) : Or(left, right);

    static bool? Or(bool? left, bool? right)
    {
        if (left == true || right == true) return true;
        if (left == false && right == false) return false;
        return null;
    }

    static bool? And(bool? left, bool? right)
    {
        if (left == false || right == false) return false;
        if (left == true && right == true) return true;
        return null;
    }

    sealed record Gate(string Kind, string Name, SyntaxNode Node, IReadOnlyList<AuthorizeSyntax> Gates);
}
