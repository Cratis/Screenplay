// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { defaultViewOptions, ViewOptionsStore } from '../ViewOptionsStore';
import { a_memento } from './given/a_memento';
import { a_configuration } from './given/a_configuration';

const key = 'screenplay.eventModelBoard.viewOptions';
const previousOptions = { detailLevel: 'overview', showProperties: true, visualizationMode: 'fillLines' } as const;

describe('when loading spacing preferences', () => {
    it('should start with comfortable spacing and medium text without changing the existing defaults', () => {
        new ViewOptionsStore(a_memento(), () => a_configuration()).options.should.deep.equal({
            detailLevel: 'full', showProperties: false, visualizationMode: 'simplified', density: 'comfortable', textSize: 'medium',
        });
    });

    it('should retain preferences saved before spacing settings existed', () => {
        new ViewOptionsStore(a_memento({ [key]: previousOptions }), () => a_configuration()).options.should.deep.equal({
            ...previousOptions, density: 'comfortable', textSize: 'medium',
        });
    });

    for (const density of ['compact', 'comfortable', 'spacious']) {
        for (const textSize of ['small', 'medium', 'large']) {
            it(`should read ${density} spacing with ${textSize} text from configuration`, () => {
                new ViewOptionsStore(a_memento(), () => a_configuration({ density, textSize })).options.should.deep.equal({
                    ...defaultViewOptions, density, textSize,
                });
            });
        }
    }

    for (const invalid of [undefined, null, false, 7, {}, 'unknown']) {
        it(`should independently fall back for invalid spacing values ${String(invalid)}`, () => {
            const storage = a_memento({ [key]: previousOptions });
            new ViewOptionsStore(storage, () => a_configuration({ density: invalid, textSize: 'large' })).options.should.deep.equal({
                ...previousOptions, density: 'comfortable', textSize: 'large',
            });
            new ViewOptionsStore(storage, () => a_configuration({ density: 'spacious', textSize: invalid })).options.should.deep.equal({
                ...previousOptions, density: 'spacious', textSize: 'medium',
            });
        });
    }

    it('should ignore legacy spacing fields rather than overwrite valid user settings', () => {
        const storage = a_memento({ [key]: { ...previousOptions, density: 'compact', textSize: 'nonsense' } });
        const configuration = a_configuration({ density: 'spacious', textSize: 'large' });
        new ViewOptionsStore(storage, () => configuration).options.should.deep.equal({ ...previousOptions, density: 'spacious', textSize: 'large' });
        configuration.updates.should.have.lengthOf(0);
    });

    for (const malformed of [null, false, 1, 'old', []]) {
        it(`should tolerate a malformed stored root ${String(malformed)}`, () => {
            new ViewOptionsStore(a_memento({ [key]: malformed }), () => a_configuration()).options.should.deep.equal(defaultViewOptions);
        });
    }

    it('should save changed settings at user scope and read them in a later session', async () => {
        const storage = a_memento();
        const configuration = a_configuration();
        const options = { ...defaultViewOptions, ...previousOptions, density: 'spacious', textSize: 'large' } as const;
        await new ViewOptionsStore(storage, () => configuration).save(options);
        configuration.updates.should.deep.equal([
            { section: 'density', value: 'spacious', target: true },
            { section: 'textSize', value: 'large', target: true },
        ]);
        storage.values.get(key)!.should.deep.equal(previousOptions);
        new ViewOptionsStore(storage, () => configuration).options.should.deep.equal(options);
    });

    it('should not rewrite spacing settings after a native View option changes', async () => {
        const configuration = a_configuration({ density: 'spacious', textSize: 'large' });
        const store = new ViewOptionsStore(a_memento(), () => configuration);
        await store.save({ ...store.options, showProperties: true });
        configuration.updates.should.have.lengthOf(0);
    });

    it('should write only the setting that changed', async () => {
        const configuration = a_configuration();
        const store = new ViewOptionsStore(a_memento(), () => configuration);
        await store.save({ ...store.options, textSize: 'small' });
        configuration.updates.should.deep.equal([{ section: 'textSize', value: 'small', target: true }]);
    });

    it('should surface rejected configuration writes to its caller', async () => {
        const configuration = a_configuration();
        const failure = new Error('Settings are read-only');
        configuration.update = async () => { throw failure; };
        const store = new ViewOptionsStore(a_memento(), () => configuration);
        const result = await store.save({ ...store.options, density: 'compact' }).then(() => undefined, error => error);
        (result === failure).should.be.true;
        store.options.density.should.equal('comfortable');
    });
});
