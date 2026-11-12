// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Screenplay.Tool.for_RestoreCommand;

public class when_restoring_a_model_that_references_an_extension : for_ModelCheck.given.a_model
{
    string _source;
    string _cache;
    string _previousCache;
    int _first;
    int _locked;
    string _lockText;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "system Mailer\n  extension \"cratis/mail\" version 2\n");
        _source = Path.Combine(Root, "..", "source-" + Guid.NewGuid().ToString("N"));
        var version = Path.Combine(_source, "ghcr.io", "cratis", "extension-mail", "2.1.0");
        Directory.CreateDirectory(version);
        File.WriteAllText(Path.Combine(version, "contract.play"), "extension Mail\n  display name \"Mail\"\n  description \"Mail\"\n  icon \"i.svg\"\n");
        _cache = Path.Combine(Root, "..", "cache-" + Guid.NewGuid().ToString("N"));
        _previousCache = Environment.GetEnvironmentVariable(ExtensionCache.EnvironmentVariable);
        Environment.SetEnvironmentVariable(ExtensionCache.EnvironmentVariable, _cache);
    }

    void Because()
    {
        _first = ToolCommands.Run(["restore", Root, "--source", _source, "--vendor"], Output, Error);
        _lockText = File.ReadAllText(Path.Combine(Root, ExtensionLock.FileName));
        _locked = ToolCommands.Run(["restore", Root, "--locked", "--source", _source], new StringWriter(), Error);
    }

    void Destroy()
    {
        Environment.SetEnvironmentVariable(ExtensionCache.EnvironmentVariable, _previousCache);
        Directory.Delete(_source, recursive: true);
        if (Directory.Exists(_cache)) Directory.Delete(_cache, recursive: true);
    }

    [Fact] void should_succeed() => _first.ShouldEqual(0);
    [Fact] void should_pin_the_newest_release_in_the_range() => _lockText.ShouldContain("version 2.1.0");
    [Fact] void should_record_the_range_the_model_asked_for() => _lockText.ShouldContain("range \"2\"");
    [Fact] void should_vendor_the_contract() => Directory.GetFiles(Path.Combine(Root, "extensions"), "contract.extension", SearchOption.AllDirectories).Length.ShouldEqual(1);
    [Fact] void should_restore_again_from_the_lock_alone() => _locked.ShouldEqual(0);
    [Fact] void should_check_the_model_without_extension_errors() => ToolCommands.Run([Root], new StringWriter(), Error).ShouldEqual(0);
}
