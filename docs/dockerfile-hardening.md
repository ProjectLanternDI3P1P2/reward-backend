# Dockerfile hardening

Oct 19, 2026 · @Jason

## Context

The existing `Dockerfile` already had solid foundations: a multi-stage build (the .NET SDK is only used for compilation, the final image is based on a lighter runtime) and a non-root user to run the application. The goal of this review was to go further: shrink the attack surface of the image that actually runs in production, without changing how the application behaves.

## Runtime image

The runtime image moves from `aspnet:10.0` to `aspnet:10.0-noble-chiseled`. A "chiseled" image is an Ubuntu Noble base stripped of the shell, the package manager and every OS package not essential to running a .NET application. Fewer components in the image means fewer potential CVEs to track and patch.

Direct consequence: there is no shell in the final image anymore, so `docker exec -it ... sh` can no longer be used to debug it in production. The project does not need one here (the `ENTRYPOINT` is already in exec form, not shell form), but it is an operational change worth keeping in mind.

## Other changes

- **`ENV DOTNET_EnableDiagnostics=0`**: turns off the .NET diagnostics IPC channel (dotnet-trace, dotnet-dump, dotnet-counters). Removes one way of interacting with the running process from outside in production.
- **`COPY --from=build --chown=$APP_UID:$APP_UID`**: published files are explicitly owned by the image's non-root user instead of inheriting `root` ownership from the build stage.
- **`.dockerignore`**: adds `.git/` and `.idea/`. Before this change, `COPY . .` in the build stage copied the full Git history and IDE config into the build context — never into the final image thanks to the multi-stage build, but it bloated the context sent to the Docker daemon and exposed history in a build layer that could be cached or exported.

## What was deliberately not done, and why

- **No SHA digest pinning** (`@sha256:...`) of the base images. The environment used for this work had no network access to `mcr.microsoft.com` (blocked by the egress policy). An invented digest would have silently broken the build — to be done separately, from a machine with normal internet access.
- **No Docker `HEALTHCHECK`.** The project targets Kubernetes with separate liveness/readiness probes (`/health/live`, `/health/ready`, see `docs/adr/0030-expose-separate-liveness-and-readiness-health-checks.md`), handled in the k8s manifest rather than by Docker's `HEALTHCHECK` instruction. Adding one here would duplicate the architecture already agreed on — and the chiseled image has no tool (`curl`, `wget`) to run an HTTP check anyway.

## To check before merging

- [ ] Run `docker build .` locally to confirm the `aspnet:10.0-noble-chiseled` tag exists for .NET 10 and the build succeeds end to end (not tested, due to lack of registry access during this work)
- [ ] If the tag does not exist, drop the `-noble-chiseled` suffix to fall back to the standard `aspnet:10.0` image
- [ ] Decide whether SHA digest pinning of the base images goes into a follow-up commit
