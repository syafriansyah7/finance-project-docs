using Microsoft.AspNetCore.Components.WebView.Maui;
using Finance.Mobile;

namespace Finance.Mobile.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		builder.Services.AddSingleton<ISyncQueue>(sp => new SqliteSyncQueue(System.IO.Path.Combine(FileSystem.AppDataDirectory, "finance.db")));
		builder.Services.AddSingleton(sp =>
		{
			var url = Preferences.Get("api_url", "https://senate-equipment-univ-generate.trycloudflare.com");
			return new HttpClient { BaseAddress = new Uri(url) };
		});

		return builder.Build();
	}
}
