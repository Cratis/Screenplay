// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly HashSet<(string Code, SourceLocation Location, string Reason)> _negatedClaimWarnings = [];

        static IEnumerable<ClaimConditionSyntax> NegatedClaims(PolicyConditionSyntax? condition, bool underNot = false) => condition switch
        {
            ClaimConditionSyntax claim when underNot => [claim],
            NotPolicyConditionSyntax not => NegatedClaims(not.Operand, true),
            LogicalPolicyConditionSyntax logical => NegatedClaims(logical.Left, underNot).Concat(NegatedClaims(logical.Right, underNot)),
            _ => []
        };

        void WarnAboutUnknownNegatedTargets(PolicySyntax policy, Dictionary<string, SemanticProperty> properties, string? commandName, SourceLocation location)
        {
            foreach (var claim in NegatedClaims(policy.Condition))
            {
                var reason = claim switch
                {
                    { MatchesSubject: true } => NegatedSubjectRisk(properties, commandName),
                    { Matches: PathExpressionSyntax path } => NegatedPathRisk(path.Path, properties),
                    _ => null
                };
                if (reason is not null)
                {
                    var target = claim.MatchesSubject ? "subject" : ((PathExpressionSyntax)claim.Matches!).Path;
                    var message = $"Policy '{policy.Name}' negates claim '{claim.Claim}' whose comparison target '{target}' {reason}. An undecidable target stays unknown under 'not'; a final unknown policy result denies access.";
                    if (_negatedClaimWarnings.Add((DiagnosticCodes.IndeterminateNegatedClaimTarget, location, message)))
                    {
                        Warning(DiagnosticCodes.IndeterminateNegatedClaimTarget, message, location);
                    }
                }
            }
        }

        string? NegatedSubjectRisk(Dictionary<string, SemanticProperty> properties, string? commandName)
        {
            var subject = commandName is null ? properties.Values.SingleOrDefault() : properties.Values.SingleOrDefault(property => property.IsIdentifier);

            return subject is null ? "is subject, but the command has no identifier" : NegatedTypeRisk(subject.Type, subject.Type.IsOptional);
        }

        string? NegatedPathRisk(string path, Dictionary<string, SemanticProperty> properties)
        {
            var parts = path.Split('.');
            if (!properties.TryGetValue(parts[0], out var property)) return null;
            var optional = property.Type.IsOptional;
            foreach (var name in parts.Skip(1))
            {
                if (property.Type.Kind != SemanticTypeReferenceKind.CompositeType || property.Type.IsCollection) return null;
                var declaration = (syntax.Types ?? []).SingleOrDefault(type =>
                    _types.TryGetValue(type.Name, out var registered) && registered.Id == property.Type.Target);
                var member = declaration?.Properties.SingleOrDefault(candidate => candidate.Name == name);
                if (member is null) return null;
                property = new(default, member.Name, BindTypeReference(member.Type), false);
                optional |= property.Type.IsOptional;
            }

            return NegatedTypeRisk(property.Type, optional);
        }

        string? NegatedTypeRisk(SemanticTypeReference type, bool optional)
        {
            if (optional) return "can be absent or null";
            if (type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType) return "is not a scalar text type";
            var concept = type.Kind == SemanticTypeReferenceKind.Concept
                ? syntax.Concepts.Single(concept => _concepts[concept.Name].Id == type.Target)
                : null;
            var primitive = concept switch
            {
                { IsEnum: true } => SemanticPrimitiveType.Text,
                not null => Primitive(concept.Type),
                _ => type.Primitive
            };
            var text = primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;

            return text ? null : "is not a string type";
        }
    }
}
