// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

export function validatePersonaCallers(application: ApplicationSyntax, context: ParserContext): void {
    const personas = application.personas.map(persona => persona.name);
    const visit = (features: readonly FeatureSyntax[]): void => {
        for (const feature of features) {
            visit(feature.features);
            for (const slice of feature.slices) {
                for (const specification of slice.specifications) {
                    const reference = specification.givenCallerPersona;
                    if (reference != null) {
                        const count = personas.filter(name => name === reference.name).length;
                        if (count !== 1) context.error(DiagnosticCodes.InvalidSpecificationCallerPersona, `${count === 0 ? 'Unknown' : 'Ambiguous'} persona '${reference.name}' - declared personas: ${personas.join(', ')}.`, reference.location);
                    }
                }
            }
        }
    };
    for (const module of application.modules) visit(module.features);
}
