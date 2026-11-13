// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Extensions.for_ExtensionContractResolver;

public class when_resolving_references_that_are_not_locked : Specification
{
    string _root;
    ExtensionLock _locked;
    IReadOnlyList<Cratis.Screenplay.Diagnostics.Diagnostic> _diagnostics;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-restore-" + Guid.NewGuid().ToString("N"));
        foreach (var version in new[] { "2.0.0", "2.3.1", "3.0.0" })
        {
            var directory = Path.Combine(_root, "source", "ghcr.io", "cratis", "extension-stripe", version);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "contract.play"), ExtensionContractSample.Stripe + "\n# " + version);
        }
    }

    void Because()
    {
        var restorer = new ExtensionRestorer(new(Path.Combine(_root, "cache")), new DirectoryExtensionPackageSource(Path.Combine(_root, "source")));
        (_locked, _diagnostics) = restorer.Resolve([new("cratis/stripe", "^2", SourceLocation.Start), new("cratis/missing", null, SourceLocation.Start)], ExtensionLock.Empty);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);

    [Fact] void should_pin_the_newest_release_in_the_range() => _locked.Find("cratis/stripe")!.Version.ShouldEqual("2.3.1");
    [Fact] void should_record_the_range_and_the_resolved_reference() => (_locked.Find("cratis/stripe")!.Range, _locked.Find("cratis/stripe")!.Resolved).ShouldEqual(("^2", "ghcr.io/cratis/extension-stripe"));
    [Fact] void should_report_a_reference_no_release_satisfies() => _diagnostics.Count.ShouldEqual(1);
    [Fact] void should_not_pin_it() => _locked.Find("cratis/missing").ShouldBeNull();
}
