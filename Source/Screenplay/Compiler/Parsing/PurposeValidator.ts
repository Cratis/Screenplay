// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { PurposeReferenceSyntax } from '../Syntax/Purposes';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validatePurposes(application: ApplicationSyntax, context: ParserContext): void {
    const known = new Set<string>();
    for (const purpose of application.purposes ?? []) {
        if (known.has(purpose.name)) context.error(DiagnosticCodes.DuplicatePurposeDeclaration, `Duplicate purpose '${purpose.name}' - a purpose is declared once`, purpose.location);
        known.add(purpose.name);
    }
    for (const purpose of application.purposes ?? []) {
        if (purpose.interest !== null && purpose.basis !== 'legitimateInterests') context.warning(DiagnosticCodes.PurposeInterestMismatch, `Purpose '${purpose.name}' declares an interest without basis legitimateInterests`, purpose.location);
        else if (purpose.basis === 'legitimateInterests' && !purpose.interest?.trim()) context.warning(DiagnosticCodes.PurposeInterestMismatch, `Purpose '${purpose.name}' with basis legitimateInterests has no interest statement (Art. 13(1)(d))`, purpose.location);
    }
    const references: PurposeReferenceSyntax[] = [];
    const collect = (feature: FeatureSyntax): void => {
        references.push(...feature.purposes ?? [], ...feature.slices.flatMap(slice => slice.purposes ?? []));
        feature.features.forEach(collect);
    };
    for (const module of application.modules) {
        references.push(...module.purposes ?? []);
        module.features.forEach(collect);
    }
    for (const reference of references) {
        if (!known.has(reference.name)) context.warning(DiagnosticCodes.UnknownPurpose, `Unknown purpose '${reference.name}' - declare it with 'purpose ${reference.name}'`, reference.location);
    }
}
