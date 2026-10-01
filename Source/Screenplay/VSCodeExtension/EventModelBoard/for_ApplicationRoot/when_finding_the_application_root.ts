// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { findApplicationRoot } from '../ApplicationRoot';

const holding = (...files: string[]) => (file: string) => Promise.resolve(files.includes(file));

describe('when finding the application root', () => {
    it('should find the nearest folder holding an application.play above the document', async () => {
        (await findApplicationRoot('/repo/model/Projects/Registration/Register.play', '/repo', holding('/repo/model/application.play')))!
            .should.equal('/repo/model');
    });

    it('should take the document\'s own folder when it is the root', async () => {
        (await findApplicationRoot('/repo/model/application.play', '/repo', holding('/repo/model/application.play')))!
            .should.equal('/repo/model');
    });

    it('should prefer the nearer of two roots', async () => {
        (await findApplicationRoot('/repo/a/b/c.play', '/repo', holding('/repo/application.play', '/repo/a/application.play')))!
            .should.equal('/repo/a');
    });

    it('should not look outside the workspace folder', async () => {
        (await findApplicationRoot('/repo/model/Register.play', '/repo/model', holding('/repo/application.play')) === undefined).should.be.true;
    });

    it('should leave a document without a root on its own', async () => {
        (await findApplicationRoot('/repo/model/Register.play', '/repo', holding()) === undefined).should.be.true;
    });
});
