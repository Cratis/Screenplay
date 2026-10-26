// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { ScreenActionSyntax, ScreenActionSubjectKind, ScreenComponentSyntax, ScreenDataSyntax, ScreenGuardedActionSyntax, ScreenNavigateSyntax, ScreenTemplateReferenceSyntax, ScreenToolbarSyntax } from '../Syntax/Screens';
import { a_parsed_document } from './given/a_parsed_document';

const parse = (directives: string) => a_parsed_document('module Work', '  feature Items', '    slice StateView Details',
    '      command for', '      command Retry', '      command label', '      query Rows => Row[]', '      query for => Row',
    '      screen Details', '        data Row[] via query Rows', ...directives.split('\n').map(line => `        ${line}`));
const screen = (result: ReturnType<typeof parse>) => result.value.modules[0].features[0].slices[0].screens[0];

const actions: [string, string, ScreenActionSubjectKind | undefined, string | null][] = [
    ['action for', 'for', undefined, null],
    ['action for for screen', 'for', 'Screen', null],
    ['action Work.Items.Details.for for item', 'Work.Items.Details.for', 'Item', null],
    ['action label', 'label', undefined, null],
    ['action label for screen label "Retry for screen"', 'label', 'Screen', 'Retry for screen'],
    ['action for label "Retry for screen"', 'for', undefined, 'Retry for screen'],
    ['action for for item label "Retry \\"for\\" screen"', 'for', 'Item', 'Retry "for" screen'],
    ['action for for screen label $strings.for.label', 'for', 'Screen', '$strings.for.label'],
];
const guarded: [string, string, ScreenActionSubjectKind | undefined][] = [
    ['action "Retry for screen"', 'Retry for screen', undefined],
    ['action "Retry \\"for\\" screen" for item', 'Retry "for" screen', 'Item'],
    ['action $strings.for.label for screen', '$strings.for.label', 'Screen'],
];
const toolbarActions: [string, string, string, ScreenActionSubjectKind | undefined][] = [
    ['item for action Retry', 'for', 'Retry', undefined],
    ['item retry action for', 'retry', 'for', undefined],
    ['item for action for', 'for', 'for', undefined],
    ['item for action for for screen', 'for', 'for', 'Screen'],
    ['item for action Retry for item', 'for', 'Retry', 'Item'],
    ['item label action label for screen', 'label', 'label', 'Screen'],
    ['item for action to for', 'for', 'for', undefined],
    ['item for action to for for screen', 'for', 'for', 'Screen'],
    ['item for action Work.Items.Details.for for item', 'for', 'Work.Items.Details.for', 'Item'],
];

describe('when preserving for identifiers in screen headers', () => {
    it.each(actions)('should preserve the plain command and trailing metadata in %s', (header, command, subject, label) => {
        const result = parse(header);
        const action = screen(result).directives[1] as ScreenActionSyntax;
        expect(action.command).toBe(command);
        expect(action.subject).toBe(subject);
        expect(action.label).toBe(label);
        expect(result.success).toBe(true);
    });
    it.each(guarded)('should keep guarded operands opaque in %s', (header, label, subject) => {
        const result = parse(`${header}\n  when item.ready == true execute for`);
        const action = screen(result).directives[1] as ScreenGuardedActionSyntax;
        expect(action.label).toBe(label);
        expect(action.subject).toBe(subject);
        expect(action.alternatives[0].command).toBe('for');
        expect(result.success).toBe(true);
    });
    it.each(toolbarActions)('should consume the complete toolbar action header in %s', (header, name, target, subject) => {
        const result = parse(`toolbar\n  ${header}`);
        const item = (screen(result).directives[1] as ScreenToolbarSyntax).items[0];
        expect(item.name).toBe(name);
        expect(item.target).toBe(target);
        expect(item.subject).toBe(subject);
        expect(result.success).toBe(true);
    });
    it.each([
        ['item for navigate to for', 'for', 'for'],
        ['item for navigate for', 'for', 'for'],
        ['item for dialog for', 'for', 'for'],
        ['item for dialog to for', 'for', 'for'],
        ['item label navigate to label', 'label', 'label'],
    ])('should preserve non action toolbar operands in %s', (header, name, target) => {
        const result = parse(`toolbar\n  ${header}`);
        const item = (screen(result).directives[1] as ScreenToolbarSyntax).items[0];
        expect(item.name).toBe(name);
        expect(item.target).toBe(target);
        expect(item.subject).toBeUndefined();
        expect(result.success).toBe(true);
    });
    it('should preserve non action directive fields', () => {
        const result = parse('navigate to for\ndata Row via query for\ncomponent for screen\ncomponent Widget for\ntemplate for');
        const directives = screen(result).directives;
        expect((directives[1] as ScreenNavigateSyntax).screen).toBe('for');
        expect((directives[2] as ScreenDataSyntax).query).toBe('for');
        expect((directives[3] as ScreenComponentSyntax).component).toBe('for');
        expect((directives[3] as ScreenComponentSyntax).name).toBe('screen');
        expect((directives[4] as ScreenComponentSyntax).name).toBe('for');
        expect((directives[5] as ScreenTemplateReferenceSyntax).name).toBe('for');
        expect(result.success).toBe(true);
    });
    it('should preserve inputs and child labels', () => {
        const result = parse('action for for item label "Inline for screen"\n  with note from item.note\n  label "Child for screen"\ntoolbar\n  item for action for for screen label "Toolbar for item"');
        const action = screen(result).directives[1] as ScreenActionSyntax;
        expect(action.subject).toBe('Item');
        expect(action.label).toBe('Child for screen');
        expect(action.inputs![0].binding).toBe('item.note');
        expect((screen(result).directives[2] as ScreenToolbarSyntax).items[0].label).toBe('Toolbar for item');
        expect(result.success).toBe(true);
    });
    it.each([
        ['item for action for for screen label "Retry for item"', 'Retry for item'],
        ['item for action for for screen label $strings.for.label', '$strings.for.label'],
    ])('should preserve toolbar labels and parameters in %s', (header, label) => {
        const result = parse(`toolbar\n  ${header}\n    parameter note from data item.note`);
        const item = (screen(result).directives[1] as ScreenToolbarSyntax).items[0];
        expect(item.name).toBe('for');
        expect(item.target).toBe('for');
        expect(item.subject).toBe('Screen');
        expect(item.label).toBe(label);
        expect(item.parameters[0].binding.path).toBe('item.note');
        expect(result.success).toBe(true);
    });
    it.each([
        'toolbar\n  item for navigate to for for item',
        'toolbar\n  item for dialog for for screen',
        'navigate to for for item',
        'data Row via query for for screen',
        'component for screen for item',
    ])('should reject subject tails on non actions after complete operands in %s', directive => {
        expect(parse(directive).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0817')).toHaveLength(1);
    });
    it.each(['for', 'for planet', 'for item for screen', 'label "Try" for screen'])('should reject malformed tails after keyword named operands: %s', suffix => {
        const result = parse(`action for ${suffix}\ntoolbar\n  item for action for ${suffix}`);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0817')).toHaveLength(2);
    });
});
