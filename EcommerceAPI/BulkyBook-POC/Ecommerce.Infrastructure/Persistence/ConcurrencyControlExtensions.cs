using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Ecommerce.Infrastructure.Persistence
{
    /// <summary>
    /// Extensions for handling concurrency control and race conditions
    /// </summary>
    public static class ConcurrencyControlExtensions
    {
        /// <summary>
        /// Atomically reserves stock for a product using database-level concurrency control
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="productId">Product ID</param>
        /// <param name="quantity">Quantity to reserve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if stock was successfully reserved, false if insufficient stock</returns>
        public static async Task<bool> TryReserveStockAsync(
            this AppDbContext context, 
            int productId, 
            int quantity, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use raw SQL with atomic update and stock check
                var rowsAffected = await context.Database.ExecuteSqlRawAsync(
                    @"UPDATE Products 
                      SET Stock = Stock - {0} 
                      WHERE ProductId = {1} 
                        AND Stock >= {0} 
                        AND IsDeleted = 0",
                    quantity, productId);

                return rowsAffected > 0;
            }
            catch (Exception)
            {
                // If there's any database error, assume failure
                return false;
            }
        }

        /// <summary>
        /// Atomically reserves stock for multiple products in a single transaction
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="reservations">List of product reservations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all reservations succeeded, false if any failed</returns>
        public static async Task<bool> TryReserveStockBatchAsync(
            this AppDbContext context,
            List<ProductReservation> reservations,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            
            try
            {
                foreach (var reservation in reservations)
                {
                    var success = await context.TryReserveStockAsync(
                        reservation.ProductId, 
                        reservation.Quantity, 
                        cancellationToken);
                    
                    if (!success)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return false;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
        }

        /// <summary>
        /// Gets current stock with row-level locking to prevent race conditions
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="productId">Product ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current stock level</returns>
        public static async Task<int> GetCurrentStockWithLockAsync(
            this AppDbContext context,
            int productId,
            CancellationToken cancellationToken = default)
        {
            var result = await context.Database.SqlQueryRaw<int>(
                "SELECT Stock FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductId = {0} AND IsDeleted = 0",
                productId).FirstOrDefaultAsync(cancellationToken);

            return result;
        }

        /// <summary>
        /// Checks if sufficient stock is available with row-level locking
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="productId">Product ID</param>
        /// <param name="requiredQuantity">Required quantity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if sufficient stock is available</returns>
        public static async Task<bool> HasSufficientStockAsync(
            this AppDbContext context,
            int productId,
            int requiredQuantity,
            CancellationToken cancellationToken = default)
        {
            var currentStock = await context.GetCurrentStockWithLockAsync(productId, cancellationToken);
            return currentStock >= requiredQuantity;
        }
    }

    /// <summary>
    /// Represents a product stock reservation
    /// </summary>
    public class ProductReservation
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}





