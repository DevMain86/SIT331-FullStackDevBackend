using robot_controller_api.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register repository implementations for dependency injection.
builder.Services.AddScoped<IRobotCommandDataAccess, RobotCommandRepository>();
builder.Services.AddScoped<IMapDataAccess, MapRepository>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();


