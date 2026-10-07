// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyMappingSyntax } from '../Syntax/Expressions';
import { SpecificationCommandSyntax } from '../Syntax/Specifications';
import { SpecificationExampleSyntax } from '../Syntax/SpecificationExampleSyntax';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';

// Validators consume an effective command fixture without changing the authored syntax.
// Resolve the example at its use site, then its command at the declaration site.
export function specificationCommandExamples(application: ApplicationSyntax): (action: SpecificationCommandSyntax | null, scope: readonly string[]) => SpecificationCommandSyntax | null {
    const entries: { name: string; scope: readonly string[]; example?: SpecificationExampleSyntax; command?: boolean }[] = [];
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
                for (const command of slice.commands) entries.push({ name: command.name, scope, command: true });
            }
            features(feature.features, scope);
        }
    };
    examples(application.examples, []);
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
    const merge = (inherited: readonly PropertyMappingSyntax[], authored: readonly PropertyMappingSyntax[]) => [...inherited.filter(value => !authored.some(replacement => replacement.property === value.property)), ...authored];
    return (action, scope) => {
        if (!action) return action;
        const declaration = resolve(action.commandType, scope);
        const example = declaration?.example;
        if (!example) return action;
        const type = resolve(example.type, declaration.scope);
        if (!type?.command) return action;
        return { ...action, commandType: [...type.scope, type.name].join('.'), values: merge(example.values, action.values), generatedValues: merge(example.generatedValues, action.generatedValues ?? []), for: action.for ?? example.for };
    };
}
