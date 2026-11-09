// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';
import { hoverContent } from '../hover-content';
import { clauseKeywords } from '../language';
import { scanDocument } from '../symbols';

const prefix = ['module Orders', '  feature Shipping', '    slice StateView ShipOrder'];

const labelsAt = (lines: string[], line: number): string[] => {
    const plan = planCompletions(lines, line, lines[line], scanDocument(lines));
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [];
};

describe('when authoring screen visuals and placements', () => {
    it.each(['shows event', 'shows readmodel'])('should offer %s in a slice', label => {
        const lines = [...prefix, '      '];
        labelsAt(lines, 3).should.include(label);
    });

    it.each(['sketch', 'mockup', 'image'])('should offer %s in a screen', label => {
        const lines = [...prefix, '      screen Ship', '        '];
        labelsAt(lines, 4).should.include(label);
    });

    it('should explain a placement in hover text', () => {
        const lines = [...prefix, '      shows event OrderPlaced'];
        hoverContent(lines, 3, 'shows', 7, 12)!.should.contain('linked copy');
    });

    it('should explain a mockup in hover text', () => {
        const lines = [...prefix, '      screen Ship', '        mockup Screens/ship.html'];
        hoverContent(lines, 4, 'mockup', 9, 15)!.should.contain('HTML mockup page');
    });

    it.each(['sketch', 'mockup', 'image', 'shows'])('should highlight %s', keyword => {
        clauseKeywords.includes(keyword).should.equal(true);
    });
});
