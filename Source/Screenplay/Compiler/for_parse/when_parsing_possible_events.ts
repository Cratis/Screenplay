// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { parse } from '../ScreenplayCompiler';
import { ScreenplaySyntaxWalker } from '../Syntax/ScreenplaySyntaxWalker';
import { SyntaxNode } from '../Syntax/SyntaxNode';
import { toSyntaxJson } from '../Syntax/SyntaxJson';

const prefix = 'module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n';
const declaration = '          may produce\n            Added\n            Removed for accountId\n            Updated for one of accountIds\n';

describe('when parsing possible events', () => {
    it.each(['          file C.cs', '          implementation', '          implementation\n            hint "Keep the cutoff"\n            file C.cs', '          ```csharp\n          return null;\n          ```'])('should preserve the contract beside %s', payload => {
        const result = parse(prefix + declaration + payload);
        expect(result.diagnostics).toEqual([]);
        const handler = result.value.modules[0].features[0].slices[0].commands[0].handler;
        expect(handler?.possibleEvents?.events.map(entry => [entry.event, entry.property, entry.oneOf])).toEqual([['Added', null, false], ['Removed', 'accountId', false], ['Updated', 'accountIds', true]]);
        expect(JSON.stringify(toSyntaxJson(result.value))).toContain('PossibleEventsSyntax');
    });
    it.each(['Added when active', 'Added\n            Added for other', 'Added for cmd.accountId', 'Added for 123', 'Added\n              value = accountId', 'Added\n              for accountId'])('should reject %s', entry => {
        expect(parse(prefix + '          may produce\n            ' + entry + '\n          file C.cs').diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0817');
    });
    it.each(['          file C.cs\n          may produce\n            Added', '          may produce\n            Added\n          may produce\n            Removed\n          file C.cs', '          implementation\n            may produce\n              Added'])('should reject misplaced sets in %s', body => {
        expect(parse(prefix + body).diagnostics.length).toBeGreaterThan(0);
    });
    it('should reject mixed contracts', () => {
        expect(parse(prefix + declaration + '          file C.cs\n        produces Added').diagnostics.map(diagnostic => diagnostic.code)).toContain('PLAY0817');
    });
    it('should omit absent and empty sets from legacy bytes', () => {
        const handler = parse(prefix + '          file C.cs').value.modules[0].features[0].slices[0].commands[0].handler!;
        expect(toSyntaxJson({ ...handler, possibleEvents: { kind: 'PossibleEventsSyntax', events: [], location: handler.location } } as typeof handler)).toEqual(toSyntaxJson(handler));
        expect(JSON.stringify(toSyntaxJson(handler))).not.toContain('possibleEvents');
    });
    it('should visit the contract and payload exactly once', () => {
        class Walker extends ScreenplaySyntaxWalker {
            nodes: SyntaxNode[] = [];
            override visitNode(node: SyntaxNode): void { this.nodes.push(node); }
        }
        const result = parse(prefix + declaration + '          implementation\n            hint "Keep order"\n            file C.cs');
        const walker = new Walker();
        walker.visitApplication(result.value);
        expect(walker.nodes.filter(node => node.kind === 'PossibleEventsSyntax')).toHaveLength(1);
        expect(walker.nodes.filter(node => node.kind === 'PossibleEventSyntax')).toHaveLength(3);
        expect(walker.nodes.filter(node => node.kind === 'ImplementationHintSyntax')).toHaveLength(1);
        expect(walker.nodes.filter(node => node.kind === 'FileReferenceSyntax')).toHaveLength(1);
    });
});
