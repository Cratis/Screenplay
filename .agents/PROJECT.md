# Studio — Project-Specific Instructions

Project-local conventions for this repository. When this file conflicts with the shared `.ai/` corpus, this file wins for this repo.

## Documentation Comments

- Do **not** add XML doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`, etc.) to any C# code.
- Do **not** add JSDoc comments (`/** ... */`, `@param`, `@returns`, etc.) to any TypeScript or TSX code.
- Inline `//` comments are fine where the logic is non-obvious, but never add XML or JSDoc doc-comment blocks.

## Browser Testing Headers (Alice)

- If using the browser to try things out locally, always set the `X-MS-CLIENT-*` headers for Alice.
- For browser automation, set the three headers on the browser context before navigating to the page, and reapply them if a new browser context/page is created.
- When validating API calls in terminal commands (`curl`), include the same three headers in every request.

Plain text headers (Alice Admin):

```
X-MS-CLIENT-PRINCIPAL-ID: 00000000-2000-0000-0000-000000000001
X-MS-CLIENT-PRINCIPAL-NAME: Alice Admin
X-MS-CLIENT-PRINCIPAL: eyJhdXRoX3R5cCI6ImFhZCIsImNsYWltcyI6W3sidHlwIjoiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiLCJ2YWwiOiIwMDAwMDAwMC0yMDAwLTAwMDAtMDAwMC0wMDAwMDAwMDAwMDEifSx7InR5cCI6Im5hbWUiLCJ2YWwiOiJBbGljZSBBZG1pbiJ9LHsidHlwIjoicHJlZmVycmVkX3VzZXJuYW1lIiwidmFsIjoiYWxpY2VAZXhhbXBsZS5jb20ifV0sIm5hbWVfdHlwIjoiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSIsInJvbGVfdHlwIjoiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIn0=
```

## Deploying to Production Manually (from a shell)

The `Publish`/`Deploy - Production` GitHub Actions workflows are the normal path, but a manual deploy from a local shell is sometimes necessary (e.g. no PR/label-triggered release, or CI itself needs a fix first). This section is the recipe, plus every gotcha hit doing this for real — all confirmed against the live `studio-production` cluster.

### Credentials

All secrets live in `~/.studio-deploy.env` (outside the repo, never committed — see its header comment). Before anything else:

```bash
source ~/.studio-deploy.env
```

This provides `UPCLOUD_TOKEN`, `PULUMI_CONFIG_PASSPHRASE`, `REGISTRY_PASSWORD`, and the OIDC app secrets. It does **not** contain the registry username (see below) or a NuGet PAT for pushing prerelease packages (see the local-framework-package gotcha below) — both live only in `/Volumes/Code/NuGet.config` (a parent-directory NuGet config with the `ChroniclePreReleases` GitHub Packages feed credentials) and the Docker credential store.

### Getting a working kubeconfig

Do **not** trust a cached `~/.kube/config` — the control-plane load balancer's hostname changes when the LB is recreated, and a stale entry fails as a silent-looking DNS `NXDOMAIN`/connection timeout that is easy to mistake for a network/firewall block. Always fetch fresh from the UpCloud API:

```bash
source ~/.studio-deploy.env
CLUSTER_UUID=0df2e1df-89e6-4178-bd72-04dfea578071   # studio-production
curl -sS -H "Authorization: Bearer $UPCLOUD_TOKEN" \
  "https://api.upcloud.com/1.3/kubernetes/$CLUSTER_UUID/kubeconfig" \
  | python3 -c "import json,sys; print(json.load(sys.stdin)['kubeconfig'])" > /tmp/studio-production-kubeconfig.yaml
export KUBECONFIG=/tmp/studio-production-kubeconfig.yaml
kubectl get nodes   # sanity check
```

The app namespace is `studio-production`; deployments are named `core`, `admin`, `lobby` (container name matches the deployment name).

### Registry — the docs say port 5000, production actually uses 443

`Documentation/deployment/registry.md` describes a self-signed cert on port 5000. The **live** registry (`registry.cratis.studio`, a standalone VM, "managed by terraform" — not the `Registry/` Pulumi project) was since migrated to a real Let's Encrypt cert on the default HTTPS port. Port 5000 is firewalled (silently dropped, not refused — which looks exactly like a network block and cost real time to diagnose). Always use the bare hostname (port 443, no `:5000`):

