namespace Ecommerce.Infrastructure.Caching
{
    public static class CacheKeyBuilder
    {
        // Product cache keys
        public static string AllProductsAdmin() => "products_admin_all";
        public static string ProductById(int productId) => $"product_{productId}";
        public static string ProductsByCategory(int categoryId) => $"products_category_{categoryId}";
        public static string ProductsBySearch(string searchTerm) => $"products_search_{searchTerm.ToLower().Replace(" ", "_")}";
        
        // Category cache keys
        public static string AllCategories() => "categories_all";
        public static string CategoryById(int categoryId) => $"category_{categoryId}";
        
        // User cache keys
        public static string UserById(int userId) => $"user_{userId}";
        
        // Order cache keys
        public static string AllOrdersAdmin() => "orders_admin_all";
        public static string OrderById(Guid orderId) => $"order_{orderId}";
        
        // Admin product list with filters
        public static string AdminProductsWithFilters(int pageNumber, int pageSize, bool? isDeleted, int? categoryId, string? search)
        {
            var key = $"products_admin_p{pageNumber}_s{pageSize}";
            
            if (isDeleted.HasValue)
                key += $"_d{isDeleted.Value}";
            
            if (categoryId.HasValue)
                key += $"_c{categoryId.Value}";
            
            if (!string.IsNullOrWhiteSpace(search))
                key += $"_search_{search.ToLower().Replace(" ", "_")}";
            
            return key;
        }
        
        // Public product list with filters
        public static string PublicProductsWithFilters(int pageNumber, int pageSize, int? categoryId, string? searchTerm, decimal? minPrice = null, decimal? maxPrice = null, string? categoryName = null, bool? inStockOnly = null)
        {
            var key = $"products_public_p{pageNumber}_s{pageSize}";
            
            if (categoryId.HasValue && categoryId.Value > 0)
                key += $"_c{categoryId.Value}";
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
                key += $"_search_{searchTerm.ToLower().Replace(" ", "_")}";
            
            if (minPrice.HasValue)
                key += $"_minp{minPrice.Value}";
            
            if (maxPrice.HasValue)
                key += $"_maxp{maxPrice.Value}";
            
            if (!string.IsNullOrWhiteSpace(categoryName))
                key += $"_cat_{categoryName.ToLower().Replace(" ", "_")}";

            if (inStockOnly.HasValue)
                key += $"_stock_{(inStockOnly.Value ? "in" : "all")}";
            
            return key;
        }
        
        // Admin orders with filters
        public static string AdminOrdersWithFilters(int pageNumber, int pageSize, string? status, string? searchTerm)
        {
            var key = $"orders_admin_p{pageNumber}_s{pageSize}";
            
            if (!string.IsNullOrWhiteSpace(status))
                key += $"_status_{status.ToLower()}";
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
                key += $"_search_{searchTerm.ToLower().Replace(" ", "_")}";
            
            return key;
        }
        
        // Admin users with filters
        public static string AdminUsersWithFilters(int pageNumber, int pageSize, string? status, string? role, string? searchTerm)
        {
            var key = $"users_admin_p{pageNumber}_s{pageSize}";
            
            if (!string.IsNullOrWhiteSpace(status))
                key += $"_status_{status.ToLower()}";
            
            if (!string.IsNullOrWhiteSpace(role))
                key += $"_role_{role.ToLower()}";
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
                key += $"_search_{searchTerm.ToLower().Replace(" ", "_")}";
            
            return key;
        }
        
        // User-specific cache keys
        public static string UserOrders(long userId) => $"user_{userId}_orders";
        public static string UserCart(long userId) => $"user_{userId}_cart";
        public static string UserWishlist(long userId) => $"user_{userId}_wishlist";
        public static string UserProfile(long userId) => $"user_{userId}_profile";
        public static string UserById(long userId) => $"user_{userId}_details";
        public static string WishlistStatus(long userId, int productId) => $"user_{userId}_wishlist_status_{productId}";
        
        // System metrics and logs
        public static string SystemMetricsWithFilters(int pageNumber, int pageSize, string? endpointFilter, int? statusCodeFilter, DateTime? fromDate, DateTime? toDate)
        {
            var key = $"metrics_p{pageNumber}_s{pageSize}";
            
            if (!string.IsNullOrWhiteSpace(endpointFilter))
                key += $"_endpoint_{endpointFilter.ToLower().Replace("/", "_")}";
            
            if (statusCodeFilter.HasValue)
                key += $"_status_{statusCodeFilter.Value}";
            
            if (fromDate.HasValue)
                key += $"_from_{fromDate.Value:yyyyMMdd}";
            
            if (toDate.HasValue)
                key += $"_to_{toDate.Value:yyyyMMdd}";
            
            return key;
        }
        
        public static string ErrorLogsWithFilters(int pageNumber, int pageSize, string? severityFilter, string? searchTerm)
        {
            var key = $"error_logs_p{pageNumber}_s{pageSize}";
            
            if (!string.IsNullOrWhiteSpace(severityFilter))
                key += $"_severity_{severityFilter.ToLower()}";
            
            if (!string.IsNullOrWhiteSpace(searchTerm))
                key += $"_search_{searchTerm.ToLower().Replace(" ", "_")}";
            
            return key;
        }
        
        
        // Generic entity keys
        public static string AllEntities(string entityName) => $"{entityName.ToLower()}_all";
        public static string EntityById(string entityName, int id) => $"{entityName.ToLower()}_{id}";
    }
}
