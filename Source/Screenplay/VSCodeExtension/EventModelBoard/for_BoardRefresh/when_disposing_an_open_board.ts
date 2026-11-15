// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { setImmediate as settled } from 'node:timers/promises';
import { afterEach, beforeEach, describe, it, vi } from 'vitest';
import { BoardRefresh } from '../BoardRefresh';
import { a_compilation_rooted_at, document, host, reset, webview, type WatcherDouble } from './given/a_board_host';

vi.mock('vscode', async importOriginal => ({ ...await importOriginal<object>(), ...(await import('./given/a_board_host')).vscode }));
vi.mock('../boardSources', async () => ({ compileForBoard: (await import('./given/a_board_host')).compileForBoard }));
vi.mock('../compileBoard', async () => ({ boardFor: (await import('./given/a_board_host')).boardFor }));

describe('when disposing an open board', () => {
    let watcher: WatcherDouble;
    let shownWhileOpen: number;
    let compiledWhileOpen: number;

    beforeEach(async () => {
        reset();
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
        const refresh = new BoardRefresh(document, webview);
        refresh.now();
        host.compilations[0](a_compilation_rooted_at('/work/app'));
        await settled();
        watcher = host.watchers[0];
        shownWhileOpen = host.messages.length;
        watcher.fire();
        vi.advanceTimersByTime(1000);
        compiledWhileOpen = host.compilations.length;

        refresh.dispose();
        refresh.dispose();
        host.compilations[1](a_compilation_rooted_at('/work/app'));
        await settled();
        watcher.fire();
        vi.advanceTimersByTime(1000);
    });

    afterEach(() => vi.useRealTimers());

    it('should have watched the application root', () => watcher.base.should.equal('/work/app'));
    it('should have shown the board while open', () => shownWhileOpen.should.equal(1));
    it('should have compiled again when an application file changed while open', () => compiledWhileOpen.should.equal(2));
    it('should dispose the watcher once', () => watcher.disposals.should.equal(1));
    it('should not watch again', () => host.watchers.length.should.equal(1));
    it('should not map a compilation that finished after disposal', () => host.mappings.should.equal(1));
    it('should not show a compilation that finished after disposal', () => host.messages.length.should.equal(1));
    it('should not compile after disposal', () => host.compilations.length.should.equal(2));
});
