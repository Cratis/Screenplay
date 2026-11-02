// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { commentStart } from '../Parsing/SourceLineSplitter';

// Rewrites one inline JSON value (PLAY0833) as the indented structured block Screenplay writes instead. The value is
// read token by token so numbers keep their authored spelling; nothing is evaluated or normalized.

type Value =
    | { readonly kind: 'scalar'; readonly text: string }
    | { readonly kind: 'list'; readonly items: readonly Value[] }
    | { readonly kind: 'object'; readonly members: readonly { readonly name: string; readonly value: Value }[] };

export interface StructuredValueRewrite {
    /** Offset within the line where the replaced text starts. */
    readonly start: number;
    /** Replacement for the rest of the line from start; continuation lines carry their own indentation. */
    readonly text: string;
}

// The header shapes a JSON value can follow. Each returns the text that stays on the line before the block opens.
const headers: readonly ((before: string, after: string) => string | undefined)[] = [
    (before, after) => /\bthen\s+returns\s*$/.test(before) && after === '' ? `${before.trimEnd()} value` : undefined,
    (before, after) => /\bthen\s+no\s+readmodel\s+\S+\s+for\s*$/.test(before) && after === '' ? before.trimEnd() : undefined,
    (before, after) => /\bfrom\s+literal\s*$/.test(before) ? `${before.trimEnd()}${after === '' ? '' : ` ${after}`}` : undefined,
    (before, after) => /=\s*literal\s*$/.test(before) && after === '' ? before.replace(/\s*=\s*literal\s*$/, '') : undefined,
    (before, after) => /=\s*$/.test(before) && after === '' ? before.replace(/\s*=\s*$/, '') : undefined,
];

// A step header that carries one inline assignment keeps scalars only; its structured value moves under the header,
// which changes the parsed tree, so the editor leaves that rewrite to the author.
const inlineStepHeader = /^\s*(?:given|when|then|case)\s+\S+.*\s\S+\s*=\s*$/;

// The diagnostic may point at the line or at the value, so the value is found on the line: the first '{' or '[' outside a
// string that follows one of the header shapes and runs to the end of the code.
export function rewriteInlineJson(line: string, unit: string): StructuredValueRewrite | undefined {
    const comment = commentStart(line);
    const code = comment < 0 ? line : line.slice(0, comment);
    const trailing = comment < 0 ? '' : ` ${line.slice(comment).trimStart()}`;
    for (const start of openings(code)) {
        const reader = new Reader(code, start);
        const value = reader.value();
        if (value === undefined || value.kind === 'scalar') continue;
        const before = code.slice(0, start);
        const after = code.slice(reader.position).trim();
        if (inlineStepHeader.test(before)) return undefined;
        const header = headers.map(header => header(before, after)).find(text => text !== undefined);
        if (header === undefined) continue;
        const indent = /^\s*/.exec(line)![0];
        const lines: string[] = [];
        block(header.slice(indent.length), value, indent, unit, lines);
        lines[0] = lines[0] + trailing;
        return { start: indent.length, text: lines.join('\n').slice(indent.length) };
    }
    return undefined;
}

function* openings(code: string): Generator<number> {
    let inString = false;
    for (let index = 0; index < code.length; index++) {
        const current = code[index];
        if (inString && current === '\\') index++;
        else if (current === '"') inString = !inString;
        else if (!inString && (current === '{' || current === '[')) yield index;
    }
}

function block(header: string, value: Value, indent: string, unit: string, lines: string[]): void {
    if (value.kind === 'list' && value.items.length === 0) {
        lines.push(`${indent}${header} none`);
        return;
    }
    if (value.kind === 'object' && value.members.length === 0) {
        lines.push(`${indent}${header} empty`);
        return;
    }
    lines.push(`${indent}${header}`);
    const child = indent + unit;
    if (value.kind === 'list') {
        for (const item of value.items) {
            if (item.kind === 'scalar') lines.push(`${child}item ${item.text}`);
            else block('item', item, child, unit, lines);
        }
    } else if (value.kind === 'object') {
        for (const member of value.members) {
            const name = memberName(member.name);
            if (member.value.kind === 'scalar') lines.push(`${child}${name} = ${member.value.text}`);
            else block(name, member.value, child, unit, lines);
        }
    }
}

const memberName = (name: string): string => name === 'item' ? '@item' : /^[\p{L}_][\p{L}\p{N}_]*$/u.test(name) ? name : quote(name);

const quote = (text: string): string => '"' + text.replace(/["\\\n\r\t]/g, character => JSON.stringify(character).slice(1, -1)) + '"';

class Reader {
    constructor(private readonly text: string, public position: number) { }

    value(): Value | undefined {
        this.space();
        const current = this.text[this.position];
        if (current === '{') return this.object();
        if (current === '[') return this.list();
        if (current === '"') {
            const text = this.string();
            return text === undefined ? undefined : { kind: 'scalar', text: quote(text) };
        }
        const token = /^(?:-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?|true|false|null)/.exec(this.text.slice(this.position));
        if (token === null) return undefined;
        this.position += token[0].length;
        return { kind: 'scalar', text: token[0] };
    }

    private object(): Value | undefined {
        this.position++;
        const members: { name: string; value: Value }[] = [];
        this.space();
        if (this.text[this.position] === '}') {
            this.position++;
            return { kind: 'object', members };
        }
        for (;;) {
            this.space();
            const name = this.string();
            this.space();
            if (name === undefined || this.text[this.position] !== ':') return undefined;
            this.position++;
            const value = this.value();
            if (value === undefined) return undefined;
            members.push({ name, value });
            this.space();
            const next = this.text[this.position++];
            if (next === '}') return { kind: 'object', members };
            if (next !== ',') return undefined;
        }
    }

    private list(): Value | undefined {
        this.position++;
        const items: Value[] = [];
        this.space();
        if (this.text[this.position] === ']') {
            this.position++;
            return { kind: 'list', items };
        }
        for (;;) {
            const value = this.value();
            if (value === undefined) return undefined;
            items.push(value);
            this.space();
            const next = this.text[this.position++];
            if (next === ']') return { kind: 'list', items };
            if (next !== ',') return undefined;
        }
    }

    private string(): string | undefined {
        if (this.text[this.position] !== '"') return undefined;
        let end = this.position + 1;
        while (end < this.text.length && this.text[end] !== '"') end += this.text[end] === '\\' ? 2 : 1;
        if (end >= this.text.length) return undefined;
        try {
            const value = JSON.parse(this.text.slice(this.position, end + 1)) as string;
            this.position = end + 1;
            return value;
        } catch {
            return undefined;
        }
    }

    private space(): void {
        while (this.position < this.text.length && /\s/.test(this.text[this.position])) this.position++;
    }
}
