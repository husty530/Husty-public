﻿using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace Husty.OpenCvSharp
{

    public record PCAResult(Point Center, double Val1, double Val2, double AngleRadian);

    public static class PCA
    {

        public static PCAResult Compute(Mat input)
        {
            using var mean = new Mat();
            using var eigenVec = new Mat();
            using var eigenVal = new Mat();
            Cv2.PCACompute(input, mean, eigenVec, eigenVal, 2);
            var center = new Point(mean.At<float>(0, 0), mean.At<float>(0, 1));
            var val1 = Math.Sqrt(eigenVal.At<float>(0, 0));
            var val2 = Math.Sqrt(eigenVal.At<float>(1, 0));
            var angle = Math.Atan2(eigenVec.At<float>(0, 1), eigenVec.At<float>(0, 0));
            if (double.IsNaN(val1)) val1 = 0;
            if (double.IsNaN(val2)) val2 = 0;
            return new(center, val1, val2, angle);
        }

        public static PCAResult Compute(IEnumerable<Point> input)
        {
            using var mat = input.AsFloatMat();
            return Compute(mat);
        }

    }
}