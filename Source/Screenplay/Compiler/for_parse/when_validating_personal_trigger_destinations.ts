// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { parseFolder } from '../Files/PlayApplicationAssembly';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const reaction = ['module M', '  feature F', '    slice Automation S', '      event Recorded', '      reaction R', '        when External', '          produces Recorded', '            for patient'];

describe('when validating personal trigger destinations', () => {
    it.each([['PatientId', 'Uuid', 0], ['Uuid', 'PatientId', 1], ['PatientId', 'PatientId', 1], ['Uuid', 'Uuid', 0]])('should prefer the event shape for trigger %s and event %s', (triggerType, eventType, count) => {
        const source = ['concept PatientId : Uuid @pii', 'trigger External', `  patient ${triggerType}`, ...reaction];
        source.splice(6, 0, '      event External', `        patient ${eventType}`);
        expect(parse(source.join('\n')).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0515')).toHaveLength(count as number);
    });

    it('should not borrow personal trigger values for an imported event with an unknown shape', () => {
        const source = ['import Outside.External', 'concept PatientId : Uuid @pii', 'trigger External', '  patient PatientId', ...reaction];
        expect(parse(source.join('\n')).diagnostics).toEqual([]);
    });

    it('should still check a personal trigger destination when event resolution is ambiguous', () => {
        const source = ['concept PatientId : Uuid @pii', 'trigger External', '  patient PatientId', ...reaction,
            '    slice StateChange First', '      event External', '        patient Uuid',
            '    slice StateChange Second', '      event External', '        patient Uuid'];
        expect(parse(source.join('\n')).diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0515')).toHaveLength(1);
    });

    it('should prefer the event shape across documents', () => {
        const declarations = 'concept PatientId : Uuid @pii\ntrigger External\n  patient PatientId';
        const source = [...reaction];
        source.splice(3, 0, '      event External', '        patient Uuid');
        const result = parseFolder([{ path: 'types.play', source: declarations }, { path: 'application.play', source: source.join('\n') }]);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0515')).toEqual([]);
    });

    it('should retain declared trigger shapes across imported documents without changing syntax wire bytes', () => {
        const declarations = 'concept PatientId : Uuid @pii\ntrigger External\n  patient PatientId';
        const result = parseFolder([{ path: 'types.play', source: declarations }, { path: 'application.play', source: reaction.join('\n') }]);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0515').map(diagnostic => diagnostic.location.path)).toEqual(['application.play']);
        expect(toSyntaxJson(parse(declarations).value)).not.toHaveProperty('declaredTriggers');
        expect(toSyntaxJson(parse('numbers exact\n' + declarations).value)).not.toHaveProperty('declaredTriggers');
    });

    it('should not guess an undeclared trigger or an untyped declared value', () => {
        expect(parse(reaction.join('\n')).diagnostics).toEqual([]);
        expect(parse(['trigger External', '  patient', ...reaction].join('\n')).diagnostics).toEqual([]);
    });
});
