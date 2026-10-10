// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { analyzeEventSources, eventSourceCompletions } from '../event-source-authoring';
import { scanDocument } from '../symbols';

const source = 'eventsource Account\n  identifier String\n  stream Main\n  stream Other\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        stream Account.Main\n        produces event Changed\n          stream Account.Other\n          value String = id\n';

describe('when completing production routes', () => {
    it('should retain the command and production routes', () => {
        expect(analyzeEventSources(source.split('\n')).routes.map(route => route.stream)).toEqual(['Main', 'Other']);
    });
    it('should complete streams in an inline production body', () => {
        const lines = source.replace('stream Account.Other', 'stream Account.').split('\n');
        const line = lines.findIndex(line => line.includes('stream Account.'));
        const productionLine = lines.findIndex(line => line.trim() === 'stream Account.');
        expect(line).toBeGreaterThan(0);
        expect(eventSourceCompletions(lines, productionLine, lines[productionLine], scanDocument(lines))?.map(entry => entry.insertText)).toEqual(['Main', 'Other']);
    });
});
