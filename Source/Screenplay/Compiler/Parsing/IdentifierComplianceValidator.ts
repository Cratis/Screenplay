// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { ApplicationSyntax } from '../Syntax/Structure';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { ParserContext } from './ParserContext';
import { uniqueByName } from './ResponseValidator';

export function validateIdentifierCompliance(application: ApplicationSyntax, context: ParserContext): void {
    const personal = new Set(application.concepts.filter(concept => concept.attributes.some(attribute => attribute.name === 'pii')).map(concept => concept.name));
    const types = uniqueByName(application.types);
    const resolver = new AuthoringProductionResolver(application);
    const declaredTriggers = new Map<string, (readonly PropertySyntax[])[]>();
    for (const declared of application.declaredTriggers ?? []) {
        const shapes = declaredTriggers.get(declared.name) ?? [];
        shapes.push(declared.data);
        declaredTriggers.set(declared.name, shapes);
    }
    const validate = (type: TypeRefSyntax, location: SourceLocation): void => {
        if (personal.has(type.name)) context.error(DiagnosticCodes.PiiNotSupportedOnIdentifier, `Concept '${type.name}' is @pii and cannot be an event source identifier - use a surrogate Uuid identifier and keep the @pii value as a property`, location);
    };
    const property = (properties: readonly PropertySyntax[], path: string): PropertySyntax | null => {
        const [name, ...rest] = path.split('.');
        const matches = properties.filter(entry => entry.name === name);
        if (matches.length !== 1) return null;
        const value = matches[0];
        if (rest.length === 0) return value;
        return property(types.get(value.type.name)?.properties ?? [], rest.join('.'));
    };
    for (const source of application.eventSources ?? []) {
        if (source.identifier != null) validate(source.identifier, source.identifier.location);
    }
    for (const { slice } of resolver.slices) {
        for (const command of slice.commands) {
            for (const entry of command.properties.filter(entry => entry.isIdentifier)) validate(entry.type, entry.location);
            for (const production of command.produces.filter(production => resolver.isEventProduction(production, slice))) {
                if (production.for?.kind !== 'PathExpressionSyntax') continue;
                const entry = property(command.properties, production.for.path);
                if (entry !== null && !entry.isIdentifier) validate(entry.type, production.for.location);
            }
        }
        for (const trigger of slice.reactions.flatMap(reaction => reaction.triggers)) {
            if (trigger.source.kind !== 'NamedTriggerSourceSyntax') continue;
            const resolution = resolver.resolve(trigger.source.name, slice);
            const event = resolution.declaration?.node;
            const clauseData = (dependencySourcesOf(trigger).data ?? []).flatMap<PropertySyntax>(datum => datum.type === null ? [] : [{ kind: 'PropertySyntax', name: datum.name, type: datum.type, isIdentifier: false, location: datum.location }]);
            const shapes = [event?.kind === 'EventSyntax' ? event.properties : [], clauseData, ...declaredTriggers.get(trigger.source.name) ?? []];
            for (const production of trigger.produces.filter(production => resolver.isEventProduction(production, slice))) {
                if (production.for?.kind !== 'PathExpressionSyntax') continue;
                // Compliance checks both shapes when event and declared-trigger resolution disagree (#439).
                const path = production.for.path;
                const entry = shapes.map(properties => property(properties, path)).find(entry => entry !== null && personal.has(entry.type.name));
                if (entry != null) validate(entry.type, production.for.location);
            }
        }
    }
}
