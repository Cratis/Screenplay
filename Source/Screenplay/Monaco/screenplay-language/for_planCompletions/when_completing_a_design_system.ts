// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { planCompletions } from '../completion-planner';

const designSystem = [
    'design system Studio',
    '  modes light, dark',
    '  default theme Daylight',
    '  tokens',
    '    color',
    '      surface "The page background"',
    '      ink',
    '    radius',
    '      card',
    '  components',
    '    Button',
    '      parts text, background, radius',
    '      variant primary',
    '        text = color.ink',
    '        state hover',
    '          background = color.surface',
    'theme Daylight',
    '  implements Studio',
    '  palette',
    '    paper = "#ffffff"',
    '  values',
    '    color.surface = palette.paper',
    '  dark',
    '    color.ink = "#eeeeee"',
];

// What is offered after the given text typed in the middle of the design system, with the theme declared below it.
const offered = (extra: string): string[] => {
    const lines = [...designSystem.slice(0, 2), extra, ...designSystem.slice(3)];
    const plan = planCompletions(lines, 2, extra);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [`<${plan.kind}>`];
};

// What is offered in the given lines, typed at the end.
const offeredIn = (before: string[], extra: string): string[] => {
    const lines = [...before, extra];
    const plan = planCompletions(lines, lines.length - 1, extra);
    return plan.kind === 'entries' ? plan.entries.map(entry => entry.label) : [`<${plan.kind}>`];
};

describe('when completing a design system', () => {
    it('should offer the sections that are not declared yet', () => {
        offeredIn(['design system S', '  modes light'], '  ').should.have.members(['default theme', 'tokens', 'components', 'words', 'rules']);
    });

    it('should offer the token categories inside tokens', () => {
        offeredIn(['design system S', '  tokens'], '    ').should.include.members(['color', 'space', 'radius', 'z']);
    });

    it('should offer the declared themes after default theme', () => {
        offered('  default theme ').should.have.members(['Daylight']);
    });

    it('should offer only the parts of a component to style in a variant', () => {
        offeredIn(designSystem.slice(0, 13), '        ').should.include.members(['text', 'background', 'state']);
    });

    it('should offer color tokens for a text part and not radius tokens', () => {
        const labels = offeredIn(designSystem.slice(0, 13), '        text = ');
        labels.should.have.members(['color.surface', 'color.ink']);
    });

    it('should offer the states and parts inside a state', () => {
        offeredIn(designSystem.slice(0, 15), '          ').should.include.members(['text', 'background']);
    });

    it('should offer the parts after contrast', () => {
        offeredIn(['design system S', '  rules'], '    contrast text on ').should.include.members(['text', 'background']);
    });
});

describe('when completing a theme of a design system', () => {
    it('should offer implements, palette, values and the modes of the design system', () => {
        offeredIn(designSystem.slice(0, 17), '  ').should.include.members(['implements', 'palette', 'values', 'light', 'dark']);
    });

    it('should offer the design system after implements', () => {
        offeredIn(designSystem.slice(0, 17), '  implements ').should.have.members(['Studio']);
    });

    it('should offer token addresses at the start of a values line', () => {
        offeredIn(designSystem.slice(0, 21), '    ').should.include.members(['color.surface', 'color.ink', 'radius.card']);
    });

    it('should offer palette entries and tokens after the equals sign', () => {
        offeredIn(designSystem.slice(0, 21), '    color.ink = ').should.include.members(['palette.paper', 'color.surface']);
    });
});
