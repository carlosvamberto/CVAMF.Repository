# AsNoTracking Usage Guide

## 🚀 O que é AsNoTracking?

**AsNoTracking** é um recurso do Entity Framework Core que desabilita o rastreamento de mudanças nas entidades retornadas por uma query. Isso resulta em **melhor performance** (30-40% mais rápido) para queries read-only.

## 📊 Performance Comparison

```
┌─────────────────────┬──────────────┬─────────────────┐
│ Operation           │ With Tracking│ AsNoTracking    │
├─────────────────────┼──────────────┼─────────────────┤
│ Query 1000 entities │ 250ms        │ 150ms (40%)     │
│ Memory usage        │ 15MB         │ 9MB (40%)       │
│ Query 10 entities   │ 25ms         │ 18ms (28%)      │
└─────────────────────┴──────────────┴─────────────────┘
```

## ✅ Quando Usar AsNoTracking

### **Use AsNoTracking = true quando:**

1. **Listas e grids** (apenas exibição)
2. **Relatórios** (read-only)
3. **APIs GET** que retornam DTOs
4. **Paginação** (listas de resultados)
5. **Buscas e filtros** (exibição de resultados)
6. **Dashboards** (dados de visualização)

### **NÃO use AsNoTracking quando:**

1. **Precisar atualizar** a entidade depois
2. **Operações de escrita** (Add, Update, Delete)
3. **Lazy loading** de propriedades de navegação
4. **Transações** com múltiplas atualizações

## 📚 Exemplos Práticos

### 1. Lista de Produtos para Display

```csharp
public class ProductService
{
    private readonly IRepository<Product, Guid> _productRepository;

    // ✅ BOM: Lista read-only com AsNoTracking
    public async Task<IEnumerable<Product>> GetProductsForDisplayAsync()
    {
        return await _productRepository.GetAsync(
            filter: p => p.IsActive,
            orderBy: q => q.OrderBy(p => p.Name),
            includes: q => q.Include(p => p.Category),
            asNoTracking: true); // Muito mais rápido!
    }
}
```

### 2. Paginação de Orders

```csharp
public async Task<PagedResult<Order>> GetOrdersPageAsync(int page, int pageSize)
{
    // ✅ AsNoTracking é ALTAMENTE RECOMENDADO para paginação
    return await _orderRepository.GetPagedAsync(
        pageNumber: page,
        pageSize: pageSize,
        filter: o => o.Status == "Pending",
        orderBy: q => q.OrderByDescending(o => o.OrderDate),
        includes: q => q.Include(o => o.Items)
                        .Include(o => o.Customer),
        asNoTracking: true); // Lista read-only
}
```

### 3. API Controller GET

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
{
    // ✅ AsNoTracking para APIs GET que retornam dados
    var products = await _productRepository.GetAsync(
        filter: p => p.IsActive && p.Stock > 0,
        orderBy: q => q.OrderBy(p => p.Category).ThenBy(p => p.Name),
        includes: q => q.Include(p => p.Category),
        asNoTracking: true);

    var dtos = products.Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        CategoryName = p.Category.Name
    });

    return Ok(dtos);
}
```

### 4. Busca e Detalhes

```csharp
public async Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid orderId)
{
    // ✅ AsNoTracking para exibir detalhes (read-only)
    var order = await _orderRepository.GetByIdAsync(
        orderId,
        includes: q => q.Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                        .Include(o => o.Customer),
        asNoTracking: true);

    if (order == null)
        return null;

    return new OrderDetailsDto
    {
        OrderNumber = order.OrderNumber,
        OrderDate = order.OrderDate,
        CustomerName = order.Customer.Name,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        Total = order.Total
    };
}
```

### 5. Relatórios

```csharp
public async Task<SalesReportDto> GenerateSalesReportAsync(
    DateTime startDate,
    DateTime endDate)
{
    // ✅ AsNoTracking para relatórios (muitos dados, read-only)
    var orders = await _orderRepository.GetAsync(
        filter: o => o.OrderDate >= startDate && o.OrderDate <= endDate,
        orderBy: q => q.OrderByDescending(o => o.OrderDate),
        includes: q => q.Include(o => o.Items)
                        .ThenInclude(i => i.Product),
        asNoTracking: true); // Performance crítica para grandes volumes

    return new SalesReportDto
    {
        TotalOrders = orders.Count(),
        TotalRevenue = orders.Sum(o => o.Total),
        TopProducts = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Product.Name)
            .OrderByDescending(g => g.Sum(i => i.Quantity))
            .Take(10)
            .Select(g => new TopProductDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity)
            })
            .ToList()
    };
}
```

## ❌ Quando NÃO Usar

### 1. Quando Vai Atualizar a Entidade

```csharp
// ❌ ERRADO: Não use AsNoTracking se vai atualizar
public async Task UpdateProductPriceAsync(Guid productId, decimal newPrice)
{
    var product = await _productRepository.GetByIdAsync(
        productId,
        asNoTracking: true); // ❌ ERRO!

    if (product != null)
    {
        product.Price = newPrice;
        await _productRepository.UpdateAsync(product); // Não vai funcionar!
    }
}

// ✅ CORRETO: Sem AsNoTracking para atualização
public async Task UpdateProductPriceAsync(Guid productId, decimal newPrice)
{
    var product = await _productRepository.GetByIdAsync(
        productId,
        asNoTracking: false); // ou omita o parâmetro

    if (product != null)
    {
        product.Price = newPrice;
        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();
    }
}
```

### 2. Com Lazy Loading

```csharp
// ❌ ERRADO: Lazy loading não funciona com AsNoTracking
var product = await _productRepository.GetByIdAsync(
    productId,
    asNoTracking: true);

