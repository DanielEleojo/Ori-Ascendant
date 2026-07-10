using System;

namespace OriAscendant.Audio
{
    /// <summary>
    /// Pure two-source crossfade math for BGM theme changes (TECH_DESIGN §4
    /// "cross-fade between BGM tracks on path change"). The MonoBehaviour feeds
    /// dt and applies the returned volumes; this holds no Unity types so it is
    /// fully headless-testable.
    /// </summary>
    public sealed class AudioCrossfade
    {
        private float _elapsed;
        private float _duration;

        public bool IsFading { get; private set; }

        /// <summary>Volume of the incoming track [0,1].</summary>
        public float IncomingVolume { get; private set; }

        /// <summary>Volume of the outgoing track [0,1].</summary>
        public float OutgoingVolume { get; private set; } = 1f;

        public void Begin(float durationSeconds)
        {
            _duration = durationSeconds > 0f ? durationSeconds : 0.0001f;
            _elapsed = 0f;
            IsFading = true;
            IncomingVolume = 0f;
            OutgoingVolume = 1f;
        }

        /// <summary>Advances the fade. Equal-power blend (incoming = sin, outgoing = cos
        /// over a quarter period) so the summed acoustic power holds steady — a linear
        /// blend sags ~-3dB at the midpoint; clamps and completes at the duration.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsFading) return;

            _elapsed += Math.Max(0f, deltaSeconds);
            float t = _elapsed / _duration;
            if (t >= 1f)
            {
                t = 1f;
                IsFading = false;
            }
            IncomingVolume = (float)Math.Sin(t * Math.PI / 2.0);
            OutgoingVolume = (float)Math.Cos(t * Math.PI / 2.0);
        }
    }
}
