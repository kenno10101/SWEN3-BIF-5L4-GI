FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SWEN_DMS/SWEN_DMS.csproj", "SWEN_DMS/"]
COPY ["SWEN_DMS.BLL/SWEN_DMS.BLL.csproj", "SWEN_DMS.BLL/"]
COPY ["SWEN_DMS.DAL/SWEN_DMS.DAL.csproj", "SWEN_DMS.DAL/"]
COPY ["SWEN_DMS.Models/SWEN_DMS.Models.csproj", "SWEN_DMS.Models/"]
COPY ["SWEN_DMS.DTOs/SWEN_DMS.DTOs.csproj", "SWEN_DMS.DTOs/"]
RUN dotnet restore "SWEN_DMS/SWEN_DMS.csproj"
COPY . .
WORKDIR "/src/SWEN_DMS"
RUN dotnet build "SWEN_DMS.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SWEN_DMS.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SWEN_DMS.dll"]
