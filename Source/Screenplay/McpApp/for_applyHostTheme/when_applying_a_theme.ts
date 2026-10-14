// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// @vitest-environment happy-dom

import { beforeEach, describe, it } from 'vitest';
import { applyHostTheme } from '../applyHostTheme';

const root = () => document.documentElement;

beforeEach(() => {
    root().className = '';
    root().style.colorScheme = '';
});

describe('when the host theme is light', () => {
    beforeEach(() => applyHostTheme('light'));

    it('should set the light color scheme', () => root().style.colorScheme.should.equal('light'));
    it('should mark the page light', () => root().classList.contains('cratis-light').should.be.true);
    it('should not mark the page dark', () => root().classList.contains('cratis-dark').should.be.false);
});

describe('when the host theme is dark', () => {
    beforeEach(() => applyHostTheme('dark'));

    it('should set the dark color scheme', () => root().style.colorScheme.should.equal('dark'));
    it('should mark the page dark', () => root().classList.contains('cratis-dark').should.be.true);
    it('should not mark the page light', () => root().classList.contains('cratis-light').should.be.false);
});

describe('when the host gives no theme', () => {
    beforeEach(() => applyHostTheme(undefined));

    it('should set the dark color scheme', () => root().style.colorScheme.should.equal('dark'));
    it('should mark the page dark', () => root().classList.contains('cratis-dark').should.be.true);
});

describe('when the host theme changes from dark to light', () => {
    beforeEach(() => {
        applyHostTheme('dark');
        applyHostTheme('light');
    });

    it('should set the light color scheme', () => root().style.colorScheme.should.equal('light'));
    it('should leave only the light mark', () => {
        root().classList.contains('cratis-light').should.be.true;
        root().classList.contains('cratis-dark').should.be.false;
    });
});

describe('when the host theme changes from light back to dark', () => {
    beforeEach(() => {
        applyHostTheme('light');
        applyHostTheme('dark');
    });

    it('should set the dark color scheme', () => root().style.colorScheme.should.equal('dark'));
    it('should leave only the dark mark', () => {
        root().classList.contains('cratis-dark').should.be.true;
        root().classList.contains('cratis-light').should.be.false;
    });
});

describe('when the host gives a theme the board does not know', () => {
    beforeEach(() => {
        applyHostTheme('light');
        applyHostTheme('high-contrast');
    });

    it('should set the dark color scheme', () => root().style.colorScheme.should.equal('dark'));
    it('should leave only the dark mark', () => {
        root().classList.contains('cratis-dark').should.be.true;
        root().classList.contains('cratis-light').should.be.false;
    });
});
