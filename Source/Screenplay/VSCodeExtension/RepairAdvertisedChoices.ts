// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RepairFailure, object, text, array } from './RepairClient';

const maximumRetiredAddresses = 64;
const individuallyReviewed = ['PLAY0478', 'PLAY0469'];
const titles: Record<string, string> = {
    PLAY0516: 'Reorder the authored timeline (review import changes)',
    PLAY0478: 'Change routing: explicitly route to the command identifier',
    PLAY0661: 'Supply every command-read key part by name',
    PLAY0469: 'Remove the inline identifier payload (changes the event contract)',
    PLAY0664: 'Remove the redundant production route'
};

/** The consent title of a repair choice; the pinned production repair is the default. */
export function repairTitle(code: string): string {
    return titles[code] ?? 'Declare the missing produced event';
}

/** Refuses repair metadata that does not match the contract the editor implements. */
export function requireApprovedMetadata(item: Record<string, unknown>, code: string): void {
    if (item.requiredFormatting !== 'CanonicalizeTouchedDocuments' || (individuallyReviewed.includes(code) && item.canFixAll !== false)) {
        throw new RepairFailure('MalformedContract', 'Repair metadata does not match the approved contract.');
    }
}

function describe(value: unknown): string {
    const address = object(value);
    const parts = array(address.parts).map(part => `${text(object(part).kind)} ${text(object(part).key)}`);
    return `${text(address.kind)}: ${parts.join(' / ')}`;
}

/** A contract-changing repair must name what it retires; the review is bound to that exact, bounded list. */
export function retiredAddressesOf(code: string, item: Record<string, unknown>): { retiredAddresses?: string[] } {
    if (code !== 'PLAY0469') return {};
    const addresses = array(item.retiredSemanticAddresses);
    if (!addresses.length || addresses.length > maximumRetiredAddresses) {
        throw new RepairFailure('MalformedContract', 'The repair must list between 1 and 64 retired semantic addresses.');
    }
    return { retiredAddresses: addresses.map(describe) };
}