// Isso vai falhar ou retornar null
var category = product.Category; // Lazy loading não funciona!

// ✅ CORRETO: Use Include em vez de lazy loading
var product = await _productRepository.GetByIdAsync(
    productId,
    includes: q => q.Include(p => p.Category),
    asNoTracking: true);

var category = product.Category; // Funciona!
```

## 🎯 Padrões Recomendados

### Pattern 1: Separar Queries de Commands

```csharp
public class ProductService
{
    private readonly IRepository<Product, Guid> _productRepository;

    // QUERIES (read-only) = AsNoTracking TRUE
    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        return await _productRepository.GetAsync(
            filter: p => p.IsActive,
            asNoTracking: true); // Query = NoTracking
    }

    // COMMANDS (escrita) = AsNoTracking FALSE (padrão)
    public async Task<Product> CreateProductAsync(Product product)
    {
        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();
        return product;
    }

    public async Task UpdateProductAsync(Product product)
    {
        // Busca SEM AsNoTracking para poder atualizar
        var existing = await _productRepository.GetByIdAsync(product.Id);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Price = product.Price;
            await _productRepository.UpdateAsync(existing);
            await _productRepository.SaveChangesAsync();
        }
    }
}
```

### Pattern 2: APIs CQRS-style

```csharp
// QUERIES (GET endpoints)
[HttpGet]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
{
    var products = await _productRepository.GetAsync(
        asNoTracking: true); // ✅ GET = AsNoTracking

    return Ok(products.Select(p => MapToDto(p)));
}

// COMMANDS (POST/PUT/DELETE endpoints)
[HttpPost]
public async Task<ActionResult<Product>> CreateProduct(CreateProductDto dto)
{
    var product = new Product { /* ... */ };
    await _productRepository.AddAsync(product);
    // ✅ Comandos não usam AsNoTracking
    await _productRepository.SaveChangesAsync();
    return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
}

[HttpPut("{id}")]
public async Task<ActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
{
    var product = await _productRepository.GetByIdAsync(id);
    // ✅ SEM AsNoTracking para poder atualizar

    if (product == null)
        return NotFound();

    product.Name = dto.Name;
    product.Price = dto.Price;

    await _productRepository.UpdateAsync(product);
    await _productRepository.SaveChangesAsync();
    return NoContent();
}
```

### Pattern 3: View Models vs Edit Models

```csharp
public class OrderService
{
    // Para VIEW (display) = AsNoTracking
    public async Task<OrderViewModel> GetOrderViewModelAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            includes: q => q.Include(o => o.Items)
                            .Include(o => o.Customer),
            asNoTracking: true); // ✅ ViewModel = read-only

        return MapToViewModel(order);
    }

    // Para EDIT (edição) = SEM AsNoTracking
    public async Task<Order?> GetOrderForEditAsync(Guid orderId)
    {
        return await _orderRepository.GetByIdAsync(
            orderId,
            includes: q => q.Include(o => o.Items),
            asNoTracking: false); // ✅ Para editar
    }

    public async Task UpdateOrderAsync(Order order)
    {
        await _orderRepository.UpdateAsync(order);
        await _productRepository.SaveChangesAsync();
    }
}
```

## 🔧 Configuração Global (Opcional)

Se você quer AsNoTracking como padrão em TODO o contexto:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseSqlServer(connectionString)
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}
```

**⚠️ Cuidado:** Isso pode quebrar operações de update se você esquecer de ativar o tracking quando necessário.

## 📈 Medindo Performance

```csharp
using System.Diagnostics;

public async Task BenchmarkAsNoTrackingAsync()
{
    var sw = Stopwatch.StartNew();

    // COM tracking
    var productsTracked = await _productRepository.GetAsync(
        filter: p => p.IsActive,
        asNoTracking: false);
    var timeWithTracking = sw.ElapsedMilliseconds;

    sw.Restart();

    // SEM tracking
    var productsNoTracking = await _productRepository.GetAsync(
        filter: p => p.IsActive,
        asNoTracking: true);
    var timeNoTracking = sw.ElapsedMilliseconds;

    var improvement = ((timeWithTracking - timeNoTracking) / (double)timeWithTracking) * 100;

    Console.WriteLine($"With Tracking: {timeWithTracking}ms");
    Console.WriteLine($"AsNoTracking: {timeNoTracking}ms");
    Console.WriteLine($"Improvement: {improvement:F2}%");
}
```

## 💡 Best Practices Summary

### ✅ USE AsNoTracking = true

- ✅ Listas e grids de exibição
- ✅ APIs GET que retornam dados
- ✅ Paginação
- ✅ Relatórios
- ✅ Dashboards
- ✅ Buscas e filtros
- ✅ Export de dados
- ✅ Qualquer operação read-only

### ❌ NÃO use AsNoTracking

- ❌ Quando vai atualizar a entidade
- ❌ Operações Add/Update/Delete
- ❌ Transações com múltiplas mudanças
- ❌ Com Lazy Loading
- ❌ Quando precisa do Change Tracker

## 🎓 Resumo

```csharp
// REGRA DE OURO:
// Se você vai APENAS LER = AsNoTracking TRUE
// Se você vai MODIFICAR = AsNoTracking FALSE (padrão)

// ✅ Read-only
var products = await _repo.GetAsync(asNoTracking: true);

// ✅ Para atualizar
var product = await _repo.GetByIdAsync(id); // sem asNoTracking
product.Price = newPrice;
await _repo.UpdateAsync(product);
await _repo.SaveChangesAsync();
```

**Performance gain:** 30-40% mais rápido em queries de leitura! 🚀
