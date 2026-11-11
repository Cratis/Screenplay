// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_carrying_system_configuration : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source = """
        module M
          feature F
            slice StateView S
              readmodel Row
                id String key
                value String
              projection Build => Row
                from Recorded
                  id = $eventSourceId
                  value = value
              event Recorded
                value String
        """;

    const string Configured = """
        secret Key
          description "The key"

        system Mailer
          ability Send idempotent
          extension "ghcr.io/x/mail" version 2
          setting apiKey from secret Key
          setting sender = "Acme"
          activation on demand idle after 10 minutes

        """;

    const string Bare = """
        system Mailer
          ability Send idempotent

        """;

    [Fact]
    void should_expose_the_configuration_without_changing_executable_bytes_or_revision()
    {
        var plain = Bind(Bare + Source);
        var configured = Bind(Configured + Source);
        plain.Success.ShouldBeTrue();
        configured.Diagnostics.Select(d => d.Code + d.Message).ShouldBeEmpty();
        plain.Value!.SystemConfigurations.ShouldBeEmpty();
        plain.Value.Secrets.ShouldBeEmpty();
        var compilation = configured.Value!;
        compilation.Secrets.Single().ShouldEqual(new SemanticSecretDeclaration("Key", "The key"));
        var system = compilation.SystemConfigurations.Single();
        system.System.ShouldEqual("Mailer");
        system.Extension.ShouldEqual("ghcr.io/x/mail");
        system.Version.ShouldEqual("2");
        system.Settings.Select(setting => (setting.Name, setting.Secret, setting.Value)).ToArray().ShouldEqual([("apiKey", "Key", null), ("sender", null, "Acme")]);
        system.Activation.ShouldEqual(new SemanticSystemActivation(false, 10, "minute"));
        compilation.Model.Revision.ShouldEqual(plain.Value.Model.Revision);
        compilation.Model.Application.Systems.Single().Name.ShouldEqual("Mailer");
    }

    [Fact]
    void should_carry_it_through_a_copy_of_the_sidecars()
    {
        var compilation = Bind(Configured + Source).Value!;
        var copied = Cratis.Screenplay.Semantics.SemanticCompilation.Create(compilation.Model, compilation.Documents, compilation.SourceMap).WithSidecarsOf(compilation);
        copied.SystemConfigurations.Select(configuration => configuration.System).ShouldEqual(compilation.SystemConfigurations.Select(configuration => configuration.System));
        copied.Secrets.ShouldEqual(compilation.Secrets);
    }
}
