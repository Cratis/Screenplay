// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../SyntaxJson';

describe('when writing exact number literals carrying serialization hooks', () => {
    const hooked = (hook: (tag: object, replace: (value: unknown) => void) => void): string => {
        const result = parseSpecificationSource('numbers exact\nspecification S\n  when C\n    n = 9007199254740993\n');
        const tree = structuredClone(result.value![0]);
        const [parent, key] = findSlot(tree);
        hook(parent[key] as object, value => { parent[key] = value; });
        return JSON.stringify(toSyntaxJson(tree as never));
    };

    const findSlot = (value: unknown): [Record<string, unknown>, string] => {
        const stack = [value];
        while (stack.length > 0) {
            const current = stack.pop();
            if (typeof current !== 'object' || current === null) continue;
            for (const [key, member] of Object.entries(current)) {
                if ((member as { literalType?: string } | null)?.literalType === 'ExactNumber') return [current as Record<string, unknown>, key];
                stack.push(member);
            }
        }
        throw new Error('no ExactNumber tag');
    };

    it('should ignore a non-enumerable toJSON', () => {
        const json = hooked(tag => Object.defineProperty(tag, 'toJSON', { value: () => 9007199254740992 }));
        json.should.contain('"9007199254740993"');
        json.should.not.contain('9007199254740992');
    });

    it('should ignore an inherited toJSON', () => {
        const json = hooked(tag => Object.setPrototypeOf(tag, { toJSON: () => 9007199254740992 }));
        json.should.contain('"9007199254740993"');
        json.should.not.contain('9007199254740992');
    });

    const rounding = (): number => 9007199254740992;

    // Replaces every non-empty node array with a hooked copy and expects the very same output.
    const withHookedArrays = (hook: (items: unknown[]) => unknown[]): [string, string] => {
        const tree = structuredClone(parseSpecificationSource('numbers exact\nspecification S\n  when C\n    n = 9007199254740993\n').value![0]);
        const before = JSON.stringify(toSyntaxJson(structuredClone(tree) as never));
        const visit = (value: unknown): void => {
            if (typeof value !== 'object' || value === null) return;
            for (const [key, member] of Object.entries(value)) {
                visit(member);
                if (Array.isArray(member) && member.length > 0) (value as Record<string, unknown>)[key] = hook(member);
            }
        };
        visit(tree);
        return [before, JSON.stringify(toSyntaxJson(tree as never))];
    };

    it('should ignore an Array subclass carrying toJSON and species', () => {
        class Hooked extends Array<unknown> { toJSON(): unknown { return rounding(); } }
        const [before, after] = withHookedArrays(items => Hooked.from(items));
        after.should.equal(before);
    });

    it('should ignore an array with an own toJSON property', () => {
        const [before, after] = withHookedArrays(items => Object.defineProperty([...items], 'toJSON', { value: rounding, enumerable: true }));
        after.should.equal(before);
    });

    it('should emit literalType before value whatever the input key order', () => {
        const json = hooked(tag => {
            const text = (tag as { value: string }).value;
            for (const key of Object.keys(tag)) delete (tag as Record<string, unknown>)[key];
            Object.assign(tag, { value: text, literalType: 'ExactNumber' });
        });
        json.should.contain('{"literalType":"ExactNumber","value":"9007199254740993"}');
    });

    // Replaces the ExactNumber literal with a plain JS number and hooks every node array, expecting a refusal.
    const refusedWithHookedArrays = (hook: (items: unknown[]) => unknown[]): void => {
        const tree = structuredClone(parseSpecificationSource('numbers exact\nspecification S\n  when C\n    n = 9007199254740993\n').value![0]);
        const [parent, key] = findSlot(tree);
        parent[key] = 9007199254740992;
        const visit = (value: unknown): void => {
            if (typeof value !== 'object' || value === null) return;
            for (const [name, member] of Object.entries(value)) {
                visit(member);
                if (Array.isArray(member) && member.length > 0) (value as Record<string, unknown>)[name] = hook(member);
            }
        };
        visit(tree);
        (() => toSyntaxJson(tree as never)).should.throw();
    };

    it('should refuse a rounded number when forEach is overridden on the arrays', () => {
        refusedWithHookedArrays(items => Object.defineProperty(items, 'forEach', { value: () => {} }));
    });

    it('should refuse a rounded number when an Array subclass overrides the iteration methods', () => {
        class Hooked extends Array<unknown> {
            override forEach(): void {}
            override every(): boolean { return true; }
            override some(): boolean { return false; }
            override map(): never[] { return []; }
            override entries(): never { throw new Error('hooked'); }
            override [Symbol.iterator](): never { throw new Error('hooked'); }
        }
        refusedWithHookedArrays(items => Hooked.from(items));
    });
});
