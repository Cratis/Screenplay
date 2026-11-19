// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { darkTheme, lightTheme } from '@cratis/scene.blueprint.default';
import { boardScreenAppearance } from '../Screens/boardScreenAppearance';

describe('when a board says how its screens look', () => {
    it('should draw them in the design system\'s dark theme on a dark board', () =>
        boardScreenAppearance(true).theme.should.equal(darkTheme));

    it('should draw them in the design system\'s light theme on a light board', () =>
        boardScreenAppearance(false).theme.should.equal(lightTheme));

    it('should name no colour of its own', () =>
        [boardScreenAppearance(true), boardScreenAppearance(false)]
            .every(appearance => appearance.theme === darkTheme || appearance.theme === lightTheme).should.be.true);
});
