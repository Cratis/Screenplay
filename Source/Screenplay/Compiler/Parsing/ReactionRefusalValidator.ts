// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { CommandSyntax } from '../Syntax/Commands';
import { ApplicationSyntax, FeatureSyntax, SliceSyntax } from '../Syntax/Structure';
import { ParserContext } from './ParserContext';

// Invocations have no identity declaration until #383; keep that decision separate from authorization gating.
function invocationHasNoDeclaredIdentity(): boolean { return true; }

export function validateReactionRefusals(application: ApplicationSyntax, context: ParserContext): void {
    const slices: { slice: SliceSyntax; scope: readonly string[]; gated: boolean }[] = [];
    const collect = (feature: FeatureSyntax, parent: readonly string[], inherited: boolean): void => {
        const scope = [...parent, feature.name];
        const gated = inherited || feature.authorize !== null;
        feature.slices.forEach(slice => slices.push({ slice, scope: [...scope, slice.name], gated }));
        feature.features.forEach(child => collect(child, scope, gated));
    };
    application.modules.forEach(module => module.features.forEach(feature => collect(feature, [module.name], module.authorize !== null)));
    const commands = slices.flatMap(({ slice, scope, gated }) => slice.commands.map(command => ({ command, scope, gated: gated || command.authorize !== null })));
    const candidates = (reference: string, from: readonly string[]): typeof commands => {
        const segments = reference.split('.').filter(Boolean);
        const named = commands.filter(entry => entry.command.name === segments.at(-1));
        const qualifiers = segments.slice(0, -1);
        if (qualifiers.length > 0) return named.filter(entry => qualifiers.length <= entry.scope.length && qualifiers.every((value, index) => entry.scope[entry.scope.length - qualifiers.length + index] === value));
        for (let depth = from.length; depth >= 0; depth--) {
            const visible = named.filter(entry => depth <= entry.scope.length && from.slice(0, depth).every((value, index) => entry.scope[index] === value));
            if (visible.length > 0) return visible;
        }
        return [];
    };
    const resolve = (reference: string, from: readonly string[]): { command: CommandSyntax; gated: boolean } | undefined => {
        let matches = candidates(reference, from);
        const imports = application.imports.filter(entry => entry.qualifiedName.split('.').at(-1) === reference);
        if (matches.length === 0 && imports.length === 1) matches = candidates(imports[0].qualifiedName, from);
        return matches.length === 1 ? matches[0] : undefined;
    };
    for (const { slice, scope } of slices) {
        for (const invocation of slice.reactions.flatMap(reaction => reaction.triggers).flatMap(trigger => trigger.invokes)) {
            const branches = (invocation.onRefused ?? []).filter(branch => branch.selector === 'authorization');
            if (branches.length === 0) continue;
            const command = resolve(invocation.command, scope);
            if (command?.gated !== true || !invocationHasNoDeclaredIdentity()) continue;
            for (const branch of branches) {
                context.warning(DiagnosticCodes.AuthorizationRefusalWithoutIdentity,
                    `Command '${invocation.command}' is authorization-gated, but this invocation has no declared identity. This authorization refusal branch always fires in the reference runner because there is no caller; Arc runs reactor commands as the system. Declare an invoking identity once supported (#383).`,
                    branch.location);
            }
        }
    }
}
