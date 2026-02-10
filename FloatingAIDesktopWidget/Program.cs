namespace FloatingAIDesktopWidget;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        using var singleInstance = SingleInstanceManager.Create("FloatingAIDesktopWidget");
        if (!singleInstance.IsFirstInstance)
        {
            _ = singleInstance.TrySignalExistingInstance();
            return;
        }

        using var settings = new AppSettingsProvider(AppPaths.SettingsPath);
        var form = new WidgetForm(settings);

        singleInstance.StartServer(() =>
        {
            try
            {
                if (!form.IsDisposed && form.IsHandleCreated)
                {
                    form.BeginInvoke(form.Nudge);
                }
            }
            catch
            {
                // ignore
            }
        });

        Application.Run(form);
    }    
}
