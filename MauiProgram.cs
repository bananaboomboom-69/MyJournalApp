using Microsoft.Extensions.Logging;
using myjournal.Services;
using myjournal.ViewModels;

namespace myjournal;

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
		builder.Logging.AddDebug();
#endif

		// Register Database Service
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();

		// Register Services
		builder.Services.AddSingleton<IStreakService, StreakService>();
		builder.Services.AddSingleton<IJournalService, JournalService>();
		builder.Services.AddSingleton<ITagService, TagService>();
		builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
		builder.Services.AddSingleton<IThemeService, ThemeService>();
		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton<IExportService, ExportService>();

		// Register ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<EntryViewModel>();
		builder.Services.AddTransient<JournalListViewModel>();
		builder.Services.AddTransient<CalendarViewModel>();
		builder.Services.AddTransient<AnalyticsViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<ExportViewModel>();
		builder.Services.AddTransient<TagsViewModel>();

		var app = builder.Build();

		// Initialize database
		Task.Run(async () =>
		{
			var dbService = app.Services.GetRequiredService<IDatabaseService>();
			await dbService.InitializeAsync();
		}).Wait();

		return app;
	}
}
