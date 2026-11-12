// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it } from 'vitest';
import { UserExperienceActorDocument } from '../../Document/EventModelDocument';
import { toUserExperience } from '../toUserExperience';
import { drawn, screens_of } from './given/screens_of';

describe('when drawing a screen with an extension component', () => {
    let actors: UserExperienceActorDocument[];
    beforeEach(() => {
        actors = toUserExperience(screens_of('screen Checkout', '  title "Pay"', '  component Payments.PaymentForm payment',
            '    property amount = "10"', '  component scene.web.DataGrid invoices'), 'M#S', ['staff']);
    });
    it('should draw the extension component under the title', () => actors[0].elements.map(drawn).should.deep.equal([
        ['label', 'Pay', 24, 24, 852, 32],
        ['extension-component', 'Payments.PaymentForm', 24, 72, 852, 120],
    ]));
});
