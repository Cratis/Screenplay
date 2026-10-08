import assert from 'node:assert/strict';
import test from 'node:test';
import { githubApi, isLanguagePath, run, verifyImpact } from '../verify-ai-corpus-impact.mjs';

const repository = 'Cratis/Screenplay';
const number = 495;
const languageFile = { filename: 'Source/DotNET/Screenplay/Parsing/Parser.cs' };
const tracked = [{ name: 'ai-corpus: tracked' }];
const none = [{ name: 'ai-corpus: none' }];

function fixture({ labels = [], files = [languageFile], comments = [], body = '', draft = false, author = 'woksin', sources = [], exists = [] } = {}) {
    return {
        async pullRequest() { return { labels, changed_files: files.length, body, draft, user: { login: author } }; },
        async files() { return files; },
        async comments() { return comments.map(body => ({ body })); },
        async crossReferences() { return sources; },
        async issueExists(number) { return exists.includes(number); }
    };
}

function verify(options) {
    return verifyImpact({ repository, number, api: fixture(options) });
}

test('no language paths passes without a decision', async () => {
    assert.equal((await verify({ files: [{ filename: '.github/workflows/verify-semver-label.yml' }] })).code, 0);
});

test('missing label fails with exact commands for either choice', async () => {
    const result = await verify();
    assert.equal(result.code, 1);
    assert.deepEqual(result.fixes, [
        'gh pr edit 495 --repo Cratis/Screenplay --add-label "ai-corpus: tracked"',
        'gh pr edit 495 --repo Cratis/Screenplay --add-label "ai-corpus: none"'
    ]);
});

test('both labels fails and tells the author to remove the other label', async () => {
    const result = await verify({ labels: [...tracked, ...none] });
    assert.equal(result.code, 1);
    assert.match(result.fixes[0], /--remove-label "ai-corpus: none"/);
    assert.match(result.fixes[1], /--remove-label "ai-corpus: tracked"/);
});

test('tracked without a link fails', async () => {
    assert.equal((await verify({ labels: tracked })).code, 1);
});

test('tracked comment link passes only when the mock API verifies it exists', async () => {
    const options = { labels: tracked, comments: ['Corpus impact: https://github.com/Cratis/AI/issues/529'] };
    assert.equal((await verify(options)).code, 1);
    const result = await verify({ ...options, exists: [529] });
    assert.equal(result.code, 0);
    assert.equal(result.evidence, 'https://github.com/Cratis/AI/issues/529');
});

test('tracked accepts body shorthand and pull request links', async () => {
    assert.equal((await verify({ labels: tracked, body: '- Updated syntax (Cratis/AI#529)', exists: [529] })).code, 0);
    assert.equal((await verify({ labels: tracked, comments: ['https://github.com/Cratis/AI/pull/529'], exists: [529] })).code, 0);
});

test('tracked accepts a verified cross-reference from Cratis/AI, but not another repository', async () => {
    const source = { number: 529, repository: { nameWithOwner: 'Cratis/AI' } };
    assert.equal((await verify({ labels: tracked, sources: [source], exists: [529] })).code, 0);
    assert.equal((await verify({ labels: tracked, sources: [source] })).code, 1);
    assert.equal((await verify({ labels: tracked, sources: [{ ...source, repository: { nameWithOwner: 'Cratis/Other' } }], exists: [529] })).code, 1);
});

test('none without a comment reason fails, including a reason only in the body', async () => {
    assert.equal((await verify({ labels: none })).code, 1);
    assert.equal((await verify({ labels: none, body: 'Corpus impact: none - internal refactor' })).code, 1);
    assert.equal((await verify({ labels: none, comments: ['Corpus impact: none -   ', 'Corpus impact: none - ---'] })).code, 1);
});

test('none rejects deferred reasons', async () => {
    for (const reason of ['will do later', 'follow-up', 'followup', 'TBD', 'todo']) {
        assert.equal((await verify({ labels: none, comments: [`Corpus impact: none - ${reason}`] })).code, 1, reason);
    }
});

test('none with an actual reason in a comment line passes', async () => {
    const result = await verify({ labels: none, comments: ['Review notes\nCorpus impact: none - internal refactor, no behavior change\nChecks passed'] });
    assert.equal(result.code, 0);
    assert.equal(result.evidence, 'internal refactor, no behavior change');
});

test('draft and Dependabot pass with a skip notice and no evidence reads', async () => {
    for (const options of [{ draft: true }, { author: 'dependabot[bot]' }]) {
        const api = fixture(options);
        api.files = async () => { throw new Error('Must not read files for exempt PRs'); };
        const result = await verifyImpact({ repository, number, api });
        assert.equal(result.code, 0);
        assert.equal(result.decision, 'skipped');
    }
});

test('language paths match all included roots and exclude specs', () => {
    for (const path of [
        'Source/DotNET/Screenplay/Syntax/Node.cs', 'Source/DotNET/Screenplay.Mcp/Tools.cs',
        'Source/DotNET/Tool/Check.cs', 'Source/DotNET/Screenplay.CanonicalCorpus/Corpus/source.play',
        'Source/Screenplay/Compiler/Parser.ts', 'Source/Screenplay/Monaco/language.ts',
        'Source/Screenplay/VSCodeExtension/syntaxes/grammar.json', 'Source/Screenplay/EventModels/Mapping.ts',
        'Source/Screenplay/McpApp/main.ts', 'Documentation/screenplay/grammar.md', 'Samples/Library/library.play'
    ]) assert.equal(isLanguagePath(path), true, path);
    for (const path of [
        'Source/DotNET/Screenplay/Parsing/for_Parser/when_parsing.cs',
        'Source/Screenplay/Compiler/for_parse/when_parsing.ts',
        'Source/DotNET/Screenplay.CanonicalVectors.Specs/Test.cs',
        'Source/DotNET/Screenplay/Nested.Specs/Test.cs',
        'Source/DotNET/Screenplay.Other/Node.cs', 'Source/Screenplay/Views/View.ts', 'README.md'
    ]) assert.equal(isLanguagePath(path), false, path);
});

