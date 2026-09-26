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
        return PrintComments(application, writer);
    }

    /// <inheritdoc/>
    public string Print(ProjectionSyntax projection)
    {
        var writer = new ScreenplayWriter();
        WriteProjection(writer, projection);
        return PrintComments(projection, writer);
    }

    /// <inheritdoc/>
    public string Print(SpecificationSyntax specification)
    {
        var writer = new ScreenplayWriter();
        WriteSpecification(writer, specification);
        return PrintComments(specification, writer);
    }

    /// <inheritdoc/>
    public string Print(CaptureSyntax capture)
    {
        var writer = new ScreenplayWriter();
        WriteCapture(writer, capture);
        return PrintComments(capture, writer);
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
        using var anchor = writer.Anchor(application);
        if (application.Domain is not null)
        {
            writer.Line($"domain {application.Domain.Name}", application.Domain);
            writer.Blank();
        }

        foreach (var import in application.Imports)
        {
            writer.Line($"import {import.QualifiedName}", import);
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

        foreach (var behavior in application.Behaviors)
        {
            writer.Blank();
            WriteBehavior(writer, behavior);
        }

        foreach (var layout in application.Layouts ?? [])
        {
            writer.Blank();
            WriteLayout(writer, layout);
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
        using var anchor = writer.Anchor(authentication);
        writer.Line("authentication");
        using (writer.Indent())
        {
            foreach (var provider in authentication.Providers)
            {
                writer.Line(provider.Alias is null ? $"provider {provider.Name}" : $"provider {provider.Name} name {provider.Alias}", provider);
            }
        }
    }

    void WriteUiProfile(ScreenplayWriter writer, UiProfileSyntax uiProfile)
    {
        using var anchor = writer.Anchor(uiProfile);
        writer.Line($"ui profile {uiProfile.Name}");
        using (writer.Indent())
        {
            var platforms = uiProfile.Platforms.ToList();
            if (platforms.Count > 0)
            {
                writer.DirectiveLine($"target platform {string.Join(", ", platforms)}", uiProfile, "target platform");
            }

            if (uiProfile.DefaultSizeClass is not null)
            {
                writer.DirectiveLine($"target size {uiProfile.DefaultSizeClass}", uiProfile, "target size");
            }

            var packages = uiProfile.Packages.ToList();
            if (packages.Count > 0)
            {
                writer.Blank();
                writer.DirectiveLine("packages", uiProfile, "packages");
                using (writer.Indent())
                {
                    for (var index = 0; index < packages.Count; index++)
                    {
                        writer.DirectiveLine(packages[index], uiProfile, $"package:{index}");
                    }
                }
            }

            if (uiProfile.Layout is not null || uiProfile.Theme is not null)
            {
                writer.Blank();
            }

            if (uiProfile.Layout is not null)
            {
                writer.DirectiveLine($"layout {uiProfile.Layout}", uiProfile, "layout");
            }

            if (uiProfile.Theme is not null)
            {
                writer.DirectiveLine($"theme {uiProfile.Theme}", uiProfile, "theme");
            }
        }
    }

    void WriteTheme(ScreenplayWriter writer, ThemeSyntax theme)
    {
        using var anchor = writer.Anchor(theme);
        writer.Line($"theme {theme.Name}");
        using (writer.Indent())
        {
            var packages = theme.CompatibleWith.ToList();
            for (var index = 0; index < packages.Count; index++)
            {
                writer.DirectiveLine($"compatible with {packages[index]}", theme, $"compatible:{index}");
            }
        }
    }

    void WriteTrigger(ScreenplayWriter writer, TriggerSyntax trigger)
    {
        using var anchor = writer.Anchor(trigger);
        writer.Line($"trigger {trigger.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, trigger.Description);
            WriteFile(writer, trigger.File);

            foreach (var datum in trigger.Data)
            {
                var name = ReservedWords.Escape(datum.Name, ReservedWords.TriggerBody);
                writer.Line(datum.Type is null ? name : $"{name} {ScreenplaySyntaxText.TypeRef(datum.Type)}", datum);
            }
        }
    }

    void WriteReadModel(ScreenplayWriter writer, ReadModelSyntax readModel)
    {
        using var anchor = writer.Anchor(readModel);
        writer.Line($"readmodel {readModel.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, readModel.Description);
            WriteFile(writer, readModel.File);
            WriteProperties(writer, readModel.Properties, ReservedWords.ReadModelBody);
        }
    }

    void WriteReducer(ScreenplayWriter writer, ReducerSyntax reducer)
    {
        using var anchor = writer.Anchor(reducer);
        writer.Line($"reducer {reducer.Name} => {reducer.ReadModel}");
        using (writer.Indent())
        {
            WriteDescription(writer, reducer.Description);

            foreach (var rule in reducer.Rules)
            {
                writer.Line($"on {rule.Event}", rule);

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
                        writer.Line($"file {rule.File.Path}", rule.File);
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
        using var anchor = writer.Anchor(seed);
        writer.Line("seed");
        using (writer.Indent())
        {
            foreach (var group in seed.Groups)
            {
                writer.Line($"for {StringLiteral.Quote(group.EventSourceId)}", group);
                using (writer.Indent())
                {
                    foreach (var @event in group.Events)
                    {
                        writer.Line(@event.Event, @event);
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
        using var anchor = writer.Anchor(concept);
        var attributes = concept.Attributes.ToList();
        writer.Line($"concept {concept.Name} : {concept.Type}{string.Concat(attributes.Select(attribute => $" @{attribute.Name}"))}");
        var validations = concept.Validations?.ToList() ?? [];
        var reasoned = attributes.Where(attribute => attribute.Reason is not null).ToList();
        if (!concept.IsEnum && validations.Count == 0 && reasoned.Count == 0 && concept.File is null)
        {
            return;
        }

        using (writer.Indent())
        {
            WriteFile(writer, concept.File);

            foreach (var attribute in reasoned)
            {
                writer.DirectiveLine($"{attribute.Name} reason {StringLiteral.Quote(attribute.Reason!)}", concept, $"reason:{attribute.Name}");
            }

            if (concept.IsEnum)
            {
                var values = concept.Values.ToList();
                for (var index = 0; index < values.Count; index++)
                {
                    writer.DirectiveLine(ReservedWords.Escape(values[index], ReservedWords.ConceptBody), concept, $"value:{index}");
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
        using var anchor = writer.Anchor(type);
        writer.Line($"type {type.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, type.Description);
            WriteFile(writer, type.File);
            WriteProperties(writer, type.Properties, ReservedWords.None);
        }
    }

    void WritePolicy(ScreenplayWriter writer, PolicySyntax policy)
    {
        using var anchor = writer.Anchor(policy);
        writer.Line($"policy {policy.Name}");
        using (writer.Indent())
        {
            if (policy.Condition is not null)
            {
                writer.Line($"require {ScreenplaySyntaxText.PolicyCondition(policy.Condition)}", policy.Condition);
            }

            if (policy.File is not null)
            {
                WriteFile(writer, policy.File);
                if (policy.Code is not null)
                {
                    WriteOmittedCode(writer, policy.Code, ReadsOneImplementation("a policy"));
                }
            }
            else if (policy.Code is not null)
            {
                WriteCodeBlock(writer, policy.Code);
            }
        }
    }

    void WritePersona(ScreenplayWriter writer, PersonaSyntax persona)
    {
        using var anchor = writer.Anchor(persona);
        writer.Line($"persona {persona.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, persona.Description);

            var policies = persona.Policies.ToList();
            for (var index = 0; index < policies.Count; index++)
            {
                writer.DirectiveLine($"policy {policies[index]}", persona, $"policy:{index}");
            }
        }
    }

    void WriteModule(ScreenplayWriter writer, ModuleSyntax module)
    {
        using var anchor = writer.Anchor(module);
        writer.Line($"module {module.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, module.Description);
            var members = new List<PrintableMember>();
            if (module.Authorize is not null)
            {
                AddMembers(members, [module.Authorize], 0, authorize => WriteAuthorize(writer, authorize));
            }

            AddMembers(members, module.Behaviors, 1, behavior => WriteAttachedBehavior(writer, behavior));
            AddMembers(members, module.UsedBehaviors, 2, uses => WriteUsesBehavior(writer, uses));
            AddSeparatedMembers(members, writer, module.ScreenTemplates, 3, WriteScreenTemplate);
            AddSeparatedMembers(members, writer, module.DialogTemplates ?? [], 4, WriteDialogTemplate);
            AddSeparatedMembers(members, writer, module.Forms ?? [], 5, WriteForm);
            AddSeparatedMembers(members, writer, module.Contributions ?? [], 6, WriteContribution);
            AddSeparatedMembers(members, writer, module.Features, 7, WriteFeature);
            WriteMembers(members);
        }
    }

    void WriteContribution(ScreenplayWriter writer, ContributionSyntax contribution)
    {
        using var anchor = writer.Anchor(contribution);
        writer.Line($"contribute to {contribution.ContributionPoint}");
        using (writer.Indent())
        {
            if (contribution.Navigate is not null)
            {
                writer.Line(WriteScreenNavigate(contribution.Navigate), contribution.Navigate);
            }

            if (contribution.Label is not null)
            {
                writer.DirectiveLine($"label {ScreenplaySyntaxText.LocalizableString(contribution.Label)}", contribution, "label");
            }

            if (contribution.Order is not null)
            {
                writer.DirectiveLine($"order {contribution.Order}", contribution, "order");
            }
        }
    }

    void WriteForm(ScreenplayWriter writer, FormSyntax form)
    {
        using var anchor = writer.Anchor(form);
        writer.Line($"form {form.Name} for {form.For}");
        using (writer.Indent())
        {
            if (form.Populate is not null)
            {
                writer.Line(WriteFormPopulate(form.Populate), form.Populate);
            }

            foreach (var field in form.Fields)
            {
                writer.Line(WriteFormField(field), field);
            }

            if (form.OnSubmit is not null)
            {
                writer.Line($"on submit {WriteScreenNavigate(form.OnSubmit)}", form.OnSubmit);
            }

            WriteAttachments(writer, form.Behaviors, form.UsedBehaviors);
        }
    }

    string WriteFormPopulate(FormPopulateSource populate) => populate switch
    {
        FormPopulateViaQuerySyntax viaQuery => viaQuery.By is null
            ? $"populate via query {viaQuery.Query}"
            : $"populate via query {viaQuery.Query} by {viaQuery.By}",
        FormPopulateFromItemSyntax => "populate from item",
        _ => throw new UnsupportedSyntaxForPrinting("form populate source", populate.GetType().Name)
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
        using var anchor = writer.Anchor(layout);
        writer.Line($"layout {layout.Name}");
        using (writer.Indent())
        {
            WriteSlots(writer, layout.Slots);
            WriteArrangement(writer, layout.Arrangement);
            WriteAttachments(writer, layout.Behaviors, layout.UsedBehaviors);
        }
    }

    void WriteScreenTemplate(ScreenplayWriter writer, ScreenTemplateSyntax template)
    {
        using var anchor = writer.Anchor(template);
        writer.Line($"screen template {template.Name}");
        using (writer.Indent())
        {
            if (template.FitsSlot is not null)
            {
                writer.FitsSlotLine($"fits slot {template.FitsSlot}", template);
                writer.Blank();
            }

            WriteSlots(writer, template.Slots);
            WriteArrangement(writer, template.Arrangement);
            WriteAttachments(writer, template.Behaviors, template.UsedBehaviors);
        }
    }

    void WriteDialogTemplate(ScreenplayWriter writer, DialogTemplateSyntax template)
    {
        using var anchor = writer.Anchor(template);
        writer.Line($"dialog template {template.Name}");
        using (writer.Indent())
        {
            WriteSlots(writer, template.Slots);
            WriteArrangement(writer, template.Arrangement);
            WriteAttachments(writer, template.Behaviors, template.UsedBehaviors);
        }
    }

    /// <summary>
    /// Writes every slot the structure declares, one per line.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> to write to.</param>
    /// <param name="slots">The <see cref="SlotSyntax">slots</see> to write.</param>
    /// <remarks>
    /// Every slot is written, including one only an arrangement named - so a printed document always states
    /// its slots explicitly, and says each of them exactly once.
    /// </remarks>
    void WriteSlots(ScreenplayWriter writer, IEnumerable<SlotSyntax> slots)
    {
        foreach (var slot in slots)
        {
            writer.Line(slot.Contributes is null ? slot.Name : $"{slot.Name} contributes {slot.Contributes}", slot);
        }
    }

    void WriteArrangement(ScreenplayWriter writer, ArrangementSyntax? arrangement)
    {
        if (arrangement is null)
        {
            return;
        }

        using var anchor = writer.Anchor(arrangement);

        writer.Blank();
        writer.Line(arrangement.Mode == ArrangementMode.Freeform ? "arrangement freeform" : "arrangement flow");
        using (writer.Indent())
        {
            if (arrangement.Mode == ArrangementMode.Freeform)
            {
                var first = true;
                foreach (var variant in arrangement.Variants ?? [])
                {
                    if (!first)
                    {
                        writer.Blank();
                    }

                    first = false;
                    WriteVariant(writer, variant);
                }

                return;
            }

            if (arrangement.Root is not null)
            {
                WriteArrangementChildren(writer, arrangement.Root);
            }

            foreach (var arrangementOverride in arrangement.Overrides ?? [])
            {
                writer.Blank();
                WriteArrangementOverride(writer, arrangementOverride);
            }
        }
    }

    void WriteArrangementChildren(ScreenplayWriter writer, ArrangementNodeSyntax node)
    {
        if (node is ArrangementContainerSyntax { Kind: ArrangementContainerKind.Flat } flat)
        {
            foreach (var child in flat.Children)
            {
                WriteArrangementNode(writer, child);
            }

            return;
        }

        WriteArrangementNode(writer, node);
    }

    void WriteArrangementNode(ScreenplayWriter writer, ArrangementNodeSyntax node)
    {
        using var anchor = writer.Anchor(node);
        switch (node)
        {
            case ArrangementSlotSyntax slot:
                writer.Line(WriteArrangementSlotLine(slot));
                break;
            case ArrangementContainerSyntax container:
                var keyword = container.Kind switch
                {
                    ArrangementContainerKind.Row => "row",
                    ArrangementContainerKind.Column => "column",
                    ArrangementContainerKind.Grid => "grid",
                    _ => throw new UnsupportedSyntaxForPrinting("nested arrangement container kind", container.Kind.ToString()),
                };
                writer.Line(container.Gap is null ? keyword : $"{keyword} gap {container.Gap}");
                using (writer.Indent())
                {
                    foreach (var child in container.Children)
                    {
                        WriteArrangementNode(writer, child);
                    }
                }

                break;
            default:
                throw new UnsupportedSyntaxForPrinting("arrangement node", node.GetType().Name);
        }
    }

    void WriteArrangementOverride(ScreenplayWriter writer, ArrangementOverrideSyntax arrangementOverride)
    {
        using var anchor = writer.Anchor(arrangementOverride);
        writer.Line($"when {WriteOverrideCondition(arrangementOverride)}");
        using (writer.Indent())
        {
            WriteArrangementChildren(writer, arrangementOverride.Root);
        }
    }

    void WriteVariant(ScreenplayWriter writer, VariantSyntax variant)
    {
        using var anchor = writer.Anchor(variant);
        writer.Line($"variant width {variant.Width}, height {variant.Height}");
        using (writer.Indent())
        {
            foreach (var place in variant.Places)
            {
                writer.Line(WritePlaceLine(place), place);
            }
        }
    }

    string WriteArrangementSlotLine(ArrangementSlotSyntax slot)
    {
        var line = slot.Name;
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

    string WriteOverrideCondition(ArrangementOverrideSyntax arrangementOverride)
    {
        if (arrangementOverride.Width is not null && arrangementOverride.Height is not null)
        {
            return $"width {arrangementOverride.Width}, height {arrangementOverride.Height}";
        }

        if (arrangementOverride.Width is not null)
        {
            return $"width {arrangementOverride.Width}";
        }

        return $"height {arrangementOverride.Height}";
    }

    string WritePlaceLine(PlaceSyntax place) =>
        place.Hidden
            ? $"place {place.SlotName} hidden"
            : $"place {place.SlotName} at {place.X},{place.Y} size {place.SizeWidth},{place.SizeHeight}";

    void WriteFeature(ScreenplayWriter writer, FeatureSyntax feature)
    {
        using var anchor = writer.Anchor(feature);
        writer.Line($"feature {feature.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, feature.Description);
            var members = new List<PrintableMember>();
            if (feature.Authorize is not null)
            {
                AddMembers(members, [feature.Authorize], 0, authorize => WriteAuthorize(writer, authorize));
            }

            AddMembers(members, feature.Behaviors, 1, behavior => WriteAttachedBehavior(writer, behavior));
            AddMembers(members, feature.UsedBehaviors, 2, uses => WriteUsesBehavior(writer, uses));
            AddSeparatedMembers(members, writer, feature.Features, 3, WriteFeature);
            AddSeparatedMembers(members, writer, feature.Slices, 4, WriteSlice);
            AddSeparatedMembers(members, writer, feature.Contributions ?? [], 5, WriteContribution);
            WriteMembers(members);
        }
    }

    void WriteSlice(ScreenplayWriter writer, SliceSyntax slice)
    {
        using var anchor = writer.Anchor(slice);
        writer.Line($"slice {slice.Type} {slice.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, slice.Description);
            WriteFile(writer, slice.File);

            var members = new List<PrintableMember>();
            AddSeparatedMembers(members, writer, slice.Commands, 0, WriteCommand);
            AddSeparatedMembers(members, writer, slice.Events, 1, WriteEvent);
            AddSeparatedMembers(members, writer, slice.Constraints, 2, WriteConstraint);
            AddSeparatedMembers(members, writer, slice.Queries, 3, WriteQuery);

            // A read model comes before whatever builds it - the shape first, then where it comes from.
            AddSeparatedMembers(members, writer, slice.ReadModels ?? [], 4, WriteReadModel);
            AddSeparatedMembers(members, writer, slice.Projections, 5, WriteProjection);
            AddSeparatedMembers(members, writer, slice.Reducers ?? [], 6, WriteReducer);
            AddSeparatedMembers(members, writer, slice.Captures, 7, WriteCapture);
            AddSeparatedMembers(members, writer, slice.Reactions, 8, WriteReaction);
            AddSeparatedMembers(members, writer, slice.Screens, 9, WriteScreen);
            AddSeparatedMembers(members, writer, slice.Specifications, 10, WriteSpecification);
            WriteMembers(members);
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

    // A declaration's file reference is printed directly under its header, right after the description, because
    // it belongs to the declaration itself rather than to any of the members that follow it.
    void WriteFile(ScreenplayWriter writer, FileReferenceSyntax? file)
    {
        if (file is not null)
        {
            writer.Line($"file {file.Path}", file);
        }
    }
}
