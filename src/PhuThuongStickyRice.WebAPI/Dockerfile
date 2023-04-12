FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /PhuThuongStickyRice.Monolith

# Copy csproj and restore as distinct layers
COPY ./PhuThuongStickyRice.Application/*.csproj ./PhuThuongStickyRice.Application/
COPY ./PhuThuongStickyRice.ArchTests/*.csproj ./PhuThuongStickyRice.ArchTests/
COPY ./PhuThuongStickyRice.BackgroundServer/*.csproj ./PhuThuongStickyRice.BackgroundServer/
COPY ./PhuThuongStickyRice.Blazor.Modules/*.csproj ./PhuThuongStickyRice.Blazor.Modules/
COPY ./PhuThuongStickyRice.BlazorServerSide/*.csproj ./PhuThuongStickyRice.BlazorServerSide/
COPY ./PhuThuongStickyRice.BlazorWebAssembly/*.csproj ./PhuThuongStickyRice.BlazorWebAssembly/
COPY ./PhuThuongStickyRice.ContractTests/*.csproj ./PhuThuongStickyRice.ContractTests/
COPY ./PhuThuongStickyRice.CrossCuttingConcerns/*.csproj ./PhuThuongStickyRice.CrossCuttingConcerns/
COPY ./PhuThuongStickyRice.Domain/*.csproj ./PhuThuongStickyRice.Domain/
COPY ./PhuThuongStickyRice.EndToEndTests/*.csproj ./PhuThuongStickyRice.EndToEndTests/
COPY ./PhuThuongStickyRice.GraphQL/*.csproj ./PhuThuongStickyRice.GraphQL/
COPY ./PhuThuongStickyRice.Infrastructure/*.csproj ./PhuThuongStickyRice.Infrastructure/
COPY ./PhuThuongStickyRice.IntegrationTests/*.csproj ./PhuThuongStickyRice.IntegrationTests/
COPY ./PhuThuongStickyRice.Migrator/*.csproj ./PhuThuongStickyRice.Migrator/
COPY ./PhuThuongStickyRice.Persistence/*.csproj ./PhuThuongStickyRice.Persistence/
COPY ./PhuThuongStickyRice.UnitTests/*.csproj ./PhuThuongStickyRice.UnitTests/
COPY ./PhuThuongStickyRice.WebAPI/*.csproj ./PhuThuongStickyRice.WebAPI/
COPY ./PhuThuongStickyRice.WebMVC/*.csproj ./PhuThuongStickyRice.WebMVC/
COPY ./PhuThuongStickyRice.Monolith.sln .
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish ./PhuThuongStickyRice.WebAPI/PhuThuongStickyRice.WebAPI.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /PhuThuongStickyRice.Monolith
COPY --from=build-env /PhuThuongStickyRice.Monolith/out .

ENTRYPOINT ["dotnet", "PhuThuongStickyRice.WebAPI.dll"]