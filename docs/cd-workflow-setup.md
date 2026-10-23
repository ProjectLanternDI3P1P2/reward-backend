# CD workflow setup

Oct 19, 2026 · @Jason

## Context

The project did not yet have a pipeline that publishes the API's Docker image to a registry. This workflow (`.github/workflows/cd.yaml`) adds that continuous delivery (CD) step, deliberately kept separate from the CI pipeline (`ci.yaml`) so its runs stay independently visible in the Actions tab.

Branch protection requires CI to pass before a pull request can reach `main`. CD then publishes that already-validated commit: it does not re-validate anything, it delivers.

## Triggers

The workflow runs in three cases:

- a push to `main`
- a `v*` version tag, created automatically by release-please once its release pull request is merged
- a manual run (`workflow_dispatch`), restricted to `main`

A concurrency group (`cd-docker-<ref>`) cancels any in-progress run on the same branch as soon as a new commit arrives, so a floating tag such as `:main` is never overwritten by older code still waiting in the queue. Runs triggered by a release tag are never cancelled.

## Workflow steps

1. Check out the code, without persisting credentials
2. Compute the image tags: `<image>:<release-tag>` for a `v*` tag, or `<image>:<branch>` + `<image>:<branch>-<short-sha>` for a branch push
3. Set up Docker Buildx
4. Authenticate to Docker Hub using the repository secrets
5. Build and push the image, with OCI labels pointing to the source repository and originating commit
6. Verify each published tag (`docker buildx imagetools inspect`)
7. Write a summary to the GitHub Actions Step Summary

## Required GitHub secrets

Two secrets must be set under Settings → Secrets and variables → Actions in the repository for the Docker Hub login step to work:

| Secret | Purpose |
| --- | --- |
| `DOCKERHUB_USERNAME` | Docker Hub account used for the push |
| `DOCKERHUB_TOKEN` | Docker Hub access token for that account |

Without these two secrets, the "Log in to Docker Hub" step fails and the workflow stops before the build.

## Post-publish verification

After every successful run:

- each published tag is explicitly checked with `docker buildx imagetools inspect` to confirm it is reachable on Docker Hub
- a summary (build source, image digest, list of tags) is written to the Summary tab of the GitHub Actions run, readable without digging through raw logs

The workflow never runs from a pull request, and manual runs are restricted to `main`: the Docker Hub credentials therefore cannot be used to publish an arbitrary branch.
