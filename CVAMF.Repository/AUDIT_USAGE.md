# Audit Fields Usage Guide

Os **Audit Fields** (campos de auditoria) permitem rastrear automaticamente **quem** e **quando** criou ou modificou uma entidade. Isso é essencial para:
- Auditoria e compliance
- Rastreabilidade de mudanças
- Segurança e accountability
- Análise de comportamento de usuários
- Troubleshooting e debugging

## ✨ Características Principais

- ✅ **Totalmente Opcional**: Não é obrigatório usar audit fields
- ✅ **Automático**: Campos preenchidos automaticamente pelo repositório
- ✅ **Flexível**: Use apenas nas entidades que precisam de auditoria
- ✅ **Compatível**: Funciona com todas as outras features (Soft Delete, Include, AsNoTracking)
- ✅ **Timezone Consistente**: Sempre usa `DateTime.UtcNow`

## 📋 Campos de Auditoria

A interface `IAuditable` define quatro campos:

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }       // Quando foi criado
    string? CreatedBy { get; set; }        // Quem criou (username, email, userId)
    DateTime? UpdatedAt { get; set; }      // Quando foi atualizado pela última vez
    string? UpdatedBy { get; set; }        // Quem atualizou
}
```

## 🏗️ Classes Base Disponíveis

### Apenas Audit Fields

#### Para Primary Key `Guid`:
```csharp
public class Product : EntityBaseAuditable
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy são herdados
}
```

#### Para Primary Key `int`:
```csharp
public class Category : EntityBaseAuditableInt
{
    public string Name { get; set; } = string.Empty;

    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy são herdados
}
```

### Audit Fields + Soft Delete (Tudo Junto!)

#### Para Primary Key `Guid`:
```csharp
// Com IsDeleted
public class Product : EntityBaseAuditableSoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Audit: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    // Soft Delete: IsDeleted, DeletedAt, DeletedBy
}

// Com Deleted (alternativa)
public class Product : EntityBaseAuditableSoftDeleteAlt
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Audit: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
    // Soft Delete: Deleted, DeletedAt, DeletedBy
}
```

#### Para Primary Key `int`:
```csharp
// Com IsDeleted
public class Category : EntityBaseAuditableSoftDeleteInt
{
    public string Name { get; set; } = string.Empty;

    // Audit + Soft Delete fields herdados
}

// Com Deleted (alternativa)
public class Category : EntityBaseAuditableSoftDeleteAltInt
{
    public string Name { get; set; } = string.Empty;

    // Audit + Soft Delete fields herdados
}
```

### Implementação Manual da Interface

Se preferir não usar classes base:

```csharp
public class Product : EntityBase, IAuditable
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Implementação manual da IAuditable
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

## 💡 Como Usar

### 1. Criação com Audit (Automático)

```csharp
var product = new Product
{
    Name = "Laptop",
    Price = 1299.99m
};

// O repositório preenche CreatedAt e CreatedBy automaticamente
await _productRepository.AddAsync(product, "admin@example.com");
await _productRepository.SaveChangesAsync();

// Resultado:
// product.CreatedAt = DateTime.UtcNow
// product.CreatedBy = "admin@example.com"
```

### 2. Criação em Lote com Audit

```csharp
var products = new List<Product>
{
    new Product { Name = "Mouse", Price = 29.99m },
    new Product { Name = "Keyboard", Price = 79.99m },
    new Product { Name = "Monitor", Price = 299.99m }
};

// Todos receberão o mesmo CreatedAt e CreatedBy
await _productRepository.AddRangeAsync(products, "admin@example.com");
await _productRepository.SaveChangesAsync();
```

### 3. Atualização com Audit

```csharp
var product = await _productRepository.GetByIdAsync(productId);
if (product != null)
{
    product.Price = 999.99m;

    // O repositório preenche UpdatedAt e UpdatedBy automaticamente
    await _productRepository.UpdateAsync(product, "manager@example.com");
    await _productRepository.SaveChangesAsync();

    // Resultado:
    // product.UpdatedAt = DateTime.UtcNow
    // product.UpdatedBy = "manager@example.com"
}
```

### 4. Atualização em Lote com Audit

```csharp
var products = await _productRepository.GetAsync(
    filter: p => p.Category == "Electronics",
    includes: null,
    asNoTracking: false); // tracking necessário para update

foreach (var product in products)
{
    product.Price *= 0.9m; // 10% discount
}

await _productRepository.UpdateRangeAsync(products, "promotion@example.com");
await _productRepository.SaveChangesAsync();
```

### 5. Sem Informação de Auditoria (Opcional)

Se você não passar o parâmetro, os campos de audit não serão preenchidos automaticamente:

```csharp
// Método sem audit
await _productRepository.AddAsync(product);

// Ou passar null explicitamente
await _productRepository.AddAsync(product, createdBy: null);
```

