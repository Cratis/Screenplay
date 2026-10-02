// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { DiagnosticCodes } from '@cratis/screenplay-compiler';
import { WorkspaceApplication } from '../WorkspaceApplication';
import { an_application, placeOrder } from './given/an_application';

// A file placed in a feature that grows a module construct is reported in its placement, and setting what the
// application already holds changes nothing.
describe('when a file changes', () => {
    let application: WorkspaceApplication;
    let unchanged: boolean;
    let deleted: boolean[];
    const slice = 'Ordering/Orders/PlaceOrder.play';

    beforeEach(() => {
        application = an_application();
        application.diagnosticsFor(slice);
        unchanged = application.set('Ordering/./Orders/PlaceOrder.play', placeOrder);
        application.set(slice, 'screen template Misplaced\n  content');
        deleted = [application.delete('application.play'), application.delete('application.play')];
    });

    it('should say setting the same text changed nothing', () => {
        unchanged.should.be.false;
    });

    it('should say only the first removal removed anything', () => {
        deleted.should.deep.equal([true, false]);
    });

    it('should report what the placed file cannot hold, naming its placement', () => {
        application.diagnosticsFor(slice).map(diagnostic => [diagnostic.code, diagnostic.message.includes('feature \'Ordering.Orders\'')])
            .should.deep.equal([[DiagnosticCodes.UnexpectedInPlacedFile, true]]);
    });

    it('should no longer hold the removed file', () => {
        (application.placementOf('application.play') === undefined).should.be.true;
    });
});
