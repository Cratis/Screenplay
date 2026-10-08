// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExpressionSyntax, PropertyMappingSyntax, SpecificationCallerSyntax, SpecificationEventSyntax, SpecificationSyntax } from '@cratis/screenplay-compiler';
import { emptyGuid } from '../Document/identity';
import { SliceSpecificationDocument, SpecificationCallerDocument, SpecificationStepDocument } from '../Document/EventModelDocument';
import { expressionText } from './expressionText';
import { EventOwners } from './EventOwners';
import { SliceScope } from './SliceScope';

// A slice's specifications as the board shows them - the port of Studio's SpecificationSyntaxVisitor. A
// step's event points at its producer when the model declares one. A literal value is carried as it is; a path
// or a context expression names something resolved while the application runs, so it is carried as written.
// A State Change slice's own command is drawn once by the board: a specification's When that names it - by
// name, or by the id it points at - is that same card, with the values the specification sets on it.
export function toSpecifications(
    specifications: readonly SpecificationSyntax[], scope: SliceScope, owners: EventOwners, commandId: string | undefined): SliceSpecificationDocument[] {
    return specifications.map(specification => {
        const at = (kind: string, index: number) => scope.idOf(`specification:${specification.name}:${kind}`, String(index));
        const step = (event: SpecificationEventSyntax, kind: string, index: number): SpecificationStepDocument => ({
            id: at(kind, index),
            name: factName(event),
            eventId: owners.idFor(event.eventType) ?? emptyGuid,
            values: valuesOf(event.values),
        });
        const document: SliceSpecificationDocument = {
            id: scope.idOf('specification', specification.name),
            name: specificationName(specification),
            given: specification.given.map((event, index) => step(event, 'given', index)),
            thenEvents: specification.thenEvents.map((event, index) => step(event, 'then', index)),
            thenErrors: [
                ...specification.thenErrors.map((error, index) => error.name !== null && error.name.trim().length > 0
                    ? { id: at('error', index), name: 'error', message: error.name }
                    : { id: at('error', index), name: 'error' }),
                ...(specification.thenDenied == null ? [] : [{ id: at('denied', 0), name: 'denied' }]),
            ],
            collapsed: false,
        };
        if (specification.givenCaller != null) document.caller = callerOf(specification.givenCaller);
        if (specification.thenReturns != null) {
            document.thenReturns = specification.thenReturns.kind === 'ScalarSpecificationReturnSyntax'
                ? { value: valueOf(specification.thenReturns.value) }
                : { fields: valuesOf(specification.thenReturns.fields) };
        }
        if (specification.thenAbsentReadModels?.length) {
            document.thenAbsentReadModels = specification.thenAbsentReadModels.map(absent => ({ name: absent.name, key: valueOf(absent.key) }));
        }
        if (specification.when !== null) {
            const when = { id: at('when', 0), name: specification.when.commandType, values: valuesOf(specification.when.values) };
            document.when = commandId === undefined ? when : { ...when, commandId };
            if (specification.when.generatedValues?.length) document.when.generatedValues = valuesOf(specification.when.generatedValues);
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
    if (specification.whenAppended !== null) return { name: `append ${factName(specification.whenAppended)}`, values: valuesOf(specification.whenAppended.values) };
    if (specification.whenClock !== null) return { name: `clock ${specification.whenClock.instant}`, values: {} };
    if (specification.whenTrigger !== null) return { name: `trigger ${specification.whenTrigger.trigger}`, values: valuesOf(specification.whenTrigger.values) };
    if (specification.whenCapture !== null) return { name: `capture ${specification.whenCapture.capture}`, values: valuesOf(specification.whenCapture.record) };
    if (specification.whenQuery !== null) return { name: `query ${specification.whenQuery.query}`, values: valuesOf(specification.whenQuery.arguments) };
    return undefined;
}

// @cratis/event-models 0.118.2 reads and renders the specification name in its header. Linked
// event cards can override step names, so carry every explicit route there as well, labeled by
// role and occurrence. Keep links and payloads intact; this is display-only, not executable routing.
function specificationName(specification: SpecificationSyntax): string {
    const routes = [
        ...specification.given.map((event, index) => ({ event, role: `given ${index + 1}` })),
        ...(specification.whenAppended ? [{ event: specification.whenAppended, role: 'when append' }] : []),
        ...specification.thenEvents.map((event, index) => ({ event, role: `then ${index + 1}` })),
    ].filter(({ event }) => event.stream || event.noStream);
    const details = routes.map(({ event, role }) => `${role}: ${event.eventType} — ${routeDetails(event)}`);
    if (routes.length > 0) details.push(routeAvailability);
    if (specification.when?.generatedValues?.length) {
        details.push(`generated (not request inputs): ${mappingText(specification.when.generatedValues)}`);
    }
    const returns = specification.thenReturns;
    if (returns != null) {
        details.push(returns.kind === 'ScalarSpecificationReturnSyntax'
            ? `then returns ${expressionText(returns.value)}`
            : `then returns { ${mappingText(returns.fields)} }`);
    }
    for (const absent of specification.thenAbsentReadModels ?? []) {
        details.push(`then no readmodel ${absent.name} for ${expressionText(absent.key)}`);
    }
    if (details.length === 0) return specification.name;
    return `${specificationTitle(specification.name)} — ${details.join(' | ')}`;
}

function mappingText(values: readonly PropertyMappingSyntax[]): string {
    return values.map(value => `${value.property} = ${expressionText(value.source)}`).join(', ');
}

// Match the pinned board's unexported specificationTitle before adding spaces in route summaries.
function specificationTitle(name: string): string {
    if (/\s/.test(name)) return name;
    return name
        .replace(/(\p{Ll})(\p{Lu})/gu, '$1 $2')
        .replace(/(\p{Lu}+)(\p{Lu}\p{Ll})/gu, '$1 $2')
        .replace(/(\p{L})(\d)/gu, '$1 $2')
        .replace(/(\d)(\p{L})/gu, '$1 $2');
}

// Card names are plain text, not HTML. The header also carries these details because an owned
// event's current name takes precedence over the step name on the board.
const routeAvailability = 'Syntax-only (PLAY0268) (#457); the board displays routing intent, not an executable route assertion.';

function factName(event: SpecificationEventSyntax): string {
    if (!event.stream && !event.noStream) return event.eventType;
    return `${event.eventType} — ${routeDetails(event)}; ${routeAvailability}`;
}

function routeDetails(event: SpecificationEventSyntax): string {
    const route = event.stream;
    return [event.for ? `for ${expressionText(event.for)}` : '',
        route ? `stream ${route.eventSource}.${route.stream}` : 'no stream',
        route?.streamId ? `streamId = ${expressionText(route.streamId.source)}` : route?.streamIdParts.length ? `streamId ${mappingText(route.streamIdParts)}` : ''].filter(Boolean).join('; ');
}

function callerOf(caller: SpecificationCallerSyntax): SpecificationCallerDocument {
    // A Map keeps claim types such as '__proto__' and 'toString' as the data they are.
    const claims = new Map<string, string>();
    caller.claims.forEach(claim => claims.set(claim.type, claims.has(claim.type) ? `${claims.get(claim.type)}, ${claim.value}` : claim.value));
    return { authenticated: caller.authenticated, roles: [...caller.roles], claims: Object.fromEntries(claims) };
}

function valuesOf(values: readonly PropertyMappingSyntax[]): Record<string, unknown> {
    return Object.fromEntries(values.map(value => [value.property, valueOf(value.source)]));
}

function valueOf(expression: ExpressionSyntax): unknown {
    return expression.kind === 'LiteralExpressionSyntax' ? expression.value : expressionText(expression);
}
