// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { VisualizedModel } from '../../VisualizedModel';
import { activateWithKeyboard, toolbarButton } from '../../../Views/for_DependencyMapView/given/real_board_dom';

const play = (name: string) => `module Work
  feature Record
    slice StateChange ${name}
      event ${name}Recorded`;

export const readabilityModel: VisualizedModel = {
    application: 'Work',
    documents: [{ path: 'work.play', source: play('Work') }],
    changes: [{ path: 'work.play', source: play('ChangedWork') }],
};

export function renderedBoard(host: Element) {
    const board = host.querySelector<HTMLElement>('[data-board-density]');
    (board !== null).should.be.true;
    return board!;
}

export function menuChoice(label: string) {
    const choice = [...document.body.querySelectorAll<HTMLButtonElement>('.event-modeling-menu-dropdown-menu button')]
        .find(button => button.textContent?.trim() === label);
    if (!choice) throw new Error(`Missing menu choice: ${label}`);
    return choice;
}

export async function choose(host: Element, label: string, menu = 'Spacing and text size') {
    await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, menu));
    await activateWithKeyboard(menuChoice(label));
}

export async function checkedChoices(host: Element, menu = 'Spacing and text size') {
    await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, menu));
    const checked = [...document.body.querySelectorAll<HTMLButtonElement>('.event-modeling-menu-dropdown-menu button')]
        .filter(button => button.querySelector('.pi-check'))
        .map(button => button.textContent?.trim());
    await activateWithKeyboard(toolbarButton(host.querySelector('.screenplay-board-view')!, menu));
    return checked;
}

export function assertReadability(host: Element, density: string, scale: string) {
    renderedBoard(host).getAttribute('data-board-density')!.should.equal(density);
    renderedBoard(host).style.getPropertyValue('--board-text-scale').should.equal(scale);
}
