﻿using OpenCvSharp;
using Intel.RealSense;

namespace Husty.OpenCvSharp.DepthCamera
{
    /// <summary>
    /// Native API type -> OpenCvSharp format
    /// </summary>
    internal class RealsenseConverter
    {

        // ------- Fields ------- //

        private readonly int _cWidth;
        private readonly int _cHeight;
        private readonly int _dWidth;
        private readonly int _dHeight;


        // ------- Constructor ------- //

        internal RealsenseConverter(int cWidth, int cHeight, int dWidth, int dHeight)
        {
            _cWidth = cWidth;
            _cHeight = cHeight;
            _dWidth = dWidth;
            _dHeight = dHeight;
        }


        // ------- Methods ------- //

        internal void ToColorMat(VideoFrame frame, ref Mat colorMat)
        {
            if (colorMat.Type() != MatType.CV_8UC3) colorMat = new Mat(_cHeight, _cWidth, MatType.CV_8UC3);
            unsafe
            {
                var rgbData = (byte*)frame.Data;
                var pixels = colorMat.DataPointer;
                for (int i = 0; i < colorMat.Width * colorMat.Height; i++)
                {
                    pixels[i * 3 + 0] = rgbData[i * 3 + 2];
                    pixels[i * 3 + 1] = rgbData[i * 3 + 1];
                    pixels[i * 3 + 2] = rgbData[i * 3 + 0];
                }
            }
        }

        internal void ToPointCloudMat(Frame frame, ref Mat pointCloudMat)
        {
            if (pointCloudMat.Type() != MatType.CV_16UC3) pointCloudMat = new Mat(_dHeight, _dWidth, MatType.CV_16UC3);
            unsafe
            {
                var pData = (float*)(new PointCloud().Process(frame).Data);
                var pixels = (ushort*)pointCloudMat.Data;
                int index = 0;
                for (int i = 0; i < pointCloudMat.Width * pointCloudMat.Height; i++)
                {
                    pixels[index] = (ushort)(pData[index++] * 1000);
                    pixels[index] = (ushort)(pData[index++] * 1000);
                    pixels[index] = (ushort)(pData[index++] * 1000);
                }
            }
        }

    }
}
