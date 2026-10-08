// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { dependencySources, dependencySourcesOf } from '../Syntax/DependencySources';
import { eventDeclarations } from '../Syntax/EventDeclarations';
import { EffectiveSpecification, EffectiveSpecificationStep, EffectiveSpecificationValue, EffectiveSpecificationApplication } from '../Syntax/EffectiveSpecification';
import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationCommandSyntax, SpecificationEventSyntax, SpecificationReadModelSyntax } from '../Syntax/Specifications';
import { SpecificationExampleSyntax } from '../Syntax/SpecificationExampleSyntax';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';

// Validators consume effective fixtures without changing the authored syntax.
// Resolve the example at its use site, then its type at the declaration site.
export function specificationExamples(application: ApplicationSyntax) {
    const entries: { name: string; scope: readonly string[]; example?: SpecificationExampleSyntax; kind?: string }[] = [];
    const examples = (nodes: readonly SpecificationExampleSyntax[] | undefined, scope: readonly string[]) => {
        for (const example of nodes ?? []) entries.push({ name: example.name, scope, example });
    };
    const features = (nodes: readonly FeatureSyntax[], parent: readonly string[]) => {
        for (const feature of nodes) {
            const scope = [...parent, feature.name];
            examples(feature.examples, scope);
            for (const slice of feature.slices) {
                const scope = [...parent, feature.name, slice.name];
                examples(slice.examples, scope);
                for (const name of new Set(eventDeclarations(slice).map(event => event.name))) entries.push({ name, scope, kind: 'event' });
                for (const command of slice.commands) entries.push({ name: command.name, scope, kind: 'command' });
                for (const model of slice.readModels) entries.push({ name: model.name, scope, kind: 'readmodel' });
            }
            features(feature.features, scope);
        }
    };
    examples(application.examples, []);
    for (const node of [...application.concepts, ...application.types]) entries.push({ name: node.name, scope: [], kind: 'type' });
    for (const module of application.modules) { examples(module.examples, [module.name]); features(module.features, [module.name]); }
    const resolve = (reference: string, scope: readonly string[]) => {
        const candidates = (reference: string) => {
            const parts = reference.split('.');
            const named = entries.filter(entry => entry.name === parts.at(-1));
            const qualifiers = parts.slice(0, -1);
            if (qualifiers.length) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((part, index) => entry.scope[entry.scope.length - qualifiers.length + index] === part));
            for (let depth = scope.length; depth >= 0; depth--) {
                const visible = named.filter(entry => depth <= entry.scope.length && scope.slice(0, depth).every((part, index) => entry.scope[index] === part));
                if (visible.length) return visible;
            }
            return [];
        };
        let matches = candidates(reference);
        const imported = application.imports.filter(entry => entry.qualifiedName.split('.').at(-1) === reference);
        if (!matches.length && imported.length === 1) matches = candidates(imported[0].qualifiedName);
        return matches.length === 1 ? matches[0] : undefined;
    };
    const fixture = (reference: string, kind: string, scope: readonly string[]) => {
        const declaration = resolve(reference, scope);
        const example = declaration?.example;
        if (!example) return null;
        const type = resolve(example.type, declaration.scope);
        return type?.kind === kind ? { example, name: [...type.scope, type.name].join('.') } : null;
    };
    const merge = (inherited: readonly PropertyMappingSyntax[], authored: readonly PropertyMappingSyntax[]) => [...inherited.filter(value => !authored.some(replacement => replacement.property === value.property)), ...authored];
    return {
        resolvedExamples: entries.filter(entry => entry.example !== undefined).flatMap(entry => {
            const type = resolve(entry.example!.type, entry.scope);
            return type?.kind === undefined ? [] : [{ example: entry.example!, kind: type.kind, name: [...type.scope, type.name].join('.'), scope: type.scope }];
        }),
        eventStep: (step: SpecificationEventSyntax, role: string, scope: readonly string[]): EffectiveSpecificationStep => {
            const resolved = fixture(step.eventType, 'event', scope);
            const example = resolved?.example ?? null;
            const inherited = example?.stream ?? example?.noStream ?? null;
            const authored = step.stream ?? step.noStream ?? null;
            const route = authored ?? inherited;
            const effective = resolved === null ? step : { ...step, eventType: resolved.name, values: merge(example!.values, step.values), for: step.for ?? example!.for, stream: route?.kind === 'SpecificationStreamSyntax' ? route : null, noStream: route?.kind === 'SpecificationNoStreamSyntax' ? route : null };
            const values: EffectiveSpecificationValue[] = effective.values.map(value => {
                const prior = example?.values.find(prior => prior.property === value.property);
                const replacement = step.values.includes(value);
                return { property: value.property, value: value.source, origin: replacement ? prior === undefined ? 'authored' : 'override' : 'example', overriddenValue: replacement ? prior?.source ?? null : null };
            });
            if (effective.for !== null) values.push({ property: 'for', value: effective.for, origin: step.for === null ? 'example' : example?.for == null ? 'authored' : 'override', overriddenValue: step.for === null ? null : example?.for ?? null });
            return { role, authored: step, effective, example, values, route: route === null ? null : { value: route, origin: authored === null ? 'example' : inherited === null ? 'authored' : 'override', overriddenValue: authored === null ? null : inherited } };
        },
        command: (action: SpecificationCommandSyntax | null, scope: readonly string[]): SpecificationCommandSyntax | null => {
            if (!action) return action;
            const effective = fixture(action.commandType, 'command', scope);
            if (!effective) return action;
            const { example, name } = effective;
            return { ...action, commandType: name, values: merge(example.values, action.values), generatedValues: merge(example.generatedValues, action.generatedValues ?? []), for: action.for ?? example.for };
        },
        event: (step: SpecificationEventSyntax, scope: readonly string[]): SpecificationEventSyntax => {
            const effective = fixture(step.eventType, 'event', scope);
            if (!effective) return step;
            const route = step.stream ?? step.noStream ?? effective.example.stream ?? effective.example.noStream;
            return { ...step, eventType: effective.name, values: merge(effective.example.values, step.values), for: step.for ?? effective.example.for, stream: route?.kind === 'SpecificationStreamSyntax' ? route : null, noStream: route?.kind === 'SpecificationNoStreamSyntax' ? route : null };
        },
        readModel: (step: SpecificationReadModelSyntax, scope: readonly string[]): SpecificationReadModelSyntax => {
            const effective = fixture(step.name, 'readmodel', scope);
            if (!effective) return step;
            return { ...step, name: effective.name, properties: merge(effective.example.values, step.properties) };
        },
    };
}

