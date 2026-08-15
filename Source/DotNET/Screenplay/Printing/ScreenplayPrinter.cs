// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Represents an implementation of <see cref="IScreenplayPrinter"/> that renders a Screenplay syntax tree
/// back to <c>.play</c> source text.
/// </summary>
/// <remarks>
/// The printer walks the tree recursively, tracking indentation with a <see cref="ScreenplayWriter"/> and
/// inverting the exact keyword and expression forms the parsers accept. It also implements the syntax
/// visitor interfaces so it composes with <see cref="IScreenplayCompiler"/>'s visitor overloads.
/// </remarks>
public sealed partial class ScreenplayPrinter :
    IScreenplayPrinter,
    IApplicationSyntaxVisitor<string>,
    IProjectionSyntaxVisitor<string>,
    ISpecificationSyntaxVisitor<string>,
    ICaptureSyntaxVisitor<string>
{
    /// <inheritdoc/>
    public string Print(ApplicationSyntax application)
    {
        var writer = new ScreenplayWriter();
        WriteApplication(writer, application);
        return writer.ToString();
    }

    /// <inheritdoc/>
    public string Print(ProjectionSyntax projection)
    {
        var writer = new ScreenplayWriter();
        WriteProjection(writer, projection);
        return writer.ToString();
    }

    /// <inheritdoc/>
    public string Print(SpecificationSyntax specification)
    {
        var writer = new ScreenplayWriter();
        WriteSpecification(writer, specification);
        return writer.ToString();
    }

    /// <inheritdoc/>
    public string Print(CaptureSyntax capture)
    {
        var writer = new ScreenplayWriter();
        WriteCapture(writer, capture);
        return writer.ToString();
    }

    /// <inheritdoc/>
    string IApplicationSyntaxVisitor<string>.Visit(ApplicationSyntax syntax) => Print(syntax);

    /// <inheritdoc/>
    string IProjectionSyntaxVisitor<string>.Visit(ProjectionSyntax syntax) => Print(syntax);

    /// <inheritdoc/>
    string ISpecificationSyntaxVisitor<string>.Visit(SpecificationSyntax syntax) => Print(syntax);

    /// <inheritdoc/>
    string ICaptureSyntaxVisitor<string>.Visit(CaptureSyntax syntax) => Print(syntax);

    void WriteApplication(ScreenplayWriter writer, ApplicationSyntax application)
    {
        if (application.Domain is not null)
        {
            writer.Line($"domain {application.Domain.Name}");
            writer.Blank();
        }

        foreach (var import in application.Imports)
        {
            writer.Line($"import {import.QualifiedName}");
        }

        foreach (var concept in application.Concepts)
        {
            writer.Blank();
            WriteConcept(writer, concept);
        }

        foreach (var type in application.Types ?? [])
        {
            writer.Blank();
            WriteType(writer, type);
        }

        foreach (var policy in application.Policies)
        {
            writer.Blank();
            WritePolicy(writer, policy);
        }

        foreach (var persona in application.Personas ?? [])
        {
            writer.Blank();
            WritePersona(writer, persona);
        }

        if (application.Authentication is not null)
        {
            writer.Blank();
            WriteAuthentication(writer, application.Authentication);
        }

        foreach (var theme in application.Themes ?? [])
        {
            writer.Blank();
            WriteTheme(writer, theme);
        }

        foreach (var trigger in application.Triggers ?? [])
        {
            writer.Blank();
            WriteTrigger(writer, trigger);
        }

        foreach (var uiProfile in application.UiProfiles ?? [])
        {
            writer.Blank();
            WriteUiProfile(writer, uiProfile);
        }

        foreach (var module in application.Modules)
        {
            writer.Blank();
            WriteModule(writer, module);
        }

        foreach (var seed in application.Seeds ?? [])
        {
            writer.Blank();
            WriteSeed(writer, seed);
        }
    }

    void WriteAuthentication(ScreenplayWriter writer, AuthenticationSyntax authentication)
    {
        writer.Line("authentication");
        using (writer.Indent())
        {
            foreach (var provider in authentication.Providers)
            {
                writer.Line(provider.Alias is null ? $"provider {provider.Name}" : $"provider {provider.Name} name {provider.Alias}");
            }
        }
    }

    void WriteUiProfile(ScreenplayWriter writer, UiProfileSyntax uiProfile)
    {
        writer.Line($"ui profile {uiProfile.Name}");
        using (writer.Indent())
        {
            var platforms = uiProfile.Platforms.ToList();
            if (platforms.Count > 0)
            {
                writer.Line($"target platform {string.Join(", ", platforms)}");
            }

            if (uiProfile.DefaultSizeClass is not null)
            {
                writer.Line($"target size {uiProfile.DefaultSizeClass}");
            }

            var packages = uiProfile.Packages.ToList();
            if (packages.Count > 0)
            {
                writer.Blank();
                writer.Line("packages");
                using (writer.Indent())
                {
                    foreach (var package in packages)
                    {
                        writer.Line(package);
                    }
                }
            }

            if (uiProfile.Theme is not null)
            {
                writer.Blank();
                writer.Line($"theme {uiProfile.Theme}");
            }
        }
    }

    void WriteTheme(ScreenplayWriter writer, ThemeSyntax theme)
    {
        writer.Line($"theme {theme.Name}");
        using (writer.Indent())
        {
            foreach (var package in theme.CompatibleWith)
            {
                writer.Line($"compatible with {package}");
            }
        }
    }

    void WriteTrigger(ScreenplayWriter writer, TriggerSyntax trigger)
    {
        writer.Line($"trigger {trigger.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, trigger.Description);

            foreach (var datum in trigger.Data)
            {
                var name = ReservedWords.Escape(datum.Name, ReservedWords.TriggerBody);
                writer.Line(datum.Type is null ? name : $"{name} {ScreenplaySyntaxText.TypeRef(datum.Type)}");
            }
        }
    }

    void WriteReadModel(ScreenplayWriter writer, ReadModelSyntax readModel)
    {
        writer.Line($"readmodel {readModel.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, readModel.Description);
            WriteProperties(writer, readModel.Properties, ReservedWords.ReadModelBody);
        }
    }

    void WriteReducer(ScreenplayWriter writer, ReducerSyntax reducer)
    {
        writer.Line($"reducer {reducer.Name} => {reducer.ReadModel}");
        using (writer.Indent())
        {
            WriteDescription(writer, reducer.Description);

            foreach (var rule in reducer.Rules)
            {
                writer.Line($"on {rule.Event}");

                // A rule that only names its event is complete on its own, as a reaction trigger is.
                if (rule is { Description: null, File: null, Code: null })
                {
                    continue;
                }

                using (writer.Indent())
                {
                    WriteDescription(writer, rule.Description);

                    if (rule.File is not null)
                    {
                        writer.Line($"file {rule.File.Path}");
                    }

                    if (rule.Code is not null)
                    {
                        WriteCodeBlock(writer, rule.Code);
                    }
                }
            }
        }
    }

    void WriteSeed(ScreenplayWriter writer, SeedSyntax seed)
    {
        writer.Line("seed");
        using (writer.Indent())
        {
            foreach (var group in seed.Groups)
            {
                writer.Line($"for {StringLiteral.Quote(group.EventSourceId)}");
                using (writer.Indent())
                {
                    foreach (var @event in group.Events)
                    {
                        writer.Line(@event.Event);
                        using (writer.Indent())
                        {
                            WriteMappings(writer, @event.Properties, ReservedWords.None);
                        }
                    }
                }
            }
        }
    }

    void WriteConcept(ScreenplayWriter writer, ConceptSyntax concept)
    {
        var attributes = concept.Attributes.ToList();
        writer.Line($"concept {concept.Name} : {concept.Type}{string.Concat(attributes.Select(attribute => $" @{attribute.Name}"))}");
        var validations = concept.Validations?.ToList() ?? [];
        var reasoned = attributes.Where(attribute => attribute.Reason is not null).ToList();
        if (!concept.IsEnum && validations.Count == 0 && reasoned.Count == 0)
        {
            return;
        }

        using (writer.Indent())
        {
            foreach (var attribute in reasoned)
            {
                writer.Line($"{attribute.Name} reason {StringLiteral.Quote(attribute.Reason!)}");
            }

            if (concept.IsEnum)
            {
                foreach (var value in concept.Values)
                {
                    writer.Line(ReservedWords.Escape(value, ReservedWords.ConceptBody));
                }
            }

            foreach (var validation in validations)
            {
                WriteValidate(writer, validation, impliedSubject: true);
            }
        }
    }

    void WriteType(ScreenplayWriter writer, TypeSyntax type)
    {
        writer.Line($"type {type.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, type.Description);
            WriteProperties(writer, type.Properties, ReservedWords.None);
        }
    }

    void WritePolicy(ScreenplayWriter writer, PolicySyntax policy)
    {
        writer.Line($"policy {policy.Name}");
        using (writer.Indent())
        {
            if (policy.Condition is not null)
            {
                writer.Line($"require {ScreenplaySyntaxText.PolicyCondition(policy.Condition)}");
            }

            if (policy.Code is not null)
            {
                WriteCodeBlock(writer, policy.Code);
            }
        }
    }

    void WritePersona(ScreenplayWriter writer, PersonaSyntax persona)
    {
        writer.Line($"persona {persona.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, persona.Description);

            foreach (var policy in persona.Policies)
            {
                writer.Line($"policy {policy}");
            }
        }
    }

    void WriteModule(ScreenplayWriter writer, ModuleSyntax module)
    {
        writer.Line($"module {module.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, module.Description);

            foreach (var layout in module.Layouts)
            {
                writer.Blank();
                WriteLayout(writer, layout);
            }

            foreach (var form in module.Forms ?? [])
            {
                writer.Blank();
                WriteForm(writer, form);
            }

            foreach (var contribution in module.Contributions ?? [])
            {
                writer.Blank();
                WriteContribution(writer, contribution);
            }

            foreach (var feature in module.Features)
            {
                writer.Blank();
                WriteFeature(writer, feature);
            }
        }
    }

    void WriteContribution(ScreenplayWriter writer, ContributionSyntax contribution)
    {
        writer.Line($"contribute to {contribution.ContributionPoint}");
        using (writer.Indent())
        {
            if (contribution.Navigate is not null)
            {
                writer.Line(WriteScreenNavigate(contribution.Navigate));
            }

            if (contribution.Label is not null)
            {
                writer.Line($"label {ScreenplaySyntaxText.LocalizableString(contribution.Label)}");
            }

            if (contribution.Order is not null)
            {
                writer.Line($"order {contribution.Order}");
            }
        }
    }

    void WriteForm(ScreenplayWriter writer, FormSyntax form)
    {
        writer.Line($"form {form.Name} for {form.For}");
        using (writer.Indent())
        {
            if (form.Populate is not null)
            {
                writer.Line(WriteFormPopulate(form.Populate));
            }

            foreach (var field in form.Fields)
            {
                writer.Line(WriteFormField(field));
            }

            if (form.OnSubmit is not null)
            {
                writer.Line($"on submit {WriteScreenNavigate(form.OnSubmit)}");
            }
        }
    }

    string WriteFormPopulate(FormPopulateSource populate) => populate switch
    {
        FormPopulateViaQuerySyntax viaQuery => viaQuery.By is null
            ? $"populate via query {viaQuery.Query}"
            : $"populate via query {viaQuery.Query} by {viaQuery.By}",
        FormPopulateFromItemSyntax => "populate from item",
        _ => string.Empty
    };

    string WriteFormField(FormFieldSyntax field)
    {
        var head = $"field {field.Property}";
        if (field.From is not null)
        {
            head += $" from {field.From}";
        }
        else if (field.ComposeUsing is not null)
        {
            head += $" compose using {field.ComposeUsing}";
        }

        if (field.Label is not null)
        {
            head += $" label {ScreenplaySyntaxText.LocalizableString(field.Label)}";
        }

        return head;
    }

    void WriteLayout(ScreenplayWriter writer, LayoutSyntax layout)
    {
        writer.Line($"layout {layout.Name}");
        using (writer.Indent())
        {
            if (layout.Arrangement == LayoutArrangement.Freeform)
            {
                writer.Line("arrangement freeform");

                foreach (var variant in layout.Variants ?? [])
                {
                    writer.Blank();
                    WriteVariant(writer, variant);
                }

                return;
            }

            if (layout.Template is null)
            {
                return;
            }

            writer.Line("template");
            using (writer.Indent())
            {
                WriteTemplateChildren(writer, layout.Template.Root);

                foreach (var templateOverride in layout.Template.Overrides)
                {
                    writer.Blank();
                    WriteTemplateOverride(writer, templateOverride);
                }
            }
        }
    }

    void WriteTemplateChildren(ScreenplayWriter writer, TemplateNodeSyntax node)
    {
        if (node is TemplateContainerSyntax { Kind: TemplateContainerKind.Flat } flat)
        {
            foreach (var child in flat.Children)
            {
                WriteTemplateNode(writer, child);
            }

            return;
        }

        WriteTemplateNode(writer, node);
    }

    void WriteTemplateNode(ScreenplayWriter writer, TemplateNodeSyntax node)
    {
        switch (node)
        {
            case TemplateSlotSyntax slot:
                writer.Line(WriteTemplateSlotLine(slot));
                break;
            case TemplateContainerSyntax container:
                var keyword = container.Kind switch
                {
                    TemplateContainerKind.Row => "row",
                    TemplateContainerKind.Column => "column",
                    TemplateContainerKind.Grid => "grid",
                    _ => string.Empty,
                };
                writer.Line(container.Gap is null ? keyword : $"{keyword} gap {container.Gap}");
                using (writer.Indent())
                {
                    foreach (var child in container.Children)
                    {
                        WriteTemplateNode(writer, child);
                    }
                }

                break;
        }
    }

    void WriteTemplateOverride(ScreenplayWriter writer, TemplateOverrideSyntax templateOverride)
    {
        writer.Line($"when {WriteOverrideCondition(templateOverride)}");
        using (writer.Indent())
        {
            WriteTemplateChildren(writer, templateOverride.Root);
        }
    }

    void WriteVariant(ScreenplayWriter writer, VariantSyntax variant)
    {
        writer.Line($"variant width {variant.Width}, height {variant.Height}");
        using (writer.Indent())
        {
            foreach (var place in variant.Places)
            {
                writer.Line(WritePlaceLine(place));
            }
        }
    }

    string WriteTemplateSlotLine(TemplateSlotSyntax slot)
    {
        var line = slot.Name;
        if (slot.Contributes is not null)
        {
            line += $" contributes {slot.Contributes}";
        }

        if (slot.Width is not null)
        {
            line += $" width {slot.Width}";
        }

        if (slot.Height is not null)
        {
            line += $" height {slot.Height}";
        }

        if (slot.Grow)
        {
            line += " grow";
        }

        if (slot.Span is not null)
        {
            line += $" span {slot.Span}";
        }

        return line;
    }

    string WriteOverrideCondition(TemplateOverrideSyntax templateOverride)
    {
        if (templateOverride.Width is not null && templateOverride.Height is not null)
        {
            return $"width {templateOverride.Width}, height {templateOverride.Height}";
        }

        if (templateOverride.Width is not null)
        {
            return $"width {templateOverride.Width}";
        }

        return $"height {templateOverride.Height}";
    }

    string WritePlaceLine(PlaceSyntax place) =>
        place.Hidden
            ? $"place {place.SlotName} hidden"
            : $"place {place.SlotName} at {place.X},{place.Y} size {place.SizeWidth},{place.SizeHeight}";

    void WriteFeature(ScreenplayWriter writer, FeatureSyntax feature)
    {
        writer.Line($"feature {feature.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, feature.Description);

            foreach (var nested in feature.Features)
            {
                writer.Blank();
                WriteFeature(writer, nested);
            }

            foreach (var slice in feature.Slices)
            {
                writer.Blank();
                WriteSlice(writer, slice);
            }

            foreach (var contribution in feature.Contributions ?? [])
            {
                writer.Blank();
                WriteContribution(writer, contribution);
            }
        }
    }

    void WriteSlice(ScreenplayWriter writer, SliceSyntax slice)
    {
        writer.Line($"slice {slice.Type} {slice.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, slice.Description);

            foreach (var command in slice.Commands)
            {
                writer.Blank();
                WriteCommand(writer, command);
            }

            foreach (var @event in slice.Events)
            {
                writer.Blank();
                WriteEvent(writer, @event);
            }

            foreach (var constraint in slice.Constraints)
            {
                writer.Blank();
                WriteConstraint(writer, constraint);
            }

            foreach (var query in slice.Queries)
            {
                writer.Blank();
                WriteQuery(writer, query);
            }

            // A read model comes before whatever builds it - the shape first, then where it comes from.
            foreach (var readModel in slice.ReadModels ?? [])
            {
                writer.Blank();
                WriteReadModel(writer, readModel);
            }

            foreach (var projection in slice.Projections)
            {
                writer.Blank();
                WriteProjection(writer, projection);
            }

            foreach (var reducer in slice.Reducers ?? [])
            {
                writer.Blank();
                WriteReducer(writer, reducer);
            }

            foreach (var capture in slice.Captures)
            {
                writer.Blank();
                WriteCapture(writer, capture);
            }

            foreach (var reaction in slice.Reactions)
            {
                writer.Blank();
                WriteReaction(writer, reaction);
            }

            foreach (var screen in slice.Screens)
            {
                writer.Blank();
                WriteScreen(writer, screen);
            }

            foreach (var specification in slice.Specifications)
            {
                writer.Blank();
                WriteSpecification(writer, specification);
            }
        }
    }

    void WriteDescription(ScreenplayWriter writer, string? description)
    {
        if (description is null)
        {
            return;
        }

        if (!description.Contains('\n'))
        {
            writer.Line($"description {StringLiteral.Quote(description)}");
            return;
        }

        writer.Line("description");
        using (writer.Indent())
        {
            WriteFencedText(writer, description);
        }
    }
}
