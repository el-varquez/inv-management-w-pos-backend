### Building and running your application

The Compose stack has two services: `server` (this API) and `db` (PostgreSQL 16,
data persisted in the `db-data` volume). The API connects to the database over the
Compose network at `Host=db`, and applies EF Core migrations automatically on
startup, so the schema is created on first run.

Before starting, make sure `.env` exists (copy from `.env.example`) and sets:

- `POSTGRES_PASSWORD` — password for the bundled `db` service; Compose interpolates
  it into both the database and the server's connection string.
- `Jwt__Key` — at least 32 chars; Compose passes it to the server container.

Then start everything with:
`docker compose up --build`.

Your application will be available at http://localhost:8080.

To create the initial super admin (after the stack is up):
`docker compose exec server dotnet POS.API.dll create-admin`.

### Deploying your application to the cloud

First, build your image, e.g.: `docker build -t myapp .`.
If your cloud uses a different CPU architecture than your development
machine (e.g., you are on a Mac M1 and your cloud provider is amd64),
you'll want to build the image for that platform, e.g.:
`docker build --platform=linux/amd64 -t myapp .`.

Then, push it to your registry, e.g. `docker push myregistry.com/myapp`.

Consult Docker's [getting started](https://docs.docker.com/go/get-started-sharing/)
docs for more detail on building and pushing.

### References
* [Docker's .NET guide](https://docs.docker.com/language/dotnet/)
* The [dotnet-docker](https://github.com/dotnet/dotnet-docker/tree/main/samples)
  repository has many relevant samples and docs.