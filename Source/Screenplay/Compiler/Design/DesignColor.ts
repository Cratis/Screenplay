// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Reads the two color notations a theme value may use - #rgb, #rgba, #rrggbb, #rrggbbaa and rgb()/rgba() - and measures the
// WCAG contrast between two of them. The port of the C# DesignColor; any other value is not a color it can judge.
const hexPattern = /^#([0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{4}|[0-9a-fA-F]{3})$/;
const rgbPattern = /^rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*[\d.]+\s*)?\)$/;

export interface DesignRgb {
    readonly red: number;
    readonly green: number;
    readonly blue: number;
}

export function parseDesignColor(value: string): DesignRgb | undefined {
    const text = value.trim();
    const hex = hexPattern.exec(text);
    if (hex !== null) {
        let digits = hex[1];
        if (digits.length === 3 || digits.length === 4) {
            digits = [...digits.slice(0, 3)].map(digit => digit + digit).join('');
        }
        return { red: Number.parseInt(digits.slice(0, 2), 16), green: Number.parseInt(digits.slice(2, 4), 16), blue: Number.parseInt(digits.slice(4, 6), 16) };
    }
    const rgb = rgbPattern.exec(text);
    if (rgb === null) return undefined;
    const color = { red: Number.parseInt(rgb[1], 10), green: Number.parseInt(rgb[2], 10), blue: Number.parseInt(rgb[3], 10) };
    return color.red <= 255 && color.green <= 255 && color.blue <= 255 ? color : undefined;
}

// The contrast ratio between two colors, or undefined when either is not a color this can read.
export function designContrast(foreground: string, background: string): number | undefined {
    const first = parseDesignColor(foreground);
    const second = parseDesignColor(background);
    if (first === undefined || second === undefined) return undefined;
    const firstLuminance = luminance(first);
    const secondLuminance = luminance(second);
    const [lighter, darker] = firstLuminance >= secondLuminance ? [firstLuminance, secondLuminance] : [secondLuminance, firstLuminance];
    return (lighter + 0.05) / (darker + 0.05);
}

const luminance = (color: DesignRgb): number => (0.2126 * channel(color.red)) + (0.7152 * channel(color.green)) + (0.0722 * channel(color.blue));

function channel(value: number): number {
    const scaled = value / 255;
    return scaled <= 0.03928 ? scaled / 12.92 : Math.pow((scaled + 0.055) / 1.055, 2.4);
}
