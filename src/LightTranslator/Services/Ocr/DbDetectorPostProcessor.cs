using LightTranslator.Models;

namespace LightTranslator.Services.Ocr;

internal sealed class DbDetectorPostProcessor
{
    private readonly float _pixelThreshold;
    private readonly float _boxThreshold;
    private readonly float _unclipRatio;

    public DbDetectorPostProcessor(
        float pixelThreshold,
        float boxThreshold,
        float unclipRatio
    )
    {
        if (pixelThreshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelThreshold)
            );
        }

        if (boxThreshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(boxThreshold)
            );
        }

        if (unclipRatio < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unclipRatio)
            );
        }

        _pixelThreshold = pixelThreshold;
        _boxThreshold = boxThreshold;
        _unclipRatio = unclipRatio;
    }

    public IReadOnlyList<PixelRect> Process(
        float[,] probabilityMap,
        int imageWidth,
        int imageHeight
    )
    {
        ArgumentNullException.ThrowIfNull(
            probabilityMap
        );

        if (imageWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(imageWidth)
            );
        }

        if (imageHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(imageHeight)
            );
        }

        var mapHeight =
            probabilityMap.GetLength(
                0
            );

        var mapWidth =
            probabilityMap.GetLength(
                1
            );

        if (mapWidth == 0 ||
            mapHeight == 0)
        {
            return Array.Empty<PixelRect>();
        }

        var visited =
            new bool[
                mapHeight,
                mapWidth
            ];

        var results =
            new List<PixelRect>();

        for (var row = 0;
             row < mapHeight;
             row++)
        {
            for (var column = 0;
                 column < mapWidth;
                 column++)
            {
                if (visited[
                        row,
                        column
                    ] ||
                    probabilityMap[
                        row,
                        column
                    ] < _pixelThreshold)
                {
                    continue;
                }

                var component =
                    ReadComponent(
                        probabilityMap,
                        visited,
                        column,
                        row
                    );

                if (component.AverageProbability <
                    _boxThreshold)
                {
                    continue;
                }

                var rectangle =
                    ScaleAndClip(
                        component,
                        mapWidth,
                        mapHeight,
                        imageWidth,
                        imageHeight
                    );

                if (!rectangle.IsEmpty)
                {
                    results.Add(
                        rectangle
                    );
                }
            }
        }

        return
            results
                .OrderBy(
                    rectangle => rectangle.Y
                )
                .ThenBy(
                    rectangle => rectangle.X
                )
                .ToArray();
    }

    private Component ReadComponent(
        float[,] probabilityMap,
        bool[,] visited,
        int startX,
        int startY
    )
    {
        var mapHeight =
            probabilityMap.GetLength(
                0
            );

        var mapWidth =
            probabilityMap.GetLength(
                1
            );

        var queue =
            new Queue<(
                int X,
                int Y
            )>();

        queue.Enqueue(
            (
                startX,
                startY
            )
        );

        visited[
            startY,
            startX
        ] = true;

        var minimumX = startX;
        var maximumX = startX;
        var minimumY = startY;
        var maximumY = startY;
        double probabilitySum = 0;
        var pixelCount = 0;

        while (queue.Count > 0)
        {
            var current =
                queue.Dequeue();

            probabilitySum +=
                probabilityMap[
                    current.Y,
                    current.X
                ];

            pixelCount++;

            minimumX =
                Math.Min(
                    minimumX,
                    current.X
                );

            maximumX =
                Math.Max(
                    maximumX,
                    current.X
                );

            minimumY =
                Math.Min(
                    minimumY,
                    current.Y
                );

            maximumY =
                Math.Max(
                    maximumY,
                    current.Y
                );

            for (var offsetY = -1;
                 offsetY <= 1;
                 offsetY++)
            {
                for (var offsetX = -1;
                     offsetX <= 1;
                     offsetX++)
                {
                    if (offsetX == 0 &&
                        offsetY == 0)
                    {
                        continue;
                    }

                    var nextX =
                        current.X +
                        offsetX;

                    var nextY =
                        current.Y +
                        offsetY;

                    if (nextX < 0 ||
                        nextY < 0 ||
                        nextX >= mapWidth ||
                        nextY >= mapHeight ||
                        visited[
                            nextY,
                            nextX
                        ] ||
                        probabilityMap[
                            nextY,
                            nextX
                        ] < _pixelThreshold)
                    {
                        continue;
                    }

                    visited[
                        nextY,
                        nextX
                    ] = true;

                    queue.Enqueue(
                        (
                            nextX,
                            nextY
                        )
                    );
                }
            }
        }

        return
            new Component(
                minimumX,
                minimumY,
                maximumX + 1,
                maximumY + 1,
                probabilitySum /
                pixelCount
            );
    }

    private PixelRect ScaleAndClip(
        Component component,
        int mapWidth,
        int mapHeight,
        int imageWidth,
        int imageHeight
    )
    {
        var width =
            component.Right -
            component.Left;

        var height =
            component.Bottom -
            component.Top;

        var horizontalExpansion =
            width *
            (
                _unclipRatio - 1f
            ) /
            2f;

        var verticalExpansion =
            height *
            (
                _unclipRatio - 1f
            ) /
            2f;

        var left =
            Math.Clamp(
                component.Left -
                horizontalExpansion,
                0,
                mapWidth
            );

        var top =
            Math.Clamp(
                component.Top -
                verticalExpansion,
                0,
                mapHeight
            );

        var right =
            Math.Clamp(
                component.Right +
                horizontalExpansion,
                0,
                mapWidth
            );

        var bottom =
            Math.Clamp(
                component.Bottom +
                verticalExpansion,
                0,
                mapHeight
            );

        var scaledLeft =
            Math.Clamp(
                (int)Math.Floor(
                    left *
                    imageWidth /
                    mapWidth
                ),
                0,
                imageWidth
            );

        var scaledTop =
            Math.Clamp(
                (int)Math.Floor(
                    top *
                    imageHeight /
                    mapHeight
                ),
                0,
                imageHeight
            );

        var scaledRight =
            Math.Clamp(
                (int)Math.Ceiling(
                    right *
                    imageWidth /
                    mapWidth
                ),
                0,
                imageWidth
            );

        var scaledBottom =
            Math.Clamp(
                (int)Math.Ceiling(
                    bottom *
                    imageHeight /
                    mapHeight
                ),
                0,
                imageHeight
            );

        return
            new PixelRect(
                scaledLeft,
                scaledTop,
                scaledRight - scaledLeft,
                scaledBottom - scaledTop
            );
    }

    private sealed record Component(
        int Left,
        int Top,
        int Right,
        int Bottom,
        double AverageProbability
    );
}
