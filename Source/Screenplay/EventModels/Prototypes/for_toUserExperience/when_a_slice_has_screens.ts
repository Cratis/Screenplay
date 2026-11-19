// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { parse, ScreenSyntax } from '@cratis/screenplay-compiler';
import { ActorType, PrototypeLayout } from '../../Document/EventModelDocument';
import { toUserExperience, userActor } from '../toUserExperience';

// The screens of a one-slice document whose slice body is the given lines, indented under the slice.
function screens_of(...lines: string[]): readonly ScreenSyntax[] {
    const source = ['module M', '  feature F', '    slice StateView S', ...lines.map(line => `      ${line}`)].join('\n');
    return parse(source).value.modules[0].features[0].slices[0].screens;
}

describe('when a slice has screens', () => {
    const screens = screens_of('screen Orders', '  title "Orders"');
    const actors = toUserExperience(screens);

    it('should give the role a row', () => actors.length.should.equal(1));
    it('should say the role has a screen', () => actors[0].hasScreen.should.be.true);
    it('should give the row to whoever uses the slice', () => actors[0].id.should.equal(userActor.id));
    it('should say the row is a UI role', () => actors[0].type.should.equal(ActorType.uiRole));
    it('should say nothing about the window the screen was designed in', () => {
        actors[0].windowWidth.should.equal(0);
        actors[0].windowHeight.should.equal(0);
    });
    it('should leave the layout as the one the board defaults to', () => actors[0].layout.should.equal(PrototypeLayout.absolute));
});

describe('when a slice has several screens', () => {
    const screens = screens_of('screen Orders', '  title "Orders"', 'screen OrderDetail', '  title "Order"');
    const actors = toUserExperience(screens);

    it('should still give the role one row', () => actors.length.should.equal(1));
    it('should say it has a screen', () => actors[0].hasScreen.should.be.true);
});

describe('when several roles use the slice', () => {
    const screens = screens_of('screen Orders', '  title "Orders"');
    const actors = toUserExperience(screens, ['buyer', 'seller']);

    it('should give each of them a row', () => actors.map(actor => actor.id).should.deep.equal(['buyer', 'seller']));
    it('should say each has a screen', () => actors.map(actor => actor.hasScreen).should.deep.equal([true, true]));
});

describe('when a slice has no screen', () => {
    it('should give it no row at all', () => toUserExperience([]).length.should.equal(0));
});
