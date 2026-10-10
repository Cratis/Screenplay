// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Describes declaration presence, identity-preserving renames and moves.
/// </summary>
/// <param name="Declaration">The changed declaration.</param>
/// <param name="Change">The declaration change.</param>
/// <param name="Move">The kind of move, or null for other changes.</param>
/// <param name="BeforeOwner">The baseline owner for an owner change.</param>
/// <param name="AfterOwner">The candidate owner for an owner change.</param>
/// <param name="BeforeDocuments">The baseline source documents.</param>
/// <param name="AfterDocuments">The candidate source documents.</param>
public sealed record DeclarationChange(
    ComparedDeclaration Declaration,
    DeclarationChangeKind Change,
    DeclarationMove? Move,
    string? BeforeOwner,
    string? AfterOwner,
    IReadOnlyList<ModelDocumentLocation> BeforeDocuments,
    IReadOnlyList<ModelDocumentLocation> AfterDocuments);
