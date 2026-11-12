---
title: "Build a Screenplay extension: create, verify and publish a package"
description: "A tutorial that creates an extension package with cratis extension new, reads its contract and manifest, implements an ability and a component, adds the icon, marketplace images and placeholders, verifies it with cratis extension verify and publishes it."
---

# Build an extension

In this tutorial you create an extension named `Hello` with a C# backend and a React component, change what it does, check it and publish it. It follows the files that `cratis extension new` creates from the [extension templates](https://github.com/Cratis/Templates/blob/main/Documentation/extensions/index.mdx), so what you see here is what the template produces.

You should have read the [extensions overview](extensions.md). To use the package from a model afterwards, see [Using an extension](using-an-extension.md).

This page links the reference pages instead of repeating them: the [contract reference](extension-contracts.md), the [backend SDK pages](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/index.md) in Stage and the [frontend contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md) in Scene.

## 1. Create the package

```bash
cratis extension new -n Hello --backend csharp --frontend react --publisher acme
```

Run `cratis extension new` with no arguments in a terminal and it asks the same questions. The package passes `cratis extension verify` without edits, so you start from something that works. The options are in [`cratis extension new`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-new.md). The CLI does not generate files itself: it instantiates the `cratis-extension` template, so `dotnet new cratis-extension -n Hello --Backend csharp --Frontend react --Publisher acme` creates the same package.

The folder holds:

| Path | What it is |
| --- | --- |
| `contract.play` | The contract and manifest. The only file the Screenplay compiler reads. |
| `assets/` | The icon and a marketplace image. |
| `placeholders/` | A static HTML placeholder for each component. |
| `src/` | The backend and frontend source and their specifications. |
| `scripts/` | `test.sh`, `build.sh`, `package.sh`, `verify.sh` and `publish.sh`. |
| `.extensionignore` | What the build does not ship: `src/`, `scripts/`, `dist/` and similar. |

## 2. Read the contract and the manifest

`contract.play` declares everything the package contributes:

```screenplay
extension Hello
  display name "Hello"
  description "The Hello extension"
  icon "assets/icon.svg"
  image "assets/marketplace.svg"
    caption "Hello in a marketplace listing"
  requires stage "^5"
  requires platform web
  activation on demand idle after 10 minutes

  setting greeting public

  ability Greet safe
    name String
    returns
      message String

  component HelloPanel
    kind react
    platform web
    placeholder "placeholders/HelloPanel.html"
    property label String
    raises greeted
      message String
    invokes Greet

  backend runtime dotnet 10 assembly "backend/Hello.dll"
  frontend "frontend/index.js"
```

