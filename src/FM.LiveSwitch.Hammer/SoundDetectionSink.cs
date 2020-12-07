using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FM.LiveSwitch.Hammer
{
    class SoundDetectionSink : NullAudioSink
    {
        public override string Label => "Sound Detection Sink";

        public TaskCompletionSource<bool> SoundDetected { get; set; }

        public override bool ProcessFrame(AudioFrame frame)
        {
            var dataBuffer = frame.LastBuffer.DataBuffer;

            var samples = new List<int>();
            for (var i = 0; i < dataBuffer.Length; i += sizeof(short))
            {
                samples.Add(dataBuffer.Read16Signed(i));
            }

            var max = samples.Max();
            var min = samples.Min();
            if (min < -1 && max > 1) // not silent
            {
                var soundDetected = SoundDetected;
                if (soundDetected != null && !soundDetected.Task.IsCompleted)
                {
                    soundDetected.TrySetResult(true);
                }
            }

            return base.ProcessFrame(frame);
        }
    }
}
