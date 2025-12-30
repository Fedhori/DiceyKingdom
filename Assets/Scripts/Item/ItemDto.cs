using System;

namespace Data
{
    [Serializable]
    public sealed class ItemDto
    {
        public string id;
        public float damageMultiplier = 1f;
        public float attackSpeed = 1f;
        public float bulletSize = 1f;
        public float bulletSpeed = 1f;
    }
}
