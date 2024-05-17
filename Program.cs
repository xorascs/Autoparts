using Autoparts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure services
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Create and migrate the database
CreateDatabase(app);

// Configure the HTTP request pipeline
ConfigureMiddleware(app);

app.Run();

// Method to configure services
void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Add controllers with views
    services.AddControllersWithViews();

    // Configure Entity Framework with SQL Server
    services.AddDbContext<Context>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DbConnection")));

    // Configure distributed memory cache
    services.AddDistributedMemoryCache();

    // Add HttpContextAccessor
    services.AddHttpContextAccessor();

    // Configure session with essential cookies
    services.AddSession(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });
}

// Method to configure middleware
void ConfigureMiddleware(WebApplication app)
{
    // Configure exception handling
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    // Enable HTTPS redirection
    app.UseHttpsRedirection();

    // Enable serving static files
    app.UseStaticFiles();

    // Enable routing
    app.UseRouting();

    // Enable authorization
    app.UseAuthorization();

    // Enable session
    app.UseSession();

    // Configure default route
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
}

// Method to create and migrate the database
void CreateDatabase(WebApplication app)
{
    var services = app.Services;
    using var scope = services.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var context = serviceProvider.GetRequiredService<Context>();

        // Check if the database can connect
        if (!context.Database.CanConnect())
        {
            Console.WriteLine("Database does not exist.");
            return;
        }

        // Apply any pending migrations
        context.Database.Migrate();

        // Add initial data to the database
        Context.FillDatabase(context);
    }
    catch (Exception ex)
    {
        // Log the exception
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating or migrating the database.");
    }
}
