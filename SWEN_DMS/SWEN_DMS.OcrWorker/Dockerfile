FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SWEN_DMS.OcrWorker/SWEN_DMS.OcrWorker.csproj", "SWEN_DMS.OcrWorker/"]
RUN dotnet restore "SWEN_DMS.OcrWorker/SWEN_DMS.OcrWorker.csproj"
COPY . .
WORKDIR "/src/SWEN_DMS.OcrWorker"
RUN dotnet publish "SWEN_DMS.OcrWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SWEN_DMS.OcrWorker.dll"]