export function specificationCommandExamples(application: ApplicationSyntax): (action: SpecificationCommandSyntax | null, scope: readonly string[]) => SpecificationCommandSyntax | null {
    return specificationExamples(application).command;
}

export function expandSpecificationExamples(application: ApplicationSyntax): ApplicationSyntax {
    return expandEffectiveSpecificationExamples(application).application;
}

export function expandEffectiveSpecificationExamples(application: ApplicationSyntax): EffectiveSpecificationApplication {
    const effective = specificationExamples(application);
    const specifications: EffectiveSpecification[] = [];
    const features = (nodes: readonly FeatureSyntax[], parent: readonly string[]): FeatureSyntax[] => nodes.map(feature => {
        const scope = [...parent, feature.name];
        return {
            ...feature,
            features: features(feature.features, scope),
            slices: feature.slices.map(slice => {
                const scope = [...parent, feature.name, slice.name];
                const expanded = { ...slice, specifications: slice.specifications.map(specification => {
                    const steps: EffectiveSpecificationStep[] = [];
                    const event = (step: SpecificationEventSyntax, role: string): SpecificationEventSyntax => {
                        const pair = effective.eventStep(step, role, scope);
                        steps.push(pair);
                        return pair.effective as SpecificationEventSyntax;
                    };
                    const result = {
                        ...specification,
                        given: specification.given.map(step => event(step, 'given')),
                        givenReadModels: specification.givenReadModels.map(step => effective.readModel(step, scope)),
                        when: effective.command(specification.when, scope),
                        whenAppended: specification.whenAppended === null ? null : event(specification.whenAppended, 'when append'),
                        thenEvents: specification.thenEvents.map(step => event(step, 'then')),
                        thenReadModels: specification.thenReadModels.map(step => effective.readModel(step, scope)),
                    };
                    specifications.push({ authored: specification, effective: result, steps });
                    return result;
                }) };
                dependencySources.set(expanded, dependencySourcesOf(slice));
                return expanded;
            }),
        };
    });
    return { application: { ...application, modules: application.modules.map(module => ({ ...module, features: features(module.features, [module.name]) })) }, specifications };
}
