// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { AuthoringProductionKind, AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { EventSyntax, PropertySyntax } from '../Syntax/Declarations';
import { dependencySourcesOf } from '../Syntax/DependencySources';
import { AutoMapMode, MappingSyntax, ProjectionBlockSyntax, ProjectionSyntax } from '../Syntax/Projections';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { ProjectionShapes, projectionShapes } from './ProjectionShapes';

const aggregates = new Set(['AddMappingSyntax', 'SubtractMappingSyntax', 'IncrementMappingSyntax', 'DecrementMappingSyntax', 'CountMappingSyntax']);

const message = (owner: string, name: string, concept: string, property: string): string =>
    `${owner} '${name}' copies secret concept '${concept}' into read model property '${property}' - the value becomes part of the read model and its query results`;

const enabled = (mode: AutoMapMode, inherited: boolean): boolean => mode === 'Enabled' ? true : mode === 'Disabled' ? false : inherited;

/**
 * Pairs the read-model properties AutoMap fills from same-named event properties, mirroring the C# ProjectionAutoMap.
 * @param mappings The explicit mappings of the block.
 * @param targets The read-model or element properties.
 * @param sources The event properties.
 * @param isJoin Whether the block is a join, which keeps auto-mapping beside aggregates and skips explicitly mapped sources.
 * @returns The target and source of every auto-mapped pair.
 */
function autoMapped(mappings: readonly MappingSyntax[], targets: readonly PropertySyntax[], sources: readonly PropertySyntax[], isJoin: boolean): [PropertySyntax, PropertySyntax][] {
    if (!isJoin && mappings.length > 0 && mappings.every(mapping => aggregates.has(mapping.kind))) return [];
    const mapped = new Set(mappings.map(mapping => mapping.property.split('.').at(-1)!.toLowerCase()));
    const usedSources = new Set(isJoin ? mappings.flatMap(mapping => mapping.kind === 'SetMappingSyntax' && mapping.source?.kind === 'PathExpressionSyntax' ? [mapping.source.path.toLowerCase()] : []) : []);
    const pairs: [PropertySyntax, PropertySyntax][] = [];
    for (const source of sources) {
        const target = targets.find(candidate => candidate.name.toLowerCase() === source.name.toLowerCase());
        if (!target || mapped.has(target.name.toLowerCase()) || usedSources.has(source.name.toLowerCase()) || !sameType(target, source)) continue;
        mapped.add(target.name.toLowerCase());
        pairs.push([target, source]);
    }
    return pairs;
}

function sameType(target: PropertySyntax, source: PropertySyntax): boolean {
    return target.type.name === source.type.name && target.type.isCollection === source.type.isCollection && (!source.type.isOptional || target.type.isOptional);
}

/** Walks one projection and reports every read-model property it copies a secret value into. */
class SecretProjectionMappings {
    readonly #reported = new Set<string>();

    constructor(
        private readonly projection: ProjectionSyntax,
        private readonly slice: SliceSyntax,
        private readonly scope: readonly string[],
        private readonly secrets: ReadonlySet<string>,
        private readonly shapes: ProjectionShapes,
        private readonly resolver: AuthoringProductionResolver,
        private readonly context: ParserContext) { }

    validate(): void {
        const autoMap = this.projection.autoMap !== 'Disabled';
        const shared = this.projection.blocks.filter(block => block.kind !== 'ProjectionVariantSyntax');
        const variants = this.projection.blocks.flatMap(block => block.kind === 'ProjectionVariantSyntax' ? [block] : []);
        if (variants.length === 0) {
            const readModel = this.projection.readModel ?? this.projection.name;
            this.walk(shared, this.shapes.viewProperties(readModel, this.scope), readModel, autoMap);
        }
        for (const variant of variants) this.walk([...shared, ...variant.blocks], this.shapes.viewProperties(variant.name, this.scope), variant.name, autoMap);
    }

