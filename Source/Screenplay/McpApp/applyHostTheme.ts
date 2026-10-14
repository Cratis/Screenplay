// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Puts the page in the theme the host asked for, in the host context's `theme`. The board's own colors
// follow `color-scheme` (@cratis/event-models/theme), and the @cratis/components tokens follow the
// `cratis-dark` and `cratis-light` classes, which also win over the operating system's preference. Both
// are set, so the board and the page around it agree. A host that says nothing gets dark, as before.
export function applyHostTheme(theme: string | undefined): void {
    const light = theme === 'light';
    const root = document.documentElement;
    root.classList.toggle('cratis-light', light);
    root.classList.toggle('cratis-dark', !light);
    root.style.colorScheme = light ? 'light' : 'dark';
}
