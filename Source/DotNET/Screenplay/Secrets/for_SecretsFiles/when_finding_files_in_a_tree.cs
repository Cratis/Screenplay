// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets.for_SecretsFiles;

public class when_finding_files_in_a_tree : Specification
{
    DirectoryInfo _root;
    IEnumerable<SecretsFileReference> _files;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("secretsfiles");
        File.WriteAllText(Path.Combine(_root.FullName, "invoicing.secrets"), "apiKey = enc:v1:q83vEjRWeJASNFZ4kKvN7w==");
        Directory.CreateDirectory(Path.Combine(_root.FullName, "nested", "deeper"));
        File.WriteAllText(Path.Combine(_root.FullName, "nested", "deeper", "orders.secrets"), "// empty");
        File.WriteAllText(Path.Combine(_root.FullName, "invoicing.play"), "module Invoicing");
    }

    void Because() => _files = new SecretsFiles().FindIn(_root.FullName);

    [Fact] void should_find_both_secrets_files() => _files.Count().ShouldEqual(2);
    [Fact] void should_find_the_root_file() => _files.First().RelativePath.ShouldEqual("invoicing.secrets");
    [Fact] void should_find_the_nested_file() => _files.Last().RelativePath.ShouldEqual(Path.Combine("nested", "deeper", "orders.secrets"));
    [Fact] void should_not_find_other_files() => _files.Any(_ => _.Path.EndsWith(".play", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_read_content() => new SecretsFiles().ReadContent(_files.First()).ShouldEqual("apiKey = enc:v1:q83vEjRWeJASNFZ4kKvN7w==");

    void Destroy() => _root.Delete(true);
}