test('spec-only changes pass and moving out of a language root still needs a decision', async () => {
    assert.equal((await verify({ files: [{ filename: 'Source/DotNET/Screenplay/for_Parser/when_parsing.cs' }] })).code, 0);
    assert.equal((await verify({ files: [{ filename: 'Other/Parser.cs', previous_filename: languageFile.filename }] })).code, 1);
});

function response(data, status = 200) {
    return { ok: status >= 200 && status < 300, status, async json() { return data; } };
}

test('REST adapter reads live state and paginates files/comments', async () => {
    const requests = [];
    const fetchApi = async (url, options) => {
        requests.push({ url, options });
        if (url.endsWith('/pulls/495')) return response({ draft: false, user: { login: 'woksin' }, labels: tracked, body: '', changed_files: 101 });
        if (url.includes('/files?')) return response(url.endsWith('page=1') ? Array.from({ length: 100 }, (_, i) => ({ filename: `Other/${i}` })) : [languageFile]);
        if (url.includes('/comments?')) return response(url.endsWith('page=1') ? Array.from({ length: 100 }, () => ({ body: '' })) : [{ body: 'Cratis/AI#529' }]);
        if (url.endsWith('/Cratis/AI/issues/529')) return response({ number: 529 });
        throw new Error(`Unexpected API read ${url}`);
    };
    const result = await verifyImpact({ repository, number, api: githubApi('token', fetchApi) });
    assert.equal(result.code, 0);
    assert.equal(requests.length, 6);
    assert.ok(requests.every(({ options }) => options.headers.Authorization === 'Bearer token' && options.signal));
});

test('GraphQL adapter pages cross-references on the live PR', async () => {
    const cursors = [];
    const api = githubApi('token', async (url, options) => {
        assert.equal(url, 'https://api.github.com/graphql');
        const { variables, query } = JSON.parse(options.body);
        cursors.push(variables.cursor);
        assert.equal(variables.number, 495);
        assert.match(query, /CROSS_REFERENCED_EVENT/);
        return response({ data: { repository: { pullRequest: { timelineItems: {
            nodes: [{ source: { number: 529, repository: { nameWithOwner: 'Cratis/AI' } } }],
            pageInfo: { hasNextPage: variables.cursor === null, endCursor: 'next' }
        } } } } });
    });
    assert.equal((await api.crossReferences(repository, number)).length, 2);
    assert.deepEqual(cursors, [null, 'next']);
});

test('API cannot-run errors are not policy failures; only an issue 404 means nonexistent', async () => {
    assert.equal(await githubApi('token', async () => response({}, 404)).issueExists(529), false);
    for (const status of [403, 429, 500]) {
        await assert.rejects(githubApi('token', async () => response({}, status)).issueExists(529), /GitHub API/);
    }
    await assert.rejects(githubApi('token', async () => response({ errors: [{ message: 'denied' }] })).crossReferences(repository, number), /GraphQL/);
    const api = fixture();
    api.pullRequest = async () => ({ changed_files: 3001, draft: false, user: { login: 'woksin' }, labels: tracked });
    await assert.rejects(verifyImpact({ repository, number, api }), /incomplete/);
});

test('runner reports exit 2 and step summary for an API failure', async () => {
    const logs = [];
    let summaryText;
    const code = await run({
        env: { GITHUB_REPOSITORY: repository, PR_NUMBER: '495', GITHUB_TOKEN: 'token', GITHUB_RUN_ID: '123', GITHUB_STEP_SUMMARY: 'summary' },
        fetchApi: async () => response({}, 403), log: line => logs.push(line),
        summary: async (path, text) => { assert.equal(path, 'summary'); summaryText = text; }
    });
    assert.equal(code, 2);
    assert.match(logs[0], /^::error::/);
    assert.match(logs[0], /gh run rerun 123 --repo Cratis\/Screenplay --failed/);
    assert.match(summaryText, /could not run/);
});

test('runner never reads the event payload and uses live draft status for its notice', async () => {
    const logs = [];
    assert.equal(await run({
        env: { GITHUB_REPOSITORY: repository, PR_NUMBER: '495', GITHUB_TOKEN: 'token', GITHUB_EVENT_PATH: '/nonexistent/event.json' },
        fetchApi: async () => response({ draft: true }), log: line => logs.push(line)
    }), 0);
    assert.match(logs[0], /^::notice::Draft/);
});

test('runner returns exit 1 for a policy failure with a summary and fix commands', async () => {
    const logs = [];
    let summaryText;
    const code = await run({
        env: { GITHUB_REPOSITORY: repository, PR_NUMBER: '495', GITHUB_TOKEN: 'token', GITHUB_STEP_SUMMARY: 'summary' },
        fetchApi: async url => url.endsWith('/pulls/495')
            ? response({ draft: false, user: { login: 'woksin' }, changed_files: 1, labels: [] })
            : response([languageFile]),
        log: line => logs.push(line), summary: async (_, text) => { summaryText = text; }
    });
    assert.equal(code, 1);
    assert.match(logs[0], /^::error::.*gh pr edit 495/);
    assert.match(summaryText, /Matched language paths:/);
});
