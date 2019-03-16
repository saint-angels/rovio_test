using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNavigator : MonoBehaviour
{
    public void Init(LevelService levelService)
    {
        //Init graph
        var graph = AstarPath.active.data.pointGraph;
        graph.limits = new Vector3(LevelGrid.TileSize.x, LevelGrid.TileSize.y);
        graph.Scan();

        foreach (var node in graph.nodes)
        {
            Vector3 nodeWorldPosition = (Vector3)node.position;
            Vector2Int nodeGridPosition = LevelGrid.ToGridCoordinates(nodeWorldPosition.x, nodeWorldPosition.y);
            EntityComponent entityAtNode = levelService.GetEntityAtPosition(nodeGridPosition.x, nodeGridPosition.y);
            if (entityAtNode != null && entityAtNode.Type == EntityType.Obstacle)
            {
                node.Walkable = false;
            }
        }
    }

    public List<Vector2Int> GetPath(Vector2Int from, Vector2Int to)
    {
        Vector2 fromWorldPosition = LevelGrid.ToWorldCoordinates(from);
        Vector2 toWorldPosition = LevelGrid.ToWorldCoordinates(to);

        var path = ABPath.Construct(fromWorldPosition, toWorldPosition, null);
        AstarPath.StartPath(path);
        path.BlockUntilCalculated();

        if (path.error)
        {
            Debug.LogErrorFormat("No path was found from:{0} to:{1}", from, to);
            return null;
        }
        else
        {
            List<Vector2Int> gridPath = new List<Vector2Int>();
            foreach (Vector3 nodePosition in path.vectorPath)
            {
                Vector2Int nodeGridPosition = LevelGrid.ToGridCoordinates(nodePosition.x, nodePosition.y);
                gridPath.Add(nodeGridPosition);
            }
            return gridPath;
        }
    }

    public void ApplyActionOnNeighbours(Vector2 worldPosition, int maxDepth, Action<int, Vector2Int> action)
    {
        GraphNode selectedNode = AstarPath.active.data.pointGraph.GetNearest(worldPosition).node;
        if (selectedNode == null)
        {
            Debug.LogErrorFormat("Can't find graph node at point {0}", worldPosition);
        }
        else
        {
            SelectNeighbours(selectedNode, 1, maxDepth, action);
        }
    }

    private void SelectNeighbours(GraphNode node, int currentDepth, int maxDepth, Action<int, Vector2Int> action)
    {
        if (currentDepth <= maxDepth)
        {
            node.GetConnections((neighbour) =>
            {
                if (neighbour.Walkable)
                {
                    Vector3 neighbourWorldPosition = (Vector3)neighbour.position;
                    Vector2Int neigbourGridCoordinates = LevelGrid.ToGridCoordinates(neighbourWorldPosition.x, neighbourWorldPosition.y);
                    action(currentDepth, neigbourGridCoordinates);
                    SelectNeighbours(neighbour, currentDepth + 1, maxDepth, action);
                }
            });

        }
    }
}
