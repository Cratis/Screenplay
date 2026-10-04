// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Syntax;

internal static partial class ImplementationInvariants
{
    internal static void Validate(SyntaxNode node)
    {
        OperationInvariants.Validate(node);

        // Legacy structural trees remain transportable. Authoring also requires exact print/parse fidelity.
        // The new wrapper must never silently pick a payload, including before that admission step.
        if (node is HandlerSyntax { Implementation: not null } handler)
        {
            ValidateHandler(handler);
        }

        if (node is ValidationRuleSyntax rule && NamedRuleError(rule, true) is { } ruleError)
        {
            throw new InvalidSyntaxJson(ruleError);
        }

        if (node is ConceptSyntax concept && (concept.Validations ?? []).OfType<DeclarativeValidateSyntax>().SelectMany(block => block.Rules).Any(rule => rule.Implementation is not null))
        {
            throw new InvalidSyntaxJson("Implementation wrappers on named rules are supported only on commands.");
        }

        if (node is ImplementationSyntax { Hints: null })
        {
            throw new InvalidSyntaxJson("Implementation hints must be a collection.");
        }

        if (node is ImplementationHintSyntax hintNode && ImplementationHintText.IsBlank(hintNode.Text))
        {
            throw new InvalidSyntaxJson("An implementation hint must be nonblank.");
        }
    }

    internal static void ValidateAuthoring(ApplicationSyntax application) => new AuthoringWalker().VisitApplication(application);

    // Also used by the public binder, which does not run authoring validation.
    internal static string? NamedRuleError(ValidationRuleSyntax rule, bool commandOwner)
    {
        if (rule.Implementation is not { } implementation) return null;
        if (!commandOwner) return "Implementation wrappers on named rules are supported only on commands.";
        if (rule.Rule != ValidationRuleKind.Rule || rule.Value is not PathExpressionSyntax name || string.IsNullOrEmpty(name.Path) || !NameRegex().IsMatch(name.Path))
        {
            return "An implementation wrapper requires a named rule with a valid predicate name.";
        }

        if (rule.File is not null && rule.Code is not null) return "A named rule has at most one file or inline payload.";
        if (implementation.Hints?.Any(hint => hint is null || ImplementationHintText.IsBlank(hint.Text)) != false)
        {
            return "Implementation hints must be a collection of nonblank hints.";
        }

        return null;
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*\z", RegexOptions.None, 1000)]
    private static partial Regex NameRegex();

    static void ValidateHandler(HandlerSyntax handler)
    {
        if (handler.File is not null && handler.Code is not null)
        {
            throw new InvalidSyntaxJson("A handler has at most one file or inline payload.");
        }

        if (handler.File is null && handler.Code is null && handler.Implementation is null)
        {
            throw new InvalidSyntaxJson("A bare handler requires a payload or implementation wrapper.");
        }
    }

    sealed class AuthoringWalker : ScreenplaySyntaxWalker
    {
        public override void VisitHandler(HandlerSyntax syntax)
        {
            ValidateHandler(syntax);
            base.VisitHandler(syntax);
        }

        public override void VisitNode(SyntaxNode node) => Validate(node);
    }
}