## 🔗 Combinando com Outras Features

### Audit + Include + AsNoTracking

```csharp
// Buscar para atualizar (sem AsNoTracking)
var order = await _orderRepository.GetByIdAsync(
    orderId,
    includes: q => q.Include(o => o.Items),
    asNoTracking: false);

if (order != null)
{
    order.Status = "Shipped";
    await _orderRepository.UpdateAsync(order, "shipping@example.com");
    await _orderRepository.SaveChangesAsync();
}
```

### Audit + Soft Delete

```csharp
var product = await _productRepository.GetByIdAsync(productId);
if (product != null)
{
    // Soft delete também registra quem deletou
    await _productRepository.SoftDeleteAsync(product, "admin@example.com");
    await _productRepository.SaveChangesAsync();

    // Resultado:
    // product.IsDeleted = true
    // product.DeletedAt = DateTime.UtcNow
    // product.DeletedBy = "admin@example.com"
    // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy permanecem inalterados
}
```

### Audit + UnitOfWork

```csharp
public class ProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Product> CreateProductAsync(string name, decimal price, string userName)
    {
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        var product = new Product
        {
            Name = name,
            Price = price
        };

        await productRepo.AddAsync(product, userName);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<bool> UpdateProductPriceAsync(Guid productId, decimal newPrice, string userName)
    {
        var productRepo = _unitOfWork.Repository<Product, Guid>();

        var product = await productRepo.GetByIdAsync(productId);
        if (product == null)
            return false;

        product.Price = newPrice;
        await productRepo.UpdateAsync(product, userName);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
```

## 📊 Queries de Auditoria

### Listar Entidades Criadas por um Usuário

```csharp
var productsByUser = await _productRepository.GetAsync(
    filter: p => p.CreatedBy == "admin@example.com",
    orderBy: q => q.OrderByDescending(p => p.CreatedAt),
    asNoTracking: true);
```

### Listar Entidades Modificadas Recentemente

```csharp
var recentlyUpdated = await _productRepository.GetAsync(
    filter: p => p.UpdatedAt.HasValue && 
                 p.UpdatedAt.Value >= DateTime.UtcNow.AddDays(-7),
    orderBy: q => q.OrderByDescending(p => p.UpdatedAt),
    asNoTracking: true);
```

### Relatório de Auditoria Completo

```csharp
public class AuditReport
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    public DateTime Timestamp { get; set; }
    public string? PerformedBy { get; set; }
}

public async Task<List<AuditReport>> GetProductAuditReportAsync(Guid productId)
{
    var product = await _productRepository.GetByIdAsync(productId);
    if (product == null)
        return new List<AuditReport>();

    var report = new List<AuditReport>
    {
        new AuditReport
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Action = "Created",
            Timestamp = product.CreatedAt,
            PerformedBy = product.CreatedBy
        }
    };

    if (product.UpdatedAt.HasValue)
    {
        report.Add(new AuditReport
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Action = "Updated",
            Timestamp = product.UpdatedAt.Value,
            PerformedBy = product.UpdatedBy
        });
    }

    // Se implementa soft delete
    if (product is ISoftDeletable softDeletable && softDeletable.IsDeleted)
    {
        report.Add(new AuditReport
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Action = "Deleted",
            Timestamp = softDeletable.DeletedAt!.Value,
            PerformedBy = softDeletable.DeletedBy
        });
    }

    return report.OrderBy(r => r.Timestamp).ToList();
}
```

## 🔧 Configuração no DbContext

### Índices para Performance

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Product>(entity =>
    {
        // Índice para queries por CreatedBy
        entity.HasIndex(p => p.CreatedBy);

        // Índice para queries por CreatedAt
        entity.HasIndex(p => p.CreatedAt);

        // Índice composto para queries por usuário e data
        entity.HasIndex(p => new { p.CreatedBy, p.CreatedAt });

        // Índice para UpdatedAt (queries de entidades modificadas)
        entity.HasIndex(p => p.UpdatedAt)
            .HasFilter("UpdatedAt IS NOT NULL");
    });
}
```

### Valores Padrão (Opcional)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        // CreatedAt é obrigatório, usar valor padrão no banco
        entity.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()"); // SQL Server
            // .HasDefaultValueSql("NOW()"); // PostgreSQL
            // .HasDefaultValueSql("CURRENT_TIMESTAMP"); // MySQL
    });
}
```

## 🎯 Obtendo Informação do Usuário Atual

### ASP.NET Core (Web API / MVC)

```csharp
public class ProductController : ControllerBase
{
    private readonly IRepository<Product, Guid> _productRepository;

    public ProductController(IRepository<Product, Guid> productRepository)
    {
        _productRepository = productRepository;
    }

    private string GetCurrentUserName()
    {
        // Opção 1: Username
        return User.Identity?.Name ?? "anonymous";

        // Opção 2: Email claim
        // return User.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous";

        // Opção 3: User ID
        // return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };

        await _productRepository.AddAsync(product, GetCurrentUserName());
        await _productRepository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null)
            return NotFound();

        product.Name = request.Name;
        product.Price = request.Price;

        await _productRepository.UpdateAsync(product, GetCurrentUserName());
        await _productRepository.SaveChangesAsync();

        return NoContent();
    }
}
```

