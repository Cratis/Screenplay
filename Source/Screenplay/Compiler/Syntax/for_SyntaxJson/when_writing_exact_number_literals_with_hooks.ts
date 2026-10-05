// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parseSpecificationSource } from '../../ScreenplayCompiler';
import { toSyntaxJson } from '../SyntaxJson';

describe('when writing exact number literals carrying serialization hooks', () => {
    const hooked = (hook: (tag: object) => void): string => {
        const result = parseSpecificationSource('numbers exact\nspecification S\n  when C\n    n = 9007199254740993\n');
        const tree = structuredClone(result.value![0]);
        hook(findTag(tree));
        return JSON.stringify(toSyntaxJson(tree as never));
    };

    const findTag = (value: unknown): object => {
        const stack = [value];
        while (stack.length > 0) {
            const current = stack.pop();
            if (typeof current !== 'object' || current === null) continue;
            if ((current as { literalType?: string }).literalType === 'ExactNumber') return current;
            stack.push(...Object.values(current));
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
});
