using UnityEngine;

namespace Game.UI.Tooltip
{
    public readonly struct TooltipPresentationOptions
    {
        public bool useShowDelayOverride { get; }
        public float showDelayOverride { get; }
        public bool useScreenOffsetOverride { get; }
        public Vector2 screenOffsetOverride { get; }
        public bool useEdgePaddingOverride { get; }
        public float edgePaddingOverride { get; }

        public TooltipPresentationOptions(
            bool useShowDelayOverride,
            float showDelayOverride,
            bool useScreenOffsetOverride,
            Vector2 screenOffsetOverride,
            bool useEdgePaddingOverride,
            float edgePaddingOverride)
        {
            this.useShowDelayOverride = useShowDelayOverride;
            this.showDelayOverride = showDelayOverride;
            this.useScreenOffsetOverride = useScreenOffsetOverride;
            this.screenOffsetOverride = screenOffsetOverride;
            this.useEdgePaddingOverride = useEdgePaddingOverride;
            this.edgePaddingOverride = edgePaddingOverride;
        }
    }
}
