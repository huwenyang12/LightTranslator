using System.IO;
using System.Text.Json;
using LightTranslator.Models;

namespace LightTranslator.Services.Settings;

public sealed class SettingsService : ISettingsService
{
    private readonly string _baseDirectory;
    private readonly string _settingsFilePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ??
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                ),
                "Bridgo"
            );

        _settingsFilePath = Path.Combine(
            _baseDirectory,
            "settings.json"
        );
    }

    public async Task<AppSettings> LoadAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!File.Exists(_settingsFilePath))
        {
            return AppSettings.CreateDefault();
        }

        await using var stream = File.OpenRead(_settingsFilePath);

        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
            stream,
            _jsonOptions,
            cancellationToken
        );

        return settings ?? AppSettings.CreateDefault();
    }

    public async Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default
    )
    {
        Directory.CreateDirectory(_baseDirectory);

        await using var stream = File.Create(_settingsFilePath);

        await JsonSerializer.SerializeAsync(
            stream,
            settings,
            _jsonOptions,
            cancellationToken
        );
    }
}