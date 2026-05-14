# Multi-Targeting Support

## 📦 O que é Multi-Targeting?

**Multi-targeting** permite que um único pacote NuGet funcione em **múltiplas versões do .NET** simultaneamente. O CVAMF.Repository suporta:

- ✅ **.NET 9.0** (com EF Core 9.x)
- ✅ **.NET 10.0** (com EF Core 10.x)

## 🎯 Como Funciona?

Quando você instala o pacote em seu projeto, o NuGet **automaticamente seleciona** a DLL compatível com a versão do .NET do seu projeto:

```
CVAMF.Repository.1.4.0.nupkg
├── lib/
│   ├── net9.0/
│   │   └── CVAMF.Repository.dll (compilado para .NET 9 + EF Core 9.x)
│   └── net10.0/
│       └── CVAMF.Repository.dll (compilado para .NET 10 + EF Core 10.x)
```

### Exemplo Prático:

**Projeto usando .NET 9:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CVAMF.Repository" Version="1.4.0" />
    <!-- NuGet usa automaticamente lib/net9.0/CVAMF.Repository.dll -->
  </ItemGroup>
</Project>
```

**Projeto usando .NET 10:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CVAMF.Repository" Version="1.4.0" />
    <!-- NuGet usa automaticamente lib/net10.0/CVAMF.Repository.dll -->
  </ItemGroup>
</Project>
```

## ✅ Benefícios

### 1. **Compatibilidade Ampla**
- Um único pacote funciona em projetos .NET 9 e .NET 10
- Sem necessidade de versões separadas do pacote

### 2. **Otimização por Versão**
- Cada build usa a versão correta do Entity Framework Core
- .NET 9.0 → EF Core 9.x
- .NET 10.0 → EF Core 10.x

### 3. **Facilidade de Migração**
- Atualize seu projeto de .NET 9 para .NET 10 sem mudar o pacote
- O mesmo `CVAMF.Repository` funciona em ambos

### 4. **Manutenção Simplificada**
- Apenas um código-fonte para todas as versões
- Testes executados em todas as plataformas suportadas

## 🚀 Instalação

Não há diferença na instalação. Use o mesmo comando para qualquer versão do .NET:

```bash
dotnet add package CVAMF.Repository
```

Ou via NuGet Package Manager:

```
Install-Package CVAMF.Repository
```

O NuGet faz o resto automaticamente!

## 🔍 Verificando a Versão Usada

Para ver qual DLL está sendo usada em seu projeto:

```bash
# No diretório do seu projeto
dotnet list package --include-transitive | Select-String "CVAMF.Repository"
```

Ou inspecione a pasta de build:

```bash
# As DLLs ficam em bin/Debug ou bin/Release/{targetframework}
dir bin/Debug/net9.0/CVAMF.Repository.dll
dir bin/Debug/net10.0/CVAMF.Repository.dll
```

## 📋 Dependências por Versão

O pacote inclui as dependências corretas automaticamente:

### .NET 9.0
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="9.0.0" />
```

### .NET 10.0
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />
```

## ⚙️ Configuração Técnica

Para quem deseja entender como funciona internamente, veja o `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- TargetFrameworks (plural) define múltiplos targets -->
    <TargetFrameworks>net9.0;net10.0</TargetFrameworks>
  </PropertyGroup>

  <!-- Dependências condicionais por framework -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  </ItemGroup>
</Project>
```

## 🛠️ Build do Pacote

Ao compilar o projeto, todas as versões são buildadas automaticamente:

```bash
dotnet build -c Release

# Output:
# → bin/Release/net9.0/CVAMF.Repository.dll
# → bin/Release/net10.0/CVAMF.Repository.dll
# → bin/Release/CVAMF.Repository.1.4.0.nupkg (contendo ambas DLLs)
```

## 🔄 Migração entre Versões do .NET

### Cenário: Atualizando de .NET 9 para .NET 10

**Antes (.NET 9):**
```xml
<TargetFramework>net9.0</TargetFramework>
<PackageReference Include="CVAMF.Repository" Version="1.4.0" />
```

**Depois (.NET 10):**
```xml
<TargetFramework>net10.0</TargetFramework>
<!-- Mesma versão do pacote! -->
<PackageReference Include="CVAMF.Repository" Version="1.4.0" />
```

Basta mudar o `TargetFramework` do seu projeto. O NuGet automaticamente troca para a DLL correta.

## ❓ FAQ

### 1. **Posso usar em .NET 8?**
A versão atual (1.4.0) suporta apenas .NET 9 e 10. Se precisar de .NET 8, use uma versão anterior do pacote ou solicite suporte.

### 2. **O código é o mesmo para todas as versões?**
Sim! O código-fonte é idêntico. Apenas as dependências (EF Core) variam.

### 3. **Qual versão devo usar no meu projeto?**
Use a mesma versão do .NET que seu projeto está configurado. O NuGet cuida disso automaticamente.

### 4. **Posso ter projetos .NET 9 e .NET 10 na mesma solution?**
Sim! Cada projeto usará a DLL correta do pacote.

### 5. **O pacote fica maior com multi-targeting?**
Sim, ligeiramente, pois contém múltiplas DLLs. Mas o benefício de compatibilidade compensa.

### 6. **Preciso instalar múltiplas versões do pacote?**
Não! Um único pacote (`CVAMF.Repository 1.4.0`) funciona em todas as versões suportadas.

## 📊 Comparação

| Abordagem | Vantagens | Desvantagens |
|-----------|-----------|--------------|
| **Pacote Único (sem multi-targeting)** | Pacote menor | Funciona apenas em uma versão do .NET |
| **Múltiplos Pacotes** | Builds separados | Confusão de versões, manutenção complexa |
| **Multi-Targeting** ✅ | Compatibilidade ampla, fácil de usar | Pacote ligeiramente maior |

## 🎓 Recursos

- [Documentação oficial do .NET sobre Multi-Targeting](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
- [NuGet Multi-Targeting](https://learn.microsoft.com/en-us/nuget/create-packages/multiple-target-frameworks-project-file)

## 🚀 Conclusão

Multi-targeting torna o **CVAMF.Repository** compatível com diferentes versões do .NET **sem esforço adicional** para o usuário. Instale uma vez, funciona em qualquer versão suportada! 🎉
