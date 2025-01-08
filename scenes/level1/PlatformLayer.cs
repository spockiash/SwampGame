using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;
using SwampGame.data;

public partial class PlatformLayer : TileMapLayer
{
	[Export]
	public bool ShowDebugGraph = true;
	private const int CollisionLayer = 0;
	private const int CellIsEmpty = -1;
	private const int MaxFallScanDepth = 500;
	
	private AStar2D _astarGraph = new AStar2D();
	private Array<Vector2I> _usedTiles;
	private PackedScene _graphPoint;
	private List<PointInfo> _pointInfoList;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Initialize the list
		_pointInfoList = new List<PointInfo>();
		// Load graph point as packed scene
		_graphPoint = ResourceLoader.Load<PackedScene>("res://scenes/utilities/GraphPoint.tscn");
		// Load used tiles in current layer
		_usedTiles = GetUsedCells();
		BuildGraph();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void BuildGraph()
	{
		AddGraphPoints();
	}

	private void AddGraphPoints()
	{
		// Loop through used tiles
		foreach (var tile in _usedTiles)
		{
			AddLeftEdge(tile);
		}
	}

	#region Tile edge & Wall graph points
	private void AddLeftEdge(Vector2I tile)
	{
		// If tile above exists, it is not an edge
		if (TileAboveExist(tile))
		{
			return;
		}
		// If the tile to the left (X - 1) is empty
		if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y)) == CellIsEmpty)
		{
			var tileAbove = new Vector2I(tile.X, tile.Y - 1); // The graph points to follow, are one tile above the ground
			var existingPointId = TileAlreadyExistInGraph(tileAbove);
			// If the point has not been added
			if (existingPointId == -1)
			{
				long pointId = _astarGraph.GetAvailablePointId();
				// Create new point info object
				var pointInfo = new PointInfo(pointId, (Vector2I)MapToLocal(tileAbove));
				// Flag the tile as left edge
				pointInfo.IsLeftEdge = true;
				// Add the tile to point info list
				_pointInfoList.Add(pointInfo);
				// Ad the point to the astar graph
				_astarGraph.AddPoint(pointId, (Vector2I)MapToLocal(tileAbove));
				AddVisualPoint(tileAbove);
			}
			else
			{
				_pointInfoList.Single(x => x.PointId == existingPointId).IsLeftEdge = true; // flag the tile as left edge
				AddVisualPoint(tileAbove, new Color("#73eff7"));
			}
		}
	}

	private void AddVisualPoint(Vector2I tile, Color? color = null, float scale = 1)
	{
		// If the graph should not be shown, return
		if (!ShowDebugGraph)
		 return;
		// instantiate vis. point
		var visualPoint = _graphPoint.Instantiate() as Sprite2D;
		// null check to avoid exceptions
		if (visualPoint == null)
			return;
		// apply custom color
		if (color != null)
		{
			visualPoint.Modulate = (Color)color;
		}
		// apply custom scale
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (scale != 1.0f && scale > 0.1f)
		{
			visualPoint.Scale = new Vector2(scale, scale);
		}
		visualPoint.Position = MapToLocal(tile);
		AddChild(visualPoint);
	}

	private long TileAlreadyExistInGraph(Vector2I tileAbove)
	{
		var localPos = MapToLocal(tileAbove);
		// If the graph cointains points
		if (_astarGraph.GetPointCount() > 0)
		{
			// Find closest point to graph
			var pointId = _astarGraph.GetClosestPoint(localPos);
			// If the points have the same local coordinates
			if (_astarGraph.GetPointPosition(pointId) == localPos)
			{
				return pointId; // Return point id, the tile already exists
			}
		}
		return -1; // if the node was not found , return -1
	}

	private bool TileAboveExist(Vector2I tile)
	{
		// If a tile does not exist above (Y - 1)
		return GetCellSourceId(new Vector2I(tile.X, tile.Y - 1)) != CellIsEmpty;
	}

	#endregion

}