### Serviço com HttpContextAccessor

```csharp
public class AuditService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.Identity?.Name ?? "system";
    }
}

// No Startup.cs / Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
```

## ⚠️ Observações Importantes

### 1. **Timezone Consistente**
Todos os campos de data/hora usam `DateTime.UtcNow` para consistência global.

```csharp
// ✅ CORRETO: UTC é usado automaticamente
await _productRepository.AddAsync(product, "user");
// product.CreatedAt usa DateTime.UtcNow

// ❌ EVITE: Não defina manualmente
product.CreatedAt = DateTime.Now; // timezone local!
```

### 2. **Conversão para Timezone Local**

```csharp
// Converter para timezone local na apresentação
var localCreatedAt = product.CreatedAt.ToLocalTime();

// Ou usar TimeZoneInfo para timezone específico
var brazilTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
var brazilTime = TimeZoneInfo.ConvertTimeFromUtc(product.CreatedAt, brazilTimeZone);
```

### 3. **Campos Opcionais**

`UpdatedAt` e `UpdatedBy` são `nullable` porque a entidade pode nunca ter sido atualizada:

```csharp
if (product.UpdatedAt.HasValue)
{
    Console.WriteLine($"Last updated: {product.UpdatedAt.Value} by {product.UpdatedBy}");
}
else
{
    Console.WriteLine("Never updated");
}
```

### 4. **Entidades Sem Audit**

Se a entidade **não implementa** `IAuditable`, os métodos com `createdBy`/`updatedBy` funcionam normalmente mas não preenchem nada:

```csharp
// EntityBase NÃO implementa IAuditable
public class SimpleEntity : EntityBase
{
    public string Name { get; set; }
}

// Esse método funciona, mas não preenche nenhum campo de audit
await _simpleRepository.AddAsync(entity, "user"); // OK, mas ignored
```

## 🚀 Resumo de Classes Base

| Classe Base | Primary Key | Audit | Soft Delete | Campo Delete |
|-------------|-------------|-------|-------------|--------------|
| `EntityBase` | Guid | ❌ | ❌ | - |
| `EntityBaseInt` | int | ❌ | ❌ | - |
| `EntityBaseAuditable` | Guid | ✅ | ❌ | - |
| `EntityBaseAuditableInt` | int | ✅ | ❌ | - |
| `EntityBaseSoftDelete` | Guid | ❌ | ✅ | IsDeleted |
| `EntityBaseSoftDeleteInt` | int | ❌ | ✅ | IsDeleted |
| `EntityBaseSoftDeleteAlt` | Guid | ❌ | ✅ | Deleted |
| `EntityBaseSoftDeleteAltInt` | int | ❌ | ✅ | Deleted |
| `EntityBaseAuditableSoftDelete` | Guid | ✅ | ✅ | IsDeleted |
| `EntityBaseAuditableSoftDeleteInt` | int | ✅ | ✅ | IsDeleted |
| `EntityBaseAuditableSoftDeleteAlt` | Guid | ✅ | ✅ | Deleted |
| `EntityBaseAuditableSoftDeleteAltInt` | int | ✅ | ✅ | Deleted |

## 📝 Exemplo Completo

```csharp
// Entidade com Audit + Soft Delete
public class Product : EntityBaseAuditableSoftDelete
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

// Serviço
public class ProductService
{
    private readonly IRepository<Product, Guid> _productRepository;

    public async Task<Product> CreateProductAsync(string name, decimal price, string userName)
    {
        var product = new Product
        {
            Name = name,
            Price = price,
            Category = "Electronics"
        };

        // Audit automático: CreatedAt e CreatedBy preenchidos
        await _productRepository.AddAsync(product, userName);
        await _productRepository.SaveChangesAsync();

        return product;
    }

    public async Task<bool> UpdateProductAsync(Guid productId, decimal newPrice, string userName)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null || product.IsDeleted)
            return false;

        product.Price = newPrice;

        // Audit automático: UpdatedAt e UpdatedBy preenchidos
        await _productRepository.UpdateAsync(product, userName);
        await _productRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteProductAsync(Guid productId, string userName)
    {
        // Soft delete + audit: DeletedAt e DeletedBy preenchidos
        var deleted = await _productRepository.SoftDeleteAsync(productId, userName);
        if (deleted)
        {
            await _productRepository.SaveChangesAsync();
        }
        return deleted;
    }
}
```

Audit Fields são **opcionais**, **automáticos** e **poderosos** para rastreabilidade completa! 🎉
