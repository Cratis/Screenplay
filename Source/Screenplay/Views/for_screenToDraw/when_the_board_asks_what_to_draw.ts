// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { screenDocumentKey } from '@cratis/screenplay-event-models';
import { screenToDraw } from '../Screens/screenToDraw';

const read = (text: string | undefined) => text === undefined ? undefined : text === 'unreadable' ? undefined : { read: text };
const screens = { [screenDocumentKey('slice', 'actor')]: 'a document' };

describe('when the board asks what to draw', () => {
    it('should draw the screen of the cell it asks for', () =>
        screenToDraw(screens, 'slice', 'actor', read, true)!.should.deep.equal({ read: 'a document' }));

    it('should draw nothing for a cell with no screen', () =>
        (screenToDraw(screens, 'slice', 'another actor', read, true) === undefined).should.be.true);

    it('should draw nothing for a slice with no screen', () =>
        (screenToDraw(screens, 'another slice', 'actor', read, true) === undefined).should.be.true);

    it('should draw nothing when the document cannot be read', () =>
        (screenToDraw({ [screenDocumentKey('slice', 'actor')]: 'unreadable' }, 'slice', 'actor', read, true) === undefined).should.be.true);

    it('should draw nothing until there is a runtime to draw with', () =>
        (screenToDraw(screens, 'slice', 'actor', read, false) === undefined).should.be.true);

    it('should draw nothing for a cell outside a slice', () =>
        (screenToDraw(screens, '', 'actor', read, true) === undefined).should.be.true);

    it('should draw nothing for a cell with no role', () =>
        (screenToDraw(screens, 'slice', '', read, true) === undefined).should.be.true);
});
