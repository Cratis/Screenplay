// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SyntaxNode } from './SyntaxNode';

// 'time zone "<id>"' or 'time zone configurable default "<id>"' at the application root (decisions 0106/0107).
export interface TimeZoneSyntax extends SyntaxNode {
    readonly kind: 'TimeZoneSyntax';
    readonly id: string;
    readonly configurable: boolean;
}

// 'calendar "<id>"' or 'calendar from $context.<member>' in a command.
export interface CommandCalendarSyntax extends SyntaxNode {
    readonly kind: 'CommandCalendarSyntax';
    readonly id: string | null;
    readonly contextMember: string | null;
}
