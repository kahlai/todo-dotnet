#sample from https://github.com/docker/awesome-compose/blob/master/aspnet-mssql/app/aspnetapp/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 as base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . /src
WORKDIR /src
RUN ls
RUN dotnet build "TodoApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TodoApi.csproj" -c Release -o /app/publish

FROM base AS final
#LABEL io.openshift.expose-services 8080/tcp:http
#ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TodoApi.dll"]
