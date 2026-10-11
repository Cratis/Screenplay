// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it, vi } from 'vitest';
import { DependencyMapLevel } from '../DependencyMapLevel';
import { defaultDependencyMapOptions, type DependencyMapOptions } from '../DependencyMapOptions';
import { dependencyMapMenuItems } from '../dependencyMapMenuItems';

const choose = (id: string, options: DependencyMapOptions = defaultDependencyMapOptions) => {
    const onChange = vi.fn();
    dependencyMapMenuItems(options, onChange).find(item => item.id === id)!.onClick!();
    return onChange.mock.calls[0][0] as DependencyMapOptions;
};

describe('when choosing the dependency map options from the view menu', () => {
    it('should mark the level and the kinds that are drawn', () => {
        dependencyMapMenuItems(defaultDependencyMapOptions, vi.fn()).filter(item => item.checked).map(item => item.id)
            .should.deep.equal(['level-module', 'kind-usesFactsFrom', 'kind-reactsTo', 'kind-decidesFrom']);
    });
    it('should ask for edges between features', () => choose('level-feature').level.should.equal(DependencyMapLevel.Feature));
    it('should keep the kinds when the level changes', () => choose('level-feature').kinds.should.deep.equal(defaultDependencyMapOptions.kinds));
    it('should add a kind that is not drawn', () => choose('kind-asks').kinds.should.include('asks'));
    it('should remove a kind that is drawn', () => choose('kind-reactsTo').kinds.should.not.include('reactsTo'));
    it('should keep the menu open while kinds are chosen', () => {
        dependencyMapMenuItems(defaultDependencyMapOptions, vi.fn()).filter(item => item.id.startsWith('kind-')).every(item => item.keepOpen).should.be.true;
    });
});