```bash
source ~/.studio-deploy.env
echo "$REGISTRY_PASSWORD" | docker login registry.cratis.studio -u admin --password-stdin
```

The username (`admin`) isn't in the env file — it's stored in the local Docker credential helper from a prior login (`echo "registry.cratis.studio:5000" | docker-credential-desktop get`) if you need to recover it.

### Building images locally

The deployed image tags in the cluster follow an ad-hoc `0.0.0-subjectN` pattern (not `:latest`, despite what `Deployment/Pulumi.production.yaml` shows) — bump `N` for each manual build. Only rebuild the services you actually changed (check `kubectl get deployment <name> -o jsonpath='{.spec.template.spec.containers[0].image}'` for the currently-running tag/version first).

**Always pass `--platform linux/amd64` explicitly.** A plain `docker build` on Apple Silicon defaults to `arm64`, which builds and pushes fine but then fails at deploy time with `ImagePullBackOff` / `no match for platform in manifest` — a confusing failure that looks like a registry or auth problem but is purely an architecture mismatch (the cluster nodes are `amd64`).

```bash
COMMIT=$(git rev-parse --short HEAD)
docker build --platform linux/amd64 -f Source/Core/Dockerfile \
  -t registry.cratis.studio/core:0.0.0-subjectN \
  --build-arg VERSION=0.0.0-subjectN --build-arg COMMIT=$COMMIT .
docker push registry.cratis.studio/core:0.0.0-subjectN
```

Same for `Source/Lobby/Dockerfile` → `lobby`, `Source/Admin/Dockerfile` → `admin`.

#### Gotcha: local-only prerelease framework packages

If `Directory.Packages.props` pins a `Cratis.*`/`Cratis.Chronicle.*`/`Cratis.Fundamentals` version that only exists in a per-machine local NuGet feed (check `dotnet nuget list source` for `*-local` entries), the Docker build's isolated restore can't see it and fails with `NU1102`. Either:

- Push the package(s) to the shared `ChroniclePreReleases` GitHub Packages feed (credentials in `/Volumes/Code/NuGet.config`) — needs a PAT with `write:packages` scope, which the stored one may not have (the `gh` CLI token doesn't either; check `gh auth status` scopes), **or**
- Temporarily vendor the `.nupkg` files into the build context and add a repo-root `nuget.config` pointing at them, with a matching temporary `COPY` line in the Dockerfile — build, then revert the Dockerfile and delete both before doing anything else (never commit a `nuget.config` containing the `ChroniclePreReleases` PAT).

### Rolling out

```bash
kubectl set image deployment/core core=registry.cratis.studio/core:0.0.0-subjectN -n studio-production
kubectl rollout restart deployment/core -n studio-production   # needed even after set image, if the tag string is reused
kubectl rollout status deployment/core -n studio-production --timeout=180s
```

`kubectl set image` alone does **not** trigger a new pull+rollout if the image string is unchanged from a prior failed attempt (e.g. after fixing the platform and rebuilding under the *same* tag) — follow it with `rollout restart`.

### Known live infra gotchas (already fixed once, may recur)

- **Speaches (`transcription` deployment) STT cache directory ownership**: its persistent volume at `/home/ubuntu/.cache/huggingface` can end up owned by `root` while the container runs as `ubuntu` (uid 1000), so the app can never create its model cache and every transcription fails with `huggingface_hub.errors.CacheNotFound`. Fix via a root debug container attached to the pod: `kubectl debug -n studio-production <pod> -it --image=busybox --target=transcription -- chown -R 1000:1000 /proc/1/root/home/ubuntu/.cache/huggingface`, then re-trigger a model pull (`POST /v1/models/<model-id>` on the pod). Worth an init container fix (matching the existing `trust-chronicle-ca` init container pattern) so this can't recur silently.
- **Ollama (`llm` deployment)** has no model-pull step in its deployment or persistent volume init — if the volume is ever fresh/empty, chat requests 404 until a model is pulled. `ChatClient.cs`'s `OllamaChatClient` self-heals this automatically (pulls on a 404 and retries once), mirroring the equivalent Speaches self-heal already in `SpeechRecognizer.cs`.
