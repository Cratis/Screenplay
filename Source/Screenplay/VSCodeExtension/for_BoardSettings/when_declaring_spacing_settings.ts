// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { describe, it } from 'vitest';

const manifest = JSON.parse(readFileSync(new URL('../package.json', import.meta.url), 'utf8'));
const settings = manifest.contributes.configuration.properties;

describe('when declaring spacing settings', () => {
    for (const [name, choices, defaultValue] of [
        ['density', ['compact', 'comfortable', 'spacious'], 'comfortable'],
        ['textSize', ['small', 'medium', 'large'], 'medium'],
    ] as const) {
        it(`should offer ${name} as an application-scoped user preference with a readable default`, () => {
            const setting = settings[`screenplay.eventModelBoard.${name}`];
            (setting !== undefined).should.be.true;
            setting.type.should.equal('string');
            setting.enum.should.deep.equal(choices);
            setting.default.should.equal(defaultValue);
            setting.scope.should.equal('application');
        });
    }
});
