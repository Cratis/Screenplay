// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { UserExperienceActorDocument } from '../../Document/EventModelDocument';
import { toUserExperience } from '../toUserExperience';
import { drawn, screens_of } from './given/screens_of';

describe('when drawing guarded actions', () => {
    let actors: UserExperienceActorDocument[];
    beforeEach(() => {
        actors = toUserExperience(screens_of('screen V', '  template Detail', '    actions', '      section choices',
            '        action "Close invoice"', '          when item.status == "draft" execute Cancel',
            '          when item.status == "sent" execute Archive', '          otherwise execute Review',
            '          navigate to List', '        action $strings.actions.retry',
            '          when item.failed == true execute Retry', '          otherwise hidden',
            '        action Register', '          label "Register invoice"'), 'M#S', ['staff', 'accountant']);
    });
    it('should draw one labeled button per guarded action alongside plain actions', () => actors[0].elements.map(drawn).should.deep.equal([
        ['button-hot', 'Close invoice', 24, 24, 160, 40],
        ['button', '$strings.actions.retry', 200, 24, 160, 40],
        ['button', 'Register invoice', 376, 24, 160, 40],
    ]));
    it('should preserve the supplied audience', () => actors.map(actor => actor.id).should.deep.equal(['staff', 'accountant']));
    it('should share the same prototype across the audience', () => actors[1].elements.should.deep.equal(actors[0].elements));
});
