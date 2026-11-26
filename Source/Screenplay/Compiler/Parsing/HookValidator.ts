// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import type { ConditionSyntax } from '../Syntax/Conditions';
import type { SlotSyntax } from '../Syntax/Layouts';
import type { OutletSyntax } from '../Syntax/Outlets';
import type { ScreenActionSubjectKind, ScreenSyntax, ToolbarButtonSyntax } from '../Syntax/Screens';
import { allFeatures, buttons, contributions, OutletInventory, screenDirectives } from './OutletInventory';
import { ParserContext } from './ParserContext';

const readsItem = (condition: ConditionSyntax): boolean => condition.kind === 'ComparisonConditionSyntax'
    ? condition.left.startsWith('item.') : readsItem(condition.left) || readsItem(condition.right);
const hook = (button: ToolbarButtonSyntax): string => `Hook '${button.id}'`;

export class HookValidator {
    constructor(private readonly inventory: OutletInventory, private readonly context: ParserContext) {}

    validate(): void {
        for (const { structure, outlet } of this.inventory.offers) this.checkOutlet(outlet, [structure.slots], null, outlet.subject);
        for (const module of this.inventory.application.modules) {
            this.checkChanges(module.outlets ?? [], null);
            for (const feature of allFeatures(module.features)) this.checkChanges(feature.outlets ?? [], null);
            for (const contribution of contributions(module)) {
                const [slots, subject] = this.offered(contribution.contributionPoint);
                buttons(contribution.items).forEach(button => this.checkButton(button, slots, null, subject));
            }
        }
        for (const { slice } of this.inventory.slices) for (const screen of slice.screens) this.checkScreen(screen);
    }

    private checkScreen(screen: ScreenSyntax): void {
        this.checkChanges(screen.outlets ?? [], screen);
        const template = screen.directives.find(directive => directive.kind === 'ScreenTemplateReferenceSyntax');
        const slots = template === undefined ? [[]] : this.inventory.templates.has(template.name) ? [this.inventory.templates.get(template.name)!] : [];
        for (const directive of screenDirectives(screen.directives)) {
            if (directive.kind !== 'ScreenToolbarSyntax') continue;
            directive.buttons?.forEach(button => this.checkButton(button, slots, screen, null));
            directive.items.forEach(item => this.checkEnabled(item.enabledWhen, item.subject, `Toolbar item '${item.name}'`, item));
        }
    }

    private offered(name: string): [readonly (readonly SlotSyntax[])[], ScreenActionSubjectKind | null] {
        const offers = this.inventory.offers.filter(offer => offer.outlet.name === name);
        return [offers.map(offer => offer.structure.slots), offers.map(offer => offer.outlet.subject).find(subject => subject != null) ?? null];
    }

    private checkChanges(outlets: readonly OutletSyntax[], screen: ScreenSyntax | null): void {
        for (const outlet of outlets.filter(outlet => outlet.syntaxKind == null)) {
            const [slots, subject] = this.offered(outlet.name);
            this.checkOutlet(outlet, slots, screen, outlet.subject ?? subject);
        }
    }

    private checkOutlet(outlet: OutletSyntax, slots: readonly (readonly SlotSyntax[])[], screen: ScreenSyntax | null, subject: ScreenActionSubjectKind | null | undefined): void {
        buttons(outlet.items).forEach(button => this.checkButton(button, slots, screen, subject));
    }

    private checkButton(button: ToolbarButtonSyntax, slots: readonly (readonly SlotSyntax[])[], screen: ScreenSyntax | null, inherited: ScreenActionSubjectKind | null | undefined): void {
        const subject = button.subject ?? inherited;
        this.checkEnabled(button.enabledWhen, subject, hook(button), button);
        if (button.slice == null) return;
        const matches = this.inventory.matchingSlices(button.slice);
        if (matches.length !== 1 || matches[0].slice.type !== 'StateChange') return;
        const slice = matches[0].slice;
        const unresolved = slice.commands.flatMap(command => command.properties).find(property => !property.isGenerated && !(property.isIdentifier && subject != null));
        if (button.opens === 'Runs') {
            if (unresolved !== undefined) this.context.error(DiagnosticCodes.RunsHookNeedsForm, `${hook(button)} runs slice '${button.slice}' directly, but nothing supplies its input '${unresolved.name}' without a form - remove 'runs' so the slice's screen asks for it`, button.location);
            return;
        }
        const target = slice.screens[0];
        if (target === undefined) {
            this.checkScreenless(button, unresolved?.name);
            return;
        }
        if (button.opens === 'Slot' && button.opensSlot != null) this.checkSlot(button, button.opensSlot, slots, screen, target);
    }

    private checkScreenless(button: ToolbarButtonSyntax, unresolved: string | undefined): void {
        if (button.opens == null && unresolved === undefined) return;
        const fix = unresolved === undefined ? "write 'runs' instead of 'opens'" : `give the slice a screen - its input '${unresolved}' needs a form`;
        this.context.error(DiagnosticCodes.HookToScreenlessSlice, `${hook(button)} opens slice '${button.slice}', which has no screen to open and cannot run - ${fix}`, button.location);
    }

    private checkSlot(button: ToolbarButtonSyntax, name: string, slots: readonly (readonly SlotSyntax[])[], screen: ScreenSyntax | null, target: ScreenSyntax): void {
        if (slots.length === 0) return;
        const slot = slots.flat().find(slot => slot.name === name);
        if (slot === undefined) this.context.error(DiagnosticCodes.HookSlotNotDeclared, `${hook(button)} opens in slot '${name}', which the template offering it does not declare`, button.location);
        else if (screen?.directives.some(directive => directive.kind === 'ScreenTemplateReferenceSyntax' && directive.slots.some(filled => filled.name === name))) this.context.error(DiagnosticCodes.HookSlotAlreadyFilled, `${hook(button)} opens in slot '${name}', which screen '${screen.name}' already fills`, button.location);
        else if (slot.role != null && (target.fits?.length ?? 0) > 0 && !target.fits!.includes(slot.role)) this.context.error(DiagnosticCodes.HookSlotRoleNotFitted, `${hook(button)} opens in slot '${name}', a ${slot.role.toLowerCase()} slot, but screen '${target.name}' fits ${(target.fits ?? []).map(fit => fit.toLowerCase()).join(', ')}`, button.location);
    }

    private checkEnabled(condition: ConditionSyntax | null | undefined, subject: ScreenActionSubjectKind | null | undefined, owner: string, node: { readonly location: ToolbarButtonSyntax['location'] }): void {
        if (condition != null && subject === 'Screen' && readsItem(condition)) this.context.error(DiagnosticCodes.EnabledWhenWithoutItem, `${owner} is enabled when a condition on 'item' holds, but it acts 'for screen' - there is no item to read; act 'for item'`, node.location);
    }
}
