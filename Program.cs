using GloryLikeBackend.Data;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;




var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "FrontendCors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
            {
                return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                       && (uri.Scheme == Uri.UriSchemeHttp
                           || uri.Scheme == Uri.UriSchemeHttps)
                       && uri.IsLoopback;
            });
        }
        else
        {
            policy.WithOrigins(
                "https://bothfind.com",
                "https://www.bothfind.com");
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISkillAndJobService, SkillAndJobService>();
builder.Services.AddScoped<ISkillQuestionnaireService, SkillQuestionnaireService>();
builder.Services.AddHttpClient<IOpenAiSkillQuestionnaireGenerator, OpenAiSkillQuestionnaireGenerator>();
builder.Services.AddScoped<ISkillDepthAssessmentService, SkillDepthAssessmentService>();
builder.Services.AddScoped<IJobOfferService, JobOfferService>();
builder.Services.AddScoped<IVacancyService, VacancyService>();
builder.Services.AddScoped<ITalentRadarService, TalentRadarService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICompanyAccessService, CompanyAccessService>();
builder.Services.AddScoped<ICompanyTeamService, CompanyTeamService>();
builder.Services.AddScoped<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddScoped<ICompanyHiringPlanService, CompanyHiringPlanService>();
builder.Services.AddScoped<IOrganizationReportsService, OrganizationReportsService>();
builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection(
        SmtpOptions.SectionName));
builder.Services.Configure<TeamInvitationOptions>(
    builder.Configuration.GetSection(
        TeamInvitationOptions.SectionName));
builder.Services.Configure<SocialAuthOptions>(
    builder.Configuration.GetSection(
        SocialAuthOptions.SectionName));
builder.Services.AddScoped<
    IRegistrationEmailSender,
    SmtpRegistrationEmailSender>();
builder.Services.AddSingleton(TimeProvider.System);
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

app.UseCors(frontendCorsPolicy);

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
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
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
