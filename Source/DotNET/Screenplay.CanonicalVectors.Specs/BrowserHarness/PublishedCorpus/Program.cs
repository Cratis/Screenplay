// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;

// Writes the folder source form of the published screens corpus to the destination folder and prints the package
// version it came from. Exit 2 when the destination is missing or the form has no documents.
if (args.Length != 1)
{
    Console.Error.WriteLine("usage: PublishedCorpus <destination>");
    return 2;
}

var form = ScreenCompositionCorpus.V1.SourceForms.Single(candidate => candidate.Name == "folder");
var documents = form.Documents.ToArray();
if (documents.Length == 0)
{
    Console.Error.WriteLine("the published folder source form has no documents");
    return 2;
}

var destination = Path.GetFullPath(args[0]);
foreach (var document in documents)
{
    var path = Path.GetFullPath(Path.Combine(destination, document.DisplayPath));
    if (!path.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"refusing a document path outside the destination: {document.DisplayPath}");
        return 2;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, document.Text);
}

var version = typeof(ScreenCompositionCorpus).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
Console.WriteLine($"{documents.Length} documents from Cratis.Screenplay.CanonicalCorpus {version.Split('+')[0]}");
return 0;
