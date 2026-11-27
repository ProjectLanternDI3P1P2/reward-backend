# Own and run database migrations per microservice

Each microservice owns and versions its own Entity Framework Core migrations.

Developers create migrations with EF Core tooling when the service data model
changes.

Application startup does not automatically execute `Database.Migrate()` outside
development environments.

Deployed environments use a dedicated migration execution mechanism or service.

## Considered Options

Running migrations automatically when every application instance starts is simple,
but it couples normal startup to schema mutation and becomes unsafe when several
instances start concurrently.

Sharing migrations between services would contradict database ownership and
couple independently evolving schemas.

A dedicated migration execution step keeps schema changes explicit and separate
from serving application traffic.

## Consequences

Migration files live with the persistence code of the microservice that owns the
database.

Production-like deployments must execute migrations before or as a controlled
part of rollout.

Each service can evolve its schema independently without coordinating a shared
migration project.

## GitOps migration command

The published API image accepts `--migrate`. It applies this service's pending
EF migrations and exits without starting the API or seeding Development data.
A missing migration assembly or database/configuration error exits nonzero.
Normal production startup still does not migrate the database.

GitOps runs this mode in a dedicated PreSync Job using the same immutable image
as the Deployment, with schema-specific connection settings and a mounted
password file. Existing EF migration locking serializes runs for the same
migration history. Review schema compatibility with the currently running API
before release; rolling back an image never rolls back the schema.

The test workflow exercises the published command on a disposable PostgreSQL
database, including repeat execution and missing-credentials failure. No
production database is used by that check.
