using UnityEngine;

public enum BladeHand
{
    Right,
    Left
}

public struct BladePose3D
{
    public BladePose3D(BladeHand hand, Vector3 position, Quaternion rotation, Vector3 direction, bool active)
    {
        Hand = hand;
        Position = position;
        Rotation = rotation;
        Direction = direction;
        Active = active;
    }

    public BladeHand Hand { get; }
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Direction { get; }
    public bool Active { get; }
}
