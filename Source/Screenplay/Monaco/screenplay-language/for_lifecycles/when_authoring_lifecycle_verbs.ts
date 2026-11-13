// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { stateItems, workflowItems } from '../completion-items';

describe('when authoring lifecycle verbs', () => {
    const stateLabels = stateItems.map(item => item.label);
    const action = workflowItems.find(item => item.label === 'action')!;
    const workflow = workflowItems.find(item => item.label === 'workflow') ?? action;

    it('should offer entering invokes and entering starts', () => {
        expect(stateLabels).toContain('entering invokes');
        expect(stateLabels).toContain('entering starts');
    });

    it('should not offer the deprecated entering forms', () => {
        expect(stateLabels).not.toContain('entering send');
        expect(stateLabels).not.toContain('entering start');
    });

    it('should insert an action that invokes a command and wires refused', () => {
        expect(action.insertText).toContain('invokes');
        expect(action.insertText).not.toContain('sends');
        expect(action.insertText).toContain('refused ->');
    });

    it('should not teach sends in any workflow snippet', () => {
        expect(workflow.insertText).not.toContain(' sends ');
    });
});
