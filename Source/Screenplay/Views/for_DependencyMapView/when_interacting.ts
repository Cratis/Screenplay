// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { parse } from '@cratis/screenplay-compiler';
import { dependencyMapFor } from '@cratis/screenplay-event-models';
import { DependencyMapView } from '../DependencyMapView';
import { DependencyMapLevel } from '../DependencyMapLevel';
import { hooks, invoke } from './given/component_hooks';

vi.mock('react', async importOriginal => ({ ...await importOriginal<typeof import('react')>(), ...(await import('./given/component_hooks')).componentHooks }));

const map = dependencyMapFor(parse(`module Work
  feature First
    slice StateView FirstView
      event FirstRecorded
      readmodel First
      projection First
        from SecondRecorded
  feature Second
    slice StateView SecondView
      event SecondRecorded
      readmodel Second
      projection Second
        from FirstRecorded
`).value);

const render = (model = map) => hooks.render(() => DependencyMapView({ map: model }));
const buttons = () => render().filter(element => element.props.role === 'button');
const edges = () => buttons().filter(element => String(element.props.className).includes('__edge'));

beforeEach(() => {
    hooks.reset();
    invoke(render().find(element => element.type === 'select')!, 'onChange', { target: { value: DependencyMapLevel.Feature } });
});

describe('when selecting reciprocal edges', () => {
    it('should make both directed edges selectable', () => {
        edges().length.should.equal(2);
        for (let index = 0; index < 2; index++) {
            invoke(edges()[index], 'onClick', { currentTarget: { focus: () => invoke(edges()[index], 'onFocus') } });
            (edges()[index].props['aria-pressed'] === true).should.be.true;
            (edges()[1 - index].props['aria-pressed'] === false).should.be.true;
        }
    });
    it('should move the roving Tab stop to the focused item', () => {
        const node = buttons().find(element => String(element.props.className).includes('__node'))!;
        invoke(node, 'onFocus');
        buttons().filter(element => element.props.tabIndex === 0).map(element => element.key).should.deep.equal([node.key]);
    });
    it('should move arrow-key focus from an edge to a node and wrap backward', () => {
        const items = buttons();
        const targets = items.map(item => ({ focus: () => invoke(item, 'onFocus') }));
        const currentTarget = Object.assign(targets[1], { ownerSVGElement: { querySelectorAll: () => targets } });
        invoke(items[1], 'onKeyDown', { key: 'ArrowRight', preventDefault: vi.fn(), currentTarget });
        (buttons().find(item => item.props.tabIndex === 0)!.key === items[2].key).should.be.true;
        invoke(items[0], 'onKeyDown', { key: 'ArrowLeft', preventDefault: vi.fn(), currentTarget: Object.assign(targets[0], { ownerSVGElement: { querySelectorAll: () => targets } }) });
        (buttons().find(item => item.props.tabIndex === 0)!.key === items[items.length - 1].key).should.be.true;
    });
    it('should select an edge with Enter and clear it with Escape', () => {
        invoke(edges()[0], 'onKeyDown', { key: 'Enter', preventDefault: vi.fn() });
        (edges()[0].props['aria-pressed'] === true).should.be.true;
        invoke(render()[0], 'onKeyDown', { key: 'Escape' });
        edges().every(edge => !edge.props['aria-pressed']).should.be.true;
    });
});

describe('when switching the shown model', () => {
    it('should retain the edge level and kind filters but clear the selection', () => {
        invoke(render().find(element => element.type === 'input' && element.props.value === 'asks')!, 'onChange', { target: { checked: true } });
        invoke(edges()[0], 'onKeyDown', { key: 'Enter', preventDefault: vi.fn() });
        const changed = render(dependencyMapFor(parse('module Other').value));
        (changed.find(element => element.type === 'select')!.props.value === DependencyMapLevel.Feature).should.be.true;
        (changed.find(element => element.type === 'input' && element.props.value === 'asks')!.props.checked === true).should.be.true;
        changed.filter(element => element.props.role === 'button').every(element => !element.props['aria-pressed']).should.be.true;
        render().filter(element => element.props.role === 'button').every(element => !element.props['aria-pressed']).should.be.true;
    });
});
