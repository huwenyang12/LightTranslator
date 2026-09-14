using LightTranslator.ViewModels;
using LightTranslator.Services.Settings;
using LightTranslator.Services.Translation;
namespace LightTranslator.Tests;

public class FirstRunSettingsViewModelTests
{

    [Fact]
    public async Task TestApiKeyAsync_WhenValidatorSucceeds_AllowsSave()
    {
        var validator =
            new FakeApiKeyValidator();

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "test-key"
            };

        var succeeded =
            await viewModel.TestApiKeyAsync();

        Assert.True(
            succeeded
        );

        Assert.True(
            viewModel.ApiKeyTestSucceeded
        );

        Assert.True(
            viewModel.CanSave
        );

        Assert.Equal(
            "test-key",
            validator.ValidatedApiKey
        );
    }

    [Fact]
    public void ApiKey_WhenChangedAfterSuccessfulValidation_InvalidatesValidation()
    {
        var viewModel =
            new FirstRunSettingsViewModel
            {
                ApiKey = "key-a",
                ApiKeyTestSucceeded = true
            };

        viewModel.ApiKey =
            "key-b";

        Assert.False(
            viewModel.ApiKeyTestSucceeded
        );

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public async Task TestApiKeyAsync_WhenValidatorFails_DoesNotAllowSave()
    {
        var validator =
            new FakeApiKeyValidator
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "validation failed"
                    )
            };

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "invalid-key"
            };

        var succeeded =
            await viewModel.TestApiKeyAsync();

        Assert.False(
            succeeded
        );

        Assert.False(
            viewModel.ApiKeyTestSucceeded
        );

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public async Task TestApiKeyAsync_WhenValidatorSucceeds_SetsSuccessMessage()
    {
        var validator =
            new FakeApiKeyValidator();

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "test-key"
            };

        await viewModel.TestApiKeyAsync();

        Assert.Equal(
            "连接成功",
            viewModel.ApiKeyTestMessage
        );
    }


    [Fact]
    public async Task TestApiKeyAsync_WhenValidatorFails_SetsFailureMessage()
    {
        var validator =
            new FakeApiKeyValidator
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "validation failed"
                    )
            };

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "invalid-key"
            };

        await viewModel.TestApiKeyAsync();

        Assert.Equal(
            "连接失败",
            viewModel.ApiKeyTestMessage
        );
    }

    [Fact]
    public async Task ApiKey_WhenChangedAfterValidation_ClearsTestMessage()
    {
        var validator =
            new FakeApiKeyValidator();

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "key-a"
            };

        await viewModel.TestApiKeyAsync();

        Assert.Equal(
            "连接成功",
            viewModel.ApiKeyTestMessage
        );

        viewModel.ApiKey =
            "key-b";

        Assert.Equal(
            string.Empty,
            viewModel.ApiKeyTestMessage
        );
    }

    [Fact]
    public async Task TestApiKeyAsync_WhileValidationIsRunning_SetsIsTestingApiKey()
    {
        var validator =
            new PendingApiKeyValidator();

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "test-key"
            };

        var testTask =
            viewModel.TestApiKeyAsync();

        Assert.True(
            viewModel.IsTestingApiKey
        );

        validator.Complete();

        await testTask;

        Assert.False(
            viewModel.IsTestingApiKey
        );
    }

    [Fact]
    public async Task TestApiKeyAsync_WhenApiKeyChangesDuringValidation_DoesNotValidateChangedKey()
    {
        var validator =
            new PendingApiKeyValidator();

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "key-a"
            };

        var testTask =
            viewModel.TestApiKeyAsync();

        Assert.True(
            viewModel.IsTestingApiKey
        );

        viewModel.ApiKey =
            "key-b";

        validator.Complete();

        var succeeded =
            await testTask;

        Assert.False(
            succeeded
        );

        Assert.False(
            viewModel.ApiKeyTestSucceeded
        );

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public async Task TestApiKeyAsync_WhenApiKeyChangesDuringFailedValidation_DoesNotShowFailureForChangedKey()
    {
        var validator =
            new PendingApiKeyValidator();

        var viewModel =
            new FirstRunSettingsViewModel(
                apiKeyValidator:
                    validator
            )
            {
                ApiKey =
                    "key-a"
            };

        var testTask =
            viewModel.TestApiKeyAsync();

        Assert.True(
            viewModel.IsTestingApiKey
        );

        viewModel.ApiKey =
            "key-b";

        validator.Fail(
            new InvalidOperationException(
                "validation failed"
            )
        );

        var succeeded =
            await testTask;

        Assert.False(
            succeeded
        );

        Assert.False(
            viewModel.ApiKeyTestSucceeded
        );

        Assert.Equal(
            string.Empty,
            viewModel.ApiKeyTestMessage
        );

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public async Task SaveStartWithWindowsAsync_PersistsCurrentValue()
    {
        var startupPersistence =
            new FakeStartWithWindowsSettingsPersistence();

        var viewModel =
            new FirstRunSettingsViewModel(
                startupPersistence:
                    startupPersistence
            )
            {
                StartWithWindows = true
            };

        await viewModel.SaveStartWithWindowsAsync();

        Assert.True(
            startupPersistence.SavedValue
        );
    }


    [Fact]
    public void CanSave_WhenApiKeyIsEmpty_ReturnsFalse()
    {
        var viewModel =
            new FirstRunSettingsViewModel();

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public void CanSave_WhenApiKeyHasValueButNotValidated_ReturnsFalse()
    {
        var viewModel =
            new FirstRunSettingsViewModel
            {
                ApiKey = "test-key"
            };

        Assert.False(
            viewModel.CanSave
        );
    }

    [Fact]
    public async Task SaveAsync_WhenApiKeyValidated_PersistsApiKey()
    {
        var persistence =
            new FakeFirstRunSettingsPersistence();

        var viewModel =
            new FirstRunSettingsViewModel(
                persistence
            )
            {
                ApiKey = "test-key",
                ApiKeyTestSucceeded = true
            };

        var saved =
            await viewModel.SaveAsync();

        Assert.True(
            saved
        );

        Assert.Equal(
            "test-key",
            persistence.SavedApiKey
        );
    }

    private sealed class FakeFirstRunSettingsPersistence
        : IFirstRunSettingsPersistence
    {
        public string? SavedApiKey { get; private set; }

        public Task SaveAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            SavedApiKey =
                apiKey;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeStartWithWindowsSettingsPersistence
        : IStartWithWindowsSettingsPersistence
    {
        public bool SavedValue { get; private set; }

        public Task SaveAsync(
            bool enabled,
            CancellationToken cancellationToken = default
        )
        {
            SavedValue =
                enabled;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeApiKeyValidator
        : IApiKeyValidator
    {
        public string? ValidatedApiKey { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task ValidateAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            ValidatedApiKey =
                apiKey;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class PendingApiKeyValidator
        : IApiKeyValidator
    {
        private readonly TaskCompletionSource _completion =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

        public Task ValidateAsync(
            string apiKey,
            CancellationToken cancellationToken = default
        )
        {
            return _completion.Task;
        }

        public void Complete()
        {
            _completion.SetResult();
        }

        public void Fail(
            Exception exception
        )
        {
            _completion.SetException(
                exception
            );
        }
    }
}