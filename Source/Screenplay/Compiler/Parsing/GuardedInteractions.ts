// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { SourceLocation } from '../Diagnostics/SourceLocation';
import { ScreenDataSyntax } from '../Syntax/Screens';
import { SliceSyntax } from '../Syntax/Structure';
import { comparisons, validateArguments, validateItemPath, viewProperties } from './GuardedActionInputs';
import { validateAlternativeShadowing } from './GuardedActionShadowing';
import { GuardedInteractionBinding, InteractionAncestor } from './InteractionAlternatives';
import { InteractionUse } from './InteractionUse';
import { ParserContext } from './ParserContext';
import { RefusalDeclarations } from './RefusalDeclarations';

export const sameInteractionLocation = (first: SourceLocation, second: SourceLocation): boolean =>
    first.path === second.path && first.line === second.line && first.column === second.column;

// C# visits child collections in this order rather than their authored order.
const structuralChildren: Readonly<Record<string, readonly string[]>> = {
    application: ['behavior', 'layout', 'module'],
    module: ['screen template', 'dialog', 'form', 'contribute', 'feature', 'on', 'uses'],
    feature: ['feature', 'slice', 'contribute', 'on', 'uses'],
    layout: ['slot', 'arrangement', 'on', 'uses'],
    'screen template': ['slot', 'arrangement', 'on', 'uses'],
    dialog: ['slot', 'arrangement', 'on', 'uses'],
};

function structuralOrder<T extends { readonly location: SourceLocation; readonly ancestors: readonly InteractionAncestor[] }>(sites: readonly T[], isUse: (site: T) => boolean): readonly T[] {
    const children = (group: readonly T[], depth: number, parent: string): T[] => {
        const groups = new Map<string, { ancestor: InteractionAncestor; sites: T[] }>();
        for (const site of group) {
            const ancestor = site.ancestors[depth] ?? { keyword: isUse(site) ? 'uses' : 'on', location: site.location };
            const key = JSON.stringify(ancestor.location);
            if (!groups.has(key)) groups.set(key, { ancestor, sites: [] });
            groups.get(key)!.sites.push(site);
        }
        const order = structuralChildren[parent] ?? [];
        const rank = (keyword: string) => order.includes(keyword) ? order.indexOf(keyword) : order.length;
        return [...groups.values()].sort((first, second) => rank(first.ancestor.keyword) - rank(second.ancestor.keyword))
            .flatMap(entry => ['on', 'uses'].includes(entry.ancestor.keyword) ? entry.sites : children(entry.sites, depth + 1, entry.ancestor.keyword));
    };
    return children(sites, 0, 'application');
}

// Subject-dependent interaction checks share the guarded screen-action path and input validators.
export class GuardedInteractions {
    constructor(private readonly declarations: RefusalDeclarations, private readonly context: ParserContext) {}

    structural(): void {
        const sites = [...this.context.guardedInteractions, ...this.context.interactionUses]
            .filter(site => !site.ancestors.some(ancestor => ancestor.keyword === 'screen'));
        for (const site of structuralOrder(sites, site => 'behavior' in site)) {
            if ('behavior' in site) this.validateUse(site, [], undefined);
            else if (site.ancestors.some(ancestor => ancestor.keyword === 'behavior')) validateAlternativeShadowing(site.alternatives, this.context);
            else this.validate(site, [], undefined, this.context);
        }
    }

    at(location: SourceLocation, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax, container = false): void {
        for (const binding of this.context.guardedInteractions) {
            const attachment = container ? binding.ancestors.at(-1)?.location : binding.location;
            if (attachment !== undefined && sameInteractionLocation(attachment, location)) this.validate(binding, subjects, slice, this.context);
        }
        if (container) {
            for (const uses of this.context.interactionUses) {
                const attachment = uses.ancestors.at(-1)?.location;
                if (attachment !== undefined && sameInteractionLocation(attachment, location)) this.validateUse(uses, subjects, slice);
            }
        }
    }

    useAt(location: SourceLocation, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax): void {
        for (const uses of this.context.interactionUses.filter(uses => sameInteractionLocation(uses.location, location))) this.validateUse(uses, subjects, slice);
    }

    private validateUse(uses: InteractionUse, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax | undefined): void {
        const matches = this.context.interactionBehaviors.filter(behavior => behavior.name === uses.behavior);
        if (matches.length !== 1) return;
        const site = new ParserContext(this.context.reader);
        for (const binding of this.context.guardedInteractions) {
            if (binding.ancestors.some(ancestor => ancestor.keyword === 'behavior' && sameInteractionLocation(ancestor.location, matches[0].location))) {
                this.validate(binding, subjects, slice, site, uses.location);
            }
        }
        for (const diagnostic of site.diagnostics) {
            if (!this.context.diagnostics.some(existing => existing.code === diagnostic.code && existing.message === diagnostic.message && existing.severity === diagnostic.severity && sameInteractionLocation(existing.location, diagnostic.location))) {
                this.context.warning(diagnostic.code, diagnostic.message, diagnostic.location, diagnostic.arguments);
            }
        }
    }

    private validate(binding: GuardedInteractionBinding, subjects: readonly ScreenDataSyntax[], slice: SliceSyntax | undefined, context: ParserContext, attachment?: SourceLocation): void {
        const properties = subjects.length === 1 ? viewProperties(subjects[0].type.name, slice, this.declarations) ?? this.declarations.types.get(subjects[0].type.name)?.properties ?? null : null;
        if (subjects.length !== 1) {
            context.warning(DiagnosticCodes.UnresolvedActionSubject, "A guarded interaction requires exactly one nearest 'data' subject; component context and selection do not supply 'item'", attachment ?? binding.location);
        }
        for (const alternative of binding.alternatives) {
            for (const comparison of comparisons(alternative.condition)) validateItemPath(comparison.left, comparison.location, properties, slice, this.declarations, context);
            for (const execution of alternative.executions) validateArguments(execution.command, execution.arguments, properties, slice, this.declarations, context);
        }
        for (const execution of binding.otherwise) validateArguments(execution.command, execution.arguments, properties, slice, this.declarations, context);
        if (attachment === undefined) validateAlternativeShadowing(binding.alternatives, context);
    }
}
