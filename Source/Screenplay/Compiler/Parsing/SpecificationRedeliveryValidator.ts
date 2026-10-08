// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { EventSyntax } from '../Syntax/Declarations';
import { SpecificationRedeliverySyntax } from '../Syntax/SpecificationRedeliverySyntax';
import { SpecificationEventSyntax } from '../Syntax/Specifications';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';
import { sameRedeliveryValue } from './RedeliveryValues';
import { expandSpecificationExamples } from './SpecificationCommandExamples';

export function validateSpecificationRedelivery(application: ApplicationSyntax, context: ParserContext): void {
    const declarations = new RefusalDeclarations(expandSpecificationExamples(application));
    for (const { slice } of declarations.slices) for (const specification of slice.specifications) {
        const action = specification.whenRedelivered;
        if (action == null) continue;
        const event = declarations.event(action.eventType, slice);
        const reaction = declarations.resolve(action.reaction, slice, owner => owner.reactions);
        if (reaction === null || event === null || !reaction.node.triggers.some(trigger => trigger.source.kind === 'NamedTriggerSourceSyntax' && declarations.event(trigger.source.name, reaction.slice) === event)) {
            context.error(DiagnosticCodes.UnknownRedeliveryReaction, `Reaction '${action.reaction}' must resolve unambiguously and observe event '${action.eventType}'.`, action.location);
            continue;
        }
        const candidates = specification.given.filter(given => declarations.event(given.eventType, slice) === event).map(given => matches(given, action, event, declarations));
        const count = candidates.filter(match => match === true).length;
        if (count < 2 && candidates.some(match => match === null))
            context.error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, `Cannot locate redelivery of '${action.eventType}' uniquely: a given source or locator value is not decidable; state explicit concrete 'for' and values.`, action.location);
        else if (count !== 1)
            context.error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, `Redelivery of '${action.eventType}' matches ${count} given occurrences; use 'for' or values to identify exactly one.`, action.location);
    }
}

function matches(given: SpecificationEventSyntax, action: SpecificationRedeliverySyntax, event: EventSyntax, declarations: RefusalDeclarations): boolean | null {
    const comparisons: (boolean | null)[] = [];
    if (action.for !== null) comparisons.push(given.for === null ? null : sameRedeliveryValue(action.for, given.for, null, declarations));
    for (const locator of action.values) {
        const values = given.values.filter(value => value.property === locator.property);
        comparisons.push(values.length === 1 ? sameRedeliveryValue(locator.source, values[0].source, declarations.property(event.properties, locator.property)?.type ?? null, declarations) : null);
    }
    return comparisons.includes(false) ? false : comparisons.includes(null) ? null : true;
}
