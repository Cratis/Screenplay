// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, it, vi } from 'vitest';
import { EventModelBoard } from '@cratis/event-models';
import { DependencyMapView, DependencyView } from '@cratis/screenplay-views';
import { dependencyMapFor } from '@cratis/screenplay-event-models';
import { parse } from '@cratis/screenplay-compiler';
import { BoardApp } from '../BoardApp';
import { BoardTools } from '../BoardTools';
import { ShownModel } from '../ShownModel';
import { hooks, invoke } from '../../Views/for_DependencyMapView/given/component_hooks';

vi.mock('react', async importOriginal => ({ ...await importOriginal<typeof import('react')>(), ...(await import('../../Views/for_DependencyMapView/given/component_hooks')).componentHooks }));
vi.mock('@cratis/event-models', () => ({ EventModelBoard: () => null, MenuDropdownOpenProvider: () => null, readEventModelDocument: (document: unknown) => document }));
vi.mock('../BoardTools', () => ({ BoardTools: () => null }));
vi.mock('../boardChrome', () => ({ boardChrome: {} }));
vi.mock('../useHost', () => ({ useHost: () => ({ model: {}, refresh: vi.fn(), toggleFullscreen: vi.fn() }) }));
vi.mock('../compileBoards', () => ({ compileBoards: () => compiled }));
const compiled = {
    current: { document: {}, dependencies: dependencyMapFor(parse('module Current').value), errors: 0 },
    proposed: { document: {}, dependencies: dependencyMapFor(parse('module Proposed').value), errors: 0 },
};
const render = () => hooks.render(BoardApp);
const tools = () => render().find(element => element.type === BoardTools)!;

beforeEach(() => hooks.reset());

describe('when switching between the board and dependency map', () => {
    it('should keep the board mounted while the map is visible', () => {
        const originalKey = render().find(element => element.type === EventModelBoard)!.key;
        invoke(tools(), 'onView', DependencyView.Map);
        render().filter(element => element.type === EventModelBoard).length.should.equal(1);
        (render().find(element => element.type === EventModelBoard)!.key === originalKey).should.be.true;
        render().some(element => element.props['aria-hidden'] === true && element.props.inert === true && String(element.props.className).includes('screenplay-board-view is-hidden') && element.props.hidden !== true).should.be.true;
        invoke(tools(), 'onView', DependencyView.Board);
        render().filter(element => element.type === EventModelBoard).length.should.equal(1);
    });
    it('should keep the map mounted across Current and Proposed switches', () => {
        invoke(tools(), 'onView', DependencyView.Map);
        const originalKey = render().find(element => element.type === DependencyMapView)!.key;
        invoke(tools(), 'onShow', ShownModel.Current);
        const current = render().find(element => element.type === DependencyMapView)!;
        (current.key === originalKey).should.be.true;
        (current.props.map === compiled.current.dependencies).should.be.true;
    });
});
