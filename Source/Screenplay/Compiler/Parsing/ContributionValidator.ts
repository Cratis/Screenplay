// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { ContributionSyntax } from '../Syntax/Layouts';
import type { FeatureSyntax } from '../Syntax/Structure';
import { usesBlueprintOutlets } from './BlueprintOutletFacts';
import { OutletInventory } from './OutletInventory';
import { ParserContext } from './ParserContext';

export function validateContributions(inventory: OutletInventory, context: ParserContext): void {
    const validate = (contribution: ContributionSyntax, module: string): void => {
        const own = inventory.offers.filter(offer => offer.module === module && offer.outlet.name === contribution.contributionPoint);
        if (own.length > 1) context.warning(DiagnosticCodes.AmbiguousReference, `Ambiguous outlet '${contribution.contributionPoint}' - ${own.length} templates in module '${module}' declare it (${own.map(offer => `${offer.structure.name}.${offer.outlet.name}`).join(', ')})`, contribution.location);
        else if (own.length === 0) validateElsewhere(contribution, inventory, context, module);
        if (contribution.navigate != null && !inventory.screens.some(screen => screen.name === contribution.navigate!.screen.split('.').at(-1))) context.warning(DiagnosticCodes.UnknownScreen, `Unknown screen '${contribution.navigate.screen}' - nothing in scope declares it`, contribution.navigate.location);
    };
    const features = (declarations: readonly FeatureSyntax[], module: string): void => {
        for (const feature of declarations) {
            feature.contributions?.forEach(contribution => validate(contribution, module));
            features(feature.features, module);
        }
    };
    for (const module of inventory.application.modules) {
        module.contributions?.forEach(contribution => validate(contribution, module.name));
        features(module.features, module.name);
    }
}

function validateElsewhere(contribution: ContributionSyntax, inventory: OutletInventory, context: ParserContext, module: string): void {
    const elsewhere = [...new Set(inventory.offers.filter(offer => offer.module !== module && offer.outlet.name === contribution.contributionPoint).map(offer => offer.module ?? ''))];
    if (elsewhere.length > 1) context.warning(DiagnosticCodes.AmbiguousReference, `Ambiguous outlet '${contribution.contributionPoint}' - ${elsewhere.length} modules declare it (${elsewhere.join(', ')})`, contribution.location);
    else if (elsewhere.length === 0 && !usesBlueprintOutlets(inventory.application)) context.error(DiagnosticCodes.UnknownOutlet, `Contribution to unknown outlet '${contribution.contributionPoint}' - no layout or template declares 'outlet ${contribution.contributionPoint} of <kind>'`, contribution.location);
}
