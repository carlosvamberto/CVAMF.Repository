# Include (Eager Loading) Usage Examples

Este guia mostra como usar **Include** para carregar entidades relacionadas automaticamente.

## 📚 Índice

- [O que é Include?](#o-que-é-include)
- [GetByIdAsync com Include](#getbyidasync-com-include)
- [GetAllAsync com Include](#getallasyncom-include)
- [GetAsync com Include](#getasync-com-include)
- [GetPagedAsync com Include](#getpagedasync-com-include)
- [GetFirstOrDefaultAsync com Include](#getfirstordefaultasync-com-include)
- [ThenInclude (Includes Aninhados)](#theninclude-includes-aninhados)
- [Múltiplos Includes](#múltiplos-includes)
- [Performance e Best Practices](#performance-e-best-practices)

## O que é Include?

**Include** (ou Eager Loading) permite carregar entidades relacionadas em uma única consulta ao banco de dados, evitando o problema de N+1 queries.

### ❌ Sem Include (Lazy Loading - Múltiplas Queries)

```csharp
var order = await _orderRepository.GetByIdAsync(orderId);
// 1ª query busca o order

foreach (var item in order.Items) // Cada acesso = 1 query adicional!
{
    Console.WriteLine(item.Product.Name); // N+1 queries problem!
}
```

### ✅ Com Include (Eager Loading - Uma Query)

```csharp
var order = await _orderRepository.GetByIdAsync(
    orderId,
    includes: q => q.Include(o => o.Items)
                    .ThenInclude(i => i.Product));
// Uma única query traz tudo!

foreach (var item in order.Items)
{
    Console.WriteLine(item.Product.Name); // Sem query adicional
}
```

## GetByIdAsync com Include

### Exemplo Básico

```csharp
public class OrderService
{
    private readonly IRepository<Order, Guid> _orderRepository;

    // Carregar Order com Items
    public async Task<Order?> GetOrderWithItemsAsync(Guid orderId)
    {
        return await _orderRepository.GetByIdAsync(
            orderId,
            includes: q => q.Include(o => o.Items));
    }
}
```

### Entidades de Exemplo

```csharp
public class Order : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }

    // Navigation properties
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem : EntityBaseInt
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Customer : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = new();
}

public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

### Include Múltiplas Relações

```csharp
public async Task<Order?> GetCompleteOrderAsync(Guid orderId)
{
    return await _orderRepository.GetByIdAsync(
        orderId,
        includes: q => q.Include(o => o.Items)
                        .Include(o => o.Customer));
}
```

## GetAllAsync com Include

### Carregar Todos com Relações

```csharp
public async Task<IEnumerable<Order>> GetAllOrdersWithDetailsAsync()
{
    return await _orderRepository.GetAllAsync(
        includes: q => q.Include(o => o.Items)
                        .Include(o => o.Customer));
}
```

### Produtos com Categoria

```csharp
public class ProductService
{
    private readonly IRepository<Product, Guid> _productRepository;

    public async Task<IEnumerable<Product>> GetAllProductsWithCategoryAsync()
    {
        return await _productRepository.GetAllAsync(
            includes: q => q.Include(p => p.Category));
    }
}
```

## GetAsync com Include

### Filtro + Include

```csharp
public async Task<IEnumerable<Order>> GetActiveOrdersWithDetailsAsync()
{
    return await _orderRepository.GetAsync(
        filter: o => o.Status == "Active",
        orderBy: q => q.OrderByDescending(o => o.OrderDate),
        includes: q => q.Include(o => o.Items)
                        .Include(o => o.Customer));
}
```

### Busca Complexa com Include

```csharp
public async Task<IEnumerable<Order>> SearchOrdersAsync(
    string customerName,
    DateTime? startDate,
    DateTime? endDate)
{
    return await _orderRepository.GetAsync(
        filter: o => o.Customer.Name.Contains(customerName) &&
                     (!startDate.HasValue || o.OrderDate >= startDate) &&
                     (!endDate.HasValue || o.OrderDate <= endDate),
        orderBy: q => q.OrderByDescending(o => o.OrderDate),
        includes: q => q.Include(o => o.Customer)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Product));
}
```

## GetPagedAsync com Include

### Paginação com Relações

```csharp
public async Task<PagedResult<Order>> GetOrdersPagedWithDetailsAsync(
    int page,
    int pageSize,
    string? status = null)
{
    return await _orderRepository.GetPagedAsync(
        pageNumber: page,
        pageSize: pageSize,
        filter: status != null ? o => o.Status == status : null,
        orderBy: q => q.OrderByDescending(o => o.OrderDate),
        includes: q => q.Include(o => o.Customer)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Product));
}
```

### Uso em Controller

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<Order>>> GetOrders(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? status = null)
{
    var result = await _orderService.GetOrdersPagedWithDetailsAsync(
        page, 
        pageSize, 
        status);

    return Ok(result);
}
```

## GetFirstOrDefaultAsync com Include

### Buscar Primeira Ocorrência com Relações

```csharp
public async Task<Order?> FindOrderByNumberAsync(string orderNumber)
{
    return await _orderRepository.GetFirstOrDefaultAsync(
        filter: o => o.OrderNumber == orderNumber,
        includes: q => q.Include(o => o.Items)
                        .Include(o => o.Customer));
}
```

### Última Order do Cliente

```csharp
public async Task<Order?> GetCustomerLastOrderAsync(Guid customerId)
{
    return await _orderRepository.GetFirstOrDefaultAsync(
        filter: o => o.CustomerId == customerId,
        includes: q => q.Include(o => o.Items)
                            .ThenInclude(i => i.Product));
}
```

## ThenInclude (Includes Aninhados)

### Include em Cascata

```csharp
// Order -> Items -> Product -> Category
public async Task<Order?> GetOrderWithFullDetailsAsync(Guid orderId)
{
    return await _orderRepository.GetByIdAsync(
        orderId,
        includes: q => q.Include(o => o.Customer)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Product)
                                .ThenInclude(p => p.Category));
}
```

### Múltiplos ThenInclude

```csharp
public async Task<Order?> GetComplexOrderAsync(Guid orderId)
{
    return await _orderRepository.GetByIdAsync(
        orderId,
        includes: q => q.Include(o => o.Items)
                            .ThenInclude(i => i.Product)
                                .ThenInclude(p => p.Category)
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Product)
                                .ThenInclude(p => p.Supplier)
                        .Include(o => o.Customer)
                            .ThenInclude(c => c.Address));
}
```

## Múltiplos Includes

### Várias Relações no Mesmo Nível

```csharp
public async Task<Product?> GetProductWithAllRelationsAsync(Guid productId)
{
    return await _productRepository.GetByIdAsync(
        productId,
        includes: q => q.Include(p => p.Category)
                        .Include(p => p.Supplier)
                        .Include(p => p.Reviews)
                        .Include(p => p.Images));
}
```

### Lista com Múltiplos Includes

```csharp
public async Task<IEnumerable<Product>> GetProductsForCatalogAsync()
{
    return await _productRepository.GetAsync(
        filter: p => p.IsActive && p.Stock > 0,
        orderBy: q => q.OrderBy(p => p.Category.Name)
                       .ThenBy(p => p.Name),
        includes: q => q.Include(p => p.Category)
                        .Include(p => p.Images)
                        .Include(p => p.Reviews));
}
```

## Performance e Best Practices

### ✅ Boas Práticas

1. **Carregue apenas o que precisa**
```csharp
// ✅ Bom - Específico
var order = await _orderRepository.GetByIdAsync(
    orderId,
    includes: q => q.Include(o => o.Items));

// ❌ Ruim - Demais
var order = await _orderRepository.GetByIdAsync(
    orderId,
    includes: q => q.Include(o => o.Items)
                    .Include(o => o.Customer)
                    .Include(o => o.Shipping)
                    .Include(o => o.Payment)
                    .Include(o => o.AuditLogs)); // Muita coisa!
```

2. **Use filtros antes de Include**
```csharp
// ✅ Bom - Filtra primeiro
var activeOrders = await _orderRepository.GetAsync(
    filter: o => o.Status == "Active", // Reduz quantidade antes
    includes: q => q.Include(o => o.Items));

// ❌ Ruim - Carrega tudo e filtra depois
var allOrders = await _orderRepository.GetAllAsync(
    includes: q => q.Include(o => o.Items));
var activeOrders = allOrders.Where(o => o.Status == "Active");
```

3. **Evite Includes em Loops**
```csharp
// ❌ PÉSSIMO - N+1 problem
foreach (var orderId in orderIds)
{
    var order = await _orderRepository.GetByIdAsync(
        orderId,
        includes: q => q.Include(o => o.Items)); // Múltiplas queries!
}

// ✅ ÓTIMO - Uma query
var orders = await _orderRepository.GetAsync(
    filter: o => orderIds.Contains(o.Id),
    includes: q => q.Include(o => o.Items));
```

4. **Projete apenas campos necessários para listas grandes**
```csharp
// Para listas, considere projetar para DTOs
var orderSummaries = await _orderRepository.GetAsync(
    filter: o => o.Status == "Pending",
    includes: q => q.Include(o => o.Customer));

var dtos = orderSummaries.Select(o => new OrderSummaryDto
{
    Id = o.Id,
    OrderNumber = o.OrderNumber,
    CustomerName = o.Customer.Name,
    Total = o.Total
}).ToList();
```

### ⚠️ Cuidados

1. **Cartesian Explosion** - Múltiplos Includes de coleções
```csharp
// ⚠️ Cuidado - Pode gerar muitos dados duplicados
var product = await _productRepository.GetByIdAsync(
    productId,
    includes: q => q.Include(p => p.Reviews)      // 100 reviews
                    .Include(p => p.Images)       // 10 images
                    .Include(p => p.Categories)); // 5 categories
// Resultado pode ter 100 * 10 * 5 = 5000 linhas!
```

2. **Split Queries quando necessário**
```csharp
// Para múltiplas coleções, considere split queries no DbContext:
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer(
        connectionString,
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
}
```

### 📊 Monitoramento

```csharp
// Use logging para ver queries geradas
public async Task<Order?> GetOrderAsync(Guid orderId)
{
    _logger.LogInformation("Loading order {OrderId} with includes", orderId);

    var order = await _orderRepository.GetByIdAsync(
        orderId,
        includes: q => q.Include(o => o.Items)
                        .Include(o => o.Customer));

    _logger.LogInformation(
        "Loaded order with {ItemCount} items", 
        order?.Items.Count ?? 0);

    return order;
}
```

## Exemplos Completos

### API Controller Completo

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IRepository<Order, Guid> _orderRepository;

    public OrdersController(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(
            id,
            includes: q => q.Include(o => o.Items)
                            .ThenInclude(i => i.Product)
                            .Include(o => o.Customer));

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<Order>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var result = await _orderRepository.GetPagedAsync(
            pageNumber: page,
            pageSize: pageSize,
            filter: status != null ? o => o.Status == status : null,
            orderBy: q => q.OrderByDescending(o => o.OrderDate),
            includes: q => q.Include(o => o.Customer)
                            .Include(o => o.Items));

        return Ok(result);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<Order>>> GetCustomerOrders(
        Guid customerId)
    {
        var orders = await _orderRepository.GetAsync(
            filter: o => o.CustomerId == customerId,
            orderBy: q => q.OrderByDescending(o => o.OrderDate),
            includes: q => q.Include(o => o.Items)
                            .ThenInclude(i => i.Product));

        return Ok(orders);
    }
}
```
