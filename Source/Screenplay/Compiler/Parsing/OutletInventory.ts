// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthoringProductionResolver } from '../Syntax/AuthoringProductionResolver';
import type { ContributionSyntax, LayoutMembers, SlotSyntax } from '../Syntax/Layouts';
import type { OutletItemSyntax, OutletSyntax } from '../Syntax/Outlets';
import type { ReadModelSyntax } from '../Syntax/Declarations';
import type { QuerySyntax } from '../Syntax/Queries';
import type { ScreenDirectiveSyntax, ScreenSyntax, ToolbarButtonSyntax } from '../Syntax/Screens';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import type { ApplicationSyntax, FeatureSyntax, ModuleSyntax } from '../Syntax/Structure';

export function* allFeatures(features: readonly FeatureSyntax[]): Generator<FeatureSyntax> {
    for (const feature of features) { yield feature; yield* allFeatures(feature.features); }
}

export function* screenDirectives(directives: readonly ScreenDirectiveSyntax[]): Generator<ScreenDirectiveSyntax> {
    for (const directive of directives) {
        yield directive;
        if (directive.kind === 'ScreenSectionSyntax') yield* screenDirectives(directive.directives);
        if (directive.kind === 'ScreenTemplateReferenceSyntax' || directive.kind === 'ScreenComponentSyntax') {
            for (const slot of directive.slots) yield* screenDirectives(slot.directives);
        }
    }
}

export function buttons(items: readonly OutletItemSyntax[]): ToolbarButtonSyntax[] {
    return items.flatMap(item => item.kind === 'ToolbarButtonSyntax' ? [item] : item.kind === 'OutletGroupSyntax' ? item.buttons : []);
}

export function ids(item: OutletItemSyntax): string[] {
    return item.kind === 'OutletGroupSyntax' ? [item.id, ...item.buttons.map(button => button.id)] : [item.id];
}

export function contributions(module: ModuleSyntax): ContributionSyntax[] {
    return [...(module.contributions ?? []), ...[...allFeatures(module.features)].flatMap(feature => feature.contributions ?? [])];
}

export function structures(module: ModuleSyntax): LayoutMembers[] {
    return [...(module.screenTemplates ?? []), ...(module.dialogTemplates ?? [])];
}

export interface OutletOffer { readonly module: string | null; readonly structure: LayoutMembers; readonly outlet: OutletSyntax }

export class OutletInventory extends ScreenplaySyntaxWalker {
    readonly slices;
    readonly offers: OutletOffer[] = [];
    readonly queries = new Map<string, QuerySyntax>();
    readonly readModels = new Map<string, ReadModelSyntax>();
    readonly screens: ScreenSyntax[] = [];
    readonly slots = new Set<string>();
    readonly modules = new Map<string, Set<string>>();
    readonly contributed = new Map<string, OutletItemSyntax[]>();
    readonly templates = new Map<string, readonly SlotSyntax[]>();

    constructor(readonly application: ApplicationSyntax) {
        super();
        this.slices = new AuthoringProductionResolver(application).slices;
        for (const layout of application.layouts ?? []) this.offer(layout, null);
        for (const module of application.modules) {
            this.modules.set(module.name, new Set(featurePaths(module.features)));
            for (const structure of structures(module)) this.offer(structure, module.name);
            for (const contribution of contributions(module)) {
                this.contributed.set(contribution.contributionPoint, [...this.contributed.get(contribution.contributionPoint) ?? [], ...contribution.items]);
            }
            for (const template of module.screenTemplates ?? []) if (!this.templates.has(template.name)) this.templates.set(template.name, template.slots);
        }
        this.visitApplication(application);
    }

    private offer(structure: LayoutMembers, module: string | null): void {
        if (module === null) for (const slot of structure.slots) this.slots.add(slot.name);
        for (const outlet of structure.outlets ?? []) this.offers.push({ module, structure, outlet });
    }

    override visitQuery(query: QuerySyntax): void {
        if (!this.queries.has(query.name)) this.queries.set(query.name, query);
        super.visitQuery(query);
    }
    override visitReadModel(model: ReadModelSyntax): void {
        if (!this.readModels.has(model.name)) this.readModels.set(model.name, model);
        super.visitReadModel(model);
    }
    override visitScreen(screen: ScreenSyntax): void { this.screens.push(screen); super.visitScreen(screen); }

    resolve(name: string, module: string | null): OutletSyntax | undefined {
        return this.offers.find(offer => offer.module === module && offer.outlet.name === name)?.outlet
            ?? this.offers.find(offer => offer.module !== null && offer.outlet.name === name)?.outlet
            ?? this.offers.find(offer => offer.outlet.name === name)?.outlet;
    }

    matchingSlices(reference: string) {
        const wanted = reference.split('.');
        return this.slices.filter(entry => entry.scope.length >= wanted.length && entry.scope.slice(-wanted.length).join('.') === wanted.join('.'));
    }
}

function* featurePaths(features: readonly FeatureSyntax[], prefix = ''): Generator<string> {
    for (const feature of features) {
        const path = prefix.length === 0 ? feature.name : `${prefix}.${feature.name}`;
        yield path;
        yield* featurePaths(feature.features, path);
    }
}
