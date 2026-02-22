using System;
using UnityEngine;
using SystemRandom = System.Random;

namespace Game.Domain.Battle
{
    public interface IRollSource
    {
        int Next(int minInclusive, int maxInclusive);
    }

    public sealed class SystemRandomRollSource : IRollSource
    {
        readonly SystemRandom random;

        public SystemRandomRollSource()
            : this(new SystemRandom())
        {
        }

        public SystemRandomRollSource(int seed)
            : this(new SystemRandom(seed))
        {
        }

        public SystemRandomRollSource(SystemRandom random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Next(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                Debug.LogWarning(
                    $"[SystemRandomRollSource] maxInclusive({maxInclusive}) was lower than minInclusive({minInclusive}) and has been clamped.");
                maxInclusive = minInclusive;
            }

            if (maxInclusive == int.MaxValue)
            {
                Debug.LogWarning(
                    "[SystemRandomRollSource] int.MaxValue cannot be used as inclusive upper bound and has been clamped by one.");
                return random.Next(minInclusive, int.MaxValue);
            }

            return random.Next(minInclusive, maxInclusive + 1);
        }
    }
}
