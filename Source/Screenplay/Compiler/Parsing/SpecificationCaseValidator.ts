// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { EventSyntax, PropertySyntax, TypeRefSyntax } from '../Syntax/Declarations';
import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import { EventSourceCatalog } from '../Syntax/EventSourceCatalog';
import { EventSourceResolutionKind } from '../Syntax/EventSources';
import { SpecificationSyntax } from '../Syntax/Specifications';
import { commandDestinationType } from './CommandDestinationTypes';
import { ExpressionSyntax, PropertyMappingSyntax } from '../Syntax/Expressions';
import { ApplicationSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';
import { compatibleValue } from './ResponseValidator';
import { specificationExamples } from './SpecificationCommandExamples';

const primitives = new Set(['Uuid', 'String', 'Int', 'Decimal', 'Bool', 'Date', 'DateTime']);

export function validateSpecificationCases(application: ApplicationSyntax, context: ParserContext, standaloneSpecifications?: readonly SpecificationSyntax[]): void {
    const declarations = new RefusalDeclarations(application);
    const examples = specificationExamples(application);
    const knownType = (name: string) => primitives.has(name) || declarations.concepts.has(name) || declarations.types.has(name);
    const composites = new Map([...declarations.types].map(([name, type]) => [name, new Map(type.properties.map(property => [property.name, property]))]));
    const owners = standaloneSpecifications === undefined ? declarations.slices : [{ scope: [], slice: {
        kind: 'SliceSyntax' as const, type: 'StateChange' as const, name: '', description: null, location: application.location,
        events: [], commands: [], queries: [], projections: [], captures: [], reactions: [], constraints: [], readModels: [], screens: [], specifications: standaloneSpecifications,
    } }];
    for (const { slice, scope } of owners) {
        const names = new Map<string, number>();
        for (const specification of slice.specifications) {
            const expandedNames = (specification.cases?.length ?? 0) === 0 ? [specification.name] : specification.cases!.map(row => `${specification.name}_${row.name}`);
            for (const name of expandedNames) names.set(name, (names.get(name) ?? 0) + 1);
            if ((specification.cases?.length ?? 0) > 0) names.set(specification.name, (names.get(specification.name) ?? 0) + 1);
        }
        for (const specification of slice.specifications) {
            for (const row of specification.cases ?? []) {
                if ((names.get(`${specification.name}_${row.name}`) ?? 0) > 1) context.error(DiagnosticCodes.SpecificationCaseNameCollision, `Case '${row.name}' derives specification '${specification.name}_${row.name}', which collides in this ${standaloneSpecifications === undefined ? 'slice' : 'document'}.`, row.location);
            }
            for (const parameter of specification.parameters ?? []) {
                if (standaloneSpecifications === undefined && !knownType(parameter.type.name)) context.error(DiagnosticCodes.IncompatibleSpecificationParameterType, `Parameter '${parameter.name}' has unknown type '${parameter.type.name}'.`, parameter.type.location);
                for (const row of specification.cases ?? []) {
                    for (const assignment of row.values.filter(value => value.property === parameter.name)) {
                        if (!compatibleValue(assignment.source, parameter.type, declarations.concepts, composites)) context.error(DiagnosticCodes.InvalidSpecificationCaseValue, `Case '${row.name}' parameter '${parameter.name}' requires a concrete value of type '${parameter.type.name}'.`, assignment.source.location);
                    }
                }
            }
            const check = (expression: ExpressionSyntax | null, target: TypeRefSyntax | undefined, property: string) => {
                if (expression?.kind !== 'CaseValueExpressionSyntax' || target === undefined) return;
                const parameter = specification.parameters?.find(parameter => parameter.name === expression.parameter);
                if (parameter === undefined) return;
                if (parameter.type.isOptional && !target.isOptional) context.error(DiagnosticCodes.OptionalSpecificationParameterTarget, `Optional parameter '${parameter.name}' cannot supply required target '${property}'.`, expression.location);
                if (standaloneSpecifications !== undefined && (!knownType(parameter.type.name) || !knownType(target.name))) return;
                const sourceConcept = declarations.concepts.get(parameter.type.name);
                const targetConcept = declarations.concepts.get(target.name);
                const compatible = parameter.type.name === target.name || sourceConcept?.type !== 'Enum' && sourceConcept?.type === target.name || targetConcept?.type !== 'Enum' && targetConcept?.type === parameter.type.name;
                if (!compatible || parameter.type.isCollection !== target.isCollection) context.error(DiagnosticCodes.IncompatibleSpecificationParameterType, `Parameter '${parameter.name}' type '${parameter.type.name}' is incompatible with target '${property}' type '${target.name}'.`, expression.location);
            };
            const assignments = (values: readonly PropertyMappingSyntax[], properties: readonly PropertySyntax[] | null) => {
                for (const value of values) check(value.source, declarations.property(properties, value.property)?.type, value.property);
            };
            const command = examples.command(specification.when, scope);
            if (command !== null) {
                const properties = declarations.resolve(command.commandType, slice, slice => slice.commands)?.node.properties ?? null;
                assignments(command.values, properties);
                assignments(command.generatedValues ?? [], properties);
                check(command.for, properties?.find(property => property.isIdentifier)?.type, 'for');
            }
            if (command !== null && specification.thenReturns != null) {
                const declaration = declarations.resolve(command.commandType, slice, slice => slice.commands)?.node;
                const response = declaration?.response;
                if (specification.thenReturns.kind === 'ScalarSpecificationReturnSyntax' && response?.kind === 'ScalarCommandResponseSyntax') check(specification.thenReturns.value, declaration?.properties.find(property => property.name === response.source.property)?.type, 'value');
                if (specification.thenReturns.kind === 'RecordSpecificationReturnSyntax' && response?.kind === 'RecordCommandResponseSyntax') {
                    for (const value of specification.thenReturns.fields) {
                        const field = response.fields.find(field => field.name === value.property);
                        check(value.source, field?.type ?? declaration?.properties.find(property => property.name === field?.source.property)?.type, value.property);
                    }
                }
            }
            for (const authored of [...specification.given, ...specification.thenEvents, ...(specification.whenAppended === null ? [] : [specification.whenAppended])]) {
                const event = examples.eventStep(authored, 'given', scope).effective as typeof authored;
                assignments(event.values, declarations.event(event.eventType, slice)?.properties ?? null);
                const route = event.stream == null ? null : new EventSourceCatalog(application).resolve(event.stream.eventSource, event.stream.stream);
                const source = route?.kind === EventSourceResolutionKind.Unique ? route.sources[0] : undefined;
                const stream = route?.kind === EventSourceResolutionKind.Unique ? route.streams[0] : undefined;
                check(event.for, event.stream != null && source === undefined ? undefined : source?.identifier ?? eventDestinationType(event.eventType, slice, application, declarations), 'for');
                if (event.stream?.streamId != null) check(event.stream.streamId.source, stream?.streamId ?? undefined, 'streamId');
                for (const part of event.stream?.streamIdParts ?? []) check(part.source, stream?.streamIdParts.find(declaration => declaration.name === part.property)?.type, part.property);
            }
            for (const authored of [...specification.givenReadModels, ...specification.thenReadModels]) {
                const model = examples.readModel(authored, scope);
                assignments(model.properties, declarations.resolve(model.name, slice, slice => slice.readModels)?.node.properties ?? null);
            }
            if (specification.whenTrigger != null) {
                const trigger = (application.declaredTriggers ?? []).find(trigger => trigger.name === specification.whenTrigger!.trigger);
                for (const value of specification.whenTrigger.values) check(value.source, trigger?.data.find(property => property.name === value.property)?.type, value.property);
            }
            for (const error of specification.thenErrors) check(error.caseValue ?? null, { kind: 'TypeRefSyntax', name: 'String', isOptional: false, isCollection: false, location: error.location }, 'message');
            for (const absent of specification.thenAbsentReadModels ?? []) {
                const model = declarations.resolve(absent.name, slice, slice => slice.readModels)?.node;
                const keyed = declarations.slices.flatMap(({ slice: owner }) => owner.queries.filter(query => query.by !== null && declarations.resolve(query.returnType.name, owner, slice => slice.readModels)?.node === model));
                check(absent.key, model !== undefined && keyed.length === 1 ? keyed[0].by!.type : undefined, 'for');
            }
            for (const query of [...(specification.thenQueries ?? []), ...(specification.whenQuery === null ? [] : [specification.whenQuery])]) {
                const declaration = declarations.resolve(query.query, slice, slice => slice.queries);
                const parameters = declaration === null ? [] : [...(declaration.node.by === null ? [] : [declaration.node.by]), ...declaration.node.filters].map(parameter => ({ ...parameter, kind: 'PropertySyntax' as const, isIdentifier: false }));
                assignments(query.arguments, parameters);
                const properties = declaration === null ? null : declarations.resolve(declaration.node.returnType.name, declaration.slice, slice => slice.readModels)?.node.properties ?? null;
                const results = query.kind === 'SpecificationQuerySyntax' ? query.results : specification.thenResults;
                for (const result of results) assignments(result.properties, properties);
            }
        }
    }
}

function uniqueDestinationType(types: readonly TypeRefSyntax[]): TypeRefSyntax | undefined {
    const distinct = new Map(types.map(type => [JSON.stringify([type.name, type.isOptional, type.isCollection]), type]));
    return distinct.size === 1 ? [...distinct.values()][0] : undefined;
}

function eventDestinationType(reference: string, local: SliceSyntax, application: ApplicationSyntax, declarations: RefusalDeclarations): TypeRefSyntax | undefined {
    const resolver = new AuthoringProductionResolver(application);
    const event = declarations.event(reference, local);
    if (event === null) return undefined;
    const commandTypes = (event: EventSyntax, owners = declarations.slices): TypeRefSyntax[] => owners.flatMap(({ slice }) => slice.commands.flatMap(command => command.produces
        .filter(produced => resolver.isEventProduction(produced, slice) && declarations.event(produced.event, slice) === event)
        .map(produced => commandDestinationType(command, produced, application, { resolver, slice })).filter((type): type is TypeRefSyntax => type !== null)));
    const producerTypes = (event: EventSyntax, visited: Set<EventSyntax>): TypeRefSyntax[] => {
        if (visited.has(event)) return [];
        visited.add(event);
        const types = commandTypes(event);
        for (const { slice } of declarations.slices) {
            for (const trigger of slice.reactions.flatMap(reaction => reaction.triggers)) {
                const sourceName = trigger.source.kind === 'NamedTriggerSourceSyntax' ? trigger.source.name : undefined;
                const source = sourceName === undefined ? null : declarations.event(sourceName, slice);
                for (const produced of trigger.produces.filter(produced => declarations.event(produced.event, slice) === event)) {
                    if (produced.for?.kind === 'PathExpressionSyntax') {
                        const path = produced.for.path;
                        const type = declarations.property(source?.properties ?? null, path)?.type ?? (sourceName === undefined ? undefined
                            : (application.declaredTriggers ?? []).find(declared => declared.name === sourceName)?.data.find(property => property.name === path)?.type);
                        if (type !== undefined) types.push(type);
                    } else if (produced.for?.kind === 'LiteralExpressionSyntax') types.push({ kind: 'TypeRefSyntax', name: 'String', isOptional: false, isCollection: false, location: produced.location });
                    else if (produced.for === null && source !== null) types.push(...producerTypes(source, visited));
                }
            }
            if (slice.captures.flatMap(capture => [...capture.appends, ...capture.children.flatMap(child => child.appends), ...capture.nested.flatMap(nested => nested.appends)]).some(append => declarations.event(append.event, slice) === event))
                types.push(uniqueDestinationType(commandTypes(event)) ?? { kind: 'TypeRefSyntax', name: 'String', isOptional: false, isCollection: false, location: event.location });
        }
        return types;
    };
    return uniqueDestinationType(commandTypes(event, declarations.slices.filter(owner => owner.slice === local))) ?? uniqueDestinationType(commandTypes(event)) ?? uniqueDestinationType(producerTypes(event, new Set()));
}
