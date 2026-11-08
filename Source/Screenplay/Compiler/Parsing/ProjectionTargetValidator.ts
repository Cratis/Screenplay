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
import { projectionShapes } from './ProjectionShapes';

export function validateProjectionTargets(application: ApplicationSyntax, context: ParserContext): void {
    const resolver = new AuthoringProductionResolver(application);
    const slices = resolver.slices;
    const imported = new Set(application.imports.map(entry => entry.qualifiedName));
    const events = new Set([...slices.flatMap(({ slice }) => eventDeclarations(slice).map(event => event.name)), ...application.imports.map(entry => entry.qualifiedName.split('.').at(-1)!)]);
    const { types, property, viewProperties } = projectionShapes(application, slices);
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
    const walk = (blocks: readonly ProjectionBlockSyntax[], properties: readonly PropertySyntax[] | null, element = false, effectiveHandlers = false): void => {
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
            for (const mapping of mappings) validate(mapping.property, mapping.location);
            if (block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax') {
                const target = validate(block.property, block.location);
                walk(block.blocks, target === null ? null : types.get(target.type.name)?.properties ?? null, true);
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
            if (variants.length === 0) walk(projection.blocks, viewProperties(projection.readModel ?? projection.name, scope));
            for (const variant of variants) {
                const properties = viewProperties(variant.name, scope);
                walk(projection.blocks.filter(block => block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax'), properties);
                // Lowering combines shared handlers and turns entering events into From handlers.
                const ownsEvents = variant.entersOn.length > 0 || projection.blocks.some(block => block.kind === 'FromSyntax' || block.kind === 'JoinSyntax' || block.kind === 'AllSyntax');
                walk(variant.blocks, properties, false, ownsEvents);
            }
        }
    }
}
