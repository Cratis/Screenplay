// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { beforeEach, describe, it } from 'vitest';
import { parse } from '../../ScreenplayCompiler';
import { CommandCalendarSyntax } from '../../Syntax/Calendar';

describe('when parsing the clock operands fixture', () => {
    let calendar: CommandCalendarSyntax;

    beforeEach(() => {
        const result = parse(readFileSync(join(__dirname, '..', 'clock-operands.play'), 'utf8'));
        calendar = result.value.modules[0].features[0].slices[0].commands[0].calendar!;
    });

    it('should use the trusted context time zone member', () => {
        calendar.contextMember!.should.equal('timeZone');
    });
});
