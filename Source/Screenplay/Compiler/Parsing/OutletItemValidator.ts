// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { NavigationItemSyntax, NavigationTargetSyntax, OutletItemSyntax, OutletKind } from '../Syntax/Outlets';
import type { ToolbarButtonSyntax } from '../Syntax/Screens';
import { createNavigationScreenTargetValidator } from './NavigationScreenTargetValidator';
import { OutletInventory } from './OutletInventory';
import { ParserContext } from './ParserContext';

const kinds: Record<OutletItemSyntax['kind'], OutletKind> = { ToolbarButtonSyntax: 'Actions', OutletGroupSyntax: 'Actions', OutletFilterSyntax: 'Filters', OutletTabSyntax: 'Tabs', OutletStatSyntax: 'Stats', NavigationItemSyntax: 'Navigation' };

export class OutletItemValidator {
    private readonly navigation;
    constructor(private readonly inventory: OutletInventory, private readonly context: ParserContext) {
        this.navigation = createNavigationScreenTargetValidator(inventory.application, context);
    }

    validate(item: OutletItemSyntax, expected: OutletKind | null | undefined, owner: string, module: string | null): void {
        const kind = kinds[item.kind];
        if (expected != null && expected !== kind) {
            this.context.error(DiagnosticCodes.OutletItemWrongKind, `Item '${item.id}' of the ${owner} is a ${kind.toLowerCase()} item, but the outlet holds ${expected.toLowerCase()}`, item.location);
            return;
        }
        switch (item.kind) {
            case 'ToolbarButtonSyntax': this.validateButton(item, `the ${owner}`); break;
            case 'OutletGroupSyntax': item.buttons.forEach(button => this.validateButton(button, `group '${item.id}' of the ${owner}`)); break;
            case 'OutletFilterSyntax': this.validateFilter(item, owner); break;
            case 'OutletStatSyntax': this.validateStat(item, owner); break;
            case 'OutletTabSyntax': this.validateTab(item, owner); break;
            case 'NavigationItemSyntax': this.validateNavigation(item, owner, module); break;
        }
    }

    private validateTab(item: Extract<OutletItemSyntax, { kind: 'OutletTabSyntax' }>, owner: string): void {
        if (item.shows != null && !this.inventory.screens.some(screen => screen.name === item.shows)) this.context.error(DiagnosticCodes.UnknownScreen, `Tab '${item.id}' of the ${owner} shows screen '${item.shows}', which the document does not declare`, item.location);
    }

    validateButton(button: ToolbarButtonSyntax, owner: string): void {
        if (button.slice == null) {
            if (button.runs == null && !button.isDefault) this.context.warning(DiagnosticCodes.ToolbarButtonWithoutSlice, `Toolbar button '${button.id}' in ${owner} names no slice - add 'slice <Module>.<Feature>.<Slice>' so it invokes something`, button.location);
            return;
        }
        const matches = this.inventory.matchingSlices(button.slice);
        if (matches.length === 0) this.context.error(DiagnosticCodes.ToolbarButtonSliceNotFound, `Toolbar button '${button.id}' invokes slice '${button.slice}', which the document does not declare`, button.location);
        else if (matches.length > 1) this.context.error(DiagnosticCodes.ToolbarButtonSliceAmbiguous, `Toolbar button '${button.id}' invokes slice '${button.slice}', which matches ${matches.length} slices (${matches.map(entry => entry.scope.join('.')).join(', ')}) - qualify it with the feature or module`, button.location);
        else if (matches[0].slice.type !== 'StateChange') this.context.error(DiagnosticCodes.ToolbarButtonSliceNotStateChange, `Toolbar button '${button.id}' invokes slice '${button.slice}', a ${matches[0].slice.type} slice - a button invokes State Change slices only`, button.location);
    }

    private validateFilter(item: Extract<OutletItemSyntax, { kind: 'OutletFilterSyntax' }>, owner: string): void {
        if (item.query == null) return;
        const query = this.inventory.queries.get(item.query);
        if (query === undefined) this.context.error(DiagnosticCodes.UnknownQuery, `Filter '${item.id}' of the ${owner} sets a parameter of query '${item.query}', which the document does not declare`, item.location);
        else if (query.by?.name !== item.parameter && !query.filters.some(parameter => parameter.name === item.parameter)) this.context.error(DiagnosticCodes.OutletFilterParameterUnknown, `Filter '${item.id}' of the ${owner} sets '${item.query}.${item.parameter}', but query '${item.query}' has no parameter '${item.parameter}'`, item.location);
    }

    private validateStat(item: Extract<OutletItemSyntax, { kind: 'OutletStatSyntax' }>, owner: string): void {
        if (item.query == null) return;
        const query = this.inventory.queries.get(item.query);
        if (query === undefined) this.context.error(DiagnosticCodes.UnknownQuery, `Stat '${item.id}' of the ${owner} reads its value from query '${item.query}', which the document does not declare`, item.location);
        else if (query.returnType.isCollection) this.context.error(DiagnosticCodes.OutletStatFromListQuery, `Stat '${item.id}' of the ${owner} reads its value from query '${item.query}', which returns a list - a stat reads a field of a single-result query`, item.location);
    }

    private validateNavigation(item: NavigationItemSyntax, owner: string, module: string | null): void {
        if (this.rendersIntoUnknownSlot(item)) this.context.error(DiagnosticCodes.NavSlotUnknown, `Navigation item '${item.id}' in the ${owner} renders into slot '${item.slot}', which no layout declares`, item.location);
        this.validateTarget(item, owner, module);
        item.children?.forEach(child => this.validateNavigation(child, owner, module));
    }

    private rendersIntoUnknownSlot(item: NavigationItemSyntax): boolean {
        return item.slot != null && this.inventory.slots.size > 0 && !this.inventory.slots.has(item.slot);
    }

    private validateTarget(item: NavigationItemSyntax, owner: string, module: string | null): void {
        const target = item.target;
        if (target == null) {
            if ((item.children?.length ?? 0) === 0) this.context.error(DiagnosticCodes.NavTargetMissing, `Navigation item '${item.id}' in the ${owner} has no target and no sub-items - add 'target <Module>[.<Feature>]'`, item.location);
            return;
        }
        const features = this.inventory.modules.get(target.module);
        if (!this.targetIsReachable(item, target, features, owner, module)) return;
        if (target.feature == null && features!.size === 0) this.context.warning(DiagnosticCodes.NavModuleWithoutFeatures, `Navigation item '${item.id}' in the ${owner} targets module '${target.module}', which has no feature to go to`, target.location);
        this.navigation.validateItem(target, module);
    }

    private targetIsReachable(item: NavigationItemSyntax, target: NavigationTargetSyntax, features: ReadonlySet<string> | undefined, owner: string, module: string | null): boolean {
        if (features === undefined) this.context.error(DiagnosticCodes.NavTargetMissing, `Navigation item '${item.id}' in the ${owner} targets module '${target.module}', which does not exist`, target.location);
        else if (module !== null && module !== target.module) this.context.error(DiagnosticCodes.NavTargetOutOfScope, `Navigation item '${item.id}' of module '${module}' targets module '${target.module}'; a module's own items may only target that module`, target.location);
        else if (target.feature != null && !features.has(target.feature)) this.context.error(DiagnosticCodes.NavTargetMissing, `Navigation item '${item.id}' in the ${owner} targets feature '${target.module}.${target.feature}', which does not exist`, target.location);
        else return true;
        return false;
    }
}
