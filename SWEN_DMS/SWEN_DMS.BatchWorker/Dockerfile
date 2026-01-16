FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SWEN_DMS.BatchWorker/SWEN_DMS.BatchWorker.csproj", "SWEN_DMS.BatchWorker/"]
COPY ["SWEN_DMS.DAL/SWEN_DMS.DAL.csproj", "SWEN_DMS.DAL/"]
COPY ["SWEN_DMS.Models/SWEN_DMS.Models.csproj", "SWEN_DMS.Models/"]

RUN dotnet restore "SWEN_DMS.BatchWorker/SWEN_DMS.BatchWorker.csproj"

COPY . .

WORKDIR "/src/SWEN_DMS.BatchWorker"
RUN dotnet publish "SWEN_DMS.BatchWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SWEN_DMS.BatchWorker.dll"]
