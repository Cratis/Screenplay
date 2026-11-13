// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions.for_ExtensionAutoRestore;

public class when_an_editor_opens_a_model_with_an_unrestored_extension : Specification
{
    string _root;
    string _lock;
    string _previous;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "screenplay-auto-" + Guid.NewGuid().ToString("N"));
        var model = Path.Combine(_root, "model");
        var version = Path.Combine(_root, "source", "ghcr.io", "cratis", "extension-mail", "2.0.0");
        Directory.CreateDirectory(model);
        Directory.CreateDirectory(version);
        File.WriteAllText(Path.Combine(model, "app.play"), "system Mailer\n  extension \"cratis/mail\" version 2\n");
        File.WriteAllText(Path.Combine(version, "contract.play"), ExtensionContractSample.Stripe);
        _lock = Path.Combine(model, ExtensionLock.FileName);
        _previous = Environment.GetEnvironmentVariable(ExtensionCache.EnvironmentVariable);
        Environment.SetEnvironmentVariable(ExtensionCache.EnvironmentVariable, Path.Combine(_root, "cache"));
    }

    void Because() => ExtensionAutoRestore.UseRoot(Path.Combine(_root, "model"), new DirectoryExtensionPackageSource(Path.Combine(_root, "source")));

    void Destroy()
    {
        Environment.SetEnvironmentVariable(ExtensionCache.EnvironmentVariable, _previous);
        Directory.Delete(_root, recursive: true);
    }

    [Fact] void should_pin_the_reference_in_the_lock() => File.ReadAllText(_lock).ShouldContain("version 2.0.0");
}
