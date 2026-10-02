// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Puts the page in the dark theme. The board's own chrome - header labels, pills, row labels - is drawn in
// colors made for a dark surface and does not follow the light tokens, which is also why Studio's viewer is
// dark only. Following a light VS Code theme would leave those labels unreadable, so the board stays dark
// until @cratis/event-models draws them from its theme tokens.
export function applyDarkTheme(): void {
    const root = document.documentElement;
    root.classList.add('cratis-dark');
    root.style.colorScheme = 'dark';
}
