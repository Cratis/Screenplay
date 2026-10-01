// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, beforeEach, it } from 'vitest';
import { ActorType, PrototypeLayout, UserExperienceActorDocument } from '../../Document/EventModelDocument';
import { toUserExperience, userActor } from '../toUserExperience';
import { drawn, screens_of } from './given/screens_of';

describe('when drawing a screen with actions and a table', () => {
    let actors: UserExperienceActorDocument[];

    beforeEach(() => {
        actors = toUserExperience(screens_of(
            'screen Invoices',
            '  title "Invoices"',
            '  data Invoice[] via query AllInvoices',
            '  action Register',
            '    label "Register invoice"',
            '  action Cancel',
            '  table invoices',
            '    column number',
            '  on load',
            '    refresh AllInvoices',
        ), 'M#S');
    });

    it('should draw it for the user', () => [actors.length, actors[0].id, actors[0].type].should.deep.equal([1, userActor.id, ActorType.uiRole]));
    it('should place the elements absolutely', () => actors[0].layout.should.equal(PrototypeLayout.absolute));
    it('should lay the title, the actions side by side and the table out top to bottom, leaving the data to the table', () =>
        actors[0].elements.map(drawn).should.deep.equal([
            ['label', 'Invoices', 24, 24, 852, 32],
            ['button-hot', 'Register invoice', 24, 72, 160, 40],
            ['button', 'Cancel', 200, 72, 160, 40],
            ['data-table', 'invoices', 24, 128, 852, 220],
        ]));
    it('should size the window to the screen', () => [actors[0].windowWidth, actors[0].windowHeight].should.deep.equal([900, 480]));
    it('should give every element its own id', () => new Set(actors[0].elements.map(element => element.id)).size.should.equal(4));
});
