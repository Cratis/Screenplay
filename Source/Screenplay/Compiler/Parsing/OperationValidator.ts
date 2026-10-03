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

export function validateOperations(application: ApplicationSyntax, context: ParserContext): void {
    const resolver = new AuthoringProductionResolver(application);
    const concepts = uniqueByName(application.concepts);
    const composites = new Map([...uniqueByName(application.types)].map(([name, type]) => [name, uniqueByName(type.properties)]));
    const known = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime', ...concepts.keys(), ...composites.keys()]);
    const imported = new Set(application.imports.map(entry => entry.qualifiedName.split('.').at(-1)!));
    const systems = new Map<string, number>();
    for (const system of application.systems ?? []) {
        if (systems.has(system.name)) context.error(DiagnosticCodes.InvalidSystemDeclaration, `System '${system.name}' is declared more than once.`, system.location);
        systems.set(system.name, (systems.get(system.name) ?? 0) + 1);
    }
    const pathType = (properties: readonly PropertySyntax[], path: string): { type: TypeRefSyntax | null; missing: boolean } => {
        const [name, ...nested] = path.split('.');
        const matches = properties.filter(property => property.name === name);
        if (matches.length !== 1) return { type: null, missing: matches.length === 0 };
        const type = matches[0].type;
        if (nested.length === 0) return { type, missing: false };
        const next = composites.get(type.name);
        if (next === undefined) return { type: null, missing: known.has(type.name) };
        const resolved = pathType([...next.values()], nested.join('.'));
        return resolved.type === null ? resolved : { type: { ...resolved.type, isOptional: type.isOptional || resolved.type.isOptional, isCollection: type.isCollection || resolved.type.isCollection }, missing: false };
    };
    const covered = (type: TypeRefSyntax, path: string, mappings: ReadonlySet<string>, seen = new Set<string>()): boolean => {
        if (type.isOptional || mappings.has(path)) return true;
        const properties = composites.get(type.name);
        if (type.isCollection || seen.has(type.name) || properties === undefined) return false;
        return [...properties.values()].every(property => covered(property.type, `${path}.${property.name}`, mappings, new Set([...seen, type.name])));
    };
    const completeValue = (value: PropertyMappingSyntax['source'], type: TypeRefSyntax): boolean => {
        if (value.kind === 'ListExpressionSyntax' && type.isCollection) return value.items.every(item => completeValue(item, { ...type, isCollection: false, isOptional: false }));
        const properties = composites.get(type.name);
        if (value.kind !== 'ObjectExpressionSyntax' || properties === undefined) return true;
        return [...properties.values()].every(property => (property.type.isOptional || value.members.some(member => member.name === property.name)) && value.members.filter(member => member.name === property.name).every(member => completeValue(member.value, property.type)));
    };
    const validateMappings = (mappings: readonly PropertyMappingSyntax[], operation: OperationSyntax, command: CommandSyntax | null, required: boolean): void => {
        const names = new Set<string>();
        for (const mapping of mappings) {
            if (names.has(mapping.property)) context.error(DiagnosticCodes.InvalidOperationMapping, `Duplicate operation input mapping '${mapping.property}'.`, mapping.location);
            names.add(mapping.property);
            const target = pathType(operation.inputs, mapping.property);
            if (target.missing) context.error(DiagnosticCodes.InvalidOperationMapping, `Operation '${operation.name}' declares no input '${mapping.property}'.`, mapping.location);
            if (target.type === null) continue;
            if (command !== null && mapping.source.kind === 'PathExpressionSyntax') {
                let supplied = pathType(command.properties, mapping.source.path);
                const segments = mapping.source.path.split('.');
                const reads = (commandReadSources.get(command) ?? []).filter(read => (read.alias ?? read.readModel) === segments[0]);
                if (supplied.missing && reads.length > 0) {
                    supplied = { type: null, missing: false };
                    const views = resolver.slices.flatMap(entry => entry.slice.readModels).filter(view => view.name === reads[0].readModel);
                    if (reads.length === 1 && segments.length > 1 && views.length === 1) supplied = pathType(views[0].properties, segments.slice(1).join('.'));
                }
                if (supplied.missing) context.error(DiagnosticCodes.InvalidOperationMapping, `Command '${command.name}' declares no source '${mapping.source.path}'.`, mapping.source.location);
                const source = supplied.type;
                const type = target.type;
                if (source !== null && known.has(source.name) && known.has(type.name) && (source.name !== type.name || source.isCollection !== type.isCollection || source.isOptional && !type.isOptional))
                    context.error(DiagnosticCodes.InvalidOperationMapping, `Source '${mapping.source.path}' is incompatible with input '${mapping.property}' of operation '${operation.name}'.`, mapping.source.location);
            } else if (['LiteralExpressionSyntax', 'ObjectExpressionSyntax', 'ListExpressionSyntax'].includes(mapping.source.kind)) {
                if (!compatibleValue(mapping.source, target.type, concepts, composites) || required && !completeValue(mapping.source, target.type)) context.error(DiagnosticCodes.InvalidOperationMapping, `Value is incompatible with input '${mapping.property}' of operation '${operation.name}'.`, mapping.source.location);
            } else if (command === null) context.error(DiagnosticCodes.InvalidOperationSpecification, 'Operation assertions require concrete input values.', mapping.source.location);
        }
        if (required) for (const input of operation.inputs) {
            if (!covered(input.type, input.name, names)) context.error(DiagnosticCodes.InvalidOperationMapping, `Production of operation '${operation.name}' supplies no value for required input '${input.name}'.`, operation.location);
        }
    };
    for (const { slice } of resolver.slices) {
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
            validateMappings(production.mappings, resolution.declaration.node, command, true);
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
            const commandEntries = resolver.slices.flatMap(entry => entry.slice.commands.map(command => ({ command, slice: entry.slice, scope: entry.scope })));
            const from = resolver.slices.find(entry => entry.slice === slice)!.scope;
            const segments = (specification.when?.commandType ?? '').split('.');
            const qualifiers = segments.slice(0, -1);
            let commands = commandEntries.filter(entry => entry.command.name === segments.at(-1));
            if (qualifiers.length > 0) commands = commands.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
            else for (let depth = from.length; depth >= 0; depth--) {
                const visible = commands.filter(entry => depth <= entry.scope.length && from.slice(0, depth).every((value, index) => entry.scope[index] === value));
                if (visible.length > 0) { commands = visible; break; }
            }
            const command = commands.length === 1 ? commands[0] : null;
            if (command === null || specification.whenAppended !== null || specification.whenQuery !== null || specification.whenTrigger !== null || specification.whenClock !== null || specification.whenCapture !== null)
                context.error(DiagnosticCodes.InvalidOperationSpecification, 'Operation fixtures and assertions require a declared command action.', specification.location);
            const duplicates = new Map<SyntaxNode, Set<string>>();
            for (const step of steps) {
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
                if (step.kind === 'SpecificationOperationSyntax') validateMappings(step.values, node, null, false);
            }
        }
    }
}
