// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

describe('when declaring dependencies in a workspace application', () => {
    let application: WorkspaceApplication;
    beforeEach(() => {
        application = new WorkspaceApplication();
        application.set('application.play', 'module A\n  depends on C\n  feature F\n    slice StateView V\n      projection P\n        from E\nmodule B\n  feature G\n    slice StateChange W\n      event E\nmodule C\n  depends on A');
    });
    it('should surface undeclared coupling as a warning', () => {
        application.diagnosticsFor('application.play').filter(item => item.code === 'PLAY0552').map(item => item.severity).should.deep.equal(['warning']);
    });
    it('should surface unused and mutual declarations as information', () => {
        application.diagnosticsFor('application.play').filter(item => item.code === 'PLAY0553' || item.code === 'PLAY0556').map(item => item.severity).should.deep.equal(['information', 'information', 'information', 'information']);
    });
});
