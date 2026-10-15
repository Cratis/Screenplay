// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
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
    /// <exception cref="Syntax.Serialization.InvalidSyntaxJson">A command contains ambiguous or duplicate stream headers, or an invalid stream candidate structure.</exception>
    public string Print(ApplicationSyntax application)
    {
        ValidateEventSourceExport(application);
        var writer = DocumentWriter(application);
        WriteApplication(writer, application);
        return PrintComments(application, writer);
    }

    /// <inheritdoc/>
    public string Print(ProjectionSyntax projection)
    {
        var writer = DocumentWriter(projection);
        WriteProjection(writer, projection);
        return PrintComments(projection, writer);
    }

    /// <inheritdoc/>
    public string Print(SpecificationSyntax specification)
    {
        var writer = DocumentWriter(specification);
        foreach (var example in specification.Examples)
        {
            WriteSpecificationExample(writer, example);
            writer.Blank();
        }
        WriteSpecification(writer, specification);
        return PrintComments(specification, writer);
    }

    /// <inheritdoc/>
    public string Print(CaptureSyntax capture)
    {
        var writer = DocumentWriter(capture);
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

    static ScreenplayWriter DocumentWriter(SyntaxNode root)
    {
        SourceNumericModes.Validate(root);
        var source = (ISourceSyntax)root;
        var writer = new ScreenplayWriter { NumericMode = source.SourceOptions.NumericMode };
        if (writer.NumericMode == NumericMode.Exact) writer.Line("numbers exact");
        return writer;
    }

    static void WriteSection<T>(ScreenplayWriter writer, IEnumerable<T> items, Action<ScreenplayWriter, T> write)
    {
        foreach (var item in items)
        {
            writer.Blank();
            write(writer, item);
        }
    }

    static void WriteOptionalSection<T>(ScreenplayWriter writer, T? item, Action<ScreenplayWriter, T> write)
        where T : class
    {
        if (item is not null)
        {
            writer.Blank();
            write(writer, item);
        }
    }

    static bool IsBareConcept(
        ConceptSyntax concept,
        List<ConceptAttributeSyntax> attributes,
        List<ValidateSyntax> validations,
        List<ConceptAttributeSyntax> reasoned) =>
        !concept.IsEnum && validations.Count == 0 && reasoned.Count == 0 && concept.Description is null && concept.File is null && concept.Generation is null &&
        !attributes.Exists(HasAttributeDirective);

    static bool HasAttributeDirective(ConceptAttributeSyntax attribute) =>
        attribute.Scope is not null || attribute.SpecialCategory is not null || attribute.Criminal;

    static string PackReference(SampleDataPackReferenceSyntax reference) => reference.IsBlueprint ? $"blueprint {reference.Name}" : reference.Name;

    static string SampleDataValue(SampleDataFieldSyntax field) => field.Kind switch
    {
        SampleDataValueKind.Text => StringLiteral.Quote(field.Value ?? string.Empty),
        SampleDataValueKind.Null => "null",
        _ => field.Value ?? string.Empty
    };

    void WriteApplication(ScreenplayWriter writer, ApplicationSyntax application)
    {
        using var anchor = writer.Anchor(application);
        WriteApplicationHeader(writer, application);
        WriteApplicationDeclarations(writer, application);
        WriteApplicationExperience(writer, application);
        WriteApplicationComposition(writer, application);
    }

    void WriteApplicationHeader(ScreenplayWriter writer, ApplicationSyntax application)
    {
        if (application.Domain is not null)
        {
            writer.Line($"domain {application.Domain.Name}", application.Domain);
            writer.Blank();
        }

        foreach (var import in application.Imports)
        {
            ImplementationInvariants.Validate(import);
            var origin = import.Origin is null ? string.Empty : $" from {StringLiteral.Quote(import.Origin)}";
            writer.Line($"import {import.QualifiedName}{origin}", import);
        }

        WriteFileImports(writer, application.FileImports);
    }

    void WriteApplicationDeclarations(ScreenplayWriter writer, ApplicationSyntax application)
    {
        WriteSection(writer, application.Systems, WriteSystem);
        WriteSection(writer, application.EventSources, WriteEventSource);
        WriteSection(writer, application.Concepts, WriteConcept);
        WriteSection(writer, application.Types ?? [], WriteType);
        WriteSection(writer, application.Purposes, WritePurpose);
        WriteSection(writer, application.Policies, WritePolicy);
        WriteSection(writer, application.Personas ?? [], WritePersona);
    }

    void WriteApplicationExperience(ScreenplayWriter writer, ApplicationSyntax application)
    {
        WriteOptionalSection(writer, application.Authentication, WriteAuthentication);
        WriteOptionalSection(writer, application.Identity, WriteIdentity);
        WriteSection(writer, application.Themes ?? [], WriteTheme);
        WriteSection(writer, application.SampleDataPacks, WriteSampleDataPack);
        WriteSection(writer, application.Triggers ?? [], WriteTrigger);
        WriteSection(writer, application.Behaviors, WriteBehavior);
        WriteSection(writer, application.Layouts ?? [], WriteLayout);
    }

    void WriteApplicationComposition(ScreenplayWriter writer, ApplicationSyntax application)
    {
        WriteSection(writer, application.Exposures, WriteExposure);
        WriteSection(writer, application.InstanceContributions, WriteInstanceContributions);
        WriteSection(writer, application.UiProfiles ?? [], WriteUiProfile);
        WriteSection(writer, application.Templates, WriteTemplateAssignment);
        WriteSection(writer, application.Examples, WriteSpecificationExample);
        WriteSection(writer, application.Modules, WriteModule);
        WriteSection(writer, application.Seeds ?? [], WriteSeed);
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

            WriteUiProfileList(writer, uiProfile, ("packages", "package"), [.. uiProfile.Packages]);
            WriteUiProfileList(writer, uiProfile, ("icons", "icon"), [.. uiProfile.Icons]);
            WriteUiProfileAppearance(writer, uiProfile);
            WriteSampleDataSelection(writer, uiProfile.SampleData);
        }
    }

    void WriteUiProfileList(ScreenplayWriter writer, UiProfileSyntax uiProfile, (string Heading, string ItemKey) names, List<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        writer.Blank();
        writer.DirectiveLine(names.Heading, uiProfile, names.Heading);
        using (writer.Indent())
        {
            for (var index = 0; index < items.Count; index++)
            {
                writer.DirectiveLine(items[index], uiProfile, DirectiveLocationKeys.ForValue(names.ItemKey, items, index));
            }
        }
    }

    void WriteUiProfileAppearance(ScreenplayWriter writer, UiProfileSyntax uiProfile)
    {
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

    void WriteSampleDataSelection(ScreenplayWriter writer, SampleDataSelectionSyntax? selection)
    {
        if (selection is null)
        {
            return;
        }

        writer.Blank();
        writer.Line("sample data", selection);
        using (writer.Indent())
        {
            if (selection.Active is not null)
            {
                writer.Line($"active {PackReference(selection.Active)}", selection.Active);
            }

            foreach (var mapping in selection.Mappings)
            {
                writer.Line($"shape {mapping.Shape} maps to {mapping.ReadModel}", mapping);
            }

            foreach (var binding in selection.Bindings)
            {
                writer.Line($"bind {binding.ReadModel} to {PackReference(binding.Pack)}", binding);
            }
        }
    }

    void WriteSampleDataPack(ScreenplayWriter writer, SampleDataPackSyntax pack)
    {
        using var anchor = writer.Anchor(pack);
        writer.Line($"sample data {pack.Name}");
        using (writer.Indent())
        {
            if (pack.Title is not null)
            {
                writer.Line($"title {StringLiteral.Quote(pack.Title)}");
            }

            if (pack.Locale is not null)
            {
                writer.Line($"locale {pack.Locale}");
            }

            foreach (var set in pack.Sets)
            {
                writer.Line($"shape {set.Shape}", set);
                using (writer.Indent())
                {
                    foreach (var row in set.Rows)
                    {
                        writer.Line("row", row);
                        using (writer.Indent())
                        {
                            foreach (var field in row.Fields)
                            {
                                writer.Line($"{field.Name} = {SampleDataValue(field)}", field);
                            }
                        }
                    }
                }
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
                writer.DirectiveLine($"compatible with {packages[index]}", theme, DirectiveLocationKeys.ForValue("compatible", packages, index));
            }
        }
    }

    void WriteTrigger(ScreenplayWriter writer, TriggerSyntax trigger)
    {
        using var anchor = writer.Anchor(trigger);
        writer.Line($"trigger {trigger.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, trigger.Description, trigger);
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
            WriteDescription(writer, readModel.Description, readModel);
            WriteDocumentation(writer, readModel.Documentation, readModel);
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
            WriteDescription(writer, reducer.Description, reducer);
            if (reducer.From is not null) WriteObserverFilter(writer, reducer.From);

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
                    WriteDescription(writer, rule.Description, rule);

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
        ImplementationInvariants.Validate(concept);
        using var anchor = writer.Anchor(concept);
        var attributes = concept.Attributes.ToList();
        writer.Line($"concept {concept.Name} : {concept.Type}{string.Concat(attributes.Select(attribute => $" {Parsing.ConceptComplianceParser.CanonicalName(attribute.Name)}"))}");
        var validations = concept.Validations?.ToList() ?? [];
        var reasoned = attributes.Where(attribute => attribute.Reason is not null).ToList();
        if (IsBareConcept(concept, attributes, validations, reasoned))
        {
            return;
        }

        using (writer.Indent())
        {
            WriteDescription(writer, concept.Description, concept);
            WriteFile(writer, concept.File);
            if (concept.Generation is { } generation) WriteGeneration(writer, generation);

            foreach (var attribute in reasoned)
            {
                writer.DirectiveLine($"{Parsing.ConceptComplianceParser.CanonicalName(attribute.Name)} reason {StringLiteral.Quote(attribute.Reason!)}", concept, $"reason:{attribute.Name}");
            }

            foreach (var attribute in attributes)
            {
                WriteAttributeDirectives(writer, concept, attribute);
            }

            if (concept.IsEnum)
            {
                WriteEnumValues(writer, concept);
            }

            foreach (var validation in validations)
            {
                WriteValidate(writer, validation, impliedSubject: true);
            }
        }
    }

    void WriteAttributeDirectives(ScreenplayWriter writer, ConceptSyntax concept, ConceptAttributeSyntax attribute)
    {
        if (attribute.Scope is not null) writer.DirectiveLine($"secret scope {attribute.Scope}", concept, "scope:sensitive");
        if (attribute.SpecialCategory is not null) writer.DirectiveLine($"pii special {attribute.SpecialCategory}", concept, "special:pii");
        if (attribute.Criminal) writer.DirectiveLine("pii criminal", concept, "criminal:pii");
    }

    void WriteEnumValues(ScreenplayWriter writer, ConceptSyntax concept)
    {
        var values = concept.Values.ToList();
        var numbers = concept.ValueNumbers?.ToArray();
        for (var index = 0; index < values.Count; index++)
        {
            var number = numbers is null ? string.Empty : $" = {numbers[index].ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            writer.DirectiveLine(ReservedWords.Escape(values[index], ReservedWords.ConceptBody) + number, concept, DirectiveLocationKeys.ForValue("value", values, index));
        }
    }

    void WriteType(ScreenplayWriter writer, TypeSyntax type)
    {
        using var anchor = writer.Anchor(type);
        writer.Line($"type {type.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, type.Description, type);
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
            WriteDescription(writer, policy.Description, policy);
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
            WriteDescription(writer, persona.Description, persona);

            var policies = persona.Policies.ToList();
            for (var index = 0; index < policies.Count; index++)
            {
                writer.DirectiveLine($"policy {policies[index]}", persona, DirectiveLocationKeys.ForValue("policy", policies, index));
            }
        }
    }

    void WriteModule(ScreenplayWriter writer, ModuleSyntax module)
    {
        using var anchor = writer.Anchor(module);

        // A module that only places an imported file is not written in it - its body is the file's top level.
        if (module.IsPlacement)
        {
            WriteModuleBody(writer, module);
            return;
        }

        writer.Line($"module {module.Name}");
        using (writer.Indent())
        {
            WriteModuleBody(writer, module);
        }
    }

    void WriteFileImports(ScreenplayWriter writer, IEnumerable<FileImportSyntax> imports)
    {
        foreach (var import in imports)
        {
            writer.Line($"import {StringLiteral.Quote(import.Pattern)}", import);
        }
    }

    void WriteModuleBody(ScreenplayWriter writer, ModuleSyntax module)
    {
        WriteDescription(writer, module.Description, module);
        WriteDocumentation(writer, module.Documentation, module);
        foreach (var purpose in module.Purposes) writer.Line($"purpose {purpose.Name}", purpose);
        foreach (var dependency in module.DependsOn) writer.Line($"depends on {dependency.Target}", dependency);
        var members = new List<PrintableMember>();
        AddMembers(members, module.FileImports, -1, import => WriteFileImports(writer, [import]));
        if (module.Authorize is not null)
        {
            AddMembers(members, [module.Authorize], 0, authorize => WriteAuthorize(writer, authorize));
        }

        AddMembers(members, module.Behaviors, 1, behavior => WriteAttachedBehavior(writer, behavior));
        AddMembers(members, module.UsedBehaviors, 2, uses => WriteUsesBehavior(writer, uses));
        AddMembers(members, module.Templates, 3, template => WriteTemplateAssignment(writer, template));
        AddSeparatedMembers(members, writer, module.ScreenTemplates, 4, WriteScreenTemplate);
        AddSeparatedMembers(members, writer, module.DialogTemplates ?? [], 5, WriteDialogTemplate);
        AddSeparatedMembers(members, writer, module.Forms ?? [], 6, WriteForm);
        AddSeparatedMembers(members, writer, module.Contributions ?? [], 7, WriteContribution);
        AddSeparatedMembers(members, writer, module.Examples, 8, WriteSpecificationExample);
        AddSeparatedMembers(members, writer, module.Features, 9, WriteFeature);
        WriteMembers(members, module);
    }

    void WriteTemplateAssignment(ScreenplayWriter writer, TemplateAssignmentSyntax template) =>
        writer.Line($"template {WriteTemplateReference(template.Blueprint, template.Name, template.Version)}", template);

    void WriteContribution(ScreenplayWriter writer, ContributionSyntax contribution)
    {
        using var anchor = writer.Anchor(contribution);
        writer.Line($"contribute to {contribution.ContributionPoint}");
        using (writer.Indent())
        {
            if (contribution.Navigate is not null)
            {
                writer.Line(WriteScreenNavigate(contribution.Navigate), contribution.Navigate);
                using (writer.Indent())
                {
                    WriteScreenNavigateBody(writer, contribution.Navigate);
                }
            }

            WriteNavigationItem(writer, contribution);

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
            WriteDescription(writer, form.Description, form);
            if (form.Populate is not null)
            {
                writer.Line(WriteFormPopulate(form.Populate), form.Populate);
            }

            if (form.GenerationMode != FormGenerationMode.Unspecified)
            {
                writer.Line($"generation {(form.GenerationMode == FormGenerationMode.Auto ? "auto" : "manual")}", form);
            }

            foreach (var field in form.Fields)
            {
                writer.Line(WriteFormField(field), field);
            }

            WriteFormColumns(writer, form);
            WriteFormLayout(writer, form.Layout);

            if (form.OnSubmit is not null)
            {
                writer.Line($"on submit {WriteScreenNavigate(form.OnSubmit)}", form.OnSubmit);
            }

            WriteAttachments(writer, form.Behaviors, form.UsedBehaviors);
        }
    }

    void WriteFormColumns(ScreenplayWriter writer, FormSyntax form)
    {
        if (form.ColumnMode == FormColumnMode.Unspecified)
        {
            return;
        }

        if (form.ColumnMode == FormColumnMode.Auto)
        {
            writer.Line("columns auto", form);
            return;
        }

        writer.Line("columns manual", form);
        using (writer.Indent())
        {
            foreach (var column in form.Columns)
            {
                writer.Line(
                    column.Label is null
                        ? $"column {column.Property}"
                        : $"column {column.Property} label {ScreenplaySyntaxText.LocalizableString(column.Label)}",
                    column);
            }
        }
    }

    void WriteFormLayout(ScreenplayWriter writer, CommandFormLayoutSyntax? layout)
    {
        if (layout is null)
        {
            return;
        }

        writer.Line("layout", layout);
        using (writer.Indent())
        {
            foreach (var column in layout.Columns)
            {
                writer.Line(WriteFormLayoutColumn(column), column);
            }

            if (layout.ColumnGap is not null)
            {
                writer.Line($"columnGap {WriteFormWidth(layout.ColumnGap)}", layout.ColumnGap);
            }

            if (layout.RowGap is not null)
            {
                writer.Line($"rowGap {WriteFormWidth(layout.RowGap)}", layout.RowGap);
            }

            foreach (var placement in layout.Placements)
            {
                writer.Line(WriteFormPlacement(placement), placement);
            }
        }
    }

    string WriteFormLayoutColumn(FormLayoutColumnSyntax column)
    {
        var text = $"column {column.Index}";
        if (column.Width is not null) text += $" width {WriteFormWidth(column.Width)}";
        if (column.MinWidth is not null) text += $" min {WriteFormWidth(column.MinWidth)}";
        if (column.MaxWidth is not null) text += $" max {WriteFormWidth(column.MaxWidth)}";
        return text;
    }

    string WriteFormPlacement(FormFieldPlacementSyntax placement)
    {
        var text = $"place {placement.Field} row {placement.Row} column {placement.Column}";
        if (placement.RowSpan is not null) text += $" rowSpan {placement.RowSpan}";
        if (placement.ColumnSpan is not null) text += $" columnSpan {placement.ColumnSpan}";
        if (placement.Width is not null) text += $" width {WriteFormWidth(placement.Width)}";
        return text;
    }

    string WriteFormWidth(FormWidthSyntax width) => width.Unit switch
    {
        FormWidthUnitSyntax.Auto => "auto",
        FormWidthUnitSyntax.Fraction => width.Value!.Value.ToString("0.###", CultureInfo.InvariantCulture) + "fr",
        FormWidthUnitSyntax.Pixels => width.Value!.Value.ToString("0.###", CultureInfo.InvariantCulture) + "px",
        FormWidthUnitSyntax.Percent => width.Value!.Value.ToString("0.###", CultureInfo.InvariantCulture) + "%",
        _ => throw new UnsupportedSyntaxForPrinting("form width unit", width.Unit.ToString())
    };

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

    void WriteFeature(ScreenplayWriter writer, FeatureSyntax feature)
    {
        using var anchor = writer.Anchor(feature);
        if (feature.IsPlacement)
        {
            WriteFeatureBody(writer, feature);
            return;
        }

        writer.Line($"feature {feature.Name}");
        using (writer.Indent())
        {
            WriteFeatureBody(writer, feature);
        }
    }

    void WriteFeatureBody(ScreenplayWriter writer, FeatureSyntax feature)
    {
        WriteDescription(writer, feature.Description, feature);
        WriteDocumentation(writer, feature.Documentation, feature);
        foreach (var purpose in feature.Purposes) writer.Line($"purpose {purpose.Name}", purpose);
        foreach (var dependency in feature.DependsOn) writer.Line($"depends on {dependency.Target}", dependency);
        var members = new List<PrintableMember>();
        AddMembers(members, feature.FileImports, -1, import => WriteFileImports(writer, [import]));
        if (feature.Authorize is not null)
        {
            AddMembers(members, [feature.Authorize], 0, authorize => WriteAuthorize(writer, authorize));
        }

        AddMembers(members, feature.Behaviors, 1, behavior => WriteAttachedBehavior(writer, behavior));
        AddMembers(members, feature.UsedBehaviors, 2, uses => WriteUsesBehavior(writer, uses));
        AddMembers(members, feature.Templates, 3, template => WriteTemplateAssignment(writer, template));
        AddSeparatedMembers(members, writer, feature.Examples, 4, WriteSpecificationExample);
        AddSeparatedMembers(members, writer, feature.Features, 5, WriteFeature);
        AddSeparatedMembers(members, writer, feature.Slices, 6, WriteSlice);
        AddSeparatedMembers(members, writer, feature.Contributions ?? [], 7, WriteContribution);
        WriteMembers(members, feature);
    }

    void WriteSlice(ScreenplayWriter writer, SliceSyntax slice)
    {
        ImplementationInvariants.Validate(slice);
        using var anchor = writer.Anchor(slice);
        writer.Line($"slice {slice.Type} {slice.Name}");
        using (writer.Indent())
        {
            if (slice.Direction is { } direction)
            {
                writer.DirectiveLine($"direction {direction.ToString().ToLowerInvariant()}", slice, "direction");
            }

            WriteDescription(writer, slice.Description, slice);
            WriteDocumentation(writer, slice.Documentation, slice);
            foreach (var purpose in slice.Purposes) writer.Line($"purpose {purpose.Name}", purpose);
            WriteFile(writer, slice.File);

            var members = new List<PrintableMember>();
            AddSeparatedMembers(members, writer, slice.Commands, 0, WriteCommand);
            AddSeparatedMembers(members, writer, slice.Events, 1, WriteEvent);
            AddSeparatedMembers(members, writer, slice.Operations, 2, WriteOperation);
            AddSeparatedMembers(members, writer, slice.Constraints, 3, WriteConstraint);
            AddSeparatedMembers(members, writer, slice.Queries, 4, WriteQuery);

            // A read model comes before whatever builds it - the shape first, then where it comes from.
            AddSeparatedMembers(members, writer, slice.ReadModels ?? [], 5, WriteReadModel);
            AddSeparatedMembers(members, writer, slice.Projections, 6, WriteProjection);
            AddSeparatedMembers(members, writer, slice.Reducers ?? [], 7, WriteReducer);
            AddSeparatedMembers(members, writer, slice.Captures, 8, WriteCapture);
            AddSeparatedMembers(members, writer, slice.Reactions, 9, WriteReaction);
            AddMembers(members, slice.Templates, 10, template => WriteTemplateAssignment(writer, template));
            AddSeparatedMembers(members, writer, slice.Screens, 11, WriteScreen);
            AddSeparatedMembers(members, writer, slice.Examples, 12, WriteSpecificationExample);
            AddSeparatedMembers(members, writer, slice.Specifications, 13, WriteSpecification);
            WriteMembers(members, slice);
        }
    }

    void WriteDocumentation(ScreenplayWriter writer, string? documentation, SyntaxNode owner)
    {
        if (documentation is null) return;
        writer.DirectiveLine("documentation", owner, "documentation");
        using (writer.Indent())
        {
            writer.Line("```markdown");
            foreach (var line in documentation.Split('\n')) writer.Line(line);
            writer.Line("```");
        }
    }

    void WriteDescription(ScreenplayWriter writer, string? description, SyntaxNode? owner = null)
    {
        if (description is null)
        {
            return;
        }

        if (!description.Contains('\n'))
        {
            var line = $"description {StringLiteral.Quote(description)}";
            if (owner is null)
            {
                writer.Line(line);
            }
            else
            {
                writer.DirectiveLine(line, owner, "description");
            }

            return;
        }

        if (owner is null)
        {
            writer.Line("description");
        }
        else
        {
            writer.DirectiveLine("description", owner, "description");
        }
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
