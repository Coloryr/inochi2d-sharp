using Inochi2dSharp.Core.Param;

namespace Inochi2dSharp.Core.Test;

public class ParameterTests
{
    private static void PrintArray(float[][] arr)
    {
        foreach (var row in arr)
        {
            foreach (var item in row)
            {
                Console.Write($" {item}");
            }
            Console.WriteLine();
        }
    }

    private static void RunTest(float[][] input, float[][] expect, float[][] axisPoints, string description)
    {
        var param = new Parameter
        {
            AxisPoints = axisPoints
        };

        var bind = new ValueParameterBinding(param)
        {
            // Assign values to ValueParameterBinding and consider NaN as !isSet_
            Values = input,
            IsSet = new bool[input.Length][]
        };
        for (int x = 0; x < input.Length; x++)
        {
            bind.IsSet[x] = new bool[input[0].Length];
            for (int y = 0; y < input[0].Length; y++)
            {
                bind.IsSet[x][y] = !float.IsNaN(input[x][y]);
            }
        }

        // Run the interpolation
        bind.ReInterpolate();

        // Check results with a fudge factor for rounding error
        float epsilon = 0.0001f;
        for (int x = 0; x < bind.Values.Length; x++)
        {
            for (int y = 0; y < bind.Values[0].Length; y++)
            {
                float delta = float.Abs(expect[x][y] - bind.Values[x][y]);
                if (float.IsNaN(delta) || delta > epsilon)
                {
                    Console.WriteLine("Output mismatch at {0}, {1}", x, y);
                    Console.WriteLine("Expected:");
                    PrintArray(expect);
                    Console.WriteLine("Output:");
                    PrintArray(bind.Values);
                    Console.WriteLine(description);
                    Assert.Fail();
                }
            }
        }
    }

    private static void RunTestUniform(float[][] input, float[][] expect, string description)
    {
        float[][] axisPoints =
        [
            // Initialize axisPoints as uniformly spaced
            new float[input.Length],
            new float[input[0].Length],
        ];
        if (input.Length > 1)
        {
            for (int x = 0; x < input.Length; x++)
            {
                axisPoints[0][x] = x / (float)(input.Length - 1);
            }
        }
        if (input[0].Length > 1)
        {
            for (int y = 0; y < input[0].Length; y++)
            {

                axisPoints[1][y] = y / (float)(input[0].Length - 1);
            }
        }

        RunTest(input, expect, axisPoints, description);
    }

    [SetUp]
    public void Setup()
    {

    }

    private readonly float x = float.NaN;

    [Test]
    public void Test1()
    {
        RunTestUniform(
            [[1f], [x], [x], [4f]],
            [[1f], [2f], [3f], [4f]],
            "1d-uniform-interpolation"
        );
    }

    [Test]
    public void Test2()
    {
        RunTest(
            [[0f], [x], [x], [4f]],
            [[0f], [1f], [3f], [4f]],
            [[0f, 0.25f, 0.75f, 1f], [0f]],
            "1d-nonuniform-interpolation"
        );
    }

    [Test]
    public void Test3()
    {
        RunTestUniform(
            [
            [4, x, x, 10],
            [x, x, x, x],
            [x, x, x, x],
            [1, x, x, 7]
        ],
            [
            [4, 6, 8, 10],
            [3, 5, 7, 9],
            [2, 4, 6, 8],
            [1, 3, 5, 7]
        ],
            "square-interpolation"
        );
    }

    [Test]
    public void Test4()
    {
        RunTestUniform(
            [
            [4, x, x, 10],
            [x, x, x, x],
            [x, x, x, x],
            [1, x, x, 7]
        ],
            [
            [4, 6, 8, 10],
            [3, 5, 7, 9],
            [2, 4, 6, 8],
            [1, 3, 5, 7]
        ],
            "square-interpolation"
        );
    }

    [Test]
    public void Test5()
    {
        RunTestUniform(
            [
            [4, x, x, x],
            [x, x, x, x],
            [x, x, x, x],
            [1, x, x, 7]
        ],
            [
            [4, 6, 8, 10],
            [3, 5, 7, 9],
            [2, 4, 6, 8],
            [1, 3, 5, 7]
        ],
            "corner-extrapolation"
        );
    }

    [Test]
    public void Test6()
    {
        RunTestUniform(
            [
            [9, x, x, 0],
            [x, x, x, x],
            [x, x, x, x],
            [0, x, x, 9]
        ],
            [
            [9, 6, 3, 0],
            [6, 5, 4, 3],
            [3, 4, 5, 6],
            [0, 3, 6, 9]
        ],
            "cross-interpolation"
        );
    }

    [Test]
    public void Test7()
    {
        RunTestUniform(
            [
            [x, x, 2, x, x],
            [x, x, x, x, x],
            [0, x, x, x, 4],
            [x, x, x, x, x],
            [x, x, 10, x, x]
        ],
            [
            [-2, 0, 2, 2, 2],
            [-1, 1, 3, 3, 3],
            [0, 2, 4, 4, 4],
            [3, 5, 7, 7, 7],
            [6, 8, 10, 10, 10]
        ],
            "diamond-interpolation"
        );
    }

    [Test]
    public void Test8()
    {
        RunTestUniform(
            [
            [x, x, x, x],
            [x, 3, 4, x],
            [x, 1, 2, x],
            [x, x, x, x]
        ],
            [
            [3, 3, 4, 4],
            [3, 3, 4, 4],
            [1, 1, 2, 2],
            [1, 1, 2, 2]
        ],
            "edge-expansion"
        );
    }

    [Test]
    public void Test9()
    {
        RunTestUniform(
            [
            [x, x, x, x],
            [x, x, 4, x],
            [x, x, x, x],
            [0, x, x, x]
        ],
            [
            [2, 3, 4, 4],
            [2, 3, 4, 4],
            [1, 2, 3, 3],
            [0, 1, 2, 2]
        ],
            "intersecting-expansion"
        );
    }

    [Test]
    public void Test10()
    {
        RunTestUniform(
            [
            [x, 5, x],
            [x, x, x],
            [0, x, x]
        ],
            [
            [4, 5, 5],
            [2, 3, 3],
            [0, 1, 1]
        ],
            "nondiagonal-gradient"
        );
    }
}