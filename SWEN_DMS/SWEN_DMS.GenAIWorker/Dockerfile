FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SWEN_DMS.GenAIWorker/SWEN_DMS.GenAIWorker.csproj", "SWEN_DMS.GenAIWorker/"]
COPY ["SWEN_DMS.DTOs/SWEN_DMS.DTOs.csproj", "SWEN_DMS.DTOs/"]
RUN dotnet restore "SWEN_DMS.GenAIWorker/SWEN_DMS.GenAIWorker.csproj"
COPY . .
WORKDIR "/src/SWEN_DMS.GenAIWorker"
RUN dotnet publish "SWEN_DMS.GenAIWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SWEN_DMS.GenAIWorker.dll"]
