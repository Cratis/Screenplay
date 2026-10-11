// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates that every operand an interaction names resolves - the command an action executes, the screen
/// it navigates to, the query it refreshes, the dialog it opens, the trigger it raises or observes, and the
/// behavior a <c>uses</c> clause attaches, along with that behavior's parameters.
/// </summary>
/// <remarks>
/// This is what makes an action a modeled reference rather than a string: an operand naming nothing is
/// reported here, at compile time, instead of becoming a control that does nothing when clicked.
/// </remarks>
/// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
/// <param name="references">The declarations an action operand can resolve against.</param>
/// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
internal sealed class InteractionReferenceValidator(ApplicationSyntax application, InteractionReferences references, ParserContext context)
{
    // What an inline behavior has: only a named behavior declares parameters, so an operand inside an inline
    // one is always a reference to something declared rather than to a parameter.
    static readonly IReadOnlySet<string> _noParameters = new HashSet<string>(StringComparer.Ordinal);

    readonly Dictionary<string, BehaviorSyntax> _behaviors = application.Behaviors.ToDictionary(behavior => behavior.Name ?? string.Empty, StringComparer.Ordinal);

    /// <summary>
    /// Validates the interactions of the application.
    /// </summary>
    /// <param name="scoped">Every slice with the scope it sits in.</param>
    public void Validate(IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> scoped)
    {
        var root = new DeclarationScope([]);

        // A named behavior is declared at the top level, so it resolves against the document as a whole.
        foreach (var behavior in application.Behaviors)
        {
            ValidateBindings(behavior.Bindings, root, behavior.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal));
        }

        // Structural attachments - a layout, a template, a module, a feature, a form. Each resolves from the
        // scope it sits in, the same as a screen's own attachment does.
        foreach (var layout in application.Layouts ?? [])
        {
            ValidateAttachments(layout.Behaviors, layout.UsedBehaviors, root);
        }

        foreach (var module in application.Modules)
        {
            ValidateModuleAttachments(module);
        }

