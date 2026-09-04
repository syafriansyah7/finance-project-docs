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

		builder.Services.AddSingleton<ISyncQueue>(sp =>
		{
			try { return new SqliteSyncQueue(System.IO.Path.Combine(FileSystem.AppDataDirectory, "finance.db")); }
			catch { return new InMemorySyncQueue(); }
		});
		builder.Services.AddSingleton(sp =>
		{
			string url;
			try { url = Preferences.Get("api_url", "http://192.168.1.8:5000"); } catch { url = "http://192.168.1.8:5000"; }
			if (string.IsNullOrWhiteSpace(url)) url = "http://192.168.1.8:5000";
			if (!url.StartsWith("http")) url = "https://" + url;
			return new HttpClient { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
		});

		return builder.Build();
	}
}
