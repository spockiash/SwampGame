#nullable enable
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
	
	[Export]
	public int JumpDistance = 5;                       // Distance between two tiles to count as a jump
	[Export]
	public int JumpHeight = 4;   
	private const int CollisionLayer = 0;
	private const int CellIsEmpty = -1;
	private const int MaxFallScanDepth = 500;
	
	private AStar2D _astarGraph = new AStar2D();
	private Array<Vector2I> _usedTiles;
	private PackedScene _graphPoint;
	private List<PointInfo> _pointInfoList;

	private List<PointInfo>? _pathfindEnemyDebug; //used for debugging, set only per one enemy

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

	public override void _Draw()
	{
		if (ShowDebugGraph)
		{
			ConnectPoints();
			DrawEnemyLine();	
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetEnemyPath(List<PointInfo> pathInfo)
	{

			_pathfindEnemyDebug = pathInfo;
			QueueRedraw();

	}

	public List<PointInfo>? GetEnemyPath()
	{
		return _pathfindEnemyDebug;
	}

	public void DrawEnemyLine()
	{
		if (_pathfindEnemyDebug == null || _pathfindEnemyDebug.Count == 0)
			return;
		PointInfo? p1 = null;
		PointInfo? p2 = null;
		foreach (var point in _pathfindEnemyDebug)
		{
			if (p1 == null)
			{
				p1 = point;
			}
			else
			{
				p2 = point;
			}

			if (p1 != null && p2 != null)
			{
				DrawDebugLine(p1.Position, p2.Position, new Color("#ff1100"));
				p1 = null;
				p2 = null;
			}
		}
	}
	
	// Expose a method to get the closest graph point
	public long GetClosestGraphPoint(Vector2 position)
	{
		return _astarGraph.GetClosestPoint(position);
	}
	
	private PointInfo GetPointInfoAtPosition(Vector2 position)
	{
		var newInfoPoint = new PointInfo(-10000, position);     // Create a new PointInfo with the position
		newInfoPoint.IsPositionPoint = true;                    // Mark it as a position point
		var tile = LocalToMap(position);                        // Get the tile position		

		// If a tile is found below
		if (GetCellSourceId(new Vector2I(tile.X, tile.Y + 1)) != CellIsEmpty)
		{
			// If a tile exist to the left
			if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y)) != CellIsEmpty)
			{
				newInfoPoint.IsLeftWall = true;   // Flag that it's a left wall
			}
			// If a tile exist to the right
			if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y)) != CellIsEmpty)
			{
				newInfoPoint.IsRightWall = true;  // Flag that it's a right wall
			}
			// If a tile doesn't exist one tile below to the left
			if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y + 1)) != CellIsEmpty)
			{
				newInfoPoint.IsLeftEdge = true;  // Flag that it's a left edge
			}
			// If a tile doesn't exist one tile below to the right
			if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y + 1)) != CellIsEmpty)
			{
				newInfoPoint.IsRightEdge = true;  // Flag that it's a right edge
			}
		}
		return newInfoPoint;
	}
	
	private System.Collections.Generic.Stack<PointInfo> ReversePathStack(System.Collections.Generic.Stack<PointInfo> pathStack)
	{
		System.Collections.Generic.Stack<PointInfo> pathStackReversed = new System.Collections.Generic.Stack<PointInfo>();
		// Reverse the path stack. It is like emptying a bucket of stones from one bucket to another. 
		// The stones at the top, will be in the bottom of the other bucket.
		while (pathStack.Count != 0)
		{
			pathStackReversed.Push(pathStack.Pop());
		}
		return pathStackReversed;
	}

	public Stack<PointInfo> GetPlaform2DPath(Vector2 startPos, Vector2 endPos)
	{
		Stack<PointInfo> pathStack = new Stack<PointInfo>();
		// Find the path between the start and end position
		var idPath = _astarGraph.GetIdPath(_astarGraph.GetClosestPoint(startPos), _astarGraph.GetClosestPoint(endPos));

		if (idPath.Count() <= 0) { return pathStack; }      // If the the path has reached its goal, return the empty path stack

		var startPoint = GetPointInfoAtPosition(startPos);  // Create the point for the start position		
		var endPoint = GetPointInfoAtPosition(endPos);      // Create the point for the end position		
		var numPointsInPath = idPath.Count();               // Get number of points in the astar path

		// loop through all the points in the path
		for (int i = 0; i < numPointsInPath; ++i)
		{
			var currPoint = GetInfoPointByPointId(idPath[i]);   // Get the current point in the idPath		

			// If there's only one point in the path
			if (numPointsInPath == 1)
			{
				continue;   // Skip the point in the aStar path, the end point will be added as the only path point at the end.
			}
			// If it's the first point in the astar path
			if (i == 0 && numPointsInPath >= 2)
			{
				// Get the next second path point in the astar path
				var secondPathPoint = GetInfoPointByPointId(idPath[i + 1]);

				// If the start point is closer to the second path point than the current point
				if (startPoint.Position.DistanceTo(secondPathPoint.Position) < currPoint.Position.DistanceTo(secondPathPoint.Position))
				{
					pathStack.Push(startPoint); // Add the start point to the path
					continue;                   // Skip adding the current point and go to the next point in the path
				}
			}
			// If it's the last point in the path 
			else if (i == numPointsInPath - 1 && numPointsInPath >= 2)
			{
				// Get the penultimate point in the astar path list
				var penultimatePoint = GetInfoPointByPointId(idPath[i - 1]);

				// If the endPoint is closer than the last point in the astar path
				if (endPoint.Position.DistanceTo(penultimatePoint.Position) < currPoint.Position.DistanceTo(penultimatePoint.Position))
				{
					continue;                   // Skip addig the last point to the path stack
				}
				// If the last point is closer
				else
				{
					pathStack.Push(currPoint);  // Add the current point to the path stack
					break;                      // Break out of the for loop
				}
			}

			pathStack.Push(currPoint);      // Add the current point			
		}
		pathStack.Push(endPoint);           // Add the end point to the path		
		return ReversePathStack(pathStack); // Return the pathstack reversed		
	}
	
	private PointInfo GetInfoPointByPointId(long pointId)
	{
		// Find and return the first point in the _pointInfoList with the given pointId
		return _pointInfoList.Where(p => p.PointId == pointId).FirstOrDefault();
	}
	
	// Expose a method to get the path between two points
	public Vector2[] GetPath(Vector2 fromPosition, Vector2 toPosition)
	{
		long startPoint = _astarGraph.GetClosestPoint(fromPosition);
		long endPoint = _astarGraph.GetClosestPoint(toPosition);

		if (startPoint != -1 && endPoint != -1)
		{
			return _astarGraph.GetPointPath(startPoint, endPoint).ToArray();
		}

		return new Vector2[0];
	}

	private void BuildGraph()
	{
		AddGraphPoints();
		// If the debug graph should not be shown
		if (!ShowDebugGraph)
		{
			ConnectPoints();    // Connect the points
		}
	}

	private void DrawDebugLine(Vector2 to, Vector2 from, Color color)
	{
		if(!ShowDebugGraph)
			return;
		DrawLine(to, from, color);
	}

	private void AddGraphPoints()
	{
		// Loop through used tiles
		foreach (var tile in _usedTiles)
		{
			AddLeftEdge(tile);
			AddRightEdge(tile);
			AddLeftWallPoint(tile);
			AddRightWallPoint(tile);
			AddFallPoint(tile);
		}
	}

	private PointInfo GetPointInfo(Vector2I tile)
	{
		foreach (var pointInfo in _pointInfoList)
		{
			if (pointInfo.Position == MapToLocal(tile))
			{
				return pointInfo;
			}
		}
		return null;
	}

	private Vector2I? FindFallPoint(Vector2 tile)
	{
		var scan = GetStartScanTileForFallPoint((Vector2I)tile);// Get the start scan tile position
		if (scan == null) { return null; }                      // If it wasn't found, return out of the method

		var tileScan = (Vector2I)scan;                          // Typecast nullable Vector2I? to Vector2I
		Vector2I? fallTile = null;                              // Initialize the falltile to null

		// Loop, and start to look for a solid tile
		for (int i = 0; i < MaxFallScanDepth; ++i)
		{
			// If the tile cell below is solid
			if (GetCellSourceId(new Vector2I(tileScan.X, tileScan.Y + 1)) != CellIsEmpty)
			{
				fallTile = tileScan;    // The fall tile was found
				break;                  // Break out of the for loop
			}
			// If a solid tile was not found, scan the next tile below the current one
			tileScan.Y++;
		}
		return fallTile;    // return the fall tile result
	}

	#region Connect graph points

	public void ConnectPoints()
	{
		foreach (var point1 in _pointInfoList)
		{
			ConnectHorizontalPoints(point1);
			ConnectJumpPoints(point1);
			ConnectFallPoint(point1);
		}
	}
	private void ConnectFallPoint(PointInfo p1)
	{
		if (p1.IsLeftEdge || p1.IsRightEdge)
		{
			var tilePos = LocalToMap(p1.Position);
			// FindFallPoint expects the exact tile coordinate. The points in the graph is one tile above: y-1			
			// Therefore we adjust the y position with: Y += 1
			tilePos.Y += 1;

			Vector2I? fallPoint = FindFallPoint(tilePos);
			if (fallPoint != null)
			{
				var pointInfo = GetPointInfo((Vector2I)fallPoint);
				Vector2 p2Map = LocalToMap(p1.Position);
				Vector2 p1Map = LocalToMap(pointInfo.Position);

				if (p1Map.DistanceTo(p2Map) <= JumpHeight)
				{
					_astarGraph.ConnectPoints(p1.PointId, pointInfo.PointId);                       // Connect the points
					DrawDebugLine(p1.Position, pointInfo.Position, new Color(0, 1, 0, 1));          // Draw a Green line between the points
				}
				else
				{
					_astarGraph.ConnectPoints(p1.PointId, pointInfo.PointId, bidirectional: false);  // Only allow edge -> fallTile direction
					DrawDebugLine(p1.Position, pointInfo.Position, new Color(1, 1, 0, 1));          // Draw a yellow line between the points									
				}
			}
		}
	}
	private void ConnectJumpPoints(PointInfo p1)
	{
		foreach (var p2 in _pointInfoList)
		{
			ConnectHorizontalPlatformJumps(p1, p2);
			ConnectDiagonalJumpRightEdgeToLeftEdge(p1, p2);
			ConnectDiagonalJumpLeftEdgeToRightEdge(p1, p2);
		}
	}
	private void ConnectDiagonalJumpRightEdgeToLeftEdge(PointInfo p1, PointInfo p2)
	{
		if (p1.IsRightEdge)
		{
			Vector2 p1Map = LocalToMap(p1.Position);
			Vector2 p2Map = LocalToMap(p2.Position);

			if (p2.IsLeftEdge                                                   // If the p2 tile is a right edge
			    && p2.Position.X > p1.Position.X                                    // And the p2 tile is to the right of the p1 tile
			    && p2.Position.Y > p1.Position.Y                                    // And the p2 tile is below the p1 tile
			    && p2Map.DistanceTo(p1Map) < JumpDistance)                          // And the distance between the p2 and p1 map position is within jump reach
			{
				_astarGraph.ConnectPoints(p1.PointId, p2.PointId);              // Connect the points
				DrawDebugLine(p1.Position, p2.Position, new Color(0, 1, 0, 1)); // Draw a green line between the points
			}
		}
	}

	private void ConnectDiagonalJumpLeftEdgeToRightEdge(PointInfo p1, PointInfo p2)
	{
		if (p1.IsLeftEdge)
		{
			Vector2 p1Map = LocalToMap(p1.Position);
			Vector2 p2Map = LocalToMap(p2.Position);
			if (p2.IsRightEdge                                                  // If the p2 tile is a right edge
			    && p2.Position.X < p1.Position.X                                    // and the p2 tile is to the left of the p1 tile
			    && p2.Position.Y > p1.Position.Y                                    // and the p2 tile is below the p1 tile
			    && p2Map.DistanceTo(p1Map) < JumpDistance)                          // And the distance between the p2 and p1 map position is within jump reach
			{
				_astarGraph.ConnectPoints(p1.PointId, p2.PointId);              // Connect the points
				DrawDebugLine(p1.Position, p2.Position, new Color(0, 1, 0, 1)); // Draw a green line between the points
			}
		}
	}
	private void ConnectHorizontalPlatformJumps(PointInfo p1, PointInfo p2)
	{
		if (p1.PointId == p2.PointId) { return; } // If the points are the same, return out of the method

		// If the points are on the same height and p1 is a right edge, and p2 is a left edge	
		if (p2.Position.Y == p1.Position.Y && p1.IsRightEdge && p2.IsLeftEdge)
		{
			// If the p2 position is to the right of the p1 position
			if (p2.Position.X > p1.Position.X)
			{
				Vector2 p2Map = LocalToMap(p2.Position);    // Get the p2 tile position
				Vector2 p1Map = LocalToMap(p1.Position);    // Get the p1 tile position				

				// If the distance between the p2 and p1 map position are within jump reach
				if (p2Map.DistanceTo(p1Map) < JumpDistance + 1)
				{
					_astarGraph.ConnectPoints(p1.PointId, p2.PointId);              // Connect the points
					DrawDebugLine(p1.Position, p2.Position, new Color(0, 1, 0, 1)); // Draw a green line between the points
				}
			}
		}
	}
	
	private void ConnectHorizontalPoints(PointInfo p1)
	{
		if (p1.IsLeftEdge || p1.IsLeftWall || p1.IsFallTile)
		{
			PointInfo closest = null;

			foreach (var p2 in _pointInfoList)
			{
				if (p1.PointId == p2.PointId) continue; // Skip self

				// Ensure points are on the same height and valid for horizontal connections
				if ((p2.IsRightEdge || p2.IsRightWall || p2.IsFallTile) &&
				    p2.Position.Y == p1.Position.Y &&
				    p2.Position.X > p1.Position.X)
				{
					if (closest == null || p2.Position.X < closest.Position.X)
					{
						closest = p2; // Update closest point
					}
				}
			}

			if (closest != null && !HorizontalConnectionCannotBeMade(LocalToMap(p1.Position), LocalToMap(closest.Position)))
			{
				_astarGraph.ConnectPoints(p1.PointId, closest.PointId); // Connect nodes
				DrawDebugLine(p1.Position, closest.Position, new Color(0, 1, 0, 1)); // Draw connection
			}
		}
	}

	
	private bool HorizontalConnectionCannotBeMade(Vector2I p1, Vector2I p2)
	{
		// Convert the position to tile coordinates
		Vector2I startScan = LocalToMap(p1);
		Vector2I endScan = LocalToMap(p2);

		// Loop through all tiles between the points
		for (int i = startScan.X; i < endScan.X; ++i)
		{
			if (GetCellSourceId(new Vector2I(i, startScan.Y)) != CellIsEmpty         // If the cell is not empty (a wall)
			    || GetCellSourceId(new Vector2I(i, startScan.Y + 1)) == CellIsEmpty)     // or the cell below is empty (an edge tile)
			{
				return true;    // Return true, the connection cannot be made
			}
		}
		return false;
	}

	#endregion
	
	#region Tile fall points
	private void AddFallPoint(Vector2I tile)
	{
		Vector2I? fallTile = FindFallPoint(tile);                                           // Find the fall tile point
		if (fallTile == null) { return; }                                                   // If the fall tile was not found, return out of the method
		var fallTileLocal = (Vector2I)MapToLocal((Vector2I)fallTile);                       // Get the local coordinates for the fall tile

		long existingPointId = TileAlreadyExistInGraph((Vector2I)fallTile);                 // Check if the point already has been added

		// If the tile doesn't exist in the graph already
		if (existingPointId == -1)
		{
			long pointId = _astarGraph.GetAvailablePointId();                               // Get the next available point id
			var pointInfo = new PointInfo(pointId, fallTileLocal);                          // Create point information, and pass in the pointId and tile
			pointInfo.IsFallTile = true;                                                    // Flag that the tile is a fall tile
			_pointInfoList.Add(pointInfo);                                                  // Add the tile to the point info list
			_astarGraph.AddPoint(pointId, fallTileLocal);                                   // Add the point to the Astar graph, in local coordinates
			AddVisualPoint((Vector2I)fallTile, new Color(1, 0.35f, 0.1f, 1), scale: 0.35f); // Add the point visually to the map (if ShowDebugGraph = true)
		}
		else
		{
			_pointInfoList.Single(x => x.PointId == existingPointId).IsFallTile = true;     // flag that it's a fall point			
			var updateInfo = _pointInfoList.Find(x => x.PointId == existingPointId);        // Find the existing point info
			updateInfo.IsFallTile = true;                                                   // Flag that it's a fall tile				
			AddVisualPoint((Vector2I)fallTile, new Color("#ef7d57"), scale: 0.30f);         // Add the point visually to the map (if ShowDebugGraph = true)
		}
	}
	
	private Vector2I? GetStartScanTileForFallPoint(Vector2I tile)
	{
		var tileAbove = new Vector2I(tile.X, tile.Y - 1);
		var point = GetPointInfo(tileAbove);
		
		if(point == null)
			return null;

		var tileScan = Vector2I.Zero;
		// If the point is left edge
		if (point.IsLeftEdge)
		{
			tileScan = new Vector2I(tile.X - 1, tile.Y - 1); // Set the start position to start scanning to the left
			return tileScan;
		}
		//if the point is right edge
		else if (point.IsRightEdge)
		{
			tileScan = new Vector2I(tile.X + 1, tile.Y - 1); // set scan start to the right of the edge
			return tileScan;
		}
		return null;
	}
	#endregion
	
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
	
	private void AddRightEdge(Vector2I tile)
	{
		// If tile above exists, it is not an edge
		if (TileAboveExist(tile))
		{
			return;
		}
		// If the tile to the right (X + 1) is empty
		if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y)) == CellIsEmpty)
		{
			var tileAbove = new Vector2I(tile.X, tile.Y - 1); // The graph points to follow, are one tile above the ground
			var existingPointId = TileAlreadyExistInGraph(tileAbove);
			// If the point has not been added
			if (existingPointId == -1)
			{
				long pointId = _astarGraph.GetAvailablePointId();
				// Create new point info object
				var pointInfo = new PointInfo(pointId, (Vector2I)MapToLocal(tileAbove));
				// Flag the tile as right edge
				pointInfo.IsRightEdge = true;
				// Add the tile to point info list
				_pointInfoList.Add(pointInfo);
				// Ad the point to the astar graph
				_astarGraph.AddPoint(pointId, (Vector2I)MapToLocal(tileAbove));
				AddVisualPoint(tileAbove, new Color("#94b0c2"));
			}
			else
			{
				_pointInfoList.Single(x => x.PointId == existingPointId).IsRightEdge = true; // flag the tile as left edge
				AddVisualPoint(tileAbove, new Color("#ffcd75"));
			}
		}
	}
	
	private void AddLeftWallPoint(Vector2I tile)
	{
		// If tile above exists, it is not an edge
		if (TileAboveExist(tile))
		{
			return;
		}
		// If the tile to the up-left (X - 1, Y - 1) is  not empty
		if (GetCellSourceId(new Vector2I(tile.X - 1, tile.Y - 1)) != CellIsEmpty)
		{
			var tileAbove = new Vector2I(tile.X, tile.Y - 1); // The graph points to follow, are one tile above the ground
			var existingPointId = TileAlreadyExistInGraph(tileAbove);
			// If the point has not been added
			if (existingPointId == -1)
			{
				long pointId = _astarGraph.GetAvailablePointId();
				// Create new point info object
				var pointInfo = new PointInfo(pointId, (Vector2I)MapToLocal(tileAbove));
				// Flag the tile as left wall
				pointInfo.IsLeftWall = true;
				// Add the tile to point info list
				_pointInfoList.Add(pointInfo);
				// Ad the point to the astar graph
				_astarGraph.AddPoint(pointId, (Vector2I)MapToLocal(tileAbove));
				// Add black point
				AddVisualPoint(tileAbove, new Color(0,0,0), scale: 0.4f);
			}
			else
			{
				_pointInfoList.Single(x => x.PointId == existingPointId).IsLeftWall = true; // flag the tile as left edge
				AddVisualPoint(tileAbove, new Color("#0066FF")); // add blue point to the shared location
			}
		}
	}
	
	private void AddRightWallPoint(Vector2I tile)
	{
		// If tile above exists, it is not an edge
		if (TileAboveExist(tile))
		{
			return;
		}
		// If the tile to the up-right (X + 1, Y - 1) is  not empty
		if (GetCellSourceId(new Vector2I(tile.X + 1, tile.Y - 1)) != CellIsEmpty)
		{
			var tileAbove = new Vector2I(tile.X, tile.Y - 1); // The graph points to follow, are one tile above the ground
			var existingPointId = TileAlreadyExistInGraph(tileAbove);
			// If the point has not been added
			if (existingPointId == -1)
			{
				long pointId = _astarGraph.GetAvailablePointId();
				// Create new point info object
				var pointInfo = new PointInfo(pointId, (Vector2I)MapToLocal(tileAbove));
				// Flag the tile as left wall
				pointInfo.IsRightWall = true;
				// Add the tile to point info list
				_pointInfoList.Add(pointInfo);
				// Ad the point to the astar graph
				_astarGraph.AddPoint(pointId, (Vector2I)MapToLocal(tileAbove));
				// Add black point
				AddVisualPoint(tileAbove, new Color(0,0,0), scale: 0.5f);
			}
			else
			{
				_pointInfoList.Single(x => x.PointId == existingPointId).IsRightWall = true; // flag the tile as left edge
				AddVisualPoint(tileAbove, new Color("#CC00FF")); // add purple point to the shared location
			}
		}
	}
	#endregion
	
	private void AddVisualPoint(Vector2I tile, Color? color = null, float scale = 1)
	{
		GD.Print($"Scale applied: {scale}");
		// If the graph should not be shown, return
		if (!ShowDebugGraph)
			return;
		// instantiate vis. point
		var visualPoint = _graphPoint.Instantiate() as Sprite2D;
		// null check to avoid exceptions
		if (visualPoint == null)
		{
			GD.PrintErr("Failed to instantiate visual point");
			return;
		}
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
	
	private bool HasClearLineOfSight(Vector2 start, Vector2 end, uint collisionMask = uint.MaxValue,
		bool collideWithAreas = false, bool collideWithBodies = true)
	{
		var spaceState = GetWorld2D().DirectSpaceState;

		// Create ray parameters
		var rayParams = PhysicsRayQueryParameters2D.Create(start, end, collisionMask);
		rayParams.CollideWithAreas = collideWithAreas;
		rayParams.CollideWithBodies = collideWithBodies;

		// Perform the raycast
		var result = spaceState.IntersectRay(rayParams);

		// If the result has no hits, line of sight is clear
		return result.Count == 0;
	}
}
