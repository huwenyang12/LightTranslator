using LightTranslator.Services.Ocr;

namespace LightTranslator.Tests;

public sealed class OcrModelProviderTests
{
    [Fact]
    public void GetRequiredPaths_WhenFilesExist_ReturnsAllBundledAssets()
    {
        using var directory =
            new TemporaryDirectory();

        directory.Write(
            "Assets/Ocr/ppocrv5_mobile_det.onnx",
            new byte[] { 1 }
        );

        directory.Write(
            "Assets/Ocr/ppocrv5_mobile_rec.onnx",
            new byte[] { 2 }
        );

        directory.WriteText(
            "Assets/Ocr/ppocrv5_dict.txt",
            "blank\na\n"
        );

        var paths =
            new OcrModelProvider(
                directory.Path
            ).GetRequiredPaths();

        Assert.Equal(
            directory.GetPath(
                "Assets/Ocr/ppocrv5_mobile_det.onnx"
            ),
            paths.DetectionModelPath
        );

        Assert.Equal(
            directory.GetPath(
                "Assets/Ocr/ppocrv5_mobile_rec.onnx"
            ),
            paths.RecognitionModelPath
        );

        Assert.Equal(
            directory.GetPath(
                "Assets/Ocr/ppocrv5_dict.txt"
            ),
            paths.DictionaryPath
        );
    }

    [Fact]
    public void GetRequiredPaths_WhenAssetMissing_ThrowsModelUnavailableWithoutLeakingPath()
    {
        using var directory =
            new TemporaryDirectory();

        var error =
            Assert.Throws<OcrModelException>(
                () =>
                    new OcrModelProvider(
                        directory.Path
                    ).GetRequiredPaths()
            );

        Assert.Equal(
            "ModelUnavailable",
            error.ErrorCode
        );

        Assert.False(
            error.Message.Contains(
                directory.Path,
                StringComparison.OrdinalIgnoreCase
            )
        );
    }

    [Fact]
    public void GetRequiredPaths_WhenAssetIsEmpty_ThrowsModelUnavailable()
    {
        using var directory =
            new TemporaryDirectory();

        directory.Write(
            "Assets/Ocr/ppocrv5_mobile_det.onnx",
            Array.Empty<byte>()
        );

        directory.Write(
            "Assets/Ocr/ppocrv5_mobile_rec.onnx",
            new byte[] { 2 }
        );

        directory.WriteText(
            "Assets/Ocr/ppocrv5_dict.txt",
            "blank\na\n"
        );

        var error =
            Assert.Throws<OcrModelException>(
                () =>
                    new OcrModelProvider(
                        directory.Path
                    ).GetRequiredPaths()
            );

        Assert.Equal(
            "ModelUnavailable",
            error.ErrorCode
        );
    }

    private sealed class TemporaryDirectory
        : IDisposable
    {
        public TemporaryDirectory()
        {
            Path =
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "LightTranslator.Tests",
                    Guid.NewGuid().ToString(
                        "N"
                    )
                );

            Directory.CreateDirectory(
                Path
            );
        }

        public string Path
        {
            get;
        }

        public string GetPath(
            string relativePath
        )
        {
            return
                System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        Path,
                        relativePath.Replace(
                            '/',
                            System.IO.Path.DirectorySeparatorChar
                        )
                    )
                );
        }

        public void Write(
            string relativePath,
            byte[] content
        )
        {
            var path =
                GetPath(
                    relativePath
                );

            Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(
                    path
                )!
            );

            File.WriteAllBytes(
                path,
                content
            );
        }

        public void WriteText(
            string relativePath,
            string content
        )
        {
            var path =
                GetPath(
                    relativePath
                );

            Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(
                    path
                )!
            );

            File.WriteAllText(
                path,
                content
            );
        }

        public void Dispose()
        {
            if (Directory.Exists(
                Path
            ))
            {
                Directory.Delete(
                    Path,
                    recursive: true
                );
            }
        }
    }
}
