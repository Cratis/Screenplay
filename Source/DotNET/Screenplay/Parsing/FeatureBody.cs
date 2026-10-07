// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Collects the body of a feature - written beneath a <c>feature</c> header, or at the top level of a file
/// imported into the feature.
/// </summary>
/// <param name="name">The name of the feature.</param>
internal sealed class FeatureBody(string name)
{
    /// <summary>
    /// What a feature body may hold, as it reads in a diagnostic.
    /// </summary>
    public const string Expected = "description, authorize, import, feature, slice, contribute, 'on <trigger>' or 'uses <Behavior>'";

    readonly List<FeatureSyntax> _features = [];
    readonly List<SliceSyntax> _slices = [];
    readonly Dictionary<string, SourceLocation> _directiveLocations = [];
    readonly List<ContributionSyntax> _contributions = [];
    readonly List<BehaviorSyntax> _behaviors = [];
    readonly List<UsesBehaviorSyntax> _usedBehaviors = [];
    readonly List<FileImportSyntax> _fileImports = [];
    readonly List<DependsOnSyntax> _dependsOn = [];
    string? _description;
    AuthorizeSyntax? _authorize;

    /// <summary>
    /// Parses one already consumed line of the body.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/>.</param>
    /// <returns><c>true</c> when the line belongs to a feature body; otherwise <c>false</c>, and nothing was reported.</returns>
    public bool TryParse(ParserContext context, SourceLine line)
    {
        switch (LineText.FirstWord(line.Content))
        {
            case "description":
                var previousDescription = _description;
                _description = DescriptionParser.Parse(context, line, _description, $"Feature '{name}'");
                if (previousDescription is null && _description is not null)
                {
                    _directiveLocations["description"] = line.Location;
                }

                return true;
            case "depends":
                DependsOnParser.Parse(context, line, _dependsOn, DiagnosticCodes.UnknownFeatureDirective);
                return true;
            case "authorize":
                _authorize = AuthorizeParser.Combine(_authorize, AuthorizeParser.Parse(context, line));
                return true;
            case "import":
                FileImportParser.Parse(context, line, _fileImports);
                return true;
            case "on":
            case "uses":
                InteractionParser.ParseAttachment(context, line, _behaviors, _usedBehaviors);
                return true;
            case "feature":
                _features.Add(ScreenplayParser.ParseFeature(context, line));
                return true;
            case "slice":
                _slices.Add(SliceParser.Parse(context, line));
                return true;
            case "contribute":
                _contributions.Add(ContributionParser.Parse(context, line));
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Builds the feature from what the body held.
    /// </summary>
    /// <param name="location">The <see cref="SourceLocation"/> of the feature header, or of the placed file.</param>
    /// <param name="isPlacement">Whether the feature only places an imported file rather than being written in it.</param>
    /// <returns>The <see cref="FeatureSyntax"/>.</returns>
    public FeatureSyntax Build(SourceLocation location, bool isPlacement = false) =>
        new(name, _features, _slices, location, _description, _contributions)
        {
            Behaviors = _behaviors,
            UsedBehaviors = _usedBehaviors,
            DependsOn = _dependsOn,
            Authorize = _authorize,
            DirectiveLocations = _directiveLocations,
            FileImports = _fileImports,
            IsPlacement = isPlacement
        };
}
