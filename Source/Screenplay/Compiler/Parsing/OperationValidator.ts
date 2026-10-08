// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { AuthoringProductionKind, AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { CommandSyntax } from '../Syntax/Commands';
import { PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { operationDeclarations, OperationSyntax } from '../Syntax/Operations';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { ApplicationSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { commandReadSources } from './CommandReadSources';
import { compatibleValue, uniqueByName } from './ResponseValidator';
import { specificationCommandExamples } from './SpecificationCommandExamples';

export function validateOperations(application: ApplicationSyntax, context: ParserContext): void {
    const resolver = new AuthoringProductionResolver(application);
    const effectiveCommand = specificationCommandExamples(application);
    const concepts = uniqueByName(application.concepts);
    const composites = new Map([...uniqueByName(application.types)].map(([name, type]) => [name, uniqueByName(type.properties)]));
    const known = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...concepts.keys(), ...composites.keys()]);
    const imported = new Set(application.imports.map(entry => entry.qualifiedName.split('.').at(-1)!));
    const imports = new Map<string, string[]>();
    for (const entry of application.imports) {
        const name = entry.qualifiedName.split('.').at(-1)!;
        imports.set(name, [...imports.get(name) ?? [], entry.qualifiedName]);
    }
    const scoped = <T>(entries: readonly { name: string; scope: readonly string[]; node: T }[]) => {
        const byName = new Map<string, typeof entries[number][]>();
        const cache = new Map<string, T | null>();
        for (const entry of entries) {
            const named = byName.get(entry.name) ?? [];
            named.push(entry);
            byName.set(entry.name, named);
        }
        return (reference: string, from: readonly string[]): T | null => {
            const key = JSON.stringify([reference, from]);
            if (cache.has(key)) return cache.get(key)!;
            const candidates = (name: string) => {
                const segments = name.split('.');
                const named = byName.get(segments.at(-1)!) ?? [];
                const qualifiers = segments.slice(0, -1);
                if (qualifiers.length > 0) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
                for (let depth = from.length; depth >= 0; depth--) {
                    const visible = named.filter(entry => depth <= entry.scope.length && from.slice(0, depth).every((value, index) => entry.scope[index] === value));
                    if (visible.length > 0) return visible;
                }
                return [];
            };
            let matches = candidates(reference);
            const imported = imports.get(reference) ?? [];
            if (matches.length === 0 && imported.length === 1) matches = candidates(imported[0]);
            const result = matches.length === 1 ? matches[0].node : null;
            cache.set(key, result);
            return result;
        };
    };
    const view = scoped(resolver.slices.flatMap(({ slice, scope }) => slice.readModels.map(node => ({ name: node.name, scope, node }))));
    const resolveCommand = scoped(resolver.slices.flatMap(({ slice, scope }) => slice.commands.map(command => ({ name: command.name, scope, node: { command, slice } }))));
    const systems = new Map<string, number>();
    for (const system of application.systems ?? []) {
        if (systems.has(system.name)) context.error(DiagnosticCodes.InvalidSystemDeclaration, `System '${system.name}' is declared more than once.`, system.location);
        systems.set(system.name, (systems.get(system.name) ?? 0) + 1);
    }
    const pathType = (properties: readonly PropertySyntax[], path: string, inheritOptionality = true): { type: TypeRefSyntax | null; missing: boolean } => {
        const [name, ...nested] = path.split('.');
        const matches = properties.filter(property => property.name === name);
        if (matches.length !== 1) return { type: null, missing: matches.length === 0 };
        const type = matches[0].type;
        if (nested.length === 0) return { type, missing: false };
        const next = composites.get(type.name);
        if (next === undefined) return { type: null, missing: known.has(type.name) };
        const resolved = pathType([...next.values()], nested.join('.'), inheritOptionality);
        return resolved.type === null ? resolved : { type: { ...resolved.type, isOptional: inheritOptionality && type.isOptional || resolved.type.isOptional, isCollection: type.isCollection || resolved.type.isCollection }, missing: false };
    };
    const covered = (type: TypeRefSyntax, path: string, mappings: ReadonlySet<string>, supplied: ReadonlySet<string>, seen = new Set<string>()): boolean => {
        if (mappings.has(path) || type.isOptional && !supplied.has(path)) return true;
        if (type.isCollection || seen.has(type.name)) return false;
        const properties = composites.get(type.name);
        if (properties === undefined) return supplied.has(path) && !known.has(type.name);
        return [...properties.values()].every(property => covered(property.type, `${path}.${property.name}`, mappings, supplied, new Set([...seen, type.name])));
    };
    const completeValue = (value: PropertyMappingSyntax['source'], type: TypeRefSyntax): boolean => {
        if (value.kind === 'ListExpressionSyntax' && type.isCollection) return value.items.every(item => completeValue(item, { ...type, isCollection: false, isOptional: false }));
        const properties = composites.get(type.name);
        if (value.kind !== 'ObjectExpressionSyntax' || properties === undefined) return true;
        return [...properties.values()].every(property => (property.type.isOptional || value.members.some(member => member.name === property.name)) && value.members.filter(member => member.name === property.name).every(member => completeValue(member.value, property.type)));
    };
    const concrete = (value: PropertyMappingSyntax['source']): boolean => {
        if (value.kind === 'LiteralExpressionSyntax') return typeof value.value !== 'number' || Number.isFinite(value.value);
        if (value.kind === 'ObjectExpressionSyntax') return value.members.every(member => concrete(member.value));
        return value.kind === 'ListExpressionSyntax' && value.items.every(concrete);
    };
    const validateMappings = (mappings: readonly PropertyMappingSyntax[], operation: OperationSyntax, command: CommandSyntax | null, required: boolean, scope: readonly string[]): void => {
        const names = new Set<string>();
        const allNames = new Set(mappings.map(mapping => mapping.property));
        const suppliedPaths = new Set<string>();
        for (const mapping of mappings) {
            const segments = mapping.property.split('.');
            for (let depth = 1; depth <= segments.length; depth++) suppliedPaths.add(segments.slice(0, depth).join('.'));
        }
        for (const mapping of mappings) {
            if (names.has(mapping.property)) context.error(DiagnosticCodes.InvalidOperationMapping, `Duplicate operation input mapping '${mapping.property}'.`, mapping.location);
            names.add(mapping.property);
            const target = pathType(operation.inputs, mapping.property, false);
            if (target.missing) context.error(DiagnosticCodes.InvalidOperationMapping, `Operation '${operation.name}' declares no input '${mapping.property}'.`, mapping.location);
            if (target.type === null) continue;
            const targetSegments = mapping.property.split('.');
            for (let depth = 1; depth < targetSegments.length; depth++) if (allNames.has(targetSegments.slice(0, depth).join('.')))
                context.error(DiagnosticCodes.InvalidOperationMapping, `Operation input mapping '${mapping.property}' overlaps a whole input mapping.`, mapping.location);
            if (command !== null && mapping.source.kind === 'PathExpressionSyntax') {
                let supplied = pathType(command.properties, mapping.source.path);
                const segments = mapping.source.path.split('.');
                const reads = (commandReadSources.get(command) ?? []).filter(read => (read.alias ?? read.readModel) === segments[0]);
                if (supplied.missing && reads.length > 0) {
                    supplied = { type: null, missing: false };
                    const resolvedView = reads.length === 1 ? view(reads[0].readModel, scope) : null;
                    if (segments.length > 1 && resolvedView !== null) supplied = pathType(resolvedView.properties, segments.slice(1).join('.'));
                }
                if (supplied.missing) context.error(DiagnosticCodes.InvalidOperationMapping, `Command '${command.name}' declares no source '${mapping.source.path}'.`, mapping.source.location);
                const source = supplied.type;
                const type = target.type;
                if (source !== null && known.has(source.name) && known.has(type.name) && (source.name !== type.name || source.isCollection !== type.isCollection || source.isOptional && !type.isOptional))
                    context.error(DiagnosticCodes.InvalidOperationMapping, `Source '${mapping.source.path}' is incompatible with input '${mapping.property}' of operation '${operation.name}'.`, mapping.source.location);
            } else if (['LiteralExpressionSyntax', 'ObjectExpressionSyntax', 'ListExpressionSyntax'].includes(mapping.source.kind)) {
                if (!compatibleValue(mapping.source, target.type, concepts, composites) || required && !completeValue(mapping.source, target.type)) context.error(DiagnosticCodes.InvalidOperationMapping, `Value is incompatible with input '${mapping.property}' of operation '${operation.name}'.`, mapping.source.location);
            }
        }
        if (required) for (const input of operation.inputs) {
            if (!covered(input.type, input.name, names, suppliedPaths)) context.error(DiagnosticCodes.InvalidOperationMapping, `Production of operation '${operation.name}' supplies no value for required input '${input.name}'.`, operation.location);
        }
    };
    for (const { slice, scope } of resolver.slices) {
        const operations = operationDeclarations(slice);
        const names = new Set(eventDeclarations(slice).map(event => event.name));
        for (const operation of operations) {
            if (names.has(operation.name)) context.error(DiagnosticCodes.ProductionDeclarationCollision, `Operation '${operation.name}' collides with another event or operation in slice '${slice.name}'.`, operation.location);
            names.add(operation.name);
            if (operation.uses.length > 0 && systems.get(operation.uses) !== 1) context.error(DiagnosticCodes.InvalidSystemReference, `Operation '${operation.name}' must use one declared system; '${operation.uses}' does not resolve uniquely.`, operation.usesLocation ?? operation.location);
            for (const input of operation.inputs) {
                if (!known.has(input.type.name) && !imported.has(input.type.name)) context.warning(DiagnosticCodes.UnknownType, `Unknown type '${input.type.name}' on input '${input.name}' of operation '${operation.name}'.`, input.type.location);
            }
        }
        for (const command of slice.commands) for (const production of command.produces) {
            const resolution = resolver.resolve(production.event, slice);
            if (resolution.kind === AuthoringProductionKind.Ambiguous && resolution.candidates.some(node => node.kind === AuthoringProductionKind.Operation)) context.error(DiagnosticCodes.InvalidProductionReference, `Production '${production.event}' is ambiguous.`, production.location);
            if (production.event.includes('.') && resolution.kind !== AuthoringProductionKind.Operation) context.error(DiagnosticCodes.InvalidProductionReference, 'Qualified productions are supported only for explicitly declared operations.', production.location);
            if (resolution.declaration?.node.kind !== 'OperationSyntax') continue;
            if (production.for !== null || production.tags.length > 0) context.error(DiagnosticCodes.InvalidOperationDeclaration, 'Operation productions cannot declare event destinations or tags.', production.location);
            validateMappings(production.mappings, resolution.declaration.node, command, true, scope);
        }
        for (const production of slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.produces)) {
            const resolution = resolver.resolve(production.event, slice);
            if (resolution.kind === AuthoringProductionKind.Ambiguous && resolution.candidates.some(node => node.kind === AuthoringProductionKind.Operation)) context.error(DiagnosticCodes.InvalidProductionReference, `Production '${production.event}' is ambiguous.`, production.location);
            if (production.event.includes('.') && resolver.resolve(production.event, slice).kind !== AuthoringProductionKind.Operation) context.error(DiagnosticCodes.InvalidProductionReference, 'Qualified productions are supported only for explicitly declared operations.', production.location);
            if (resolver.isOperation(production, slice)) context.error(DiagnosticCodes.OperationOutsideCommand, 'Operations can only be produced by commands.', production.location);
        }
        for (const specification of slice.specifications) {
            const steps = [...specification.givenOperationFailures ?? [], ...specification.thenOperations ?? [], ...specification.thenCompensated ?? []];
            if (steps.length === 0) continue;
            const command = resolveCommand(effectiveCommand(specification.when, scope)?.commandType ?? '', scope);
            if (command === null || specification.whenAppended !== null || specification.whenQuery !== null || specification.whenTrigger !== null || specification.whenClock !== null || specification.whenCapture !== null)
                context.error(DiagnosticCodes.InvalidOperationSpecification, 'Operation fixtures and assertions require a declared command action.', specification.location);
            const duplicates = new Map<SyntaxNode, Set<string>>();
            for (const step of steps) {
                if (step.kind === 'SpecificationOperationSyntax') for (const mapping of step.values) {
                    if (!concrete(mapping.source)) context.error(DiagnosticCodes.InvalidOperationSpecification, 'Operation assertions require concrete input values.', mapping.source.location);
                }
                const node = resolver.resolve(step.operation, slice).declaration?.node;
                if (node?.kind !== 'OperationSyntax') {
                    context.error(DiagnosticCodes.InvalidOperationSpecification, `'${step.operation}' does not resolve uniquely to a declared operation.`, step.location);
                    continue;
                }
                const kinds = duplicates.get(node) ?? new Set<string>();
                if (kinds.has(step.kind)) context.error(DiagnosticCodes.InvalidOperationSpecification, `Duplicate operation step '${step.operation}'.`, step.location);
                kinds.add(step.kind);
                duplicates.set(node, kinds);
                if (step.kind === 'SpecificationCompensatedSyntax' && node.compensate === null) context.error(DiagnosticCodes.InvalidOperationSpecification, `Operation '${step.operation}' declares no compensation.`, step.location);
                if (command !== null && command.command.handler == null) {
                    const produced = command.command.produces.map(production => resolver.resolve(production.event, command.slice));
                    if (produced.every(result => result.kind === AuthoringProductionKind.Event || result.kind === AuthoringProductionKind.Operation) && !produced.some(result => result.declaration?.node === node))
                        context.error(DiagnosticCodes.InvalidOperationSpecification, `Command '${command.command.name}' does not produce operation '${step.operation}'.`, step.location);
                }
                if (step.kind === 'SpecificationOperationSyntax') validateMappings(step.values, node, null, false, scope);
            }
        }
    }
}
