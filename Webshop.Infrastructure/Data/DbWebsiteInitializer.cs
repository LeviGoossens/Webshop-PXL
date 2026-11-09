using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Webshop.Application.Repositories;
using Webshop.Domain.Entities;

namespace Webshop.Infrastructure.Data;

public static class DbWebsiteInitializer
{
    /// <summary>
    /// required seed-data for proper function of the app
    /// </summary>
    /// <param name="serviceProvider">access to registered services</param>
    /// <param name="configuration">access to configured settings</param>
    public static async Task InitAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        await InitRolesAsync(serviceProvider);
        await InitUsersAsync(serviceProvider, configuration);

        var categoryRepo = serviceProvider.GetRequiredService<ICategoryRepository>();
        // 1. Create 6 main camping categories
        var existingCategories = await categoryRepo.GetAll();
        if (!existingCategories.Any())
        {
            foreach (var category in SeedData.Categories)
            {
                await categoryRepo.Add(category);
            }
        }
    }

    /// <summary>
    /// seed-data not needed for proper function, but used for testing, displaying, ...
    /// </summary>
    /// <param name="serviceProvider">access to registered services</param>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var productRepo = serviceProvider.GetRequiredService<IProductRepository>();

        var existingProducts = await productRepo.GetAll();
        if (existingProducts.Any())
        {
            return; // Stop here if products already exist in database
        }

        foreach (var product in SeedData.Products)
        {
            await productRepo.Add(product);
        }
    }

    /// <summary>
    /// seed standard roles
    /// </summary>
    /// <param name="serviceProvider">access to registered services</param>
    private static async Task InitRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in SeedData.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                await roleManager.CreateAsync(role);
            }
        }
    }
    
    /// <summary>
    /// seed standard users
    /// </summary>
    /// <param name="serviceProvider">access to registered services</param>
    /// <param name="configuration">access to configured settings</param>
    private static async Task InitUsersAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        if (await userManager.FindByEmailAsync(SeedData.AdminUser.Email!) != null)
        {
            return;
        }

        var result = await userManager.CreateAsync(SeedData.AdminUser, configuration.GetValue<string>("Superadmin_Password")!);
        if (result.Succeeded)
        {
            var createdUser = await userManager.FindByEmailAsync(SeedData.AdminUser.Email!);
            await userManager.AddToRoleAsync(createdUser!, "Admin");
        }
    }
}