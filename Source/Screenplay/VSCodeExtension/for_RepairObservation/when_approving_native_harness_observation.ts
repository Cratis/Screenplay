// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as path from 'node:path';
import { it } from 'vitest';
import { nativeObservationRoot } from '../tests/nativeObservationRoot';

const model = path.resolve('synthetic-native-host');
it('observes command guards without a local-only environment opt-in, including CI', () => {
    nativeObservationRoot(model, 'dirty', false).should.equal(path.join(model, 'command-guards'));
});
it('keeps clean observation scoped to its separately approved synthetic root', () => {
    nativeObservationRoot(model, 'clean', false).should.equal(path.join(model, 'clean-unknown'));
});
it.each(['missed-source', 'missed-state', 'missed-attachment'])('preserves optional observation for %s', caseName => {
    nativeObservationRoot(model, caseName, false).should.equal('');
    nativeObservationRoot(model, caseName, true).should.equal(path.join(model, caseName));
});
