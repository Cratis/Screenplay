// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { PropertySyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { ProjectionBlockSyntax } from '../Syntax/Projections';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { uniqueByName } from './ResponseValidator';

export function validateProjectionTargets(application: ApplicationSyntax, context: ParserContext): void {
    const slices = new AuthoringProductionResolver(application).slices;
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
    const viewProperties = (reference: string, scope: readonly string[]): readonly PropertySyntax[] | null => {
        const candidates = (name: string) => {
            const segments = name.split('.');
            const named = views.filter(entry => entry.node.name === segments.at(-1));
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
        return matches.length === 1 ? matches[0].node.properties : null;
    };
    const unknownEvent = (name: string, location: SourceLocation): void => {
        if (!events.has(name)) context.warning(DiagnosticCodes.UnknownEvent, `Unknown event '${name}' - declare it with 'event ${name}'`, location);
    };
    const removals = (blocks: readonly ProjectionBlockSyntax[]): void => {
        for (const block of blocks) {
            if (block.kind === 'RemoveWithSyntax' || block.kind === 'RemoveViaJoinSyntax') unknownEvent(block.event, block.location);
            if ('blocks' in block) removals(block.blocks);
        }
    };
    const walk = (blocks: readonly ProjectionBlockSyntax[], properties: readonly PropertySyntax[] | null): void => {
        const validate = (path: string, location: SourceLocation) => {
            const resolved = property(properties, path);
            if (resolved.missing) context.warning(DiagnosticCodes.UnknownReadModelProperty, `Projection target '${path}' is not a declared read-model property`, location);
            return resolved.value;
        };
        for (const block of blocks) {
            const mappings = 'mappings' in block ? block.mappings : block.kind === 'JoinSyntax' ? block.events.flatMap(event => event.mappings) : [];
            for (const mapping of mappings) validate(mapping.property, mapping.location);
            if (block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax') {
                const target = validate(block.property, block.location);
                walk(block.blocks, target === null ? null : types.get(target.type.name)?.properties ?? null);
            }
        }
    };
    for (const { slice } of slices) {
        for (const projection of slice.projections) removals(projection.blocks);
        for (const capture of slice.captures) {
            for (const append of [...capture.appends, ...capture.children.flatMap(child => child.appends), ...capture.nested.flatMap(child => child.appends)]) unknownEvent(append.event, append.location);
        }
    }
    for (const { slice, scope } of slices) {
        for (const projection of slice.projections) {
            const variants = projection.blocks.filter(block => block.kind === 'ProjectionVariantSyntax');
            if (variants.length === 0) walk(projection.blocks, viewProperties(projection.readModel ?? projection.name, scope));
            for (const variant of variants) {
                const properties = viewProperties(variant.name, scope);
                walk(projection.blocks.filter(block => block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax'), properties);
                walk(variant.blocks, properties);
            }
        }
    }
}
