// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { OutletItemSyntax, OutletSyntax } from '../Syntax/Outlets';
import type { ScreenSyntax } from '../Syntax/Screens';
import type { FeatureSyntax } from '../Syntax/Structure';
import { usesBlueprintOutlets } from './BlueprintOutletFacts';
import { contributions, ids, OutletInventory } from './OutletInventory';
import { OutletItemValidator } from './OutletItemValidator';
import { ParserContext } from './ParserContext';

type Inheritance = Map<string, Set<string>>;
const copy = (inherited: Inheritance): Inheritance => new Map([...inherited].map(([name, available]) => [name, new Set(available)]));

export class OutletValidator {
    private readonly items;
    constructor(private readonly inventory: OutletInventory, private readonly context: ParserContext) {
        this.items = new OutletItemValidator(inventory, context);
    }

    validate(): void {
        this.validateDeclarations();
        const application = this.inventory.application;
        this.validateScreens(application.screens ?? [], null, new Map());
        for (const module of application.modules) {
            const inherited: Inheritance = new Map();
            this.validateLevel(module.outlets ?? [], module.name, `module '${module.name}'`, inherited);
            this.validateScreens(module.screens ?? [], module.name, inherited);
            this.validateFeatures(module.features, module.name, inherited);
            for (const contribution of contributions(module)) {
                const declaration = this.inventory.resolve(contribution.contributionPoint, module.name);
                contribution.items.forEach(item => this.items.validate(item, declaration?.syntaxKind, `contribution to '${contribution.contributionPoint}'`, module.name));
            }
        }
    }

    private validateDeclarations(): void {
        for (const layout of this.inventory.application.layouts ?? []) {
            const logos = (layout.outlets ?? []).filter(outlet => outlet.logo != null);
            for (const outlet of logos.slice(1)) this.context.error(DiagnosticCodes.NavigationLogoNotAllowed, `The layout '${layout.name}' declares a second logo in outlet '${outlet.name}' - the application has one logo`, outlet.logo!.location);
        }
        for (const { module, outlet } of this.inventory.offers) {
            const owner = module === null ? `layout outlet '${outlet.name}'` : `outlet '${outlet.name}' of module '${module}'`;
            if (module !== null && outlet.logo != null) this.context.error(DiagnosticCodes.NavigationLogoNotAllowed, `The ${owner} declares a logo - the logo is the application's, declared once in a layout's navigation outlet`, outlet.logo.location);
            const seen = new Set<string>();
            for (const item of outlet.items) {
                this.addIds(item, seen, new Set(), owner);
                this.items.validate(item, outlet.syntaxKind, owner, null);
            }
        }
    }

    private validateScreens(screens: readonly ScreenSyntax[], module: string | null, inherited: Inheritance): void {
        for (const screen of screens) this.validateLevel(screen.outlets ?? [], module, `screen '${screen.name}'`, copy(inherited));
    }

    private validateFeatures(features: readonly FeatureSyntax[], module: string, inherited: Inheritance): void {
        for (const feature of features) {
            const own = copy(inherited);
            this.validateLevel(feature.outlets ?? [], module, `feature '${feature.name}'`, own);
            this.validateScreens(feature.screens ?? [], module, own);
            this.validateScreens(feature.slices.flatMap(slice => slice.screens), module, own);
            this.validateFeatures(feature.features, module, own);
        }
    }

    private validateLevel(outlets: readonly OutletSyntax[], module: string | null, level: string, inherited: Inheritance): void {
        for (const outlet of outlets) {
            const owner = `outlet '${outlet.name}' of ${level}`;
            const declaration = this.inventory.resolve(outlet.name, module);
            if (declaration === undefined && !usesBlueprintOutlets(this.inventory.application)) this.context.error(DiagnosticCodes.UnknownOutlet, `The ${level} changes outlet '${outlet.name}', which no layout or template declares - declare it with 'outlet ${outlet.name} of <kind>'`, outlet.location);
            let available = inherited.get(outlet.name);
            if (available === undefined) {
                available = new Set([...(declaration?.items ?? []), ...this.inventory.contributed.get(outlet.name) ?? []].flatMap(ids));
                inherited.set(outlet.name, available);
            }
            const known = declaration !== undefined || available.size > 0;
            this.checkHides(outlet, known, available, owner);
            if (outlet.mode === 'Override' && outlet.items.length === 0) this.context.warning(DiagnosticCodes.NavOverrideWithoutItems, `The ${owner} overrides the inherited items without items of its own, so the outlet is empty there`, outlet.location);
            const seen = new Set<string>();
            for (const item of outlet.items) {
                this.addIds(item, seen, outlet.mode === 'Extend' ? available : new Set(), owner);
                this.items.validate(item, declaration?.syntaxKind, owner, module);
            }
            this.checkPositions(outlet, known, available, seen, owner);
            const after = outlet.mode === 'Override' ? new Set<string>() : new Set(available);
            outlet.hides?.forEach(hide => after.delete(hide.id));
            seen.forEach(id => after.add(id));
            inherited.set(outlet.name, after);
        }
    }

    private checkHides(outlet: OutletSyntax, known: boolean, available: ReadonlySet<string>, owner: string): void {
        for (const hide of outlet.hides ?? []) {
            if (known && !available.has(hide.id)) this.context.error(DiagnosticCodes.OutletItemNotInherited, `The ${owner} hides '${hide.id}', which the outlet it inherits does not have`, hide.location);
        }
    }

    private checkPositions(outlet: OutletSyntax, known: boolean, available: ReadonlySet<string>, seen: ReadonlySet<string>, owner: string): void {
        for (const item of outlet.items) {
            const position = item.position;
            if (position != null && known && !available.has(position.id) && !seen.has(position.id)) this.context.error(DiagnosticCodes.OutletItemNotInherited, `Item '${item.id}' of the ${owner} stands ${position.placement.toLowerCase()} '${position.id}', which the outlet does not have`, position.location);
        }
    }

    private addIds(item: OutletItemSyntax, seen: Set<string>, inherited: ReadonlySet<string>, owner: string): void {
        for (const id of ids(item)) {
            if (seen.has(id)) this.context.error(DiagnosticCodes.DuplicateOutletItem, `The ${owner} already has an item with the id '${id}'`, item.location);
            else if (inherited.has(id)) this.context.error(DiagnosticCodes.DuplicateOutletItem, `Item '${id}' of the ${owner} repeats the id of an inherited item - choose another id, or hide the inherited one`, item.location);
            seen.add(id);
        }
    }
}
