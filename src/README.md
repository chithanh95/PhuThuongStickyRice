# Solution Structure
![alt text](/docs/imgs/code-solution-structure.png)

# Add & Run Database Migration

- Update Connection Strings:

  | Project  | Configuration File | Configuration Key |
  | -------- | ------------------ | ----------------- |
  | PhuThuongStickyRice.Migrator | [appsettings.json](PhuThuongStickyRice.Migrator/appsettings.json) | ConnectionStrings:PhuThuongStickyRice |
  | PhuThuongStickyRice.BackgroundServer | [appsettings.json](PhuThuongStickyRice.BackgroundServer/appsettings.json) | ConnectionStrings:PhuThuongStickyRice |
  | PhuThuongStickyRice.IdentityServer | [appsettings.json](../IdentityServer/IdentityServer4/PhuThuongStickyRice.IdentityServer/appsettings.json) | ConnectionStrings:PhuThuongStickyRice |
  | PhuThuongStickyRice.WebAPI | [appsettings.json](PhuThuongStickyRice.WebAPI/appsettings.json) | ConnectionStrings:PhuThuongStickyRice |
  | PhuThuongStickyRice.WebMVC | [appsettings.json](PhuThuongStickyRice.WebMVC/appsettings.json) | ConnectionStrings:PhuThuongStickyRice |


- Run Migration:
  + Option 1: Using dotnet cli:
    + Install **dotnet-ef** cli:
      ```
      dotnet tool install --global dotnet-ef --version="5.0"
      ```
    + Navigate to [PhuThuongStickyRice.Migrator](PhuThuongStickyRice.Migrator/) and run these commands:
      ```
      dotnet ef migrations add Init --context StickyRiceDbContext -o Migrations/StickyRiceDb
      dotnet ef database update --context StickyRiceDbContext
      ```
  + Option 2: Using Package Manager Console:
    + Set **PhuThuongStickyRice.Migrator** as StartUp Project
    + Open Package Manager Console, select **PhuThuongStickyRice.Migrator** as Default Project
    + Run these commands:
      ```
      Add-Migration -Context StickyRiceDbContext Init -OutputDir Migrations/StickyRiceDb
      Update-Database -Context StickyRiceDbContext
      ```  

# Build & Run Locally using Tye

- Install Tye
  ```
  dotnet tool install -g Microsoft.Tye --version "0.5.0-alpha.20555.1"
  dotnet tool update -g Microsoft.Tye --version "0.5.0-alpha.20555.1"
  ```

- Install [Tye for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-tye)

- Run
  ```
  tye run
  ```
  
- Open Tye Dashboard: http://localhost:8000/

# Build & Deploy to Kubernetes

- Build
  ```
  docker-compose build
  ```

- Tag
  ```
  docker tag PhuThuongStickyRice.backgroundserver phongnguyend/PhuThuongStickyRice.backgroundserver
  docker tag PhuThuongStickyRice.migrator phongnguyend/PhuThuongStickyRice.migrator
  docker tag PhuThuongStickyRice.webapi phongnguyend/PhuThuongStickyRice.webapi
  docker tag PhuThuongStickyRice.graphql phongnguyend/PhuThuongStickyRice.graphql
  docker tag PhuThuongStickyRice.blazor phongnguyend/PhuThuongStickyRice.blazor
  docker tag PhuThuongStickyRice.identityserver phongnguyend/PhuThuongStickyRice.identityserver
  docker tag PhuThuongStickyRice.webmvc phongnguyend/PhuThuongStickyRice.webmvc
  ```

- Push
  ```
  docker push phongnguyend/PhuThuongStickyRice.backgroundserver
  docker push phongnguyend/PhuThuongStickyRice.migrator
  docker push phongnguyend/PhuThuongStickyRice.webapi
  docker push phongnguyend/PhuThuongStickyRice.graphql
  docker push phongnguyend/PhuThuongStickyRice.blazor
  docker push phongnguyend/PhuThuongStickyRice.identityserver
  docker push phongnguyend/PhuThuongStickyRice.webmvc
  ```

