// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionLock;

public class when_round_tripping_a_lock : Specification
{
    static readonly string Digest = "sha256:" + new string('a', 64);
    static readonly string Hash = "sha256:" + new string('b', 64);

    ExtensionLock _original;
    string _text;
    CompilationResult<ExtensionLock> _parsed;

    void Because()
    {
        _original = new ExtensionLock([
            new("npm:@acme/charts", "latest", "npm:@acme/charts", "1.0.0", Digest, Hash, "acme"),
            new("cratis/stripe", "^2.3", "ghcr.io/cratis/extension-stripe", "2.3.1", Digest, Hash, "https://github.com/cratis")]);
        _text = _original.Format();
        _parsed = ExtensionLock.Parse(_text);
    }

    [Fact] void should_parse_without_diagnostics() => _parsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_every_entry() => _parsed.Value!.Entries.SequenceEqual(_original.Entries).ShouldBeTrue();
    [Fact] void should_order_entries_by_reference() => _parsed.Value!.Entries.Select(entry => entry.Reference).ShouldEqual(["cratis/stripe", "npm:@acme/charts"]);
    [Fact] void should_write_the_same_text_again() => _parsed.Value!.Format().ShouldEqual(_text);
    [Fact] void should_not_be_json() => _text.TrimStart().StartsWith("{").ShouldBeFalse();
}
