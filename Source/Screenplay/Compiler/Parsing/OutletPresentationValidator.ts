// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticCodes } from '../Diagnostics/DiagnosticCodes';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import type { ScreenDirectiveSyntax, ScreenToolbarSyntax } from '../Syntax/Screens';
import type { ApplicationSyntax } from '../Syntax/Structure';
import { validateContributions } from './ContributionValidator';
import { HookValidator } from './HookValidator';
import { OutletInventory } from './OutletInventory';
import { OutletItemValidator } from './OutletItemValidator';
import { OutletValidator } from './OutletValidator';
import { ParserContext } from './ParserContext';
import { validateScreenBindings } from './ScreenBindingValidator';

export function validateOutletPresentation(application: ApplicationSyntax, context: ParserContext): void {
    const inventory = new OutletInventory(application);
    validateContributions(inventory, context);
    new OutletValidator(inventory, context).validate();
    new ToolbarValidator(inventory, context).visitApplication(application);
    new HookValidator(inventory, context).validate();
    validateScreenBindings(inventory, context);
}

class ToolbarValidator extends ScreenplaySyntaxWalker {
    private readonly items;
    constructor(inventory: OutletInventory, private readonly context: ParserContext) { super(); this.items = new OutletItemValidator(inventory, context); }
    override visitScreenDirective(directive: ScreenDirectiveSyntax): void {
        if (directive.kind === 'ScreenToolbarSyntax') this.checkToolbar(directive);
        super.visitScreenDirective(directive);
    }
    private checkToolbar(toolbar: ScreenToolbarSyntax): void {
        const seen = new Set<string>();
        for (const button of toolbar.buttons ?? []) {
            if (seen.has(button.id)) this.context.error(DiagnosticCodes.DuplicateToolbarButton, `Toolbar '${toolbar.name}' already declares a button with the id '${button.id}'`, button.location);
            seen.add(button.id);
            this.items.validateButton(button, `toolbar '${toolbar.name}'`);
        }
    }
}
