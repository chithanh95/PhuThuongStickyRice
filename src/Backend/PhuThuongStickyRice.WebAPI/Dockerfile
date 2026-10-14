FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ./Backend/PhuThuongStickyRice.Domain/*.csproj ./Backend/PhuThuongStickyRice.Domain/
COPY ./Backend/PhuThuongStickyRice.Application/*.csproj ./Backend/PhuThuongStickyRice.Application/
COPY ./Backend/PhuThuongStickyRice.Infrastructure/*.csproj ./Backend/PhuThuongStickyRice.Infrastructure/
COPY ./Backend/PhuThuongStickyRice.Persistence/*.csproj ./Backend/PhuThuongStickyRice.Persistence/
COPY ./Backend/PhuThuongStickyRice.CrossCuttingConcerns/*.csproj ./Backend/PhuThuongStickyRice.CrossCuttingConcerns/
COPY ./Backend/PhuThuongStickyRice.WebAPI/*.csproj ./Backend/PhuThuongStickyRice.WebAPI/
COPY ./Backend/PhuThuongStickyRice.BackgroundServer/*.csproj ./Backend/PhuThuongStickyRice.BackgroundServer/
COPY ./Backend/PhuThuongStickyRice.Migrator/*.csproj ./Backend/PhuThuongStickyRice.Migrator/
COPY ./Backend/PhuThuongStickyRice.GraphQL/*.csproj ./Backend/PhuThuongStickyRice.GraphQL/
COPY ./Frontend/PhuThuongStickyRice.Web.Components/*.csproj ./Frontend/PhuThuongStickyRice.Web.Components/
COPY ./Frontend/PhuThuongStickyRice.Web/*.csproj ./Frontend/PhuThuongStickyRice.Web/
COPY ./Frontend/PhuThuongStickyRice.BlazorServerSide/*.csproj ./Frontend/PhuThuongStickyRice.BlazorServerSide/
COPY ./Frontend/PhuThuongStickyRice.WebMVC/*.csproj ./Frontend/PhuThuongStickyRice.WebMVC/

RUN dotnet restore ./PhuThuongStickyRice.WebAPI/PhuThuongStickyRice.WebAPI.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish ./Backend/PhuThuongStickyRice.WebAPI/PhuThuongStickyRice.WebAPI.csproj -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "PhuThuongStickyRice.WebAPI.dll"]
