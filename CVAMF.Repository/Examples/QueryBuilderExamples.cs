using CVAMF.Repository.Entities;
using CVAMF.Repository.Interfaces;
using CVAMF.Repository.Models;

namespace CVAMF.Repository.Examples;

/// <summary>
/// Examples demonstrating the Fluent Query Builder (v1.7.0)
/// </summary>
public class QueryBuilderExamples
{
    // Example entities (for demonstration)
    public class Customer : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Order> Orders { get; set; } = new();
    }

    public class Order : EntityBase
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderItem : EntityBase
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class Product : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }

    public class Category : EntityBase
    {
        public string Name { get; set; } = string.Empty;
    }

    // DTOs
    public class CustomerSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class ProductListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int Stock { get; set; }
    }

    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public QueryBuilderExamples(
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Product, Guid> productRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    #region Basic Examples

    /// <summary>
    /// Example 1: Simple query with filter
    /// </summary>
    public async Task<List<Customer>> GetActiveCustomers()
    {
        return await _customerRepository.Query()
            .Where(x => x.Active)
            .ToListAsync();
    }

    /// <summary>
    /// Example 2: Query with ordering
    /// </summary>
    public async Task<List<Product>> GetProductsOrderedByPrice()
    {
        return await _productRepository.Query()
            .Where(x => x.Stock > 0)
            .OrderByDescending(x => x.Price)
            .ToListAsync();
    }

    /// <summary>
    /// Example 3: Query with multiple filters
    /// </summary>
    public async Task<List<Customer>> GetUSCustomers()
    {
        return await _customerRepository.Query()
            .Where(x => x.Active)
            .Where(x => x.Country == "USA")
            .Where(x => x.CreatedAt > DateTime.UtcNow.AddYears(-1))
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    #endregion

    #region Include (Eager Loading) Examples

    /// <summary>
    /// Example 4: Query with single include
    /// </summary>
    public async Task<List<Order>> GetOrdersWithCustomers()
    {
        return await _orderRepository.Query()
            .Where(x => x.Status == "Pending")
            .Include(x => x.Customer)
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync();
    }

    /// <summary>
    /// Example 5: Query with multiple includes
    /// </summary>
    public async Task<List<Order>> GetOrdersWithDetails()
    {
        return await _orderRepository.Query()
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .Include("Items.Product") // String-based navigation for nested includes
            .OrderByDescending(x => x.OrderDate)
            .AsSplitQuery() // Better performance for multiple includes
            .ToListAsync();
    }

    /// <summary>
    /// Example 6: Query with AsNoTracking for read-only
    /// </summary>
    public async Task<List<Product>> GetProductsForDisplay()
    {
        return await _productRepository.Query()
            .Where(x => x.Stock > 0)
            .Include(x => x.Category)
            .AsNoTracking() // 30-40% faster for read-only
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    #endregion

    #region Projection (DTO) Examples

    /// <summary>
    /// Example 7: Project to DTO
    /// </summary>
    public async Task<List<ProductListDto>> GetProductList()
    {
        return await _productRepository.Query()
            .Where(x => x.Stock > 0)
            .Include(x => x.Category)
            .ProjectTo(x => new ProductListDto
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name,
                Stock = x.Stock
            })
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Example 8: Complex projection with aggregation
    /// </summary>
    public async Task<List<CustomerSummaryDto>> GetCustomerSummaries()
    {
        return await _customerRepository.Query()
            .Where(x => x.Active)
            .Include(x => x.Orders)
            .ProjectTo(x => new CustomerSummaryDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                TotalOrders = x.Orders.Count,
                TotalSpent = x.Orders.Sum(o => o.TotalAmount),
                LastOrderDate = x.Orders.Max(o => (DateTime?)o.OrderDate)
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();
    }

    #endregion

    #region Pagination Examples

    /// <summary>
    /// Example 9: Simple pagination
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsPaged(int pageNumber, int pageSize)
    {
        return await _productRepository.Query()
            .Where(x => x.Stock > 0)
            .OrderBy(x => x.Name)
            .Paginate(pageNumber, pageSize)
            .ToPagedResultAsync();
    }

    /// <summary>
    /// Example 10: Pagination with DTO projection
    /// </summary>
    public async Task<PagedResult<CustomerSummaryDto>> GetCustomerSummariesPaged(
        int pageNumber, 
        int pageSize)
    {
        return await _customerRepository.Query()
            .Where(x => x.Active)
            .Include(x => x.Orders)
            .AsNoTracking() // Must be before ProjectTo
            .ProjectTo(x => new CustomerSummaryDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                TotalOrders = x.Orders.Count,
                TotalSpent = x.Orders.Sum(o => o.TotalAmount),
                LastOrderDate = x.Orders.Max(o => (DateTime?)o.OrderDate)
            })
            .OrderByDescending(x => x.TotalSpent)
            .Paginate(pageNumber, pageSize)
            .ToPagedResultAsync();
    }

    /// <summary>
    /// Example 11: Using Skip/Take instead of Paginate
    /// </summary>
    public async Task<List<Product>> GetTopProducts(int count)
    {
        return await _productRepository.Query()
            .Where(x => x.Stock > 0)
            .OrderByDescending(x => x.Price)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    #endregion

    #region Advanced Examples

    /// <summary>
    /// Example 12: Dynamic search with optional filters
    /// </summary>
    public async Task<PagedResult<ProductListDto>> SearchProducts(
        string? searchTerm,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize)
    {
        var query = _productRepository.Query();

        // Optional filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm) || 
                                     x.Description.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        return await query
            .Include(x => x.Category)
            .AsNoTracking() // Must be before ProjectTo
            .ProjectTo(x => new ProductListDto
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name,
                Stock = x.Stock
            })
            .OrderBy(x => x.Name)
            .Paginate(pageNumber, pageSize)
            .ToPagedResultAsync();
    }

    /// <summary>
    /// Example 13: Complex ordering (multiple levels)
    /// </summary>
    public async Task<List<Customer>> GetCustomersWithComplexOrdering()
    {
        return await _customerRepository.Query()
            .Where(x => x.Active)
            .OrderBy(x => x.Country)
            .ThenBy(x => x.City)
            .ThenByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Example 14: Using execution methods
    /// </summary>
    public async Task ExecutionMethodExamples()
    {
        // Get first matching customer
        var firstCustomer = await _customerRepository.Query()
            .Where(x => x.Email == "john@example.com")
            .FirstOrDefaultAsync();

        // Count active customers
        var activeCount = await _customerRepository.Query()
            .Where(x => x.Active)
            .CountAsync();

        // Check if any pending orders exist
        var hasPendingOrders = await _orderRepository.Query()
            .Where(x => x.Status == "Pending")
            .AnyAsync();

        // Get single customer (throws if multiple)
        var uniqueCustomer = await _customerRepository.Query()
            .Where(x => x.Email == "unique@example.com")
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Example 15: Combining all features
    /// </summary>
    public async Task<PagedResult<CustomerSummaryDto>> GetTopCustomersByCountry(
        string country,
        int pageNumber,
        int pageSize)
    {
        return await _customerRepository.Query()
            .Where(x => x.Active)
            .Where(x => x.Country == country)
            .Include(x => x.Orders)
            .AsNoTracking() // Must be before ProjectTo
            .ProjectTo(x => new CustomerSummaryDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                TotalOrders = x.Orders.Count,
                TotalSpent = x.Orders.Sum(o => o.TotalAmount),
                LastOrderDate = x.Orders.Max(o => (DateTime?)o.OrderDate)
            })
            .OrderByDescending(x => x.TotalSpent)
            .ThenByDescending(x => x.TotalOrders)
            .Paginate(pageNumber, pageSize)
            .ToPagedResultAsync();
    }

    #endregion

    #region Real-World Scenarios

    /// <summary>
    /// Scenario 1: E-commerce product listing
    /// </summary>
    public async Task<PagedResult<ProductListDto>> GetProductCatalog(
        Guid? categoryId,
        decimal? maxPrice,
        bool onlyInStock,
        string? sortBy,
        int pageNumber,
        int pageSize)
    {
        var query = _productRepository.Query();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        if (onlyInStock)
        {
            query = query.Where(x => x.Stock > 0);
        }

        query = query.Include(x => x.Category);

        var projectedQuery = query
            .AsNoTracking() // Must be before ProjectTo
            .ProjectTo(x => new ProductListDto
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                CategoryName = x.Category.Name,
                Stock = x.Stock
            });

        // Dynamic sorting
        projectedQuery = sortBy switch
        {
            "price_asc" => projectedQuery.OrderBy(x => x.Price),
            "price_desc" => projectedQuery.OrderByDescending(x => x.Price),
            "name" => projectedQuery.OrderBy(x => x.Name),
            _ => projectedQuery.OrderBy(x => x.Name)
        };

        return await projectedQuery
            .Paginate(pageNumber, pageSize)
            .ToPagedResultAsync();
    }

    /// <summary>
    /// Scenario 2: Customer dashboard with analytics
    /// </summary>
    public async Task<CustomerSummaryDto?> GetCustomerDashboard(Guid customerId)
    {
        return await _customerRepository.Query()
            .Where(x => x.Id == customerId)
            .Include(x => x.Orders)
            .AsNoTracking() // Must be before ProjectTo
            .ProjectTo(x => new CustomerSummaryDto
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                TotalOrders = x.Orders.Count,
                TotalSpent = x.Orders.Sum(o => o.TotalAmount),
                LastOrderDate = x.Orders.Max(o => (DateTime?)o.OrderDate)
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Scenario 3: Recent orders report
    /// </summary>
    public async Task<List<Order>> GetRecentOrders(int days, int limit)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _orderRepository.Query()
            .Where(x => x.OrderDate >= cutoffDate)
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .OrderByDescending(x => x.OrderDate)
            .Take(limit)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();
    }

    #endregion
}
