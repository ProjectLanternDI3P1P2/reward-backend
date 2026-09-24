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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080 8081

COPY --from=build /app/publish .

# Unprivileged "app" user shipped by the aspnet image.
USER $APP_UID

ENTRYPOINT ["dotnet", "Reward.Presentation.dll"]
