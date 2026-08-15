// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>module</c> declaration - the top level namespace of a bounded context.
/// </summary>
/// <param name="Name">The name of the module.</param>
/// <param name="Layouts">The <see cref="LayoutSyntax">layouts</see> declared in the module.</param>
/// <param name="Features">The <see cref="FeatureSyntax">features</see> declared in the module.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional description of the module.</param>
/// <param name="Forms">The <see cref="FormSyntax">forms</see> declared in the module.</param>
/// <param name="Contributions">The <see cref="ContributionSyntax">contributions</see> declared directly on the module.</param>
public record ModuleSyntax(
    string Name,
    IEnumerable<LayoutSyntax> Layouts,
    IEnumerable<FeatureSyntax> Features,
    SourceLocation Location,
    string? Description = null,
    IEnumerable<FormSyntax>? Forms = null,
    IEnumerable<ContributionSyntax>? Contributions = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>layout</c> declaration - a reusable screen template with named slots.
/// </summary>
/// <param name="Name">The name of the layout.</param>
/// <param name="Slots">The <see cref="SlotSyntax">slots</see> the layout template defines, flattened regardless of arrangement.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Arrangement">The <see cref="LayoutArrangement"/> this layout's content is arranged by. Defaults to <see cref="LayoutArrangement.Flow"/>.</param>
/// <param name="Template">The <see cref="TemplateSyntax"/> tree, present when <see cref="Arrangement"/> is <see cref="LayoutArrangement.Flow"/>.</param>
/// <param name="Variants">The <see cref="VariantSyntax">variants</see>, present when <see cref="Arrangement"/> is <see cref="LayoutArrangement.Freeform"/>.</param>
public record LayoutSyntax(
    string Name,
    IEnumerable<SlotSyntax> Slots,
    SourceLocation Location,
    LayoutArrangement Arrangement = LayoutArrangement.Flow,
    TemplateSyntax? Template = null,
    IEnumerable<VariantSyntax>? Variants = null) : SyntaxNode(Location);

/// <summary>
/// Represents one named slot in a <c>layout</c> template.
/// </summary>
/// <param name="Name">The name of the slot.</param>
/// <param name="Contributes">The name of the contribution point this slot accepts contributions for, or <c>null</c> if it does not accept any.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SlotSyntax(string Name, string? Contributes, SourceLocation Location) : SyntaxNode(Location);

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
