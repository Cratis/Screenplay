// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates refusal selectors and the scope and type of refusal values without executing reactions.
/// </summary>
internal static class ReactionRefusalValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        new ValueWalker(application, declarations, context).VisitApplication(application);
        Dictionary<CommandSyntax, bool>? authorizationGates = null;
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var reaction in slice.Reactions.Where(reaction => reaction.RunsAs is not null && !reaction.Triggers.Any(trigger => (trigger.Invokes ?? []).Any() || trigger.File is not null || trigger.Code is not null)))
            {
                context.Warning(DiagnosticCodes.UnusedReactionIdentity, $"Reaction '{reaction.Name}' declares an identity but has no invocations or implementation body.", reaction.RunsAs!.Location);
            }

            foreach (var (reaction, invocation) in slice.Reactions.SelectMany(reaction => reaction.Triggers.SelectMany(trigger => trigger.Invokes ?? []).Select(invocation => (reaction, invocation))))
            {
                var command = declarations.Resolve(invocation.Command, scope, owner => owner.Commands, node => node.Name);
                var authorizationGated = command is { } invokedCommand && (authorizationGates ??= CollectAuthorizationGates(application))[invokedCommand.Node];
                var authorizationBranch = invocation.OnRefused.FirstOrDefault(branch => branch.Selector == "authorization");
                if (reaction.RunsAs is null && authorizationGated && authorizationBranch is null)
                {
                    context.Warning(DiagnosticCodes.GatedInvocationWithoutIdentity, $"Command '{invocation.Command}' is authorization-gated, but reaction '{reaction.Name}' has no declared identity. Declare 'runs as system role \"<Role>\"'.", invocation.Location);
                }

                var earlier = new List<InvocationRefusalSyntax>();
                foreach (var branch in invocation.OnRefused)
                {
                    if (earlier.Exists(previous => Covers(previous, branch)))
                    {
                        context.Warning(DiagnosticCodes.UnreachableRefusalBranch, "This refusal selector is covered by an earlier branch; the first matching branch wins.", branch.Location);
                    }

                    earlier.Add(branch);
                    if (ReferenceEquals(branch, authorizationBranch) && reaction.RunsAs is null && authorizationGated)
                    {
                        context.Warning(
                            DiagnosticCodes.AuthorizationRefusalWithoutIdentity,
                            $"Command '{invocation.Command}' is authorization-gated, but this invocation has no declared identity. This authorization refusal branch always fires in the reference runner because there is no caller. Declare 'runs as system role \"<Role>\"'; Arc runs commands as the system only for a reactor carrying [ExecuteCommandsAsSystem].",
                            branch.Location);
                    }

                    if (branch.Constraint is not { } name) continue;
                    var constraint = declarations.Resolve(name, scope, owner => owner.Constraints, node => node.Name);
                    if (constraint is not { } resolved)
                    {
                        context.Error(DiagnosticCodes.UnknownRefusalConstraint, $"Unknown or ambiguous refusal constraint '{name}'.", branch.Location);
                        continue;
                    }

                    if (command is not { } invoked || invoked.Node.Handler is not null) continue;
                    var rules = new[] { resolved.Node }.Concat(resolved.Node.AdditionalRules).ToArray();
                    if (rules.Any(rule => rule is FileConstraintSyntax)) continue;
                    var targets = rules.Select(rule => rule switch
                    {
                        UniqueEventConstraintSyntax unique => declarations.Event(unique.Event, resolved.Scope),
                        UniquePropertyConstraintSyntax unique => declarations.Event(unique.Event, resolved.Scope),
                        _ => null
                    }).ToArray();
                    var productions = invoked.Node.Produces.Select(production => declarations.Event(production.Event, invoked.Scope)).ToArray();
                    if (targets.All(target => target is not null) && productions.All(production => production is not null) &&
                        !targets.Intersect(productions).Any())
                    {
                        context.Warning(DiagnosticCodes.UnreachableRefusalBranch, $"Constraint '{name}' targets none of the events produced by '{invocation.Command}'.", branch.Location);
                    }
                }

                bool Covers(InvocationRefusalSyntax previous, InvocationRefusalSyntax branch) =>
                    (previous.Selector == "any" && (branch.Selector == "any" || branch.Selector == "validation" || branch.Selector == "constraint")) ||
                    (previous.Selector == branch.Selector && (previous.Selector != "constraint" || previous.Constraint is null ||
                        (branch.Constraint is not null && SameConstraint(previous.Constraint, branch.Constraint))));

                bool SameConstraint(string left, string right) => left == right ||
                    (declarations.Resolve(left, scope, owner => owner.Constraints, node => node.Name) is { } first &&
                    declarations.Resolve(right, scope, owner => owner.Constraints, node => node.Name) is { } second && ReferenceEquals(first.Node, second.Node));
            }
        }
    }

    static Dictionary<CommandSyntax, bool> CollectAuthorizationGates(ApplicationSyntax application)
    {
        var gates = new Dictionary<CommandSyntax, bool>(ReferenceEqualityComparer.Instance);
        foreach (var module in application.Modules)
        {
            foreach (var feature in module.Features) Collect(feature, module.Authorize is not null);
        }

        return gates;

        void Collect(FeatureSyntax feature, bool inherited)
        {
            var gated = inherited || feature.Authorize is not null;
            foreach (var command in feature.Slices.SelectMany(slice => slice.Commands))
            {
                gates[command] = gated || command.Authorize is not null;
            }

            foreach (var child in feature.Features) Collect(child, gated);
        }
    }

    sealed class ValueWalker(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context) : ScreenplaySyntaxWalker
    {
        DeclarationScope? _scope;
        InvocationRefusalSyntax? _branch;
        IEnumerable<PropertySyntax>? _properties;
        TypeRefSyntax? _target;
        bool _mapping;

        public override void VisitSlice(SliceSyntax syntax)
        {
            _scope = declarations.Slices.First(entry => ReferenceEquals(entry.Slice, syntax)).Scope;
            base.VisitSlice(syntax);
            _scope = null;
        }

        public override void VisitInvocationRefusal(InvocationRefusalSyntax syntax)
        {
            _branch = syntax;
            base.VisitInvocationRefusal(syntax);
            _branch = null;
        }

        public override void VisitProduces(ProducesSyntax syntax)
        {
            if (_branch is null || _scope is null)
            {
                base.VisitProduces(syntax);
                return;
            }

            VisitNode(syntax);
            if (syntax.For is not null) VisitExpression(syntax.For);
            if (syntax.When is not null) VisitCondition(syntax.When);
            foreach (var tag in syntax.Tags ?? []) VisitTag(tag);
            _properties = declarations.Event(syntax.Event, _scope)?.Properties;
            foreach (var mapping in syntax.Mappings) VisitPropertyMapping(mapping);
            _properties = null;
        }

        public override void VisitPropertyMapping(PropertyMappingSyntax syntax)
        {
            _mapping = _branch is not null;
            _target = declarations.Property(_properties, syntax.Property, out _)?.Type;
            base.VisitPropertyMapping(syntax);
            _target = null;
            _mapping = false;
        }

        public override void VisitObjectMember(ObjectMemberSyntax syntax)
        {
            var parent = _target;
            _target = declarations.Property(parent is null ? null : declarations.TypeProperties(parent.Name), syntax.Name, out _)?.Type;
            base.VisitObjectMember(syntax);
            _target = parent;
        }

        public override void VisitListExpression(ListExpressionSyntax syntax)
        {
            var parent = _target;
            _target = parent is null ? null : parent with { IsCollection = false };
            base.VisitListExpression(syntax);
            _target = parent;
        }

        public override void VisitRefusalExpression(RefusalExpressionSyntax syntax)
        {
            if (!_mapping || _branch is null || syntax.Member is not ("reason" or "constraint" or "message") ||
                (syntax.Member == "constraint" && _branch.Selector != "constraint"))
            {
                context.Error(DiagnosticCodes.InvalidRefusalValue, $"'$refusal.{syntax.Member}' is only available in a refusal branch's event mapping; constraint requires 'by constraint'.", syntax.Location);
            }
            else if (_target is { } target && (target.IsCollection || (declarations.Compatible(new TypeRefSyntax("String", false, false, target.Location), target) == false && !application.Concepts.Any(concept => concept.Name == target.Name && concept.Type == "String"))))
            {
                context.Error(DiagnosticCodes.InvalidRefusalValue, $"'$refusal.{syntax.Member}' is a String value, incompatible with '{target.Name}'.", syntax.Location);
            }

            base.VisitRefusalExpression(syntax);
        }
    }
}
