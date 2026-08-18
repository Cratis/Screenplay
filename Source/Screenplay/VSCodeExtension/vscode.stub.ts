// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// `vscode` is a types package here — there is no runtime module to import, which is why the two files
// closest to the editor went untested. This stands one up: enough of the API surface for the extension's
// own modules to run under the spec runner, aliased in over the module name by `vitest.config.mts`.
//
// It is a real implementation rather than a set of return values. The glob matching in particular is
// genuine, because a stub that answered a glob by comparing base names could not tell a pattern from a
// literal — and telling those apart is exactly what one of the specs is for.

export enum FileType {
    Unknown = 0,
    File = 1,
    Directory = 2,
    SymbolicLink = 64,
}

export class Position {
    constructor(
        readonly line: number,
        readonly character: number,
    ) {}
}

export class Range {
    readonly start: Position;
    readonly end: Position;

    constructor(startLine: number, startCharacter: number, endLine: number, endCharacter: number) {
        this.start = new Position(startLine, startCharacter);
        this.end = new Position(endLine, endCharacter);
    }
}

export class Uri {
    private constructor(readonly fsPath: string) {}

    static file(path: string): Uri {
        return new Uri(path);
    }

    toString(): string {
        return `file://${this.fsPath}`;
    }
}

export class DocumentLink {
    tooltip?: string;

    constructor(
        readonly range: Range,
        readonly target?: Uri,
    ) {}
}

export interface Disposable {
    dispose(): void;
}

type Listener = (...args: never[]) => unknown;

// The editor's event shape: subscribing returns something disposable, and the source fires it.
export class Emitter {
    private readonly listeners: Listener[] = [];

    readonly event = (listener: Listener): Disposable => {
        this.listeners.push(listener);
        return { dispose: () => {} };
    };

    fire(...args: never[]): void {
        for (const listener of [...this.listeners]) listener(...args);
    }
}

