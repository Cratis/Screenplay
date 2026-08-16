// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

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
    IEnumerable<DialogTemplateSyntax>? DialogTemplates = null) : SyntaxNode(Location);

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
    IEnumerable<ContributionSyntax>? Contributions = null) : SyntaxNode(Location);
