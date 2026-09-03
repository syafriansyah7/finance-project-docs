using Finance.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();
var apiUrl = builder.Configuration["ApiBaseUrl"] ?? builder.Configuration["ConnectionStrings__Api"] ?? "http://localhost:5000";
if (!apiUrl.StartsWith("http")) apiUrl = "http://" + apiUrl;
builder.Services.AddHttpClient("Api", c => c.BaseAddress = new Uri(apiUrl));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>();

app.Run();