function escapeForRegularExpression(text: string): string {
    return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// The subset of glob syntax the editor documents: `**` spans segments, `*` and `?` stay within one,
// `[...]` is a character class and `{a,b}` an alternation. There is no escape character, which is the
// property the extension has to work around and therefore the property this has to reproduce.
function globToRegularExpression(pattern: string): RegExp {
    let expression = '';
    let index = 0;

    while (index < pattern.length) {
        const character = pattern[index];

        if (character === '*' && pattern[index + 1] === '*') {
            if (pattern[index + 2] === '/') {
                expression += '(?:.*/)?';
                index += 3;
            } else {
                expression += '.*';
                index += 2;
            }
            continue;
        }

        if (character === '*') {
            expression += '[^/]*';
            index += 1;
            continue;
        }

        if (character === '?') {
            expression += '[^/]';
            index += 1;
            continue;
        }

        if (character === '[' && pattern.indexOf(']', index + 1) !== -1) {
            const close = pattern.indexOf(']', index + 1);
            const body = pattern.slice(index + 1, close);
            expression += `[${body.startsWith('!') ? `^${body.slice(1)}` : body}]`;
            index = close + 1;
            continue;
        }

        if (character === '{' && pattern.indexOf('}', index + 1) !== -1) {
            const close = pattern.indexOf('}', index + 1);
            const alternatives = pattern.slice(index + 1, close).split(',');
            expression += `(?:${alternatives.map(escapeForRegularExpression).join('|')})`;
            index = close + 1;
            continue;
        }

        expression += escapeForRegularExpression(character);
        index += 1;
    }

    return new RegExp(`^${expression}$`);
}

export interface FileSystemWatcherStub extends Disposable {
    readonly pattern: string;
    readonly ignoresChanges: boolean;
    readonly created: Emitter;
    readonly changed: Emitter;
    readonly deleted: Emitter;
    onDidCreate: Emitter['event'];
    onDidChange: Emitter['event'];
    onDidDelete: Emitter['event'];
}

// Everything a spec sets up or reads back. `reset()` between specs, because the extension's own cache is
// module-global too and a spec that inherited either would be testing the previous spec.
export const state = {
    files: new Set<string>(),
    directories: new Map<string, string[]>(),
    configuration: new Map<string, unknown>(),
    workspaceFolder: undefined as string | undefined,
    searches: [] as { include: string; exclude: string | undefined; maxResults: number | undefined }[],
    linkProviders: [] as { selector: unknown; provider: DocumentLinkProviderStub }[],
    watchers: [] as FileSystemWatcherStub[],
    createdFiles: new Emitter(),
    deletedFiles: new Emitter(),
    renamedFiles: new Emitter(),
    changedWorkspaceFolders: new Emitter(),
    changedConfiguration: new Emitter(),
};

export interface DocumentLinkProviderStub {
    provideDocumentLinks(document: unknown, token: unknown): Promise<DocumentLink[]> | DocumentLink[];
    resolveDocumentLink?(link: DocumentLink, token: unknown): DocumentLink | undefined;
}

export function reset(): void {
    state.files = new Set();
    state.directories = new Map();
    state.configuration = new Map();
    state.workspaceFolder = undefined;
    state.searches = [];
    state.linkProviders = [];
    state.watchers = [];
    state.createdFiles = new Emitter();
    state.deletedFiles = new Emitter();
    state.renamedFiles = new Emitter();
    state.changedWorkspaceFolders = new Emitter();
    state.changedConfiguration = new Emitter();
}

export const workspace = {
    getWorkspaceFolder(_uri: Uri): { uri: Uri } | undefined {
        return state.workspaceFolder === undefined ? undefined : { uri: Uri.file(state.workspaceFolder) };
    },

    getConfiguration(section: string, _scope?: unknown) {
        return {
            get<T>(key: string): T | undefined {
                return state.configuration.get(`${section}.${key}`) as T | undefined;
            },
        };
    },

    fs: {
        async stat(uri: Uri): Promise<{ type: FileType }> {
            if (state.files.has(uri.fsPath)) return { type: FileType.File };
            if (state.directories.has(uri.fsPath)) return { type: FileType.Directory };
            throw new Error(`ENOENT: ${uri.fsPath}`);
        },

        async readDirectory(uri: Uri): Promise<[string, FileType][]> {
            const children = state.directories.get(uri.fsPath);
            if (children === undefined) throw new Error(`ENOENT: ${uri.fsPath}`);
            return children.map((name) => [
                name,
                state.directories.has(`${uri.fsPath}/${name}`) ? FileType.Directory : FileType.File,
            ]);
        },
    },

    async findFiles(include: string, exclude?: string, maxResults?: number): Promise<Uri[]> {
        state.searches.push({ include, exclude, maxResults });

        const included = globToRegularExpression(include);
        const excluded = exclude === undefined ? undefined : globToRegularExpression(exclude);
        const matched = [...state.files]
            .filter((file) => included.test(file))
            .filter((file) => excluded === undefined || !excluded.test(file));

        return (maxResults === undefined ? matched : matched.slice(0, maxResults)).map((file) =>
            Uri.file(file),
        );
    },

    createFileSystemWatcher(
        pattern: string,
        _ignoreCreateEvents?: boolean,
        ignoreChangeEvents?: boolean,
        _ignoreDeleteEvents?: boolean,
    ): FileSystemWatcherStub {
        const created = new Emitter();
        const changed = new Emitter();
        const deleted = new Emitter();
        const watcher: FileSystemWatcherStub = {
            pattern,
            ignoresChanges: ignoreChangeEvents === true,
            created,
            changed,
            deleted,
            onDidCreate: created.event,
            onDidChange: changed.event,
            onDidDelete: deleted.event,
            dispose: () => {},
        };

        state.watchers.push(watcher);
        return watcher;
    },

    onDidCreateFiles: (listener: Listener) => state.createdFiles.event(listener),
    onDidDeleteFiles: (listener: Listener) => state.deletedFiles.event(listener),
    onDidRenameFiles: (listener: Listener) => state.renamedFiles.event(listener),
    onDidChangeWorkspaceFolders: (listener: Listener) => state.changedWorkspaceFolders.event(listener),
    onDidChangeConfiguration: (listener: Listener) => state.changedConfiguration.event(listener),
};

export const languages = {
    registerDocumentLinkProvider(selector: unknown, provider: DocumentLinkProviderStub): Disposable {
        state.linkProviders.push({ selector, provider });
        return { dispose: () => {} };
    },
};
