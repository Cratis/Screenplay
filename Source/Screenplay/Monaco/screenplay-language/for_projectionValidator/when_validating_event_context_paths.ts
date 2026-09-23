// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, expect } from 'vitest';
import type { editor } from 'monaco-editor';
import { Validator } from '../sub-languages/projection/Validator';

const modelFor = (content: string): editor.ITextModel => ({ getValue: () => content }) as editor.ITextModel;

describe('when validating event context paths in the standalone projection editor', () => {
    const markers = new Validator().validate(modelFor([
        'projection Orders',
        '  from OrderPlaced',
        '    bad = $eventContext.unknown',
        '    badPath = $eventContext.eventType.unknown',
        '    badCollection = $eventContext.tags.value',
        '    missing = $eventContext.',
        '    totals.$causedBy.subject = quantity',
        '    good = $eventContext.occurred.Week()',
    ].join('\n')));

    it('reports each catalog diagnostic with its source line and severity', () => {
        expect(markers.filter(marker => typeof marker.code === 'string').map(marker => [marker.code, marker.startLineNumber, marker.severity])).toEqual([
            ['PLAY0295', 3, 4],
            ['PLAY0296', 4, 4],
            ['PLAY0297', 5, 8],
            ['PLAY0298', 6, 8],
            ['PLAY0299', 7, 4],
        ]);
    });
});
