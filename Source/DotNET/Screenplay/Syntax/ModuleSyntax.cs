// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>module</c> declaration - the top level namespace of a bounded context.
/// </summary>
/// <param name="Name">The name of the module.</param>
/// <param name="ScreenTemplates">The <see cref="ScreenTemplateSyntax">screen templates</see> declared in the module.</param>
/// <param name="Features">The <see cref="FeatureSyntax">features</see> declared in the module.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional description of the module.</param>
/// <param name="Forms">The <see cref="FormSyntax">forms</see> declared in the module.</param>
/// <param name="Contributions">The <see cref="ContributionSyntax">contributions</see> declared directly on the module.</param>
/// <param name="DialogTemplates">The <see cref="DialogTemplateSyntax">dialog templates</see> declared in the module.</param>
public record ModuleSyntax(
    string Name,
    IEnumerable<ScreenTemplateSyntax> ScreenTemplates,
    IEnumerable<FeatureSyntax> Features,
    SourceLocation Location,
    string? Description = null,
    IEnumerable<FormSyntax>? Forms = null,
    IEnumerable<ContributionSyntax>? Contributions = null,
    IEnumerable<DialogTemplateSyntax>? DialogTemplates = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the module-scoped specification examples.
    /// </summary>
    public IEnumerable<SpecificationExampleSyntax> Examples { get; init; } = [];

    /// <summary>
    /// Gets the behaviors attached inline to the module. Every screen beneath it inherits them, additively
    /// with whatever is attached closer in.
    /// </summary>
    public IEnumerable<BehaviorSyntax> Behaviors { get; init; } = [];

    /// <summary>
    /// Gets the named behaviors attached to the module with <c>uses</c>.
    /// </summary>
    public IEnumerable<UsesBehaviorSyntax> UsedBehaviors { get; init; } = [];

    /// <summary>
    /// Gets the ordered authoring-only dependencies of this module.
    /// </summary>
    public IEnumerable<DependsOnSyntax> DependsOn { get; init; } = [];

    /// <summary>
    /// Gets the authorization required by every command and query in this module, in addition to their own
    /// and their enclosing features' requirements. This init member preserves the 4.0.0 positional contract.
    /// </summary>
    public AuthorizeSyntax? Authorize { get; init; }

    /// <summary>
    /// Gets the files imported inside the module - what they declare at their top level belongs to the module.
    /// </summary>
    public IEnumerable<FileImportSyntax> FileImports { get; init; } = [];

    /// <summary>
    /// Gets whether the module is not written in the document but places it - the document was imported into
    /// the module, so its top level is the module's body.
    /// </summary>
    public bool IsPlacement { get; init; }
}

/// <summary>
/// Represents a <c>feature</c> declaration - a grouping of slices, optionally nested in sub features.
/// </summary>
/// <param name="Name">The name of the feature.</param>
/// <param name="Features">The nested <see cref="FeatureSyntax">sub features</see>.</param>
/// <param name="Slices">The <see cref="SliceSyntax">slices</see> declared in the feature.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional description of the feature.</param>
/// <param name="Contributions">The <see cref="ContributionSyntax">contributions</see> declared directly on the feature.</param>
public record FeatureSyntax(
    string Name,
    IEnumerable<FeatureSyntax> Features,
    IEnumerable<SliceSyntax> Slices,
    SourceLocation Location,
    string? Description = null,
    IEnumerable<ContributionSyntax>? Contributions = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the feature-scoped specification examples.
    /// </summary>
    public IEnumerable<SpecificationExampleSyntax> Examples { get; init; } = [];

    /// <summary>
    /// Gets the behaviors attached inline to the feature. Every screen beneath it inherits them, additively
    /// with whatever is attached closer in.
    /// </summary>
    public IEnumerable<BehaviorSyntax> Behaviors { get; init; } = [];

    /// <summary>
    /// Gets the named behaviors attached to the feature with <c>uses</c>.
    /// </summary>
    public IEnumerable<UsesBehaviorSyntax> UsedBehaviors { get; init; } = [];

    /// <summary>
    /// Gets the ordered authoring-only dependencies of this feature.
    /// </summary>
    public IEnumerable<DependsOnSyntax> DependsOn { get; init; } = [];

    /// <summary>
    /// Gets the authorization required by every command and query beneath this feature, including nested
    /// features, in addition to the enclosing requirements.
    /// </summary>
    public AuthorizeSyntax? Authorize { get; init; }

    /// <summary>
    /// Gets the files imported inside the feature - what they declare at their top level belongs to the feature.
    /// </summary>
    public IEnumerable<FileImportSyntax> FileImports { get; init; } = [];

    /// <summary>
    /// Gets whether the feature is not written in the document but places it - the document was imported into
    /// the feature, so its top level is the feature's body.
    /// </summary>
    public bool IsPlacement { get; init; }
}
