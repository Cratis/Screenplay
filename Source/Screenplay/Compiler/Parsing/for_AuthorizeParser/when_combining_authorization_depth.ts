// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { parseFolder } from '../../Files/PlayApplicationAssembly';

const chain = Array<string>(127).fill('Access').join(' and ');

describe('when combining authorization depth', () => {
    it.each([128, 129, 130, 258])('should bound %s authorize lines on one command', count => {
        const source = 'module Portal\n  feature Work\n    slice StateChange Save\n      command Save\n' + Array<string>(count).fill('        authorize Access').join('\n');
        const result = parse(source, 'command.play');
        const gate = result.value.modules[0].features[0].slices[0].commands[0].authorize;
        expect(gate === null).toBe(count > 128);
        const diagnostics = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0860');
        expect(diagnostics).toHaveLength(count > 128 ? 1 : 0);
        if (count > 128) expect(diagnostics[0].location).toMatchObject({ path: 'command.play', line: 133 });
    });
    it.each(['module', 'feature', 'query'] as const)('should retain a refused %s gate after later lines', owner => {
        const prefix = owner === 'module' ? 'module Portal\n' : owner === 'feature' ? 'module Portal\n  feature Work\n' : 'module Portal\n  feature Work\n    slice StateView Browse\n      query Browse => Items\n';
        const indent = owner === 'module' ? '  ' : owner === 'feature' ? '    ' : '        ';
        const source = prefix + Array<string>(130).fill(indent + 'authorize Access').join('\n') + '\nmodule Other\n  authorize Access';
        const result = parse(source, 'owners.play');
        const module = result.value.modules[0];
        const gate = owner === 'module' ? module.authorize : owner === 'feature' ? module.features[0].authorize : module.features[0].slices[0].queries[0].authorize;
        expect(gate).toBeNull();
        expect(result.value.modules[1].authorize).not.toBeNull();
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0860')).toHaveLength(1);
    });
    it.each([1, 2])('should budget a deep line combined with %s more gates', count => {
        const source = 'module Portal\n  authorize ' + chain + '\n' + Array<string>(count).fill('  authorize Access').join('\n');
        const result = parse(source, 'module.play');
        expect(result.value.modules[0].authorize === null).toBe(count === 2);
        expect(result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0860')).toHaveLength(count === 2 ? 1 : 0);
    });
    it.each([1, 2])('should bound gates merged from %s additional files', count => {
        const result = parseFolder([
            { path: '0.play', source: 'module Portal\n  authorize ' + chain },
            ...Array.from({ length: count }, (_, index) => ({ path: `${index + 1}.play`, source: `module Portal\n  authorize Extra${index}` })),
        ]);
        expect(result.value.modules[0].authorize === null).toBe(count === 2);
        const diagnostics = result.diagnostics.filter(diagnostic => diagnostic.code === 'PLAY0860');
        expect(diagnostics).toHaveLength(count === 2 ? 1 : 0);
        if (count === 2) expect(diagnostics[0].location).toMatchObject({ path: '2.play', line: 2, column: 3 });
    });
});
