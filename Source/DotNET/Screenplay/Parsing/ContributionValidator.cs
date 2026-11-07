// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates that a contribution resolves to a contribution point some slot declares, and that the screen it
/// navigates to, if any, resolves.
/// </summary>
/// <param name="offers">The slots that offer a contribution point, with where they are declared.</param>
/// <param name="screens">The screens the document declares, scoped to where they are declared.</param>
/// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
/// <param name="usesBlueprints">Whether the document uses a blueprint, whose templates may declare outlets the document does not.</param>
internal sealed class ContributionValidator(
    IReadOnlyList<(string Module, string Structure, string Slot, string ContributionPoint)> offers,
    IReadOnlyList<Declaration> screens,
    ParserContext context,
    bool usesBlueprints)
{
    /// <summary>
    /// Validates one contribution.
    /// </summary>
    /// <param name="contribution">The <see cref="ContributionSyntax"/> to validate.</param>
    /// <param name="ownModule">The name of the module the contribution is written in.</param>
    /// <param name="scope">The <see cref="DeclarationScope"/> the contribution sits in.</param>
    public void Validate(ContributionSyntax contribution, string ownModule, DeclarationScope scope)
    {
        var ownModuleMatches = offers.Where(offer => offer.Module == ownModule && offer.ContributionPoint == contribution.ContributionPoint).ToList();
        if (ownModuleMatches.Count > 1)
        {
            var where = string.Join(", ", ownModuleMatches.Select(offer => $"{offer.Structure}.{offer.Slot}"));
            context.Warning(
                DiagnosticCodes.AmbiguousReference,
                $"Ambiguous outlet '{contribution.ContributionPoint}' - {ownModuleMatches.Count} templates in module '{ownModule}' declare it ({where})",
                contribution.Location);
        }
        else if (ownModuleMatches.Count == 0)
        {
            ValidateElsewhere(contribution, ownModule);
        }

        if (contribution.Navigate is not null)
        {
            ScreenplayValidator.Report(contribution.Navigate.Screen, scope, screens, DiagnosticCodes.UnknownScreen, "screen", contribution.Navigate.Location, context);
        }
    }

    void ValidateElsewhere(ContributionSyntax contribution, string ownModule)
    {
        var elsewhereMatches = offers.Where(offer => offer.Module != ownModule && offer.ContributionPoint == contribution.ContributionPoint)
            .Select(offer => offer.Module)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (elsewhereMatches.Count > 1)
        {
            context.Warning(
                DiagnosticCodes.AmbiguousReference,
                $"Ambiguous outlet '{contribution.ContributionPoint}' - {elsewhereMatches.Count} modules declare it ({string.Join(", ", elsewhereMatches)})",
                contribution.Location);
        }
        else if (elsewhereMatches.Count == 0 && !usesBlueprints)
        {
            context.Error(
                DiagnosticCodes.UnknownOutlet,
                $"Contribution to unknown outlet '{contribution.ContributionPoint}' - no layout or template declares 'outlet {contribution.ContributionPoint} of <kind>'",
                contribution.Location);
        }
    }
}
