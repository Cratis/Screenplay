// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { WorkspaceApplication } from '../WorkspaceApplication';

describe('when a consumer is drawn before its producer', () => {
    it('should surface PLAY0516 as information', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n    slice StateChange Write\n      event E');
        const found = application.diagnosticsFor('application.play').filter(diagnostic => diagnostic.code === 'PLAY0516');
        expect(found).toHaveLength(1);
        expect(found[0].severity).toBe('information');
    });

    it('should surface PLAY0517 as information when features use each others events', () => {
        const application = new WorkspaceApplication();
        application.set('application.play', 'module M\n  feature A\n    slice StateView ViewA\n      event EA\n      projection PA\n        from EB\n  feature B\n    slice StateView ViewB\n      event EB\n      projection PB\n        from EA');
        const found = application.diagnosticsFor('application.play').filter(diagnostic => diagnostic.code === 'PLAY0517');
        expect(found).toHaveLength(1);
        expect(found[0].severity).toBe('information');
    });
});
