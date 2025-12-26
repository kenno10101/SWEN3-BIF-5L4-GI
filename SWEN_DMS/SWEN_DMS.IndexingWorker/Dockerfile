FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["SWEN_DMS.IndexingWorker/SWEN_DMS.IndexingWorker.csproj", "SWEN_DMS.IndexingWorker/"]

# Restore dependencies
RUN dotnet restore "SWEN_DMS.IndexingWorker/SWEN_DMS.IndexingWorker.csproj"

# Copy source code
COPY . .

# Build the project
WORKDIR "/src/SWEN_DMS.IndexingWorker"
RUN dotnet build "SWEN_DMS.IndexingWorker.csproj" -c Release -o /app/build

# Publish the project
FROM build AS publish
RUN dotnet publish "SWEN_DMS.IndexingWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SWEN_DMS.IndexingWorker.dll"]