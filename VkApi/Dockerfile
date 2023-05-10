FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["VkApi/VkApi.csproj", "VkApi/"]
RUN dotnet restore "VkApi/VkApi.csproj"
COPY . .
WORKDIR "/src/VkApi"
RUN dotnet build "VkApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VkApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VkApi.dll"]
