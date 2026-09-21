using System.Reflection;
using System.Windows.Forms;
using LightTranslator.Infrastructure.Security;
using LightTranslator.Models;
using LightTranslator.Services.Logging;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Startup;
using LightTranslator.Services.Tray;
using LightTranslator.Services.Translation;
using LightTranslator.ViewModels;
using LightTranslator.Views;

namespace LightTranslator.Tests;

[Collection("Translate window WPF")]
public sealed class ProductBrandingTests
{
    [Fact]
    public void ApplicationAssembly_UsesBridgoProductIdentity()
    {
        var assembly = typeof(App).Assembly;

        Assert.Equal("Bridgo", assembly.GetName().Name);
        Assert.Equal(
            "语桥",
            assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
        );
        Assert.Equal(
            "语桥 Bridgo",
            assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
        );
    }

    [Fact]
    public void ApplicationAssembly_UsesV030ReleaseVersion()
    {
        var assembly = typeof(App).Assembly;

        Assert.Equal(
            new Version(0, 3, 0, 0),
            assembly.GetName().Version
        );
        Assert.Equal(
            "0.3.0.0",
            assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
        );
        Assert.Equal(
            "0.3.0",
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        );
    }

    [Fact]
    public void DefaultPersistentPaths_UseBridgoDirectory()
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );

        var settingsService = new SettingsService();
        var settingsPath = GetPrivateField<string>(
            settingsService,
            "_settingsFilePath"
        );

        var secretStorage = new DpapiSecretStorage();
        var secretsDirectory = GetPrivateField<string>(
            secretStorage,
            "_baseDirectory"
        );

        Assert.Equal(
            Path.Combine(localAppData, "Bridgo", "settings.json"),
            settingsPath
        );
        Assert.Equal(
            Path.Combine(localAppData, "Bridgo", "secrets"),
            secretsDirectory
        );
        Assert.Equal(
            Path.Combine(localAppData, "Bridgo", "logs", "app.log"),
            LogFilePathProvider.GetDefaultPath()
        );
    }

    [Fact]
    public void StartupRegistryValue_UsesBridgoName()
    {
        var field = typeof(RegistryStartupRegistrationBackend).GetField(
            "ValueName",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.NotNull(field);
        Assert.Equal("Bridgo", field.GetRawConstantValue());
    }

    [Fact]
    public void TrayTooltip_UsesChineseProductName()
    {
        using var backend = new NotifyIconTrayBackend();

        var notifyIcon = GetPrivateField<NotifyIcon>(
            backend,
            "_notifyIcon"
        );

        Assert.Equal("语桥", notifyIcon.Text);
    }

    [Fact]
    public void SettingsWindow_HidesTitleText_OtherWindowsUseChineseProductName()
    {
        RunOnSta(
            () =>
            {
                var settingsWindow = new FirstRunSettingsWindow(
                    new FirstRunSettingsViewModel(),
                    isFirstRun: false
                );

                var translateWindow = new TranslateWindow(
                    new TranslateViewModel(
                        new NoOpTranslationService()
                    )
                );

                var mainWindow = new MainWindow();

                Assert.Equal(string.Empty, settingsWindow.Title);
                Assert.Equal("语桥", translateWindow.Title);
                Assert.Equal("语桥", mainWindow.Title);

                settingsWindow.Close();
                translateWindow.Close();
                mainWindow.Close();
            }
        );
    }

    private static T GetPrivateField<T>(
        object instance,
        string fieldName
    )
    {
        var field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(field);

        return Assert.IsType<T>(
            field.GetValue(instance)
        );
    }

    private static void RunOnSta(Action action)
    {
        Exception? exception = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }
        );

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(exception);
    }

    private sealed class NoOpTranslationService
        : ITranslationService
    {
        public Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                new TranslationResult(
                    string.Empty,
                    null
                )
            );
        }
    }
}
