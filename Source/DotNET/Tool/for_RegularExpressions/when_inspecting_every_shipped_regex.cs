// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Tool.for_RegularExpressions;

public class when_inspecting_every_shipped_regex : Specification
{
    Dictionary<MethodInfo, Regex> _expressions = null!;

    void Because() => _expressions = ShippedAssemblies(typeof(CommandLineInformation).Assembly)
        .SelectMany(assembly => assembly.GetTypes())
        .Where(type => !(type.Namespace ?? string.Empty).Split('.').Any(part => part.StartsWith("for_", StringComparison.Ordinal)))
        .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        .Where(method => method.ReturnType == typeof(Regex) && Attribute.IsDefined(method, typeof(GeneratedRegexAttribute)))
        .ToDictionary(method => method, method => (Regex)method.Invoke(null, null)!);

    [Fact] void should_inspect_more_than_three_hundred_expressions() => _expressions.Count.ShouldBeGreaterThan(300);
    [Fact] void should_guarantee_linear_matching() => _expressions.Where(entry => !entry.Value.Options.HasFlag(RegexOptions.NonBacktracking)).Select(entry => entry.Key.ToString()).ShouldBeEmpty();
    [Fact] void should_not_depend_on_host_wall_clock_time() => _expressions.Where(entry => entry.Value.MatchTimeout != Regex.InfiniteMatchTimeout).Select(entry => entry.Key.ToString()).ShouldBeEmpty();

    static IEnumerable<Assembly> ShippedAssemblies(Assembly root)
    {
        var pending = new Queue<Assembly>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        pending.Enqueue(root);
        while (pending.TryDequeue(out var assembly))
        {
            if (!visited.Add(assembly.FullName!)) continue;
            yield return assembly;
            foreach (var reference in assembly.GetReferencedAssemblies().Where(reference => reference.Name!.StartsWith("Cratis.Screenplay", StringComparison.Ordinal)))
            {
                pending.Enqueue(Assembly.Load(reference));
            }
        }
    }
}
