// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
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
        foreach (var (persona, result) in personas)
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
        foreach (var gate in gates.Where(gate => gate.Kind != "Screen" && gate.Gates.Count > 0 && personas.All(persona => persona.Result.Caller is { } caller && Evaluate(gate.Gates, caller, application) == false)))
        {
            yield return Diagnostic.Warning(DiagnosticCodes.GateWithoutPersona, $"{gate.Kind} '{gate.Name}' is definitely denied to every declared persona's synthesized caller.", gate.Location);
        }
    }

    static IEnumerable<(string Kind, string Name, SourceLocation Location, IReadOnlyList<AuthorizeSyntax> Gates)> Gates(IEnumerable<FeatureSyntax> features, IReadOnlyList<AuthorizeSyntax> inherited)
    {
        foreach (var feature in features)
        {
            var gates = feature.Authorize is null ? inherited : [.. inherited, feature.Authorize];
            foreach (var nested in Gates(feature.Features, gates)) yield return nested;
            foreach (var slice in feature.Slices)
            {
                foreach (var command in slice.Commands) yield return ("Command", command.Name, command.Location, command.Authorize is null ? gates : [.. gates, command.Authorize]);
                foreach (var query in slice.Queries) yield return ("Query", query.Name, query.Location, query.Authorize is null ? gates : [.. gates, query.Authorize]);

                // Screens inherit the containing module and feature gates; they have no own authorize directive.
                foreach (var screen in slice.Screens) yield return ("Screen", screen.Name, screen.Location, gates);
            }
        }
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
        RoleConditionSyntax role => caller.Roles.Contains(role.Role, StringComparer.Ordinal),
        ClaimConditionSyntax { MatchesSubject: false, Matches: LiteralExpressionSyntax { Value: string value } } claim => caller.Claims.Any(atom => string.Equals(atom.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) && atom.Value == value),
        NotPolicyConditionSyntax not => !Condition(not.Operand, caller),
        LogicalPolicyConditionSyntax logical => Combine(Condition(logical.Left, caller), logical.Operator, Condition(logical.Right, caller)),
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
}