- Apply
  ```
  kubectl apply -f .k8s
  kubectl get all
  kubectl get services
  kubectl get pods
  ```

- Delete
  ```
  kubectl delete -f .k8s
  ```
  
- Use Helm
  ```
  helm install myrelease .helm/monolith
  helm list
  helm upgrade myrelease .helm/monolith
  ```

- UnInstall
  ```
  helm uninstall myrelease
  ```

# Build Nuget Packages using OctoPack

- Install OctoPack
  ```
  dotnet tool install Octopus.DotNet.Cli --global --version 4.39.1
  dotnet octo --version
  dotnet tool update Octopus.DotNet.Cli --global
  dotnet tool uninstall Octopus.DotNet.Cli --global
  dotnet tool install Octopus.DotNet.Cli --global --version <version>
  ```

- Build
  ```
  dotnet restore PhuThuongStickyRice.Monolith.sln

  dotnet build -p:Version=1.0.0.1 -c Release

  dotnet publish -p:Version=1.0.0.1 -c Release ../IdentityServer/IdentityServer4/PhuThuongStickyRice.IdentityServer/PhuThuongStickyRice.IdentityServer.csproj -o ./publish/PhuThuongStickyRice.IdentityServer
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.BackgroundServer/PhuThuongStickyRice.BackgroundServer.csproj -o ./publish/PhuThuongStickyRice.BackgroundServer
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.GraphQL/PhuThuongStickyRice.GraphQL.csproj -o ./publish/PhuThuongStickyRice.GraphQL
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.Migrator/PhuThuongStickyRice.Migrator.csproj -o ./publish/PhuThuongStickyRice.Migrator
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.WebAPI/PhuThuongStickyRice.WebAPI.csproj -o ./publish/PhuThuongStickyRice.WebAPI
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.BlazorServerSide/PhuThuongStickyRice.BlazorServerSide.csproj -o ./publish/PhuThuongStickyRice.BlazorServerSide
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.BlazorWebAssembly/PhuThuongStickyRice.BlazorWebAssembly.csproj -o ./publish/PhuThuongStickyRice.BlazorWebAssembly
  dotnet publish -p:Version=1.0.0.1 -c Release ./PhuThuongStickyRice.WebMVC/PhuThuongStickyRice.WebMVC.csproj -o ./publish/PhuThuongStickyRice.WebMVC
  ```

- Pack
  ```
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.IdentityServer --basePath=./publish/PhuThuongStickyRice.IdentityServer
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.BackgroundServer --basePath=./publish/PhuThuongStickyRice.BackgroundServer 
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.GraphQL --basePath=./publish/PhuThuongStickyRice.GraphQL
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.Migrator --basePath=./publish/PhuThuongStickyRice.Migrator
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.WebAPI --basePath=./publish/PhuThuongStickyRice.WebAPI
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.BlazorServerSide --basePath=./publish/PhuThuongStickyRice.BlazorServerSide
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.BlazorWebAssembly --basePath=./publish/PhuThuongStickyRice.BlazorWebAssembly
  dotnet octo pack --version=1.0.0.1 --outFolder=./publish --overwrite --id=PhuThuongStickyRice.WebMVC --basePath=./publish/PhuThuongStickyRice.WebMVC
  ```

# SonarQube

- Install Sonar Scanner
  ```
  dotnet tool install --global dotnet-sonarscanner
  dotnet tool list --global
  java --version
  ```

- Build & Scan
  ```
  dotnet sonarscanner begin /v:"1.0.0" /d:sonar.host.url="https://sonarcloud.io" /o:"phongnguyend" /k:"Monolith" /d:sonar.login="<token>"
  dotnet build PhuThuongStickyRice.Monolith.sln
  dotnet sonarscanner end /d:sonar.login="<token>"
  ```
