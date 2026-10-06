// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';
import { toCompleteSyntaxJson, toSyntaxJson } from '../SyntaxJson';
import { InvalidSyntaxJson } from '../InvalidSyntaxJson';
import { SyntaxNode } from '../SyntaxNode';

function literalSlot(value: unknown): Record<string, unknown> {
    if (typeof value === 'object' && value !== null) {
        const record = value as Record<string, unknown>;
        if (record.kind === 'LiteralExpressionSyntax') return record;
        for (const member of Object.values(record)) {
            try { return literalSlot(member); } catch { /* Keep looking through the syntax tree. */ }
        }
    }
    throw new Error('No literal expression.');
}

function source() {
    return structuredClone(parseSpecificationSource('numbers exact\nspecification S\n  when C\n    n = 9007199254740993\n').value![0]);
}

describe.each([{ name: 'the wire writer', writer: toSyntaxJson }, { name: 'the complete writer', writer: toCompleteSyntaxJson }])('when snapshotting caller-owned syntax with $name', ({ writer }) => {
    describe.each(['inherited', 'non-enumerable', 'class getter'])('with a %s kind', shape => {
        function hint(text: string, read: () => string): SyntaxNode {
            if (shape === 'class getter') return new class {
                text = text;
                get kind() { return read(); }
            }() as unknown as SyntaxNode;
            const descriptor = { get: read, enumerable: false };
            return (shape === 'inherited'
                ? Object.assign(Object.create(Object.defineProperty({}, 'kind', descriptor)), { text })
                : Object.defineProperty({ text }, 'kind', descriptor)) as SyntaxNode;
        }

        it('retains the node kind and reads it once', () => {
            let reads = 0;
            const node = hint('Keep this hint', () => ++reads === 1 ? 'ImplementationHintSyntax' : 'UnknownSyntax');
            JSON.stringify(writer(node)).should.equal('{"kind":"ImplementationHintSyntax","text":"Keep this hint"}');
            reads.should.equal(1);
        });

        it('still enforces Legacy node invariants after one kind read', () => {
            let reads = 0;
            const node = hint('', () => ++reads === 1 ? 'ImplementationHintSyntax' : 'UnknownSyntax');
            (() => writer(node)).should.throw(InvalidSyntaxJson, 'An implementation hint must be nonblank.');
            reads.should.equal(1);
        });
    });

    it('reports the Exact depth-96 error rather than overflowing while snapshotting', () => {
        const tree = source();
        let expression: Record<string, unknown> = { kind: 'LiteralExpressionSyntax', value: true };
        for (let depth = 0; depth < 20000; depth++) expression = { kind: 'ListExpressionSyntax', items: [expression] };
        Object.assign(literalSlot(tree), expression);
        (() => writer(tree)).should.throw(InvalidSyntaxJson, 'Syntax nesting exceeds the supported depth of 96.');
    });

    it('ignores a deeply nested unknown Exact member without overflowing', () => {
        const tree = source(), expected = JSON.stringify(writer(tree));
        let unknown: object = {};
        for (let depth = 0; depth < 20000; depth++) unknown = { unknown };
        Object.assign(tree, { unknown });
        JSON.stringify(writer(tree)).should.equal(expected);
    });

    it('reads an accessor once and validates and writes its detached value', () => {
        const tree = source(), expected = JSON.stringify(writer(tree));
        const literal = literalSlot(tree), value = literal.value;
        let reads = 0;
        Object.defineProperty(literal, 'value', { enumerable: true, get: () => ++reads === 1 ? value : 9007199254740992 });
        JSON.stringify(writer(tree)).should.equal(expected);
        reads.should.equal(1);
    });

    it('reads a proxy member once instead of trusting a later rounded value', () => {
        const tree = source(), expected = JSON.stringify(writer(tree));
        const literal = literalSlot(tree), tag = literal.value as object;
        let reads = 0;
        literal.value = new Proxy(tag, { get: (target, key, receiver) => key === 'value' && ++reads > 1 ? 9007199254740992 : Reflect.get(target, key, receiver) });
        JSON.stringify(writer(tree)).should.equal(expected);
        reads.should.equal(1);
    });

    it('copies indexed arrays once without re-reading their elements', () => {
        const tree = source(), expected = JSON.stringify(writer(tree));
        const statements = tree.when!.values as unknown as unknown[];
        const item = statements[0];
        let reads = 0;
        Object.defineProperty(statements, '0', { enumerable: true, get: () => ++reads === 1 ? item : null });
        JSON.stringify(writer(tree)).should.equal(expected);
        reads.should.equal(1);
    });

    it('cannot serialize through global object or array prototype toJSON hooks', () => {
        const tree = source(), expected = JSON.stringify(writer(tree));
        const prototypes = [Object.prototype, Array.prototype];
        const descriptors = prototypes.map(prototype => Object.getOwnPropertyDescriptor(prototype, 'toJSON'));
        let actual: string | undefined;
        try {
            for (const prototype of prototypes) Object.defineProperty(prototype, 'toJSON', { configurable: true, value: () => 9007199254740992 });
            actual = JSON.stringify(writer(tree));
        } finally {
            prototypes.forEach((prototype, index) => {
                const descriptor = descriptors[index];
                if (descriptor) Object.defineProperty(prototype, 'toJSON', descriptor);
                else Reflect.deleteProperty(prototype, 'toJSON');
            });
        }
        actual!.should.equal(expected);
    });
});
