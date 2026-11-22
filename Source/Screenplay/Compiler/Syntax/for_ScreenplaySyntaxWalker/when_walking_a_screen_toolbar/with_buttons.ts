// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { ScreenToolbarSyntax, ToolbarButtonSyntax, ToolbarItemSyntax } from '../../Screens';
import { a_recording_walker } from '../given/a_recording_walker';
import { given } from '../given/given';
import { binding, condition, location, toolbarItem } from '../given/syntax';

describe('when walking a screen toolbar with buttons', given(a_recording_walker, context => {
    const parameter = { kind: 'ScreenNavigationParameterSyntax' as const, name: 'id', binding: binding('id'), location };
    const anotherParameter = { kind: 'ScreenNavigationParameterSyntax' as const, name: 'tab', binding: binding('tab'), location };
    const presentation = { kind: 'PresentationValueSyntax' as const, name: 'color', value: 'blue', location };
    const anotherPresentation = { kind: 'PresentationValueSyntax' as const, name: 'size', value: 'large', location };
    const enabled = condition('ready');
    const buttonEnabled = condition('selected');
    const first: ToolbarItemSyntax = { ...toolbarItem('first'), parameters: [parameter, anotherParameter],
        presentation: [presentation, anotherPresentation], enabledWhen: enabled };
    const second: ToolbarItemSyntax = { ...toolbarItem('second'), enabledWhen: null };
    const button: ToolbarButtonSyntax = { kind: 'ToolbarButtonSyntax', id: 'submit', title: 'Submit', enabledWhen: buttonEnabled, location };
    const anotherButton: ToolbarButtonSyntax = { kind: 'ToolbarButtonSyntax', id: 'cancel', title: 'Cancel', enabledWhen: null, location };
    const syntax: ScreenToolbarSyntax = { kind: 'ScreenToolbarSyntax', name: 'actions', items: [first, second], buttons: [button, anotherButton], location };

    beforeEach(() => context.visitScreenDirective(syntax));

    it('should dispatch parameter bindings and truthy conditions through public hooks', () => {
        context.hooks.should.deep.equal([
            'directive', 'node', 'node', 'node', 'binding', 'node', 'node', 'binding', 'node', 'node', 'node',
            'condition', 'node', 'expression', 'node', 'node', 'node', 'condition', 'node', 'expression', 'node', 'node',
        ]);
    });

    it('should finish item children before later items and visit buttons after all items', () => {
        context.nodes.should.have.ordered.members([
            syntax, syntax, first, parameter, parameter.binding, parameter.binding,
            anotherParameter, anotherParameter.binding, anotherParameter.binding, presentation, anotherPresentation,
            enabled, enabled, enabled.right, enabled.right, second, button,
            buttonEnabled, buttonEnabled, buttonEnabled.right, buttonEnabled.right, anotherButton,
        ]);
    });
}));
