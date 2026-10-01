FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Reward.Presentation.slnx ./
COPY Reward.Contracts/Reward.Contracts.csproj Reward.Contracts/
COPY Reward.Domain/Reward.Domain.csproj Reward.Domain/
COPY Reward.Application/Reward.Application.csproj Reward.Application/
COPY Reward.Infrastructure/Reward.Infrastructure.csproj Reward.Infrastructure/
COPY Reward.Presentation/Reward.Presentation.csproj Reward.Presentation/
COPY Reward.Test/Reward.Test.csproj Reward.Test/

RUN dotnet restore Reward.Presentation.slnx

COPY . .
RUN dotnet publish Reward.Presentation/Reward.Presentation.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# Chiseled: Ubuntu Noble base stripped of the shell, package manager and every
# non-essential OS package. Smaller attack surface and fewer CVEs to patch than
# the standard Debian-based runtime image. There is no shell to exec into for
# debugging, which is acceptable since nothing after this point needs one.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime
WORKDIR /app

EXPOSE 8080 8081

# Disables the .NET diagnostics IPC channel (dotnet-trace/dotnet-dump/dotnet-counters
# attachment). Nothing in this container should be profiled remotely in production;
# this removes one more way to interact with the running process from outside it.
ENV DOTNET_EnableDiagnostics=0

# --chown: the chiseled image's non-root "app" user ($APP_UID) owns its own files
# instead of inheriting root ownership from the build stage's COPY.
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

# Unprivileged "app" user shipped by the aspnet image.
USER $APP_UID

ENTRYPOINT ["dotnet", "Reward.Presentation.dll"]
