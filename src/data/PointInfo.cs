using Godot;

namespace SwampGame.data;

public class PointInfo
{
    public bool IsFallTile;
    public bool IsLeftEdge;
    public bool IsRightEdge;
    public bool IsLeftWall;
    public bool IsRightWall;
    public bool IsPositionPoint;
    public long PointId;
    public Vector2 Position;

    public PointInfo()
    {
        
    }

    public PointInfo(long pointId, Vector2 position)
    {
        PointId = pointId;
        Position = position;
    }
}