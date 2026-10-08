// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

// Mirrors ScreenplayValidator.ValidateConstraintProperties: direct fields only, across all generations.
// Unknown/imported events, dotted paths and fields removed in a later generation remain C# binder checks.
export function validateConstraintProperties(application: ApplicationSyntax, context: ParserContext): void {
    const slices = new AuthoringProductionResolver(application).slices.map(entry => entry.slice);
    const events = slices.flatMap(eventDeclarations);
    for (const slice of slices) {
        for (const rule of slice.constraints.flatMap(constraint => [constraint, ...constraint.additionalRules])) {
            if (rule.kind !== 'UniquePropertyConstraintSyntax') continue;
            const declarations = events.filter(event => event.name === rule.event);
            if (declarations.length === 0) continue;
            for (const property of [rule.property, ...rule.additionalProperties]) {
                if (property.includes('.') || declarations.some(event => event.properties.some(field => field.name === property))) continue;
                context.error(DiagnosticCodes.UnknownConstraintProperty,
                    `Constraint '${rule.name}' names property '${property}', which event '${rule.event}' does not declare.`, rule.location);
            }
        }
    }
}