The first lines are the manifest: a display name, a description and an icon are required (`PLAY0999`), and `image` lines are for a marketplace. The rest is the contents. Nothing is restated in the manifest, and two members with the same name are `PLAY1105`. Every line is explained in [The contract file](extension-contracts.md#the-contract-file).

The sample ability is `safe`, so Stage may retry it freely. If your ability changes something in the outside world, choose `idempotent` or leave the default `once`; see [Choose the effect class](systems.md#choose-the-effect-class).

## 3. Implement an ability

The backend keeps the ability as plain code, so a specification can call it without a host. In `src/backend-csharp/Hello/Greeter.cs` the ability receives the value of the `greeting` setting and the input as JSON, and returns an outcome:

```csharp
public static AbilityOutcome Greet(string? greeting, string inputJson)
{
    // Parse the input, return AbilityOutcome.NotSent(...) when it is not what the contract declares.
    var message = $"{(string.IsNullOrWhiteSpace(greeting) ? DefaultGreeting : greeting)}, {name}!";
    return AbilityOutcome.Returned(JsonSerializer.Serialize(new { message }));
}
```

`Program.cs` registers it. Everything it declares is checked against `contract.play` when Stage connects:

```csharp
var extension = Extension.Create("Hello", "1.0.0")
    .Setting(Greeter.GreetingSetting)
    .Ability(
        Greeter.AbilityName,
        EffectClass.Safe,
        async call => Greeter.Greet(await call.Host.Setting(Greeter.GreetingSetting, call.CancellationToken), call.Input))
    .Build();

await ExtensionRunner.Run(extension, IPEndPoint.Parse(listen));
```

Keep the contract and the code in step. To add an ability, declare it in `contract.play` and register it here, with the same effect class. A backend can ask Stage for settings and secrets, raise a contract trigger and answer later through the [host services](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/host-services.md). The C# SDK is described in [Write a backend in C#](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/csharp.md). Kotlin, Go and TypeScript templates exist as well: pass `--backend kotlin`, `go` or `typescript`.

## 4. Implement a component

`src/frontend-react/src/HelloPanel.tsx` is the component. It reads the user and locale, calls the `Greet` ability through the host and raises the `greeted` signal:

```tsx
export function HelloPanelView({ label }: HelloPanelProperties) {
    const user = useUser();
    const locale = useLocale();
    const invoke = useInvoke();
    const raise = useRaise();
    // ...
    const greet = async () => {
        const result = (await invoke('Greet', { name: user.name })) as { message: string };
        setMessage(result.message);
        raise('greeted', { message: result.message });
    };
```

The properties and signals must match the `component` lines in the contract. The hooks, the `react` and `mount` kinds and the conformance kit are in Scene's [extension component contract](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/contract.md), and [Build an extension component](https://github.com/Cratis/Scene/blob/main/Documentation/extensions/build-a-component.md) builds the same component in both kinds. A screen then places it through a system that references the package, as `component Hello.HelloPanel panel`; see [Extension components](screens.md#extension-components).

## 5. Add the icon, marketplace images and placeholders

- **Icon.** Replace `assets/icon.svg`. The manifest's `icon` is required, and the file must be `png`, `jpg`, `webp` or `svg` by its content.
- **Marketplace images.** Add `image "<path>"` lines, each with an optional child `caption`. The marketplace comes later. The manifest carries the images now.
- **Placeholders.** A `react` or `mount` component needs a `placeholder`, a static HTML file that design tools and the event model board draw in place of the real component. It has no script, no event handler attributes, no `javascript:` URLs and no external references. Styles are inline and images are `data:` URIs. A placeholder that breaks the rule is `PLAY1113`.

Make the placeholder look like the component with sample values, for example `Hello|Ada|en`. See [Components](extension-contracts.md#components).

## 6. Test, build and verify

```bash
cd Hello
scripts/test.sh      # the specifications of every part
scripts/build.sh     # backend/ and frontend/
scripts/package.sh   # dist/extension
cratis extension verify .
```

`cratis extension verify` runs the checks `contract`, `manifest`, `contents`, `placeholders`, `frontend` and `backend` and lists every one with a status. It exits `0` when none failed and `5` when one did. The `frontend` and `backend` checks have to run your code, which the CLI does not do yet, so they are reported as `skipped`, never as passed. The package's own specifications run the conformance kit, and Stage performs the protocol handshake when it loads the package. See [`cratis extension verify`](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-verify.md) for the checks, `.extensionignore` and `--strict`.

## 7. Publish

```bash
scripts/publish.sh 1.0.0
```

The script builds, packages, builds an image and pushes `ghcr.io/acme/extension-hello:1.0.0`. You need Docker and a login to the registry. Sign the pushed image, because Stage's trust policy requires a signature; the signature and the publisher rules are in the CLI's [trust section](https://github.com/Cratis/cli/blob/main/Documentation/reference/extension.md#trust) and Stage's [Extension packages](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/packages.md).

A frontend-only package, created with `--kind package --component react`, has no backend and publishes to npm with `npm publish`.

A model then references the package with `extension "cratis/hello" version 1` or by its full OCI reference. Once restored, its abilities and components are available to the model. See [Using an extension](using-an-extension.md).

## Until the SDKs are published

`Cratis.Stage.Extensions` is on NuGet. The Kotlin, Go and TypeScript backend SDKs and `@cratis/scene.extensions` were not yet on their registries when this page was written. The generated scripts accept `CRATIS_STAGE_SDK` and `CRATIS_SCENE_SDK` to use local builds. See the [templates page](https://github.com/Cratis/Templates/blob/main/Documentation/extensions/index.mdx#until-the-sdks-are-published).

## Next

- [Extension contracts](extension-contracts.md) is the reference for what you declared.
- [Execution tiers and activation](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/execution-tiers.md) explains how Stage runs your backend.
