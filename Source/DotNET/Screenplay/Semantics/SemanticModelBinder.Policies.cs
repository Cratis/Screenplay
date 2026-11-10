// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly Dictionary<string, SemanticPolicyRead> _policyReads = new(StringComparer.Ordinal);
        List<PolicyInputMappingSyntax> _mappings = [];

        static SemanticLogicalOperator PolicyOperator(LogicalOperator op) => op == LogicalOperator.And
            ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or;

        static IEnumerable<string> PolicyPaths(PolicyConditionSyntax? condition) => condition switch
        {
            ClaimConditionSyntax { Matches: PathExpressionSyntax path } => [path.Path],
            LogicalPolicyConditionSyntax logical => PolicyPaths(logical.Left).Concat(PolicyPaths(logical.Right)),
            NotPolicyConditionSyntax not => PolicyPaths(not.Operand),
            _ => []
        };

        static bool PolicyUsesSubject(PolicyConditionSyntax? condition) => condition switch
        {
            ClaimConditionSyntax { MatchesSubject: true } => true,
            LogicalPolicyConditionSyntax logical => PolicyUsesSubject(logical.Left) || PolicyUsesSubject(logical.Right),
            NotPolicyConditionSyntax not => PolicyUsesSubject(not.Operand),
            _ => false
        };

        // Only an effective authorization that is exactly one anonymous policy runs without a caller.
        bool RequiresCaller(SemanticAuthorization? authorization) =>
            authorization is not null &&
            !(authorization is SemanticPolicyReference reference &&
                syntax.Policies.Any(policy => policy.Name == reference.Name && policy is { Condition: AnonymousConditionSyntax, Code: null, File: null }));

        void VerifyPersonaCallers(ImmutableArray<SemanticPolicy> policies)
        {
            foreach (var specification in expansion.Specifications.Where(specification => specification.Authored.GivenCallerPersona is not null))
            {
                var reference = specification.Authored.GivenCallerPersona!;
                var persona = syntax.Personas!.Single(persona => persona.Name == reference.Name);
                var fixture = specification.Effective.GivenCaller!;
                var caller = new SemanticCaller(fixture.Authenticated, [.. fixture.Roles], [.. fixture.Claims.Select(claim => new SemanticCallerClaim(claim.Type, claim.Value))]) { Id = fixture.Id };
                foreach (var name in persona.Policies)
                {
                    var policy = policies.FirstOrDefault(policy => policy.Name == name);
                    if (policy is null || policy.Condition is SemanticOpaquePolicyCondition || !SemanticPolicyEvaluation.AllowsCaller(policy.Condition, caller))
                    {
                        Error(DiagnosticCodes.UnsynthesizablePersonaCaller, $"Persona '{persona.Name}' caller does not satisfy policy '{name}'; use an explicit 'given caller'.", reference.Location);
                    }
                }
            }
        }

        ImmutableArray<SemanticPolicy> BindPolicies() => [.. syntax.Policies.Select(policy =>
        {
            if (policy.Description is not null) Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Policy '{policy.Name}' description is authoring metadata.", policy.Location);
            if (policy.Condition is not null && (policy.Code is not null || policy.File is not null))
            {
                Error(DiagnosticCodes.MixedPolicyImplementation, $"Policy '{policy.Name}' cannot combine 'require' with a file or inline code block", policy.Location);
                return null;
            }

            if (policy.Code is not null || policy.File is not null)
            {
                var requirement = RequireImplementation(SemanticImplementationRole.PolicyPredicate, SemanticAddress.ForApplication(_applicationIdentity), policy.File, policy.Code, policy.Name);
                if (requirement is not null)
                {
                    UsesV3 = true;
                    return new SemanticPolicy(policy.Name, new SemanticOpaquePolicyCondition(requirement.RequirementId));
                }
            }

            var inputs = BindPolicyInputs(policy);
            var reads = BindPolicyReads(policy);
            var condition = policy.Condition is null ? null : BindPolicyCondition(policy.Condition);
            return condition is null ? null : new SemanticPolicy(policy.Name, condition) { Reads = reads, Inputs = inputs };
        }).Where(policy => policy is not null).Select(policy => policy!)];

        SemanticPolicyCondition? BindPolicyCondition(PolicyConditionSyntax syntaxCondition) => syntaxCondition switch
        {
            AuthenticatedConditionSyntax => new SemanticAuthenticatedCondition(),
            AnonymousConditionSyntax => new SemanticAnonymousCondition(),
            RoleConditionSyntax role => new SemanticRoleCondition(role.Role),
            ClaimConditionSyntax { MatchesSubject: true } claim => new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Subject, null),
            ClaimConditionSyntax { Matches: LiteralExpressionSyntax { Value: string value } } claim =>
                new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Literal, value),
            ClaimConditionSyntax { Matches: PathExpressionSyntax path } claim =>
                new SemanticClaimCondition(claim.Claim, SemanticClaimTargetKind.Artifact, path.Path),
            LogicalPolicyConditionSyntax logical => BindLogicalPolicy(logical),
            NotPolicyConditionSyntax not => BindNotPolicy(not),
            StateConditionSyntax state => BindStatePolicyCondition(state),
            _ => UnsupportedPolicyCondition(syntaxCondition)
        };

        ImmutableArray<SemanticPolicyInput> BindPolicyInputs(PolicySyntax policy)
        {
            var inputs = ImmutableArray.CreateBuilder<SemanticPolicyInput>();
            foreach (var input in policy.Inputs ?? [])
            {
                if (input.From is not null)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' input '{input.Name}' declares a default source ('from {input.From}'); a default source is not yet part of the executable model, map the input under 'authorize' instead.", input.Location);
                    continue;
                }

                if (inputs.Any(existing => existing.Name == input.Name))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' declares input '{input.Name}' twice.", input.Location);
                    continue;
                }

                inputs.Add(new(input.Name, BindTypeReference(new TypeRefSyntax(input.Type, false, false, input.Location))));
            }

            return inputs.ToImmutable();
        }

        ImmutableArray<SemanticPolicyRead> BindPolicyReads(PolicySyntax policy)
        {
            _policyReads.Clear();
            var reads = ImmutableArray.CreateBuilder<SemanticPolicyRead>();
            foreach (var read in policy.Reads ?? [])
            {
                if (read.ByParts.Any() || read.By is null)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Policy '{policy.Name}' read '{read.ReadModel}' must name its key with a single 'by <property>'.", read.Location);
                    continue;
                }

                if (!_readModels.TryGetValue(read.ReadModel, out var view))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' reads read model '{read.ReadModel}', which is unresolved.", read.Location);
                    continue;
                }

                var bound = new SemanticPolicyRead(read.Alias ?? read.ReadModel, view.Model.Id, read.By);
                if (_policyReads.TryAdd(bound.Name, bound)) reads.Add(bound);
                else Error(DiagnosticCodes.DuplicateReadsAlias, $"Read alias '{bound.Name}' is duplicated in policy '{policy.Name}'.", read.Location);
            }

            return reads.ToImmutable();
        }

        SemanticStatePolicyCondition? BindStatePolicyCondition(StateConditionSyntax state)
        {
            var comparison = state.Comparison;
            var parts = comparison.Left.Split('.');
            if (comparison.LeftExpression is not null || parts.Length != 2 || !_policyReads.TryGetValue(parts[0], out var read))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"State condition operand '{comparison.Left}' must be '<read alias>.<property>' of a read the policy declares; the caller's identifier is admitted on the right of a text comparison, and artifact operands are not yet admitted.", comparison.Location);
                return null;
            }

            var view = _readModels.Values.Single(candidate => candidate.Model.Id == read.ReadModel);
            if (!view.Properties.TryGetValue(parts[1], out var property))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"State condition path '{comparison.Left}' does not resolve to a property of the read model.", comparison.Location);
                return null;
            }

            var subject = CommandValidationSubject(property.Name, property.Type);
            var textual = comparison.Operator is ComparisonOperator.Contains or ComparisonOperator.StartsWith;
            if (textual)
            {
                var membership = comparison.Operator == ComparisonOperator.Contains && subject.IsCollection && !subject.IsDate && subject.Primitive is SemanticPrimitiveType.Text;
                var text = !subject.IsCollection && subject.Primitive == SemanticPrimitiveType.Text && subject.Values.IsDefaultOrEmpty;
                if (property.Type.IsOptional || !(membership || text) || comparison.Right is not LiteralExpressionSyntax { } needle || BindLiteral(needle) is not SemanticTextValue literal)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"State condition '{comparison.Left} {(comparison.Operator == ComparisonOperator.Contains ? "contains" : "starts with")}' needs a required text property or a required text collection on the left and a text constant on the right.", comparison.Location);
                    return null;
                }

                var textOperator = comparison.Operator == ComparisonOperator.Contains ? SemanticComparisonOperator.Contains : SemanticComparisonOperator.StartsWith;
                return new SemanticStatePolicyCondition(new SemanticComparison(new(property.Id, null) { Read = read.Name }, textOperator, new(default, literal)));
            }

            var ordering = comparison.Operator is ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual or ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual;
            if (property.Type.IsOptional || subject.IsCollection || subject.IsDate ||
                comparison.Operator is not (ComparisonOperator.Equal or ComparisonOperator.NotEqual or ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual or ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual) ||
                (ordering ? !subject.IsNumber : !(subject.IsNumber || subject.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean)))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"State condition on '{comparison.Left}' is not admitted: ordering requires numbers; equality requires a required scalar text, enumeration, number or boolean; text conditions use 'contains' or 'starts with'.", comparison.Location);
                return null;
            }

            if (comparison.Right is IdentityExpressionSyntax identity && string.Equals(identity.Path, "id", StringComparison.Ordinal))
            {
                if (comparison.Operator is not (ComparisonOperator.Equal or ComparisonOperator.NotEqual) || property.Type.IsOptional || subject.IsCollection || subject.Primitive != SemanticPrimitiveType.Text || !subject.Values.IsDefaultOrEmpty)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"State condition on '{comparison.Left}' compares with the caller's identifier, which needs a required text property and '==' or '!='.", comparison.Right.Location);
                    return null;
                }

                var identityOperator = comparison.Operator == ComparisonOperator.Equal ? SemanticComparisonOperator.Equal : SemanticComparisonOperator.NotEqual;
                return new SemanticStatePolicyCondition(new SemanticComparison(new(property.Id, null) { Read = read.Name }, identityOperator, new(default, null) { CallerId = true }));
            }

            var value = comparison.Right switch
            {
                LiteralExpressionSyntax literal => BindLiteral(literal),
                PathExpressionSyntax path when subject.Values.Contains(path.Path, StringComparer.Ordinal) => SemanticValue.Text(path.Path),
                _ => null
            };
            if (value is null || value is SemanticNullValue || OperandMismatch(value, SemanticValidationRuleKind.Equal, subject) is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"State condition on '{comparison.Left}' must compare with a constant of the property's scalar type or with $identity.id; other caller details, artifact and read operands are not yet admitted.", comparison.Right.Location);
                return null;
            }

            var op = comparison.Operator switch
            {
                ComparisonOperator.Equal => SemanticComparisonOperator.Equal,
                ComparisonOperator.NotEqual => SemanticComparisonOperator.NotEqual,
                ComparisonOperator.GreaterThan => SemanticComparisonOperator.GreaterThan,
                ComparisonOperator.GreaterThanOrEqual => SemanticComparisonOperator.GreaterThanOrEqual,
                ComparisonOperator.LessThan => SemanticComparisonOperator.LessThan,
                _ => SemanticComparisonOperator.LessThanOrEqual
            };
            return new SemanticStatePolicyCondition(new SemanticComparison(new(property.Id, null) { Read = read.Name }, op, new(default, value)));
        }

        SemanticNotPolicyCondition? BindNotPolicy(NotPolicyConditionSyntax not)
        {
            UsesV7 = true;
            var operand = BindPolicyCondition(not.Operand);
            return operand is null ? null : new SemanticNotPolicyCondition(operand);
        }

        SemanticLogicalPolicyCondition? BindLogicalPolicy(LogicalPolicyConditionSyntax logical)
        {
            var left = BindPolicyCondition(logical.Left);
            var right = BindPolicyCondition(logical.Right);
            return left is null || right is null ? null :
                new SemanticLogicalPolicyCondition(left, PolicyOperator(logical.Operator), right);
        }

        SemanticPolicyCondition? UnsupportedPolicyCondition(PolicyConditionSyntax condition)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Policy claim target is not a portable literal, subject, or artifact path.", condition.Location);
            return null;
        }

        SemanticAuthorization? EffectiveAuthorization(AuthorizeSyntax? own, IEnumerable<SemanticProperty> artifactProperties, string moduleName, ImmutableArray<string> featurePath, string? commandName = null, IEnumerable<string>? queryFilterNames = null)
        {
            var module = syntax.Modules.Single(value => value.Name == moduleName);
            var scopes = EffectiveAuthorizationScopes.For(module, featurePath, own);
            SemanticAuthorization? result = null;
            foreach (var scope in scopes)
            {
                var bound = BindAuthorization(scope, artifactProperties, commandName, queryFilterNames);
                if (bound is not null)
                {
                    result = result is null ? bound : new SemanticLogicalAuthorization(result, SemanticLogicalOperator.And, bound);
                }
            }

            return result;
        }

        SemanticAuthorization? BindAuthorization(AuthorizeSyntax? authorize, IEnumerable<SemanticProperty> artifactProperties, string? commandName, IEnumerable<string>? queryFilterNames)
        {
            if (authorize is null) return null;
            var properties = artifactProperties.ToDictionary(property => property.Name, StringComparer.Ordinal);
            _mappings = authorize.Mappings?.ToList() ?? [];
            return BindAuthorizationNode(authorize.Requirement, properties, commandName, queryFilterNames);
        }

        SemanticAuthorization? BindAuthorizationNode(PolicyRequirementSyntax requirement, Dictionary<string, SemanticProperty> properties, string? commandName, IEnumerable<string>? queryFilterNames)
        {
            if (requirement is LogicalPolicyRequirementSyntax logical)
            {
                var left = BindAuthorizationNode(logical.Left, properties, commandName, queryFilterNames);
                var right = BindAuthorizationNode(logical.Right, properties, commandName, queryFilterNames);
                return left is null || right is null ? null :
                    new SemanticLogicalAuthorization(left, PolicyOperator(logical.Operator), right);
            }

            if (requirement is not PolicyReferenceSyntax reference) return null;
            var policy = syntax.Policies.FirstOrDefault(policy => policy.Name == reference.Name);
            if (policy is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Authorization policy '{reference.Name}' is unresolved.", reference.Location);
                return null;
            }

            if (PolicyUsesSubject(policy.Condition) && properties.Values.FirstOrDefault(property => property.IsGenerated && property.IsIdentifier) is { } identifier)
            {
                GeneratedReference(commandName!, identifier.Name, $"authorization policy '{policy.Name}' (implicit subject)", reference.Location);
            }

            foreach (var path in PolicyPaths(policy.Condition))
            {
                if (properties.GetValueOrDefault(path.Split('.')[0]) is { IsGenerated: true } generated)
                {
                    GeneratedReference(commandName!, generated.Name, $"authorization policy '{policy.Name}'", reference.Location);
                }
                else if (!ResolvesPolicyPath(path, properties))
                {
                    var message = queryFilterNames?.Contains(path, StringComparer.Ordinal) == true
                        ? $"Policy '{policy.Name}' artifact path '{path}' names query filter '{path}'. Policies read only the query's 'by' argument, not filters. Declare '{path}' with 'by' to authorize by it."
                        : $"Policy '{policy.Name}' artifact path '{path}' does not resolve against the authorized command properties or query arguments.";
                    Error(DiagnosticCodes.InvalidSemanticBinding, message, reference.Location);
                }
            }

            var mappings = ImmutableArray.CreateBuilder<SemanticPolicyInputMapping>();
            foreach (var mapping in _mappings)
            {
                if (!(policy.Inputs ?? []).Any(input => input.Name == mapping.Input))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' declares no input '{mapping.Input}'.", mapping.Location);
                }
                else if (!properties.TryGetValue(mapping.Property, out var mapped) || mapped.IsGenerated)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' input '{mapping.Input}' maps to '{mapping.Property}', which is not an input of the authorized command or query.", mapping.Location);
                }
                else
                {
                    mappings.Add(new(mapping.Input, mapping.Property));
                }
            }

            foreach (var read in policy.Reads ?? [])
            {
                var viaInput = read.By is not null && (policy.Inputs ?? []).Any(input => input.Name == read.By);
                var source = viaInput ? mappings.FirstOrDefault(mapping => mapping.Input == read.By)?.Property ?? read.By : read.By;
                if (source is null || !properties.TryGetValue(source, out var key) || key.IsGenerated)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Policy '{policy.Name}' read '{read.ReadModel}' takes its key from '{read.By}', which is not an input of the authorized command or query{(viaInput ? "; map the input under 'authorize'" : string.Empty)}.", reference.Location);
                }
            }

            WarnAboutUnknownNegatedTargets(policy, properties, commandName, reference.Location);

            return new SemanticPolicyReference(reference.Name) { Mappings = mappings.ToImmutable() };
        }

        bool ResolvesPolicyPath(string path, Dictionary<string, SemanticProperty> properties)
        {
            var parts = path.Split('.');
            if (parts.Any(part => part.Length == 0) || !properties.TryGetValue(parts[0], out var property)) return false;
            foreach (var name in parts.Skip(1))
            {
                if (property.Type.Kind != SemanticTypeReferenceKind.CompositeType || property.Type.IsCollection) return false;
                var declaration = (syntax.Types ?? []).SingleOrDefault(type =>
                    _types.TryGetValue(type.Name, out var registered) && registered.Id == property.Type.Target);
                var member = declaration?.Properties.SingleOrDefault(candidate => candidate.Name == name);
                if (member is null) return false;
                property = new(default, member.Name, BindTypeReference(member.Type), false);
            }

            return true;
        }
    }
}