    private walk(blocks: readonly ProjectionBlockSyntax[], targets: readonly PropertySyntax[] | null, owner: string, autoMap: boolean): void {
        if (targets === null) return;
        for (const block of blocks) {
            if (block.kind === 'FromSyntax') {
                const events = block.events.flatMap(source => this.event(source.event));
                this.copies(block.mappings, events, targets, owner, autoMap, false, block.location);
            } else if (block.kind === 'JoinSyntax') {
                for (const joined of block.events) this.copies(joined.mappings, this.event(joined.event), targets, owner, enabled(joined.autoMap, autoMap), true, joined.location);
            } else if (block.kind === 'ChildrenSyntax' || block.kind === 'NestedSyntax') {
                const property = this.shapes.property(targets, block.property).value;
                if (property !== null) this.walk(block.blocks, this.shapes.types.get(property.type.name)?.properties ?? null, `${owner}.${block.property}`, enabled(block.autoMap, autoMap));
            }
        }
    }

    private event(name: string): EventSyntax[] {
        const declaration = this.resolver.resolve(name, this.slice).declaration;
        return declaration?.kind === AuthoringProductionKind.Event ? [declaration.node as EventSyntax] : [];
    }

    private copies(mappings: readonly MappingSyntax[], events: readonly EventSyntax[], targets: readonly PropertySyntax[], owner: string, autoMap: boolean, isJoin: boolean, location: SourceLocation): void {
        for (const mapping of mappings) if (mapping.kind === 'SetMappingSyntax') this.explicit(mapping, events, targets, owner);
        if (!autoMap) return;
        for (const event of events) {
            for (const [target] of autoMapped(mappings, targets, event.properties, isJoin)) {
                if (this.secrets.has(target.type.name)) this.report(target.type.name, `${owner}.${target.name}`, location);
            }
        }
    }

    private explicit(mapping: MappingSyntax, events: readonly EventSyntax[], targets: readonly PropertySyntax[], owner: string): void {
        if (mapping.source?.kind !== 'PathExpressionSyntax' || mapping.source.path.startsWith('$')) return;
        const path = mapping.source.path;
        const concept = this.secret(this.shapes.property(targets, mapping.property).value)
            ?? events.map(event => this.secret(this.shapes.property(event.properties, path).value)).find(name => name !== null)
            ?? null;
        if (concept !== null) this.report(concept, `${owner}.${mapping.property}`, mapping.location);
    }

    private secret(property: PropertySyntax | null): string | null {
        return property !== null && this.secrets.has(property.type.name) ? property.type.name : null;
    }

    private report(concept: string, property: string, location: SourceLocation): void {
        const key = JSON.stringify([property, location.path, location.line, location.column]);
        if (this.#reported.has(key)) return;
        this.#reported.add(key);
        this.context.warning(DiagnosticCodes.SecretConceptProjected, message('Projection', this.projection.name, concept, property), location);
    }
}

/**
 * Warns when a projection or reducer copies a value whose concept is marked secret into a read-model property:
 * the value then becomes part of the read model and of every query result over it.
 * @param application The application to validate.
 * @param context The context diagnostics are reported to.
 */
export function validateSecretProjections(application: ApplicationSyntax, context: ParserContext): void {
    const secrets = new Set(application.concepts.filter(concept => concept.attributes.some(attribute => attribute.name === 'sensitive')).map(concept => concept.name));
    if (secrets.size === 0) return;
    const resolver = new AuthoringProductionResolver(application);
    const shapes = projectionShapes(application, resolver.slices);
    for (const { slice, scope } of resolver.slices) {
        for (const projection of slice.projections) new SecretProjectionMappings(projection, slice, scope, secrets, shapes, resolver, context).validate();
        // A reducer's rules are code the compiler does not read, so every secret read-model property it builds is a copied value.
        for (const reducer of dependencySourcesOf(slice).reducers ?? []) {
            for (const property of (shapes.viewProperties(reducer.readModel, scope) ?? []).filter(property => secrets.has(property.type.name)))
                context.warning(DiagnosticCodes.SecretConceptProjected, message('Reducer', reducer.name, property.type.name, `${reducer.readModel}.${property.name}`), reducer.location);
        }
    }
}
