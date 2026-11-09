// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { diagnosticText } from '../Diagnostics/DiagnosticArgument';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthoringProductionKind, AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ProjectionBlockSyntax } from '../Syntax/Projections';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { uniqueByName } from './ResponseValidator';

// The property path before a dynamic dictionary key ('.$'), or the whole target when it has none.
function baseProperty(target: string): string {
    const marker = target.indexOf('.$');
    return marker < 0 ? target : target.slice(0, marker);
}

// The event property path a '<property>.$eventContent.<path>' target reads, or null when it has none.
function eventContentKey(target: string): string | null {
    const marker = target.indexOf('.$');
    if (marker < 0) return null;
    const key = target.slice(marker + 1);
    if (!key.startsWith('$eventContent.')) return null;
    const path = key.slice('$eventContent.'.length);
    return path.length > 0 ? path : null;
}

export function validateProjectionTargets(application: ApplicationSyntax, context: ParserContext): void {
    const resolver = new AuthoringProductionResolver(application);
    const slices = resolver.slices;
    const imported = new Set(application.imports.map(entry => entry.qualifiedName));
    const events = new Set([...slices.flatMap(({ slice }) => eventDeclarations(slice).map(event => event.name)), ...application.imports.map(entry => entry.qualifiedName.split('.').at(-1)!)]);
    const types = uniqueByName(application.types);
    const scalar = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...application.concepts.map(concept => concept.name)].filter(name => !types.has(name)));
    const property = (properties: readonly PropertySyntax[] | null, path: string): { value: PropertySyntax | null; missing: boolean } => {
        const [name, ...rest] = path.split('.');
        if (properties === null) return { value: null, missing: false };
        const matches = properties.filter(entry => entry.name === name);
        if (matches.length !== 1) return { value: null, missing: matches.length === 0 };
        const value = matches[0];
        if (rest.length === 0) return { value, missing: false };
        if (scalar.has(value.type.name)) return { value: null, missing: true };
        return property(types.get(value.type.name)?.properties ?? null, rest.join('.'));
    };
    const views = slices.flatMap(({ slice, scope }) => slice.readModels.map(node => ({ node, scope })));
    const declaredEvents = slices.flatMap(({ slice, scope }) => eventDeclarations(slice).map(node => ({ node, scope })));
    // Resolves a declaration by name from a scope, as the C# ReferenceResolver does, falling back to a single import.
    const resolveScoped = <T extends { readonly name: string }>(entries: readonly { node: T; scope: readonly string[] }[], reference: string, scope: readonly string[]): T | null => {
        const candidates = (name: string) => {
            const segments = name.split('.');
            const named = entries.filter(entry => entry.node.name === segments.at(-1));
            const qualifiers = segments.slice(0, -1);
            if (qualifiers.length > 0) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
            for (let depth = scope.length; depth >= 0; depth--) {
                const visible = named.filter(entry => depth <= entry.scope.length && scope.slice(0, depth).every((value, index) => entry.scope[index] === value));
                if (visible.length > 0) return visible;
            }
            return [];
        };
        let matches = candidates(reference);
        const imports = application.imports.filter(entry => entry.qualifiedName.split('.').at(-1) === reference);
        if (matches.length === 0 && imports.length === 1) matches = candidates(imports[0].qualifiedName);
        return matches.length === 1 ? matches[0].node : null;
    };
    const viewProperties = (reference: string, scope: readonly string[]): readonly PropertySyntax[] | null => resolveScoped(views, reference, scope)?.properties ?? null;
    const eventProperties = (reference: string, scope: readonly string[]): readonly PropertySyntax[] | null => resolveScoped(declaredEvents, reference, scope)?.properties ?? null;
    const unknownEvent = (name: string, location: SourceLocation, slice: SliceSyntax): void => {
        const resolution = resolver.resolve(name, slice);
        const declared = events.has(name) || imported.has(name) || resolution.declaration?.kind === AuthoringProductionKind.Event || resolution.candidates.some(candidate => candidate.kind === AuthoringProductionKind.Event);
        if (!declared) context.warning(DiagnosticCodes.UnknownEvent, `Unknown event '${name}' - declare it with 'event ${name}'`, location);
    };
    const removals = (blocks: readonly ProjectionBlockSyntax[], slice: SliceSyntax): void => {
        for (const block of blocks) {
            if (block.kind === 'RemoveWithSyntax' || block.kind === 'RemoveViaJoinSyntax') unknownEvent(block.event, block.location, slice);
            if ('blocks' in block) removals(block.blocks, slice);
        }
    };
    const reported = new Set<string>();
    // Only a handler for named events knows which payload a $eventContent key reads; 'every' and 'all' see any event.
    const validateEventContentKeys = (block: ProjectionBlockSyntax, scope: readonly string[]): void => {
        const handlers = block.kind === 'FromSyntax'
            ? block.events.map(spec => ({ event: spec.event, mappings: block.mappings }))
            : block.kind === 'JoinSyntax' ? block.events.map(entry => ({ event: entry.event, mappings: entry.mappings })) : [];
        for (const { event, mappings } of handlers) {
            const payload = eventProperties(event, scope);
            if (payload === null) continue;
            for (const mapping of mappings) {
                const path = eventContentKey(mapping.property);
                if (path !== null && property(payload, path).missing) {
                    context.warning(DiagnosticCodes.UnknownEventContentKey, `Dynamic key '$eventContent.${path}' in '${mapping.property}' is not a property of event '${event}', so the entry is never written`, mapping.location);
                }
            }
        }
    };
    const walk = (blocks: readonly ProjectionBlockSyntax[], properties: readonly PropertySyntax[] | null, scope: readonly string[], element = false, effectiveHandlers = false): void => {
        const validate = (path: string, location: SourceLocation) => {
            const resolved = property(properties, path);
            const key = JSON.stringify([location.path, location.line, location.column]);
            if (resolved.missing && !reported.has(key)) {
                reported.add(key);
                context.warning(DiagnosticCodes.UnknownReadModelProperty, `Projection target '${path}' is not a declared ${element ? 'element' : 'read-model'} property`, location, diagnosticText('property', path));
            }
            return resolved.value;
        };
        // An 'every' without local handlers only cascades into the element scopes.
        const ownsEvents = effectiveHandlers || blocks.some(block => block.kind === 'FromSyntax' || block.kind === 'JoinSyntax' || block.kind === 'AllSyntax');
        for (const block of blocks) {
            const mappings = block.kind === 'EverySyntax' && !ownsEvents ? [] : 'mappings' in block ? block.mappings : block.kind === 'JoinSyntax' ? block.events.flatMap(event => event.mappings) : [];
            // A dynamic key names a dictionary entry, not a declared property: the property it writes into is what must exist.
            for (const mapping of mappings) {
                const basePath = baseProperty(mapping.property);
                const target = validate(basePath, mapping.location);
                // Chronicle accepts a dictionary entry only in an untyped or empty object property, and every declared Screenplay
                // type is a scalar, a collection or an object with properties.
                const key = JSON.stringify([mapping.location.path, mapping.location.line, mapping.location.column]);
                if (target !== null && mapping.property.includes('.$') && !reported.has(key)) {
                    reported.add(key);
                    context.warning(DiagnosticCodes.DynamicKeyOnNonDictionaryProperty, `'${mapping.property}' writes a dictionary entry into '${basePath}', declared as '${target.type.name}' - Screenplay has no dictionary type, so Chronicle refuses the projection when it registers it`, mapping.location);
                }
            }
            validateEventContentKeys(block, scope);
            if (block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax') {
                const target = validate(block.property, block.location);
                const elementProperties = target === null ? null : types.get(target.type.name)?.properties ?? null;
                const sortBy = block.kind === 'ChildrenSyntax' ? block.ordering?.sortBy ?? null : null;
                if (block.kind === 'ChildrenSyntax' && block.ordering && target !== null && elementProperties !== null && sortBy !== null && property(elementProperties, sortBy).missing) {
                    context.warning(DiagnosticCodes.UnknownChildrenSortProperty, `Children '${block.property}' sorts by '${sortBy}', which is not a declared property of its element type '${target.type.name}'`, block.ordering.location);
                }
                walk(block.blocks, elementProperties, scope, true);
            }
        }
    };
    for (const { slice } of slices) {
        for (const projection of slice.projections) removals(projection.blocks, slice);
        for (const capture of slice.captures) {
            for (const append of [...capture.appends, ...capture.children.flatMap(child => child.appends), ...capture.nested.flatMap(child => child.appends)]) unknownEvent(append.event, append.location, slice);
        }
    }
    for (const { slice, scope } of slices) {
        for (const projection of slice.projections) {
            const variants = projection.blocks.filter(block => block.kind === 'ProjectionVariantSyntax');
            if (variants.length === 0) walk(projection.blocks, viewProperties(projection.readModel ?? projection.name, scope), scope);
            for (const variant of variants) {
                const properties = viewProperties(variant.name, scope);
                walk(projection.blocks.filter(block => block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax'), properties, scope);
                // Lowering combines shared handlers and turns entering events into From handlers.
                const ownsEvents = variant.entersOn.length > 0 || projection.blocks.some(block => block.kind === 'FromSyntax' || block.kind === 'JoinSyntax' || block.kind === 'AllSyntax');
                walk(variant.blocks, properties, scope, false, ownsEvents);
            }
        }
    }
}
