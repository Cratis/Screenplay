// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenplayValidator
{
    /// <summary>
    /// Flattens a screen's directives, descending into the templates, slots and sections that hold more.
    /// </summary>
    /// <param name="directives">The directives to flatten.</param>
    /// <returns>Every directive, including the ones nested inside another.</returns>
    internal static IEnumerable<ScreenDirectiveSyntax> AllDirectives(IEnumerable<ScreenDirectiveSyntax> directives)
    {
        foreach (var directive in directives)
        {
            yield return directive;

            foreach (var child in AllDirectives(NestedDirectives(directive)))
            {
                yield return child;
            }

            foreach (var follower in NavigationFollowers(directive))
            {
                yield return follower;
            }
        }
    }

    /// <summary>
    /// Validates that every query asserted by a specification resolves from the specification's slice.
    /// </summary>
    /// <param name="scoped">Every slice in the document, with the scope it sits in.</param>
    /// <param name="queries">The queries the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateSpecificationQueries(
        IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> scoped,
        IReadOnlyList<Declaration> queries,
        ParserContext context)
    {
        foreach (var (slice, scope) in scoped)
        {
            foreach (var query in slice.Specifications.SelectMany(specification => specification.ThenQueries))
            {
                Report(query.Query, scope, queries, DiagnosticCodes.UnknownQuery, "query", query.Location, context);
            }
        }
    }

    /// <summary>
    /// Validates that what a screen binds to resolves - the queries it reads, the commands it invokes and
    /// the screens it navigates to.
    /// </summary>
    /// <param name="scoped">Every slice in the document, with the scope it sits in.</param>
    /// <param name="references">The queries, commands and screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A bare name resolves from the inside out - the slice, then the feature, then the module, then the
    /// document - and the innermost match wins. Reported as warnings rather than errors: a name may resolve
    /// to something outside the document, and the point is that the gap is visible.
    /// </remarks>
    static void ValidateScreenReferences(
        IReadOnlyList<(SliceSyntax Slice, DeclarationScope Scope)> scoped,
        InteractionReferences references,
        ParserContext context)
    {
        foreach (var (slice, scope) in scoped)
        {
            foreach (var directive in slice.Screens.SelectMany(screen => AllDirectives(screen.Directives)))
            {
                ValidateDirectiveReferences(directive, scope, references, context);
            }
        }
    }

    static void ValidateDirectiveReferences(ScreenDirectiveSyntax directive, DeclarationScope scope, InteractionReferences references, ParserContext context)
    {
        switch (directive)
        {
            case ScreenDataSyntax data:
                Report(data.Query, scope, references.Queries, DiagnosticCodes.UnknownQuery, "query", data.Location, context);
                break;
            case ScreenActionSyntax action:
                Report(action.Command, scope, references.Commands, DiagnosticCodes.UnknownCommand, "command", action.Location, context);
                break;
            case ScreenGuardedActionSyntax guarded:
                ValidateGuardedActionReferences(guarded, scope, references, context);
                break;
            case ScreenNavigateSyntax navigate:
                Report(navigate.Screen, scope, references.Screens, DiagnosticCodes.UnknownScreen, "screen", navigate.Location, context);
                break;
        }
    }

    static void ValidateGuardedActionReferences(ScreenGuardedActionSyntax guarded, DeclarationScope scope, InteractionReferences references, ParserContext context)
    {
        foreach (var alternative in guarded.Alternatives)
        {
            Report(alternative.Command, scope, references.Commands, DiagnosticCodes.UnknownCommand, "command", alternative.Location, context);
        }

        if (guarded.Otherwise is { Command: { } fallback })
        {
            Report(fallback, scope, references.Commands, DiagnosticCodes.UnknownCommand, "command", guarded.Otherwise.Location, context);
        }
    }

    /// <summary>
    /// Validates that what a form binds to resolves - the command it submits, the fields it maps onto that
    /// command's properties, the query it populates from and the screen it navigates to on submit.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="commandsByDeclaration">Every declared command, keyed by its own declaration.</param>
    /// <param name="references">The commands, queries and screens the document declares, scoped to where they are declared.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A form sits at module level, not inside a slice, so it resolves against the module's own scope - it
    /// can disambiguate by module but not by feature or slice, since it does not sit inside either.
    /// </remarks>
    static void ValidateFormReferences(
        ApplicationSyntax application,
        Dictionary<Declaration, CommandSyntax> commandsByDeclaration,
        InteractionReferences references,
        ParserContext context)
    {
        foreach (var module in application.Modules)
        {
            var scope = new DeclarationScope([module.Name]);
            foreach (var form in module.Forms ?? [])
            {
                ValidateForm(form, scope, commandsByDeclaration, references, context);
            }
        }
    }

    static void ValidateForm(
        FormSyntax form,
        DeclarationScope scope,
        Dictionary<Declaration, CommandSyntax> commandsByDeclaration,
        InteractionReferences references,
        ParserContext context)
    {
        Report(form.For, scope, references.Commands, DiagnosticCodes.UnknownCommand, "command", form.Location, context);

        var resolution = ReferenceResolver.Resolve(form.For, scope, references.Commands);
        if (resolution.Resolved is { } resolved && commandsByDeclaration.TryGetValue(resolved, out var command))
        {
            ValidateFormFields(form, command, context);
        }

        if (form.Populate is FormPopulateViaQuerySyntax populate)
        {
            Report(populate.Query, scope, references.Queries, DiagnosticCodes.UnknownQuery, "query", populate.Location, context);
        }

        if (form.OnSubmit is not null)
        {
            Report(form.OnSubmit.Screen, scope, references.Screens, DiagnosticCodes.UnknownScreen, "screen", form.OnSubmit.Location, context);
        }
    }

    /// <summary>
    /// Validates that every field a form declares binds to a property its command actually has - the same
    /// rename-breaks-the-build guarantee AutoMap gives a projection.
    /// </summary>
    /// <param name="form">The <see cref="FormSyntax"/> to validate.</param>
    /// <param name="command">The <see cref="CommandSyntax"/> the form binds to.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateFormFields(FormSyntax form, CommandSyntax command, ParserContext context)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();
        foreach (var field in form.Fields.Where(field => !properties.Contains(field.Property)))
        {
            context.Warning(
                DiagnosticCodes.UnknownFormFieldProperty,
                $"Form '{form.Name}' has a field for '{field.Property}', which is not a property of command '{command.Name}'",
                field.Location);
        }
    }

    static IEnumerable<ScreenDirectiveSyntax> NestedDirectives(ScreenDirectiveSyntax directive) => directive switch
    {
        ScreenTemplateReferenceSyntax template => template.Slots.Cast<ScreenDirectiveSyntax>(),
        ScreenSlotSyntax slot => slot.Directives,
        ScreenSectionSyntax section => section.Directives,
        _ => []
    };

    // A navigate hangs off an action or a table row rather than standing on its own.
    static IEnumerable<ScreenDirectiveSyntax> NavigationFollowers(ScreenDirectiveSyntax directive) => directive switch
    {
        ScreenActionSyntax { Navigate: { } afterAction } => [afterAction],
        ScreenGuardedActionSyntax { Navigate: { } afterGuardedAction } => [afterGuardedAction],
        ScreenTableSyntax { RowClick: { } onRowClick } => [onRowClick],
        _ => []
    };
}
