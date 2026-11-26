using Kitaphane.Domain.Books;
using Kitaphane.Domain.ReadingGoals;
using Microsoft.EntityFrameworkCore;

namespace Kitaphane.Application.Common;

/// <summary>
/// Database access used by application services. Queries are written with EF Core LINQ directly;
/// there is no repository layer.
/// </summary>
public interface IAppDbContext
{
    DbSet<Book> Books { get; }

    DbSet<ReadingGoal> ReadingGoals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
