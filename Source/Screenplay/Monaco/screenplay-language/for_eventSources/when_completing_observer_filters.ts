// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { eventSourceCompletions, eventSourceHover } from '../event-source-authoring';
import { scanDocument } from '../symbols';

const declarations = 'eventsource Account\n  identifier String\n  stream Main\nmodule M\n  feature F\n';

describe('when completing observer filters', () => {
    it.each(['reaction R\n        from ', 'reducer R => Row\n        from '])('should complete sources and streams under %s', header => {
        const lines = (declarations + '    slice Automation S\n      ' + header).split('\n');
        expect(eventSourceCompletions(lines, lines.length - 1, lines.at(-1)!, scanDocument(lines))?.map(entry => entry.label)).toEqual(['Account', 'Account.Main']);
    });
    it('should explain routed-only observation on hover', () => {
        const lines = (declarations + '    slice Automation S\n      reaction R\n        from Account.Main\n        when Changed').split('\n');
        expect(eventSourceHover(lines, 7, 14, 21)).toContain('Stream ids are not filtered');
    });
});
