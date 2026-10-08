// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Collects the body of a module - written beneath a <c>module</c> header, or at the top level of a file
/// imported into the module.
/// </summary>
/// <param name="name">The name of the module.</param>
internal sealed class ModuleBody(string name)
{
    /// <summary>
    /// What a module body may hold, as it reads in a diagnostic.
    /// </summary>
    public const string Expected = "description, documentation, depends on <Name>, authorize, import, screen template, dialog template, form, contribute, feature, example, 'on <trigger>' or 'uses <Behavior>'";

    readonly List<ScreenTemplateSyntax> _screenTemplates = [];
    readonly List<DialogTemplateSyntax> _dialogTemplates = [];
    readonly Dictionary<string, SourceLocation> _directiveLocations = [];
    readonly List<FeatureSyntax> _features = [];
    readonly List<SpecificationExampleSyntax> _examples = [];
    readonly List<FormSyntax> _forms = [];
    readonly List<ContributionSyntax> _contributions = [];
    readonly List<BehaviorSyntax> _behaviors = [];
    readonly List<UsesBehaviorSyntax> _usedBehaviors = [];
    readonly List<FileImportSyntax> _fileImports = [];
    readonly List<DependsOnSyntax> _dependsOn = [];
    readonly List<TemplateAssignmentSyntax> _templates = [];
    int _restatedHeaders;
    string? _description;
    string? _documentation;
    AuthorizeSyntax? _authorize;

    /// <summary>
    /// Parses one already consumed line of the body.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/>.</param>
    /// <returns><c>true</c> when the line belongs to a module body; otherwise <c>false</c>, and nothing was reported.</returns>
    public bool TryParse(ParserContext context, SourceLine line)
    {
        switch (LineText.FirstWord(line.Content))
        {
            case "description":
                var previousDescription = _description;
                _description = DescriptionParser.Parse(context, line, _description, $"Module '{name}'");
                if (previousDescription is null && _description is not null)
                {
                    _directiveLocations["description"] = line.Location;
                }

                return true;
            case "documentation":
                _documentation = DocumentationParser.Parse(context, line, _documentation, $"Module '{name}'", _directiveLocations);
                return true;
            case "depends":
                DependsOnParser.Parse(context, line, _dependsOn, DiagnosticCodes.UnknownModuleDirective);
                return true;
            case "authorize":
                _authorize = AuthorizeParser.Combine(_authorize, AuthorizeParser.Parse(context, line));
                return true;
            case "import":
                FileImportParser.Parse(context, line, _fileImports);
                return true;
            case "on":
            case "uses":
                // Attached here, a behavior covers every screen in the module - the level a confirm on
                // every destructive action belongs at.
                InteractionParser.ParseAttachment(context, line, _behaviors, _usedBehaviors);
                return true;
            case "screen":
                _screenTemplates.Add(LayoutParser.ParseScreenTemplate(context, line));
                return true;
            case "dialog":
                _dialogTemplates.Add(LayoutParser.ParseDialogTemplate(context, line));
                return true;
            case "form":
                AddForm(context, FormParser.Parse(context, line));
                return true;
            case "contribute":
                _contributions.Add(ContributionParser.Parse(context, line));
                return true;
            case "template":
                if (TemplateAssignmentParser.Parse(context, line) is { } template)
                {
                    _templates.Add(template);
                }

                return true;
            case "example":
                _examples.Add(SpecificationParser.ParseExample(context, line));
                return true;
            case "feature":
                _features.Add(ScreenplayParser.ParseFeature(context, line));
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Builds the module from what the body held.
    /// </summary>
    /// <param name="location">The <see cref="SourceLocation"/> of the module header, or of the placed file.</param>
    /// <param name="isPlacement">Whether the module only places an imported file rather than being written in it.</param>
    /// <returns>The <see cref="ModuleSyntax"/>.</returns>
    public ModuleSyntax Build(SourceLocation location, bool isPlacement = false) =>
        new(name, _screenTemplates, _features, location, _description, _forms, _contributions, _dialogTemplates)
        {
            Documentation = _documentation,
            Examples = _examples,
            Behaviors = _behaviors,
            UsedBehaviors = _usedBehaviors,
            DependsOn = _dependsOn,
            Authorize = _authorize,
            DirectiveLocations = _directiveLocations,
            FileImports = _fileImports,
            IsPlacement = isPlacement,
            Templates = _templates
        };

    internal void RecordRestatedHeader(SourceLocation location) =>
        _directiveLocations[$"{DirectiveLocationKeys.PlacementHeaderPrefix}{_restatedHeaders++}"] = location;

    void AddForm(ParserContext context, FormSyntax form)
    {
        if (_forms.Exists(existing => existing.Name == form.Name))
        {
            context.Error(DiagnosticCodes.DuplicateForm, $"Duplicate form '{form.Name}' - a form is declared once", form.Location);
            return;
        }

        _forms.Add(form);
    }
}
