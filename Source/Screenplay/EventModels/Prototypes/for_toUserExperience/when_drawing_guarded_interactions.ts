// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { toUserExperience } from '../toUserExperience';
import { drawn, screens_of } from './given/screens_of';

describe('when drawing guarded interactions', () => {
    it('draws the table rather than inventing buttons for gesture action lists', () => {
        const screens = screens_of('screen V', '  table Item', '    column status', '    on double click',
            '      when item.status == "open"', '        execute Retry', '      otherwise', '        notify info "Closed"');
        toUserExperience(screens, 'M#S')[0].elements.map(drawn).should.deep.equal([
            ['data-table', 'Item', 24, 24, 852, 220],
        ]);
    });
});
