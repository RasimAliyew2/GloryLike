using GloryLikeBackend.Data;
using GloryLikeBackend.Services;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IUserService, UserService>();


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISkillAndJobService, SkillAndJobService>();
builder.Services.AddScoped<ISkillQuestionnaireService, SkillQuestionnaireService>();
builder.Services.AddHttpClient<IOpenAiSkillQuestionnaireGenerator, OpenAiSkillQuestionnaireGenerator>();
builder.Services.AddScoped<ISkillDepthAssessmentService, SkillDepthAssessmentService>();
builder.Services.AddScoped<IJobOfferService, JobOfferService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

//app.MapGet("/db-test", async (IConfiguration config) =>
app.MapGet("/db-test", async (IConfiguration config) =>
{
    try
    {
        var cs = config.GetConnectionString("DefaultConnection");

        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("SELECT 1", conn);
        var result = await cmd.ExecuteScalarAsync();

        return Results.Ok($"SQL qoşuldu. Nəticə: {result}");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

