namespace OriAscendant.Systems
{
    /// <summary>Injectable randomness seam so Tribulation resolution is
    /// deterministic under test. Production uses <see cref="UnityRandomSource"/>.</summary>
    public interface IRandomSource
    {
        /// <summary>Uniform value in [0, 1).</summary>
        double NextDouble();
    }

    public sealed class UnityRandomSource : IRandomSource
    {
        public double NextDouble() => UnityEngine.Random.value * 0.9999999999; // clamp away from 1.0
    }
}
