using FluentValidation;
using Kitaphane.Application.Books;
using Kitaphane.Application.GoogleBooks;
using Kitaphane.Application.ReadingGoals;
using Kitaphane.Application.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace Kitaphane.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<BookService>();
        services.AddScoped<GoogleBooksService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<ReadingGoalService>();

        return services;
    }
}
