# Soft Delete Usage Guide

O **Soft Delete** permite marcar registros como excluídos sem removê-los fisicamente do banco de dados. Isso é útil para:
- Auditoria e histórico de dados
- Possibilidade de restauração
- Compliance e regulamentações
- Análise de dados históricos

## Escolhendo o Nome do Campo

Este repositório suporta **duas convenções de nomenclatura** para o campo de soft delete:

### 1. Usando `IsDeleted` (Padrão Recomendado)

```csharp
using CVAMF.Repository.Entities;

public class Product : EntityBaseSoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // IsDeleted, DeletedAt e DeletedBy são herdados de EntityBaseSoftDelete
}
```

**Ou implemente a interface diretamente:**

```csharp
public class Product : EntityBase, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Propriedades obrigatórias da interface ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### 2. Usando `Deleted` (Convenção Alternativa)

```csharp
public class Product : EntityBaseSoftDeleteAlt
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Deleted, DeletedAt e DeletedBy são herdados de EntityBaseSoftDeleteAlt
}
```

**Ou implemente a interface alternativa:**

```csharp
public class Product : EntityBase, ISoftDeletableAlternative
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Propriedades obrigatórias da interface ISoftDeletableAlternative
    public bool Deleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## Classes Base Disponíveis

### Para Primary Key `Guid`:

- `EntityBaseSoftDelete` - Usa `IsDeleted`
- `EntityBaseSoftDeleteAlt` - Usa `Deleted`

### Para Primary Key `int`:

- `EntityBaseSoftDeleteInt` - Usa `IsDeleted`
- `EntityBaseSoftDeleteAltInt` - Usa `Deleted`

## Usando Soft Delete

### 1. Soft Delete de uma Entidade

```csharp
var product = await _productRepository.GetByIdAsync(productId);
if (product != null)
{
    // Marca como deletado e registra quem deletou
    await _productRepository.SoftDeleteAsync(product, "admin@example.com");
    await _productRepository.SaveChangesAsync();
}
```

### 2. Soft Delete por ID

```csharp
var deleted = await _productRepository.SoftDeleteAsync(productId, "admin@example.com");
if (deleted)
{
    await _productRepository.SaveChangesAsync();
    Console.WriteLine("Produto soft deleted com sucesso!");
}
else
{
    Console.WriteLine("Produto não encontrado ou não suporta soft delete");
}
```

### 3. Soft Delete em Lote

```csharp
var oldProducts = await _productRepository.GetAsync(
    filter: p => p.CreatedAt < DateTime.UtcNow.AddYears(-5));

var count = await _productRepository.SoftDeleteRangeAsync(oldProducts, "system");
await _productRepository.SaveChangesAsync();
Console.WriteLine($"{count} produtos foram soft deleted");
```

### 4. Restaurar uma Entidade Soft Deleted

```csharp
// Buscar incluindo deletados (depende da sua implementação de query)
var product = await _productRepository.GetByIdAsync(productId);

if (product != null)
{
    var restored = await _productRepository.RestoreAsync(product);
    if (restored)
    {
        await _productRepository.SaveChangesAsync();
        Console.WriteLine("Produto restaurado com sucesso!");
    }
}
```

### 5. Restaurar por ID

```csharp
var restored = await _productRepository.RestoreAsync(productId);
if (restored)
{
    await _productRepository.SaveChangesAsync();
}
```

## Filtrar Registros Deletados

Para excluir registros soft deleted das consultas, adicione um filtro:

```csharp
// Para ISoftDeletable (IsDeleted)
var activeProducts = await _productRepository.GetAsync(
    filter: p => !p.IsDeleted && p.Price > 0,
    orderBy: q => q.OrderBy(p => p.Name));

// Para ISoftDeletableAlternative (Deleted)
var activeProducts = await _productRepository.GetAsync(
    filter: p => !p.Deleted && p.Price > 0,
    orderBy: q => q.OrderBy(p => p.Name));
```

## Global Query Filter (Recomendado)

