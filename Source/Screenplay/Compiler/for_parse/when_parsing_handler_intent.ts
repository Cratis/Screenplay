// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n';

describe('when parsing handler intent', () => {
    it.each(['          implementation', '          implementation\n            hint " Keep whitespace "', '          file C.cs', '          implementation\n            file C.cs', '          implementation\n            ```csharp\n            hint file implementation\n            ```'])('should model %s', body => {
        const result = parse(prefix + body);
        expect(result.diagnostics).toEqual([]);
        expect(result.value.modules[0].features[0].slices[0].commands[0].handler).not.toBeNull();
    });
    it.each([
        ['          implementation\n            hint unquoted', 'PLAY0493'],
        ['          implementation\n            hint " "', 'PLAY0493'],
        ['          implementation\n            hint "one" "two"', 'PLAY0493'],
        ['          implementation\n            provider csharp', 'PLAY0492'],
        ['          implementation\n          implementation', 'PLAY0492'],
        ['          file C.cs\n          implementation', 'PLAY0494'],
        ['          implementation\n            file C.cs\n            file D.cs', 'PLAY0494'],
        ['          implementation\n          file C.cs', 'PLAY0494'],
    ])('should reject %s', (body, code) => {
        expect(parse(prefix + body).diagnostics.map(diagnostic => diagnostic.code)).toContain(code);
    });
    it.each([
        ['', ['PLAY0045']],
        ['          unknown', ['PLAY0046']],
        ['          file /absolute/C.cs', ['PLAY0264']],
        ['          ```unregistered', ['PLAY0163', 'PLAY0046']],
        ['          ```csharp\n          return null;', ['PLAY0164']],
        ['          csharp\n            ```\n            return null;\n            ```', ['PLAY0397']],
        ['          csharp', ['PLAY0397', 'PLAY0163', 'PLAY0046']],
        ['          implementation extra', ['PLAY0492']],
        ['          implementation\n            hint "one"\n              nested', ['PLAY0493']],
        ['          implementation\n            file C.cs\n              nested', ['PLAY0492']],
        ['          implementation\n            unknown\n              nested', ['PLAY0492']],
        ['          implementation\n            ```csharp\n            ```\n            file C.cs', ['PLAY0494']],
        ['          implementation\n            file C.cs\n            ```csharp\n            return null;\n            ```', ['PLAY0494']],
        ['          implementation\n          ```csharp\n          return null;\n          ```', ['PLAY0494']],
        ['          implementation\n          unknown\n            nested', ['PLAY0494']],
    ])('should retain precise handler diagnostics for %s', (body, codes) => {
        expect(parse(prefix + body).diagnostics.map(diagnostic => diagnostic.code)).toEqual(codes);
    });
    it('should retain last-handler legacy behavior and refuse mixing produces', () => {
        const result = parse(prefix + '          file Old.cs\n        handler\n          file New.cs\n        produces Registered\n      event Registered');
        expect(result.value.modules[0].features[0].slices[0].commands[0].handler?.file?.path).toBe('New.cs');
        expect(result.diagnostics.map(diagnostic => diagnostic.code)).toEqual(['PLAY0035']);
    });
    it('should keep decoded escapes and hint order without trimming', () => {
        const result = parse(prefix + '          implementation\n            hint " first "\n            hint "Keep \\"quotes\\" and // text"');
        expect(result.value.modules[0].features[0].slices[0].commands[0].handler?.implementation?.hints.map(hint => hint.text)).toEqual([' first ', 'Keep "quotes" and // text']);
    });
    it('should preserve ordinary keyword names and legacy direct forms', () => {
        const result = parse('module M\n  feature F\n    slice StateChange S\n      command C\n        handler String\n        hint String\n        implementation String\n        file String');
        expect(result.diagnostics).toEqual([]);
        expect(result.value.modules[0].features[0].slices[0].commands[0].properties.map(property => property.name)).toEqual(['handler', 'hint', 'implementation', 'file']);
    });
    it('should visit hints and payload exactly once with original locations', () => {
        class Walker extends ScreenplaySyntaxWalker {
            nodes: SyntaxNode[] = [];
            override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
        }
        const result = parse(prefix + '          implementation\n            hint " first "\n            hint "second"\n            file C.cs');
        const walker = new Walker();
        walker.visitApplication(result.value);
        expect(walker.nodes.filter(node => node.kind === 'ImplementationHintSyntax').map(node => node.location.line)).toEqual([7, 8]);
        expect(walker.nodes.filter(node => node.kind === 'FileReferenceSyntax')).toHaveLength(1);
    });
});
