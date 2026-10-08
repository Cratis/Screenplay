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

    it.each([
        ['PatientId', 'Uuid', 'Uuid', 0],
        ['PatientId', 'Uuid', 'PatientId', 1],
        ['Uuid', 'Uuid', 'PatientId', 1],
        ['Uuid', 'PatientId', 'Uuid', 1],
        ['PatientId', 'PatientId', 'PatientId', 1]
    ])('should use event and clause types for trigger %s, event %s and clause %s', (triggerType, eventType, clauseType, count) => {
        const source = ['concept PatientId : Uuid @pii', 'trigger External', `  patient ${triggerType}`, ...reaction];
        source.splice(6, 0, '      event External', `        patient ${eventType}`);
        source.splice(11, 0, `          patient ${clauseType}`);
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`)
            .should.deep.equal(Array.from({ length: count as number }, () => 'PLAY0515@14'));
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

    it('should keep typed values local to each trigger clause', () => {
        const source = ['concept PatientId : Uuid @pii', ...reaction];
        source.splice(7, 0, '          patient PatientId');
        source.push('        when Other', '          patient Uuid', '          produces Recorded', '            for patient', '      reaction Another', '        when External', '          patient', '          produces Recorded', '            for patient');
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0515@10']);
    });

    it.each(['', 'numbers exact\n'])('should keep clause-local data outside the syntax wire projection in %s mode', mode => {
        const source = [...reaction];
        source.splice(6, 0, '          patient Uuid');
        expect(toSyntaxJson(parse(mode + source.join('\n')).value)).toEqual(toSyntaxJson(parse(mode + reaction.join('\n')).value));
    });

    it('should report only once for personal data in all three shapes', () => {
        const source = ['concept PatientId : Uuid @pii', 'trigger External', '  patient PatientId', ...reaction];
        source.splice(6, 0, '      event External', '        patient PatientId');
        source.splice(11, 0, '          patient PatientId');
        parse(source.join('\n')).diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.line}`).should.deep.equal(['PLAY0515@14']);
    });

    it('should retain clause-local shapes across imported documents', () => {
        const source = [...reaction];
        source.splice(6, 0, '          patient PatientId');
        const result = parseFolder([{ path: 'types.play', source: 'concept PatientId : Uuid @pii' }, { path: 'application.play', source: source.join('\n') }]);
        result.diagnostics.map(diagnostic => `${diagnostic.code}@${diagnostic.location.path}:${diagnostic.location.line}`).should.deep.equal(['PLAY0515@application.play:9']);
    });

    it('should not guess an undeclared trigger or an untyped declared value', () => {
        expect(parse(reaction.join('\n')).diagnostics).toEqual([]);
        expect(parse(['trigger External', '  patient', ...reaction].join('\n')).diagnostics).toEqual([]);
    });
});
