using System;
using System.Linq;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class ImageDetectionSink : NullVideoSink
    {
        public override string Label => "Image Detection Sink";

        public TaskCompletionSource<bool> ImageDetected { get; set; }

        public override bool ProcessFrame(VideoFrame frame)
        {
            var buffer = frame.LastBuffer;

            var point = new Point(buffer.Width / 2, buffer.Height / 2);

            int r, g, b;
            if (buffer.IsRgbType)
            {
                var index = point.Y * buffer.Width + point.X;
                r = buffer.GetRValue(index);
                g = buffer.GetGValue(index);
                b = buffer.GetBValue(index);
            }
            else
            {
                var yIndex = point.Y * buffer.Width + point.X;
                var uvIndex = point.Y / 2 * buffer.Width / 2 + point.X / 2;

                var y = buffer.GetYValue(yIndex);
                var u = buffer.GetUValue(uvIndex);
                var v = buffer.GetVValue(uvIndex);

                // Rec.601
                var kr = 0.299;
                var kg = 0.587;
                var kb = 0.114;

                r = Math.Max(0, Math.Min(255, (int)(y + 2 * (v - 128) * (1 - kr))));
                g = Math.Max(0, Math.Min(255, (int)(y - 2 * (u - 128) * (1 - kb) * kb / kg - 2 * (v - 128) * (1 - kr) * kr / kg)));
                b = Math.Max(0, Math.Min(255, (int)(y + 2 * (u - 128) * (1 - kb))));
            }

            var max = new[] { r, g, b }.Max();
            if (max > 15) // not black
            {
                var imageDetected = ImageDetected;
                if (imageDetected != null && !imageDetected.Task.IsCompleted)
                {
                    imageDetected.TrySetResult(true);
                }
            }

            return base.ProcessFrame(frame);
        }
    }
}
