#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
#WORKDIR /app
#EXPOSE 80
#EXPOSE 443
#
#FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
#WORKDIR /src
#COPY ["MinimalApi/MinimalApi.csproj", "MinimalApi/"]
#RUN dotnet restore "MinimalApi/MinimalApi.csproj"
#COPY . .
#WORKDIR "/src/MinimalApi"
#RUN dotnet build "MinimalApi.csproj" -c Release -o /app/build
#
#FROM build AS publish
#RUN dotnet publish "MinimalApi.csproj" -c Release -o /app/publish /p:UseAppHost=false
#
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "MinimalApi.dll"]

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS sdk
WORKDIR /app

COPY ./ ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

COPY --from=sdk /app/out .
ENTRYPOINT ["dotnet", "MinimalApi.dll"]