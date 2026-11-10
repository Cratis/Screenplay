// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Comparison;

sealed record AgentExposureSnapshot(bool HasErrors, bool HasBlock, AgentTokenKind? Tokens, IReadOnlyDictionary<AgentExposureSnapshot.Key, AgentExposureSnapshot.Operation> Operations)
{
    internal static AgentExposureSnapshot Read(AuthoringSnapshot source)
    {
        // This is parsing/validation only, never the workspace's executable-binding result.
        var compilation = source.Compilation;
        var applications = compilation.Value is { } application ? [application] : source.Sources
            .Select(pair => new ScreenplayCompiler(source.Languages).Parse(pair.Value, pair.Key).Value).OfType<ApplicationSyntax>().ToArray();

        // Physical documents retain evidence that singleton parsing and folder merging reject.
        var blocks = ReadBlocks(source);
        var targets = applications.SelectMany(AgentOperationCatalog.Of).ToArray();
        var operations = blocks.SelectMany(block => block.Operations).Select(operation => Resolve(operation, targets, source.Index, compilation.Success))
            .GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.First().Value);
        return new(!compilation.Success, blocks.Count > 0, blocks.FirstOrDefault()?.Tokens?.Kind, operations);
    }

    static List<AgentsSyntax> ReadBlocks(AuthoringSnapshot source)
    {
        var blocks = new List<AgentsSyntax>();
        foreach (var document in source.Sources.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var lines = SourceLineSplitter.Split(document.Value);
            var context = SourceOptionsParser.Create(lines, document.Key, source.Languages);
            context.AgentBlockParsed = blocks.Add;
            ScreenplayParser.Parse(context, lines);
        }
        return blocks;
    }

    static KeyValuePair<Key, Operation> Resolve(AgentOperationSyntax operation, IReadOnlyList<AgentOperationCatalog.Target> targets, AuthoringIndex index, bool sourceValid)
    {
        var candidates = targets.Where(target => target.Kind == operation.Kind).ToArray();
        var resolution = ReferenceResolver.Resolve(operation.Reference, new([]), [.. candidates.Select(target => target.Declaration)]);
        var name = resolution.Resolved is { } resolved ? candidates.Single(target => target.Declaration == resolved).QualifiedName : operation.Reference;
        var declarations = index.Declarations.Where(declaration => declaration.Kind == operation.Kind.ToString() && declaration.Address == name).ToArray();
        var incompleteReferences = false;
        var signature = sourceValid && declarations.Length == 1 ? AgentExposureSignature.Read(index, declarations[0], out incompleteReferences) : null;
        return new(new(operation.Kind, name), new(operation.Scope, signature, incompleteReferences));
    }

    internal sealed record Key(AgentOperationKind Kind, string Name);

    internal sealed record Operation(AgentScope? Scope, string? Signature, bool IncompleteReferences);
}
