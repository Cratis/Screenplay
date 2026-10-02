// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyMappingSyntax, SpecificationEventSyntax, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { emptyGuid } from '../Document/identity';
import { SliceSpecificationDocument, SpecificationStepDocument } from '../Document/EventModelDocument';
import { EventOwners } from './EventOwners';
import { SliceScope } from './SliceScope';

// A slice's specifications as the board shows them - the port of Studio's SpecificationSyntaxVisitor. A
// step's event points at its producer when the model declares one. Only literal values are carried: a path
// or a context expression names something resolved while the application runs.
export function toSpecifications(
    specifications: readonly SpecificationSyntax[], scope: SliceScope, owners: EventOwners, commandId: string | undefined): SliceSpecificationDocument[] {
    return specifications.map(specification => {
        const at = (kind: string, index: number) => scope.idOf(`specification:${specification.name}:${kind}`, String(index));
        const step = (event: SpecificationEventSyntax, kind: string, index: number): SpecificationStepDocument => ({
            id: at(kind, index),
            name: event.eventType,
            eventId: owners.idFor(event.eventType) ?? emptyGuid,
            values: valuesOf(event.values),
        });
        const document: SliceSpecificationDocument = {
            id: scope.idOf('specification', specification.name),
            name: specification.name,
            given: specification.given.map((event, index) => step(event, 'given', index)),
            thenEvents: specification.thenEvents.map((event, index) => step(event, 'then', index)),
            thenErrors: specification.thenErrors
                .filter(error => error.name !== null && error.name.trim().length > 0)
                .map((error, index) => ({ id: at('error', index), name: error.name! })),
            collapsed: false,
        };
        if (specification.when !== null) {
            const when = { id: at('when', 0), name: specification.when.commandType, values: valuesOf(specification.when.values) };
            document.when = commandId === undefined ? when : { ...when, commandId };
        } else {
            const action = actionOf(specification);
            if (action !== undefined) document.when = { id: at('when', 0), ...action };
        }
        return document;
    });
}

// What sets a specification off when it is not a command. The board has one place for the action, so the
// kind is part of its name - 'clock 2026-10-05T08:00:00Z', 'trigger DirectoryChanged' - and the values travel
// with it as a command's would.
function actionOf(specification: SpecificationSyntax): { name: string; values: Record<string, unknown> } | undefined {
    if (specification.whenAppended !== null) return { name: `append ${specification.whenAppended.eventType}`, values: valuesOf(specification.whenAppended.values) };
    if (specification.whenClock !== null) return { name: `clock ${specification.whenClock.instant}`, values: {} };
    if (specification.whenTrigger !== null) return { name: `trigger ${specification.whenTrigger.trigger}`, values: valuesOf(specification.whenTrigger.values) };
    if (specification.whenCapture !== null) return { name: `capture ${specification.whenCapture.capture}`, values: valuesOf(specification.whenCapture.record) };
    if (specification.whenQuery !== null) return { name: `query ${specification.whenQuery.query}`, values: valuesOf(specification.whenQuery.arguments) };
    return undefined;
}

function valuesOf(values: readonly PropertyMappingSyntax[]): Record<string, unknown> {
    return Object.fromEntries(values
        .filter(value => value.source.kind === 'LiteralExpressionSyntax')
        .map(value => [value.property, value.source.kind === 'LiteralExpressionSyntax' ? value.source.value : null]));
}
