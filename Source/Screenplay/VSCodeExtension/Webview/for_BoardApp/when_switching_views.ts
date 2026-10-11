// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'chai/register-should';
import { beforeEach, describe, it, vi } from 'vitest';
import { BoardViewMenu, EventModelBoard } from '@cratis/event-models';
import { ToolbarButton } from '@cratis/components/Toolbar';
import { DependencyMapView } from '@cratis/screenplay-views';
import { dependencyMapFor } from '@cratis/screenplay-event-models';
import { parse } from '@cratis/screenplay-compiler';
import { BoardApp } from '../BoardApp';
import { hooks, invoke } from '../../../Views/for_DependencyMapView/given/component_hooks';

vi.mock('react', async importOriginal => ({ ...await importOriginal<typeof import('react')>(), ...(await import('../../../Views/for_DependencyMapView/given/component_hooks')).componentHooks }));
vi.mock('@cratis/event-models', () => ({ BoardToolbarFrame: () => null, BoardViewMenu: () => null, EventModelBoard: () => null, EventModelPresentationProvider: () => null, MenuDropdownOpenProvider: () => null, readEventModelDocument: (document: unknown) => { if (document === 'unreadable') throw new Error('Invalid document'); return document; } }));
vi.mock('@cratis/components/Toolbar', () => ({ ToolbarButton: () => null }));
vi.mock('../boardChrome', () => ({ boardChrome: {} }));
vi.mock('../usePresentation', () => ({ usePresentation: () => [{}, vi.fn()], defaultPresentation: {} }));
vi.mock('../vscodeApi', () => ({ vscode: { postMessage: vi.fn() } }));
const map = dependencyMapFor(parse('module Work').value);
const render = () => hooks.render(BoardApp);
const mapButton = () => render().find(element => element.type === ToolbarButton && element.props.text === 'Map')!;

beforeEach(() => { hooks.reset(); hooks.values[0] = { document: {}, dependencies: map, problems: [] }; });

describe('when switching between the board and dependency map', () => {
    it('should keep the board mounted while the map is visible', () => {
        invoke(mapButton(), 'onClick');
        render().filter(element => element.type === EventModelBoard).length.should.equal(1);
        render().some(element => element.props['aria-hidden'] === true && element.props.inert === true && String(element.props.className).includes('screenplay-board-view is-hidden') && element.props.hidden !== true).should.be.true;
    });
});

describe('when the board document cannot be read', () => {
    beforeEach(() => { hooks.values[0] = { document: 'unreadable', dependencies: map, problems: [] }; });
    it('should leave the map reachable from the toolbar', () => {
        render().some(element => element.type === ToolbarButton && element.props.text === 'Map').should.be.true;
        invoke(mapButton(), 'onClick');
        render().filter(element => element.type === DependencyMapView).length.should.equal(1);
    });
});

describe('when the dependency map is visible', () => {
    beforeEach(() => invoke(mapButton(), 'onClick'));

    it('should offer the map\'s own settings under the view menu', () => {
        const items = (render().find(element => element.type === BoardViewMenu)!.props.items as { id: string }[]).map(item => item.id);
        items.should.include('level-module').and.include('kind-reactsTo');
    });
    it('should hand the chosen settings to the map instead of letting it draw its own controls', () => {
        (render().find(element => element.type === DependencyMapView)!.props.options !== undefined).should.be.true;
    });
});
