// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from '../Diagnostics/Diagnostic';
import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ApplicationSyntax, FeatureSyntax } from '../Syntax/Structure';
import { InteractionAncestor } from './InteractionAlternatives';
import { InteractionUse } from './InteractionUse';
import { ParserContext } from './ParserContext';

import { StructuralInteractionOwner } from './StructuralInteractionOwner';
import { StructuralInteractionSite } from './StructuralInteractionSite';

// Opaque attachments belong to effective syntax owners, not the physical document inventory.
const sites = new WeakMap<StructuralInteractionOwner, readonly StructuralInteractionSite[]>();
export const sameInteractionLocation = (first: SourceLocation, second: SourceLocation): boolean =>
    first.path === second.path && first.line === second.line && first.column === second.column;

export function recordStructuralInteractions(application: ApplicationSyntax, context: ParserContext): void {
    const owners: StructuralInteractionOwner[] = [application];
    const feature = (node: FeatureSyntax) => { owners.push(node); node.features.forEach(feature); };
    application.modules.forEach(module => { owners.push(module); module.features.forEach(feature); });
    const placed = owners.filter(owner => owner.kind !== 'ApplicationSyntax' && owner.isPlacement).at(-1) ?? application;
    const grouped = new Map<StructuralInteractionOwner, StructuralInteractionSite[]>();
    const physical = [...context.guardedInteractions, ...context.interactionUses].sort((first, second) => first.location.line - second.location.line || first.location.column - second.location.column);
    for (const site of physical) {
        const ancestor = [...site.ancestors].reverse().find(ancestor => ['module', 'feature'].includes(ancestor.keyword));
        const index = ancestor === undefined ? -1 : site.ancestors.indexOf(ancestor);
        const owner = ancestor === undefined
            ? ['behavior', 'layout'].includes(site.ancestors[0]?.keyword) ? application : placed
            : owners.find(owner => owner.kind !== 'ApplicationSyntax' && !owner.isPlacement && sameInteractionLocation(owner.location, ancestor.location)) ?? placed;
        if (!grouped.has(owner)) grouped.set(owner, []);
        grouped.get(owner)!.push({ ...site, ancestors: site.ancestors.slice(index + 1) });
    }
    for (const owner of owners) sites.set(owner, grouped.get(owner) ?? []);
}

// Called by the syntax merge with the same authored-before-placement parts it merges for each owner.
export function mergeStructuralInteractions<T extends StructuralInteractionOwner>(merged: T, parts: readonly T[], diagnostics?: Diagnostic[]): T {
    const kept: StructuralInteractionSite[] = [];
    for (const site of parts.flatMap(part => sites.get(part) ?? [])) {
        const duplicate = 'behavior' in site && site.ancestors.length === 0 && kept.find(earlier =>
            'behavior' in earlier && earlier.ancestors.length === 0 && earlier.location.path !== site.location.path && useSignature(earlier) === useSignature(site));
        if (duplicate) {
            diagnostics?.push({ code: DiagnosticCodes.DuplicateBehaviorAttachment, severity: 'warning', location: site.location,
                message: `Behavior '${site.behavior}' is already attached to the ${merged.kind === 'ModuleSyntax' ? 'module' : 'feature'} '${'name' in merged ? merged.name : ''}' in '${duplicate.location.path ?? 'another file'}' - this repeated attachment is ignored` });
        } else kept.push(site);
    }
    sites.set(merged, kept);
    return merged;
}

function useSignature(uses: InteractionUse): string {
    return [uses.behavior, ...[...uses.arguments].sort((first, second) => first.name < second.name ? -1 : first.name > second.name ? 1 : 0).map(argument => `${argument.name}=${argument.value}`)].join('\n');
}

// C# visits child collections, then inline bindings before uses within each owning declaration.
const structuralChildren: Readonly<Record<string, readonly string[]>> = {
    application: ['behavior', 'layout'],
    module: ['screen template', 'dialog', 'form', 'contribute', 'on', 'uses'],
    feature: ['contribute', 'on', 'uses'],
    layout: ['slot', 'arrangement', 'on', 'uses'],
    'screen template': ['slot', 'arrangement', 'on', 'uses'],
    dialog: ['slot', 'arrangement', 'on', 'uses'],
};

function structuralOrder(group: readonly StructuralInteractionSite[], parent: string, depth = 0): readonly StructuralInteractionSite[] {
    const groups = new Map<string, { ancestor: InteractionAncestor; sites: StructuralInteractionSite[] }>();
    for (const site of group) {
        const ancestor = site.ancestors[depth] ?? { keyword: 'behavior' in site ? 'uses' : 'on', location: site.location };
        const key = JSON.stringify(ancestor.location);
        if (!groups.has(key)) groups.set(key, { ancestor, sites: [] });
        groups.get(key)!.sites.push(site);
    }
    const order = structuralChildren[parent] ?? [];
    const rank = (keyword: string) => order.includes(keyword) ? order.indexOf(keyword) : order.length;
    return [...groups.values()].sort((first, second) => rank(first.ancestor.keyword) - rank(second.ancestor.keyword))
        .flatMap(entry => ['on', 'uses'].includes(entry.ancestor.keyword) ? entry.sites : structuralOrder(entry.sites, entry.ancestor.keyword, depth + 1));
}

export function effectiveStructuralInteractions(application: ApplicationSyntax): readonly StructuralInteractionSite[] {
    const own = (owner: StructuralInteractionOwner) => (sites.get(owner) ?? []).filter(site => !site.ancestors.some(ancestor => ancestor.keyword === 'screen'));
    const attached = (site: StructuralInteractionSite) => site.ancestors.length === 0;
    const feature = (node: FeatureSyntax): readonly StructuralInteractionSite[] => [
        ...node.features.flatMap(feature), ...structuralOrder(own(node), 'feature'),
    ];
    return [
        ...structuralOrder(own(application), 'application'),
        ...application.modules.flatMap(module => [
            ...structuralOrder(own(module).filter(site => !attached(site)), 'module'),
            ...module.features.flatMap(feature),
            ...structuralOrder(own(module).filter(attached), 'module'),
        ]),
    ];
}
