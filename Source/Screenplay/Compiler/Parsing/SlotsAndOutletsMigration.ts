// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Words the diagnostics for forms Screenplay 6 renamed when it split regions into slots and item lists into
// outlets. There is no deprecation period: a Screenplay 5 form is an error, and its message names the form that
// replaces it and the migration that rewrites it. Mirrors the C# SlotsAndOutletsMigration.

/** The command that rewrites a Screenplay 5 model for Screenplay 6. */
export const slotsAndOutletsMigrationCommand = 'screenplay migrate --from 5 --to 6';

/**
 * Words the message for a form Screenplay 6 replaced with an outlet.
 * @param old The old form, described where it was written - for example `'toolbar actions' in a feature`.
 * @param replacement The form that replaces it.
 * @returns The diagnostic message.
 */
export function replacedForSlotsAndOutlets(old: string, replacement: string): string {
    return `${old} is written '${replacement}' since Screenplay 6 - an item list a template offers is an outlet; run '${slotsAndOutletsMigrationCommand}' to rewrite the model (slots and outlets)`;
}

/**
 * Words the message for a renamed form.
 * @param old The old form, described where it was written - for example `'outlet detail' in a component`.
 * @param replacement The form that replaces it.
 * @returns The diagnostic message.
 */
export function renamedForSlotsAndOutlets(old: string, replacement: string): string {
    return `${old} is written '${replacement}' since Screenplay 6 - a region content is placed in is a slot; run '${slotsAndOutletsMigrationCommand}' to rewrite the model (slots and outlets)`;
}
