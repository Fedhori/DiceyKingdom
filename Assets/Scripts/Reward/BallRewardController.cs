using System;
using UnityEngine;

public sealed class BallRewardController : MonoBehaviour
{
    string ballId;
    int ballCount;
    Action<string, int> onSelected;

    public void Initialize(string ballId, int ballCount, Action<string, int> onSelected)
    {
        this.ballId = ballId;
        this.ballCount = ballCount;
        this.onSelected = onSelected;
    }
}