        foreach (var (slice, scope) in scoped)
        {
            foreach (var directive in slice.Screens.SelectMany(screen => ScreenplayValidator.AllDirectives(screen.Directives)))
            {
                ValidateDirective(directive, scope);
            }
        }
    }

    void ValidateModuleAttachments(ModuleSyntax module)
    {
        var moduleScope = new DeclarationScope([module.Name]);
        ValidateAttachments(module.Behaviors, module.UsedBehaviors, moduleScope);

        foreach (var template in module.ScreenTemplates)
        {
            ValidateAttachments(template.Behaviors, template.UsedBehaviors, moduleScope);
        }

        foreach (var template in module.DialogTemplates ?? [])
        {
            ValidateAttachments(template.Behaviors, template.UsedBehaviors, moduleScope);
        }

        foreach (var form in module.Forms ?? [])
        {
            ValidateAttachments(form.Behaviors, form.UsedBehaviors, moduleScope);
        }

        // Scoped from the module down, so a feature's attachment resolves the way a slice inside it does.
        foreach (var (feature, scope) in ScreenplayValidator.AllFeaturesScoped(module.Features, [module.Name]))
        {
            ValidateAttachments(feature.Behaviors, feature.UsedBehaviors, scope);
        }
    }

    void ValidateDirective(ScreenDirectiveSyntax directive, DeclarationScope scope)
    {
        switch (directive)
        {
            case ScreenBehaviorSyntax attached:
                ValidateBindings(attached.Behavior.Bindings, scope, _noParameters);
                break;
            case ScreenUsesBehaviorSyntax uses:
                ValidateUses(uses.Uses);
                break;
            case ScreenTableSyntax table:
                ValidateAttachments(table.Behaviors, table.UsedBehaviors, scope);
                break;
            case ScreenComponentSyntax component:
                ValidateComponentInteractions(component, scope);
                break;
        }
    }

    void ValidateComponentInteractions(ScreenComponentSyntax component, DeclarationScope scope)
    {
        // Preserve plain component interactions' existing reference-validation behavior.
        foreach (var behavior in component.Behaviors)
        {
            ValidateBindings(behavior.Bindings.Where(binding => binding.Alternatives.Any()), scope, _noParameters);
        }

        foreach (var uses in component.UsedBehaviors.Where(uses => _behaviors.TryGetValue(uses.Behavior, out var behavior) && behavior.Bindings.Any(binding => binding.Alternatives.Any())))
        {
            ValidateUses(uses);
        }
    }

    void ValidateAttachments(IEnumerable<BehaviorSyntax> attached, IEnumerable<UsesBehaviorSyntax> used, DeclarationScope scope)
    {
        foreach (var behavior in attached)
        {
            ValidateBindings(behavior.Bindings, scope, _noParameters);
        }

        foreach (var uses in used)
        {
            ValidateUses(uses);
        }
    }

    void ValidateUses(UsesBehaviorSyntax uses)
    {
        if (!_behaviors.TryGetValue(uses.Behavior, out var behavior))
        {
            context.Warning(
                DiagnosticCodes.UnknownUsedBehavior,
                $"Unknown behavior '{uses.Behavior}' - nothing in the document declares it",
                uses.Location);
            return;
        }

        var declared = behavior.Parameters.Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
        var supplied = new HashSet<string>(StringComparer.Ordinal);

        foreach (var argument in uses.Arguments)
        {
            supplied.Add(argument.Name);
            if (!declared.Contains(argument.Name))
            {
                context.Error(
                    DiagnosticCodes.UnknownBehaviorArgument,
                    $"Behavior '{behavior.Name}' declares no parameter '{argument.Name}'",
                    argument.Location);
            }
        }

        foreach (var missing in declared.Where(parameter => !supplied.Contains(parameter)).Order(StringComparer.Ordinal))
        {
            context.Error(
                DiagnosticCodes.MissingBehaviorArgument,
                $"Behavior '{behavior.Name}' declares a parameter '{missing}' this attachment supplies no value for",
                uses.Location);
        }
    }

    void ValidateBindings(IEnumerable<InteractionBindingSyntax> bindings, DeclarationScope scope, IReadOnlySet<string> parameters)
    {
        foreach (var binding in bindings)
        {
            ValidateTrigger(binding.Trigger);
            ValidateActions(binding.Actions, scope, parameters);
            foreach (var alternative in binding.Alternatives) ValidateActions(alternative.Actions, scope, parameters);
            if (binding.Otherwise is not null) ValidateActions(binding.Otherwise.Actions, scope, parameters);
        }
    }

    void ValidateTrigger(InteractionTriggerSyntax trigger)
    {
        switch (trigger)
        {
            case EventInteractionTriggerSyntax @event when !references.Events.Contains(@event.EventName):
                context.Warning(
                    DiagnosticCodes.UnknownInteractionEvent,
                    $"Unknown event '{@event.EventName}' - nothing in scope declares it",
                    @event.Location);
                break;
            case ApplicationTriggerInteractionTriggerSyntax applicationTrigger when !references.Triggers.Contains(applicationTrigger.TriggerName):
                context.Warning(
                    DiagnosticCodes.UnknownActionTrigger,
                    $"Unknown trigger '{applicationTrigger.TriggerName}' - nothing in the document declares it, and it is not a built-in interaction kind",
                    applicationTrigger.Location);
                break;
        }
    }

    void ValidateActions(IEnumerable<InteractionActionSyntax> actions, DeclarationScope scope, IReadOnlySet<string> parameters)
    {
        var ordered = actions.ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var action = ordered[index];

            // An operand may be the name of a parameter the attachment supplies rather than a declaration -
            // a behavior exists precisely so the same actions can point at different commands.
            ValidateOperands(action, scope, parameters);

            // Navigating replaces the screen the interaction was running on, so whatever was written after it
            // never runs. Reported rather than accepted, because the author expects those actions to happen.
            if (action is NavigateActionSyntax or NavigateBackActionSyntax && index < ordered.Count - 1)
            {
                context.Warning(
                    DiagnosticCodes.UnreachableInteractionContinuation,
                    "Actions after an unconditional navigation never run - the screen they were written for is already gone",
                    ordered[index + 1].Location);
            }

            ValidateActions(action.OnSuccess, scope, parameters);
            ValidateActions(action.OnFailure, scope, parameters);
            ValidateActions(action.OnResult, scope, parameters);
        }
    }

    void ValidateOperands(InteractionActionSyntax action, DeclarationScope scope, IReadOnlySet<string> parameters)
    {
        switch (action)
        {
            case ExecuteCommandActionSyntax execute when !parameters.Contains(execute.Command):
                ScreenplayValidator.Report(execute.Command, scope, references.Commands, DiagnosticCodes.UnknownActionCommand, "command", execute.Location, context);
                break;
            case NavigateActionSyntax navigate when !parameters.Contains(navigate.Screen):
                ScreenplayValidator.Report(navigate.Screen, scope, references.Screens, DiagnosticCodes.UnknownActionScreen, "screen", navigate.Location, context);
                break;
            case RefreshQueryActionSyntax refresh when !parameters.Contains(refresh.Query):
                ScreenplayValidator.Report(refresh.Query, scope, references.Queries, DiagnosticCodes.UnknownActionQuery, "query", refresh.Location, context);
                break;
            case OpenDialogActionSyntax open when !parameters.Contains(open.DialogTemplate) && !references.DialogTemplates.Contains(open.DialogTemplate):
                context.Warning(
                    DiagnosticCodes.UnknownActionDialogTemplate,
                    $"Unknown dialog template '{open.DialogTemplate}' - no module in the document declares it",
                    open.Location);
                break;
            case RaiseTriggerActionSyntax raise when !parameters.Contains(raise.Trigger) && !references.Triggers.Contains(raise.Trigger):
                context.Warning(
                    DiagnosticCodes.UnknownActionTrigger,
                    $"Unknown trigger '{raise.Trigger}' - nothing in the document declares it",
                    raise.Location);
                break;
        }
    }
}
