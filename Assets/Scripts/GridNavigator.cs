using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class GridNavigator : MonoBehaviour
    {
        private LevelService levelService;

        public void Init(LevelService levelService)
        {
            this.levelService = levelService;
            //Init graph
            var graph = AstarPath.active.data.pointGraph;
            graph.limits = new Vector3(LevelGrid.TileSize.x, LevelGrid.TileSize.y);
            graph.Scan();

            foreach (var node in graph.nodes)
            {
                Vector3 nodeWorldPosition = (Vector3)node.position;
                Vector2Int nodeGridPosition = LevelGrid.ToGridCoordinates(nodeWorldPosition.x, nodeWorldPosition.y);
                Entity entityAtNode = levelService.GetEntityAtPosition(nodeGridPosition.x, nodeGridPosition.y);
                if (entityAtNode != null && entityAtNode.Type == EntityType.Obstacle)
                {
                    node.Walkable = false;
                }
            }
        }

        public List<Vector2Int> GetPath(Vector2Int from, Vector2Int to, int maxSteps)
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

                //Construct a path in grid coordinates, skipping the start node
                for (int nodeIdx = 1; nodeIdx < path.vectorPath.Count && nodeIdx <= maxSteps; nodeIdx++)
                {
                    Vector3 nodeWorldPosition = path.vectorPath[nodeIdx];
                    Vector2Int nodeGridPosition = LevelGrid.ToGridCoordinates(nodeWorldPosition.x, nodeWorldPosition.y);
                    gridPath.Add(nodeGridPosition);
                }
                return gridPath;
            }
        }

        public void DoActionOnNeighbours(Vector2Int nodeGridPosition, int maxDepth, bool onlyEmpty, Action<int, Vector2Int> action)
        {
            Vector3 nodePositionWorld = LevelGrid.ToWorldCoordinates(nodeGridPosition);
            GraphNode selectedNode = AstarPath.active.data.pointGraph.GetNearest(nodePositionWorld).node;
            if (selectedNode == null)
            {
                Debug.LogErrorFormat("Can't find graph node at point {0}", nodePositionWorld);
            }
            else
            {
                Dictionary<GraphNode, int> visitedNodes = new Dictionary<GraphNode, int>();
                visitedNodes.Add(selectedNode, 0);
                DoActionOnNeighboursInternal(selectedNode, 1, maxDepth, onlyEmpty, visitedNodes, action);
            }
        }

        private void DoActionOnNeighboursInternal(GraphNode node, int currentDepth, int maxDepth, bool onlyEmptyNodes, Dictionary<GraphNode, int> visitedNodes, Action<int, Vector2Int> action)
        {
            if (currentDepth <= maxDepth)
            {
                node.GetConnections((neighbour) =>
                {
                    if (visitedNodes.ContainsKey(neighbour) == false || visitedNodes[neighbour] > currentDepth)
                    {
                        if (visitedNodes.ContainsKey(neighbour) == false)
                        {
                            visitedNodes.Add(neighbour, currentDepth);
                        }
                        else
                        {
                            visitedNodes[neighbour] = currentDepth;
                        }
                        Vector3 neighbourWorldPosition = (Vector3)neighbour.position;
                        Vector2Int neigbourGridCoordinates = LevelGrid.ToGridCoordinates(neighbourWorldPosition.x, neighbourWorldPosition.y);
                        Entity entityAtNode = levelService.GetEntityAtPosition(neigbourGridCoordinates.x, neigbourGridCoordinates.y);
                        bool nodeAcceptable = onlyEmptyNodes == false || entityAtNode == null;
                        if (nodeAcceptable)
                        {
                            action(currentDepth, neigbourGridCoordinates);
                            DoActionOnNeighboursInternal(neighbour, currentDepth + 1, maxDepth, onlyEmptyNodes, visitedNodes, action);
                        }
                    }
                });

            }
        }
    }
}