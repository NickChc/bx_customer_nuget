FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/BiznetX.Customer/BiznetX.Customer.csproj --locked-mode
RUN dotnet pack src/BiznetX.Customer/BiznetX.Customer.csproj -c Release --no-restore -o /packages
FROM scratch AS artifacts
COPY --from=build /packages/ /
