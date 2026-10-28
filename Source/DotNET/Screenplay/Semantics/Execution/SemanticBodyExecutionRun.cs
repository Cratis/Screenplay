// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticBodyExecutionRun : IDisposable, IAsyncDisposable
{
    readonly ISemanticBodyExecutionProvider? _provider;

    internal SemanticBodyExecutionRun(ISemanticBodyExecutionProviderFactory? factory, SemanticApplication application, IEnumerable<SemanticImplementationBody> bodies, CancellationToken cancellationToken)
    {
        if (factory is null)
        {
            Evaluator = new SemanticEvaluator();
            return;
        }

        var resolved = bodies.ToArray();
        var unique = resolved.GroupBy(body => body.Requirement.RequirementId, StringComparer.Ordinal)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        try
        {
            _provider = Task.Run(async () => await factory.CreateAsync(application, new ReadOnlyDictionary<string, SemanticImplementationBody>(unique), cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
            Evaluator = new SemanticEvaluator(_provider, resolved);
        }
        catch (Exception exception)
        {
            Evaluator = new SemanticEvaluator(new UnavailableProvider($"Provider factory threw {exception.GetType().Name}."), resolved);
        }
    }

    internal SemanticEvaluator Evaluator { get; }

    public void Dispose() => Task.Run(async () => await DisposeAsync().ConfigureAwait(false)).GetAwaiter().GetResult();

    public ValueTask DisposeAsync() => _provider?.DisposeAsync() ?? ValueTask.CompletedTask;

    sealed class UnavailableProvider(string diagnostic) : ISemanticBodyExecutionProvider
    {
        public ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken) =>
            new(new SemanticBodyExecutionResult(null, diagnostic));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
