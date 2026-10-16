// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { toEventModelDocument } from '../EventModelDocumentVisitor';
import { problems_the_board_finds_in } from './given/the_board_schema';

describe('when mapping first-class literal text rules', () => {
    const source = 'module M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        validate\n          value contains "A"\n          value not contains "B"\n          value starts with "A"\n          value ends with "A"\n';
    const document = toEventModelDocument(parse(source).value, 'TextRules');
    it('should retain each distinct rule on the board', () => {
        document.collections[0].modules[0].features[0].slices[0].command!.rules[0].rules.map(rule => rule.ruleType).should.deep.equal(['Contains', 'NotContains', 'StartsWith', 'EndsWith']);
    });
    it('should produce a document the board can read', () => { problems_the_board_finds_in(document).should.deep.equal([]); });
});
