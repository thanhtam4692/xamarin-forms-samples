﻿using System;
using System.IO;
using System.Reflection;

using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;

using TouchTracking;

namespace SkiaSharpFormsDemos.Transforms
{
    public partial class ShowNonAffineMatrixPage : ContentPage
    {
        SKMatrix matrix;
        SKBitmap bitmap;
        SKSize bitmapSize;

        TouchPoint[] touchPoints = new TouchPoint[4];

        public ShowNonAffineMatrixPage()
        {
            InitializeComponent();

            string resourceID = "SkiaSharpFormsDemos.Media.Banana.jpg";
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceID))
            using (SKManagedStream skStream = new SKManagedStream(stream))
            {
                bitmap = SKBitmap.Decode(skStream);
            }

            touchPoints[0] = new TouchPoint(50, 50);                  // upper-left corner
            touchPoints[1] = new TouchPoint(bitmap.Width + 50, 50);   // upper-right corner
            touchPoints[2] = new TouchPoint(50, bitmap.Height + 50);  // lower-left corner
            touchPoints[3] = new TouchPoint(bitmap.Width + 50, bitmap.Height + 50);     // lower-right corner

            bitmapSize = new SKSize(bitmap.Width, bitmap.Height);
            matrix = ComputeMatrix(bitmapSize, touchPoints[0].Center, touchPoints[1].Center, touchPoints[2].Center, touchPoints[3].Center);
        }

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            bool touchPointMoved = false;

            foreach (TouchPoint touchPoint in touchPoints)
            {
                float scale = canvasView.CanvasSize.Width / (float)canvasView.Width;
                SKPoint point = new SKPoint(scale * (float)args.Location.X,
                                            scale * (float)args.Location.Y);
                touchPointMoved |= touchPoint.ProcessTouchEvent(args.Id, args.Type, point);
            }

            if (touchPointMoved)
            {
                matrix = ComputeMatrix(bitmapSize, touchPoints[0].Center, touchPoints[1].Center, touchPoints[2].Center, touchPoints[3].Center);
                canvasView.InvalidateSurface();
            }
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            // Display the bitmap using the matrix
            canvas.Save();
            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(bitmap, 0, 0);
            canvas.Restore();

            // Display the matrix in the lower-right corner
            SKSize matrixSize = MatrixDisplay.Measure(matrix);

            MatrixDisplay.Paint(canvas, matrix,
                new SKPoint(info.Width - matrixSize.Width,
                            info.Height - matrixSize.Height));

            // Display the touchpoints
            foreach (TouchPoint touchPoint in touchPoints)
            {
                touchPoint.Paint(canvas);
            }
        }

        SKMatrix ComputeMatrix(SKSize size, SKPoint ptUL, SKPoint ptUR, SKPoint ptLL, SKPoint ptLR)
        {
            // Scale transform
            SKMatrix S = SKMatrix.MakeScale(1 / size.Width, 1 / size.Height);

            // Affine transform
            SKMatrix A = new SKMatrix
            {
                ScaleX = ptUR.X - ptUL.X,
                SkewY = ptUR.Y - ptUL.Y,
                SkewX = ptLL.X - ptUL.X,
                ScaleY = ptLL.Y - ptUL.Y,
                TransX = ptUL.X,
                TransY = ptUL.Y,
                Persp2 = 1
            };

            // Non-Affine transform
            float den = A.ScaleX * A.ScaleY - A.SkewY * A.SkewX;
            float a = (A.ScaleY * ptLR.X - A.SkewX * ptLR.Y +
                       A.SkewX * A.TransY - A.ScaleY * A.TransX) / den;
            float b = (A.ScaleX * ptLR.Y - A.SkewY * ptLR.X +
                       A.SkewY * A.TransX - A.ScaleX * A.TransY) / den;

            float scaleX = a / (a + b - 1);
            float scaleY = b / (a + b - 1);

            SKMatrix N = new SKMatrix
            {
                ScaleX = scaleX,
                ScaleY = scaleY,
                Persp0 = scaleX - 1,
                Persp1 = scaleY - 1,
                Persp2 = 1
            };

            SKMatrix intermediate;
            SKMatrix.Concat(ref intermediate, N, S);
            SKMatrix result;
            SKMatrix.Concat(ref result, A, intermediate);

            return result;
        }
    }
}