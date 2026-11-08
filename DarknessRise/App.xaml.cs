using System.Windows;
using DarknessRise.Services;

namespace DarknessRise;

public partial class App : Application
{
    public static DatabaseService DbService { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DbService = new DatabaseService();
        await DbService.InitializeAsync();
    }
}

