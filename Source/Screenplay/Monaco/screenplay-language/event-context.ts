// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Reads the generated event-context catalog. Every editor surface that describes $eventContext - projection
// completion and hover, and the host editor's context-variable docs and completions - goes through here, so
// none of them carries a list of its own.

import { eventContextPaths, eventContextRootType, eventContextTypes } from './event-context-catalog';
import type { EventContextMember, EventContextPath } from './event-context-catalog';

export { eventContextPaths };
export type { EventContextMember, EventContextPath };

/**
 * Whether a written path segment names a member. Mirrors EventContextMember.IsNamedBy in the compiler: a property
 * resolves camelCase or with only its first character uppercased; a function resolves by its exact name, with or
 * without parentheses.
 */
export function namesEventContextMember(member: EventContextMember, segment: string): boolean {
    if (member.kind === 'function') {
        return segment === member.name || segment === `${member.name}()`;
    }

    const declared = member.name[0].toUpperCase() + member.name.slice(1);
    return segment === declared || (segment.length > 0 && segment[0].toUpperCase() + segment.slice(1) === declared);
}

/** The members of the event context itself - the first segment of every path. */
export const eventContextMembers: readonly EventContextMember[] = eventContextTypes[eventContextRootType];

/**
 * The members that may follow the given segments after `$eventContext.`, or undefined when the segments do not
 * resolve or stop at a collection or function, below which nothing is addressable.
 */
export function eventContextMembersAfter(segments: readonly string[]): readonly EventContextMember[] | undefined {
    let members = eventContextMembers;
    for (const segment of segments) {
        const member = members.find((candidate) => namesEventContextMember(candidate, segment));
        if (!member || member.kind === 'collection' || member.kind === 'function') {
            return undefined;
        }

        members = eventContextTypes[member.type] ?? [];
    }

    return members;
}

/** The member the given segments name, or undefined when they do not resolve. */
export function eventContextMemberAt(segments: readonly string[]): EventContextMember | undefined {
    if (segments.length === 0) {
        return undefined;
    }

    const members = eventContextMembersAfter(segments.slice(0, -1));
    return members?.find((member) => namesEventContextMember(member, segments[segments.length - 1]));
}

/** Markdown describing $eventContext and its members, for hovers. */
export function describeEventContext(): string {
    return [
        '**$eventContext**',
        '',
        'Metadata about the event being projected. Paths are checked against the event-context catalog; `causation` and `tags` are collections and cannot be addressed below.',
        '',
        '**Members:**',
        ...eventContextMembers.map((member) => `- \`${member.name}\` (${member.type}) - ${member.description}`),
    ].join('\n');
}

/** Markdown describing one member of the event context, for hovers. */
export function describeEventContextMember(path: string, member: EventContextMember): string {
    const below = member.kind === 'collection' || member.kind === 'function' ? [] : eventContextTypes[member.type] ?? [];
    const lines = [`**$eventContext.${path}**: \`${member.type}\``, '', member.description];
    if (below.length > 0) {
        lines.push('', `**Members:** ${below.map((child) => `\`${child.name}\``).join(', ')}`);
    }

    return lines.join('\n');
}