Para filtrar automaticamente registros soft deleted em **todas** as consultas, configure um **Global Query Filter** no seu `DbContext`:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Para entidades que usam ISoftDeletable (IsDeleted)
        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => !p.IsDeleted);

        // Para entidades que usam ISoftDeletableAlternative (Deleted)
        // modelBuilder.Entity<Product>()
        //     .HasQueryFilter(p => !p.Deleted);
    }
}
```

### Ignorar o Filtro Global Quando Necessário

```csharp
// Incluir registros deletados em uma consulta específica
var allProducts = await _context.Products
    .IgnoreQueryFilters()
    .ToListAsync();
```

## Exemplo Completo com UnitOfWork

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> DeleteProductAsync(Guid productId, string userName)
    {
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        var deleted = await productRepo.SoftDeleteAsync(productId, userName);

        if (deleted)
        {
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> RestoreProductAsync(Guid productId)
    {
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        var restored = await productRepo.RestoreAsync(productId);

        if (restored)
        {
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        // Se você configurou Global Query Filter, não precisa filtrar manualmente
        return await productRepo.GetAllAsync();

        // Caso contrário, filtre manualmente:
        // return await productRepo.GetAsync(filter: p => !p.IsDeleted);
    }
}
```

## Quando Usar Soft Delete vs Delete Físico

### Use Soft Delete quando:
- ✅ Precisa manter histórico para auditoria
- ✅ Pode precisar restaurar dados deletados
- ✅ Existem regulamentações de compliance
- ✅ Relacionamentos complexos que não devem ser quebrados
- ✅ Análise de dados históricos é importante

### Use Delete Físico quando:
- ❌ Dados sensíveis que devem ser permanentemente removidos (GDPR, LGPD)
- ❌ Dados temporários ou cache
- ❌ Volume de dados é crítico (performance)
- ❌ Não há necessidade de auditoria
- ❌ Dados de teste ou desenvolvimento

## Migrando de Delete Físico para Soft Delete

1. **Adicione as propriedades à entidade:**

```csharp
public class Product : EntityBase
{
    // Propriedades existentes...

    // Novas propriedades para soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

2. **Crie uma migration:**

```bash
dotnet ef migrations add AddSoftDeleteToProduct
dotnet ef database update
```

3. **Configure o Global Query Filter:**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasQueryFilter(p => !p.IsDeleted);
}
```

4. **Substitua chamadas de `DeleteAsync` por `SoftDeleteAsync`:**

```csharp
// Antes
await _productRepository.DeleteAsync(product);

// Depois
await _productRepository.SoftDeleteAsync(product, currentUser);
```

## Performance e Índices

Para melhor performance em tabelas com soft delete, crie índices:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasIndex(p => p.IsDeleted)
        .HasFilter("IsDeleted = 0"); // SQL Server filter index

    // Ou para Deleted
    // .HasIndex(p => p.Deleted)
    // .HasFilter("Deleted = 0");
}
```

## Observações Importantes

1. **Compatibilidade**: O repositório detecta automaticamente qual interface sua entidade implementa (`ISoftDeletable` ou `ISoftDeletableAlternative`)

2. **Retorno dos Métodos**: 
   - `SoftDeleteAsync` retorna `true` se a entidade suporta soft delete, `false` caso contrário
   - `RestoreAsync` retorna `true` se a entidade foi restaurada, `false` caso contrário

3. **Entidades sem Soft Delete**: Se chamar `SoftDeleteAsync` em uma entidade que não implementa as interfaces, o método retornará `false` e nenhuma ação será executada

4. **Timezone**: `DeletedAt` sempre usa `DateTime.UtcNow` para consistência

5. **Limpeza**: Considere criar jobs periódicos para deletar fisicamente registros soft deleted antigos:

```csharp
public async Task PermanentlyDeleteOldRecordsAsync()
{
    var oldDeletedProducts = await _context.Products
        .IgnoreQueryFilters()
        .Where(p => p.IsDeleted && p.DeletedAt < DateTime.UtcNow.AddYears(-7))
        .ToListAsync();

    _context.Products.RemoveRange(oldDeletedProducts);
    await _context.SaveChangesAsync();
}
```
