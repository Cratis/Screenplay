import { appendFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

const trackedLabel = 'ai-corpus: tracked';
const noneLabel = 'ai-corpus: none';
const languagePath = /^(?:Source\/DotNET\/(?:Screenplay|Screenplay\.Mcp|Tool|Screenplay\.CanonicalCorpus)|Source\/Screenplay\/(?:Compiler|Monaco|VSCodeExtension|EventModels|McpApp)|Documentation\/screenplay|Samples)\//;
const excludedPath = /(?:^|\/)(?:for_[^/]*|[^/]*\.Specs)(?:\/|$)/;

export function isLanguagePath(path) {
    return languagePath.test(path) && !excludedPath.test(path);
}

function references(text) {
    const matches = String(text ?? '').matchAll(/(?:\bCratis\/AI#|https:\/\/github\.com\/Cratis\/AI\/(?:issues|pull)\/)([1-9]\d*)\b/gi);
    return [...matches].map(match => Number(match[1])).filter(Number.isSafeInteger);
}

function noImpactReason(comments) {
    for (const comment of comments) {
        for (const line of String(comment.body ?? '').split(/\r?\n/)) {
            const match = /^Corpus impact: none -\s*(.*)$/i.exec(line.trim());
            const reason = match?.[1].trim();
            if (reason && /\p{L}/u.test(reason) && !/later|follow-?up|TBD|todo/i.test(reason)) {
                return reason;
            }
        }
    }
    return null;
}

// The injected API keeps policy tests independent of GitHub credentials and network access.
export async function verifyImpact({ api, repository, number }) {
    const pr = await api.pullRequest(repository, number);
    if (pr.draft) {
        return { code: 0, decision: 'skipped', message: 'Draft pull request; corpus impact will be checked when ready.' };
    }
    if (pr.user.login === 'dependabot[bot]') {
        return { code: 0, decision: 'skipped', message: 'Dependabot pull request; no corpus impact decision required.' };
    }

    const files = await api.files(repository, number);
    if (files.length !== pr.changed_files) {
        throw new Error('The live changed-file list is incomplete or changed during the read. Re-run the check.');
    }
    const paths = [...new Set(files.flatMap(file => [file.filename, file.previous_filename])
        .filter(path => path && isLanguagePath(path)))];
    if (!paths.length) {
        return { code: 0, decision: 'not applicable', message: 'No language paths changed.' };
    }

    const command = `gh pr edit ${number} --repo ${repository}`;
    const labels = pr.labels.map(label => label.name);
    const tracked = labels.includes(trackedLabel);
    const none = labels.includes(noneLabel);
    if (tracked === none) {
        return {
            code: 1, decision: 'missing or conflicting', paths,
            message: 'Language pull requests need exactly one label: ai-corpus: tracked or ai-corpus: none. Add evidence first, then add the label.',
            fixes: [
                `${command}${none ? ` --remove-label "${noneLabel}"` : ''} --add-label "${trackedLabel}"`,
                `${command}${tracked ? ` --remove-label "${trackedLabel}"` : ''} --add-label "${noneLabel}"`
            ]
        };
    }

    const comments = await api.comments(repository, number);
    if (none) {
        const reason = noImpactReason(comments);
        return reason ? {
            code: 0, decision: noneLabel, paths, message: 'No corpus impact, with a recorded reason.', evidence: reason
        } : {
            code: 1, decision: noneLabel, paths,
            message: 'Add a comment line Corpus impact: none - <reason>. The reason must not defer work (later, follow-up, TBD or todo).',
            fixes: [`gh pr comment ${number} --repo ${repository} --body "Corpus impact: none - <explain why the corpus needs no change>"`]
        };
    }

    const candidates = new Set([pr.body, ...comments.map(comment => comment.body)].flatMap(references));
    for (const candidate of candidates) {
        if (await api.issueExists(candidate)) {
            return { code: 0, decision: trackedLabel, paths, message: 'Corpus adaptation is tracked.', evidence: `https://github.com/Cratis/AI/issues/${candidate}` };
        }
    }
    for (const source of await api.crossReferences(repository, number)) {
        if (source.repository?.nameWithOwner?.toLowerCase() === 'cratis/ai' && await api.issueExists(source.number)) {
            return { code: 0, decision: trackedLabel, paths, message: 'Corpus adaptation is tracked by a cross-reference.', evidence: `https://github.com/Cratis/AI/issues/${source.number}` };
        }
    }
    return {
        code: 1, decision: trackedLabel, paths,
        message: 'Link an existing Cratis/AI issue or pull request in a PR comment/body, or link this PR from that issue or pull request.',
        fixes: [`gh pr comment ${number} --repo ${repository} --body "Corpus impact: https://github.com/Cratis/AI/issues/<number>"`]
    };
}

export function githubApi(token, fetchApi = fetch) {
    async function request(endpoint, { method = 'GET', body, allowMissing = false } = {}) {
        const response = await fetchApi(`https://api.github.com/${endpoint}`, {
            method,
            headers: {
                Accept: 'application/vnd.github+json',
                Authorization: `Bearer ${token}`,
                'X-GitHub-Api-Version': '2022-11-28',
                'Content-Type': 'application/json'
            },
            ...(body ? { body: JSON.stringify(body) } : {}),
            signal: AbortSignal.timeout(15000)
        });
        if (allowMissing && response.status === 404) return null;
        if (!response.ok) throw new Error(`GitHub API ${endpoint} returned HTTP ${response.status}.`);
        return response.json();
    }

    async function pages(endpoint) {
        const result = [];
        for (let page = 1; page <= 100; page++) {
            const items = await request(`${endpoint}?per_page=100&page=${page}`);
            if (!Array.isArray(items)) throw new Error(`GitHub API ${endpoint} did not return a list.`);
            result.push(...items);
            if (items.length < 100) return result;
        }
        throw new Error(`GitHub API ${endpoint} exceeded the pagination budget; no decision was made.`);
    }

    return {
        pullRequest: (repository, number) => request(`repos/${repository}/pulls/${number}`),
        files: (repository, number) => pages(`repos/${repository}/pulls/${number}/files`),
        comments: (repository, number) => pages(`repos/${repository}/issues/${number}/comments`),
        issueExists: async number => (await request(`repos/Cratis/AI/issues/${number}`, { allowMissing: true })) !== null,
        async crossReferences(repository, number) {
            const [owner, name] = repository.split('/');
            const query = `query($owner: String!, $name: String!, $number: Int!, $cursor: String) {
                repository(owner: $owner, name: $name) {
                    pullRequest(number: $number) {
                        timelineItems(first: 100, after: $cursor, itemTypes: [CROSS_REFERENCED_EVENT]) {
                            nodes { ... on CrossReferencedEvent { source {
                                ... on Issue { number repository { nameWithOwner } }
                                ... on PullRequest { number repository { nameWithOwner } }
                            } } }
                            pageInfo { hasNextPage endCursor }
                        }
                    }
                }
            }`;
            const result = [];
            let cursor = null;
            for (let page = 1; page <= 100; page++) {
                const response = await request('graphql', { method: 'POST', body: { query, variables: { owner, name, number, cursor } } });
                if (response.errors?.length) throw new Error('GitHub GraphQL could not read cross-reference evidence.');
                const timeline = response.data?.repository?.pullRequest?.timelineItems;
                if (!Array.isArray(timeline?.nodes) || typeof timeline.pageInfo?.hasNextPage !== 'boolean') {
                    throw new Error('GitHub GraphQL returned incomplete cross-reference evidence.');
                }
                result.push(...timeline.nodes.filter(node => node?.source).map(node => node.source));
                if (!timeline.pageInfo.hasNextPage) return result;
                if (!timeline.pageInfo.endCursor || timeline.pageInfo.endCursor === cursor) {
                    throw new Error('GitHub GraphQL returned an invalid pagination cursor.');
                }
                cursor = timeline.pageInfo.endCursor;
            }
            throw new Error('GitHub cross-reference evidence exceeded the pagination budget; no decision was made.');
        }
    };
}

function annotation(text) {
    return text.replaceAll('%', '%25').replaceAll('\r', '%0D').replaceAll('\n', '%0A');
}

function markdown(text) {
    return String(text).replace(/[&<>`\[\]\\*_]/g, character => `&#${character.charCodeAt(0)};`).replace(/[\r\n]/g, ' ');
}

export async function run({ env = process.env, fetchApi = fetch, log = console.log, summary = appendFile } = {}) {
    let result;
    const repository = env.GITHUB_REPOSITORY;
    const number = Number(env.PR_NUMBER);
    try {
        if (!/^[\w.-]+\/[\w.-]+$/.test(repository ?? '') || !Number.isSafeInteger(number) || number < 1 || !env.GITHUB_TOKEN) {
            throw new Error('Set GITHUB_REPOSITORY, PR_NUMBER and GITHUB_TOKEN to read the live pull request.');
        }
        result = await verifyImpact({ api: githubApi(env.GITHUB_TOKEN, fetchApi), repository, number });
    } catch (error) {
        result = { code: 2, decision: 'could not run', message: error.message };
    }
    const rerun = /^\d+$/.test(env.GITHUB_RUN_ID ?? '') && /^[\w.-]+\/[\w.-]+$/.test(repository ?? '')
        ? `gh run rerun ${env.GITHUB_RUN_ID} --repo ${repository} --failed` : 'Re-run this check after updating comment evidence.';
    const fixes = result.code ? [...(result.fixes ?? []), rerun] : [];
    log(`::${result.code ? 'error' : 'notice'}::${annotation(`${result.message}${fixes.length ? ` Fix: ${fixes.join(' OR ')}` : ''}`)}`);
    if (env.GITHUB_STEP_SUMMARY) {
        const lines = [
            '## AI corpus impact',
            `Decision: ${markdown(result.decision)} (exit ${result.code})`,
            markdown(result.message),
            ...(result.evidence ? [`Evidence: ${markdown(result.evidence)}`] : []),
            ...(result.paths?.length ? ['Matched language paths:', ...result.paths.map(path => `- ${markdown(path)}`)] : []),
            ...fixes.map(fix => `- Fix: ${markdown(fix)}`)
        ];
        try {
            await summary(env.GITHUB_STEP_SUMMARY, `${lines.join('\n\n')}\n`);
        } catch (error) {
            log(`::error::${annotation(`Could not write the step summary: ${error.message}`)}`);
            return 2;
        }
    }
    return result.code;
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    process.exitCode = await run();
}
