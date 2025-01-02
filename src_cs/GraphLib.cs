﻿using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace src_cs
{
    /// <summary>
    /// 顶点类
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int index;

        /// <summary>
        /// 坐标
        /// </summary>
        public Point coordinates;

        /// <summary>
        /// 与该顶点相连的边
        /// </summary>
        public List<Edge> edges;

        public Vertex(int index)
        {
            this.index = index;
            this.edges = new List<Edge>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="index">节点序号</param>
        /// <param name="coordinates">节点坐标</param>
        public Vertex(int index, Point coordinates)
        {
            this.index = index;
            this.edges = new List<Edge>();
            this.coordinates = coordinates;
        }

        public void AddEdge(Edge edge)
        {
            edges.Add(edge);
        }
    }

    public sealed class StagingVertex : Vertex
    {
        public StagingVertex(int index) : base(index)
        {
        }

        public StagingVertex(int index, Point coordinates) : base(index, coordinates)
        {
        }
    }

    /// <summary>
    /// 存储货架顶点
    /// </summary>
    public sealed class StorageVertex : Vertex
    {
        public int[,] itemsLeft;
        public int[,] itemsRight;

        public StorageVertex(int index, int[,] left, int[,] right, Point coord) : base(index, coord)
        {
            this.itemsLeft = left;
            this.itemsRight = right;
        }
    }

    public sealed class Edge
    {
        public int x;
        public int y;
        public int cost;

        public Edge(int x, int y, int cost)
        {
            this.x = x;
            this.y = y;
            this.cost = cost;
        }
    }

    /// <summary>
    /// 图类
    /// </summary>
    public sealed class Graph
    {
        private int[][] distancesCache;
        private int[][][] routesCache;
        private AStarNode[] queueCacheA;
        private AStarNode[] emptyArr;
        private AStarNodeFactory nodeFactory;
        private FastPriorityQueue<AStarNode> aStarQueue;

        public List<Vertex> vertices;
        public List<Edge> edges;
        // public Order[] orders;

        public Graph()
        {
            this.vertices = new List<Vertex>();
            this.edges = new List<Edge>();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="grid">顶点网络二维数组</param>
        public Graph(Vertex[,] grid) : this()
        {
            int x = grid.GetLength(0);
            int y = grid.GetLength(1);

            // Go through the grid and add Edges and Vertices
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    if (grid[i, j] == null) continue;

                    var vertex = grid[i, j];
                    vertices.Add(vertex);

                    if (i != 0)
                    {
                        if (grid[i - 1, j] != null)
                        {
                            Edge e = new Edge(grid[i - 1, j].index, vertex.index, 1);
                            AddEdge(e);
                        }
                    }
                    if (j != 0)
                    {
                        if (grid[i, j - 1] != null)
                        {
                            Edge e = new Edge(grid[i, j - 1].index, vertex.index, 1);
                            AddEdge(e);
                        }
                    }
                }
            }
        }

        public void AddVertex(Vertex vertex)
        {
            vertex.index = vertices.Count;
            vertices.Add(vertex);
        }

        public void AddEdge(Edge edge)
        {
            edges.Add(edge);
            vertices[edge.x].AddEdge(edge);
            vertices[edge.y].AddEdge(edge);
        }

        public void Initialize(OrderInstance[][] orderInstances)
        {
            aStarQueue = new FastPriorityQueue<AStarNode>(2 * vertices.Count);
            nodeFactory = new AStarNodeFactory(2 * vertices.Count);
            queueCacheA = new AStarNode[vertices.Count];
            emptyArr = new AStarNode[vertices.Count];

            // Init cache arrays
            distancesCache = new int[vertices.Count][];
            routesCache = new int[vertices.Count][][];
            for (int i = 0; i < this.vertices.Count; i++)
            {
                distancesCache[i] = new int[this.vertices.Count];
                routesCache[i] = new int[this.vertices.Count][];
            }

            InitTourRoutes(orderInstances);

            void InitTourRoutes(OrderInstance[][] orders)
            {
                HashSet<int> verticesUsed = new HashSet<int>();
                int[][][] orderVertices = new int[orders.Length][][];
                // Init shortest distances for pick destinations
                Queue<QueueNode> q = new Queue<QueueNode>(vertices.Count);
                for (int i = 0; i < orders.Length; i++)
                {
                    orderVertices[i] = new int[orders[i].Length][];
                    for (int j = 0; j < orders[i].Length; j++)
                    {
                        orderVertices[i][j] = new int[orders[i][j].vertices.Length + 2];
                        verticesUsed.Add(orders[i][j].startLoc);
                        verticesUsed.Add(orders[i][j].targetLoc);
                        orderVertices[i][j][0] = orders[i][j].startLoc;
                        orderVertices[i][j][1] = orders[i][j].targetLoc;
                        for (int k = 0; k < orders[i][j].vertices.Length; k++)
                        {
                            verticesUsed.Add(orders[i][j].vertices[k]);
                            orderVertices[i][j][k + 2] = orders[i][j].vertices[k];
                        }
                    }
                }

                foreach (var vertex in verticesUsed)
                {
                    BFS(q, vertex);
                }
                foreach (var vertex in verticesUsed)
                {
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        distancesCache[i][vertex] = distancesCache[vertex][i];
                    }
                }

                // For each order, calculate one shortest route.
                foreach (var agent in orderVertices)
                {
                    foreach (var order in agent)
                    {
                        var vertices = order;

                        // Cache distance between each pair or items.
                        for (int j = 0; j < vertices.Length; j++)
                        {
                            for (int k = 0; k < vertices.Length; k++)
                            {
                                if (j < k)
                                {
                                    if (routesCache[vertices[j]][vertices[k]] != null) continue;
                                    var (distance, route) = AStar(vertices[j], vertices[k], null, 0, false);
                                    routesCache[vertices[j]][vertices[k]] = route;
                                    var reverseRoute = new int[route.Length];
                                    for (int i = 0; i < route.Length; i++)
                                    {
                                        reverseRoute[route.Length - i - 1] = route[i];
                                    }
                                    routesCache[vertices[k]][vertices[j]] = reverseRoute;
                                }
                            }
                        }
                    }
                }
            }
        }

        public int GetPickTime(int vertex, int position, int height)
        {
            /*
            var sVertex = vertices[vertex] as StorageVertex;
            if (position == 0) {
                return sVertex.itemsLeft[height, 1];
            }
            else {
                return sVertex.itemsRight[height, 1];
            }
            */
            return PickFormula(height, 120, 4, 120);
        }

        public Vertex FindLocation(Point coord)
        {
            foreach (var vertex in vertices)
            {
                if (vertex.coordinates == coord)
                    return vertex;
            }
            return null;
        }

        private int PickFormula(int height, int defaultTime, int timePerLevel, int locationSecureTime)
        {
            if (height == 0)
                return defaultTime;
            else if (height < 3)
                return timePerLevel * height + defaultTime;
            else
            {
                return timePerLevel * height + defaultTime + locationSecureTime;
            }
        }

        /// <summary>
        /// Finds shortest route between two picking locations.
        /// </summary>
        /// <param name="pickVertex">Pick location x.</param>
        /// <param name="target">Pick location y.</param>
        /// <param name="realTime">Time at the source vertex.</param>
        /// <param name="constraints"></param>
        /// <param name="reverseSearch">Find backwards route if true.</param>
        /// <returns></returns>
        public (int Length, int[] route) ShortestRoute(int pickVertex, int target, int pickTime, int realTime, SortedList<int, List<int>> constraints,
            bool reverseSearch, bool returnPath)
        {
            (int totalTime, int[] vertices) route = (distancesCache[pickVertex][target] + pickTime, routesCache[pickVertex][target]);
            int maxTime = reverseSearch ? realTime : realTime + route.totalTime;
            int minTime = reverseSearch ? realTime - route.totalTime : realTime;

            if (constraints != null && minTime >= 0)
            {
                foreach (var time in constraints.Keys)
                {
                    if (time >= minTime && time <= maxTime)
                    {
                        int relativeTime = time - minTime;
                        for (int i = 0; i < constraints[time].Count; i++)
                        {
                            // Cannot pick at the vertex
                            if (relativeTime <= pickTime && pickVertex == constraints[time][i])
                            {
                                return (0, null);
                            }
                            else if (relativeTime > pickTime && route.vertices[relativeTime - pickTime] == constraints[time][i])
                            {
                                if (reverseSearch)
                                {
                                    route = AStar(pickVertex, target, constraints, realTime, reverseSearch);
                                }
                                else
                                {
                                    route = AStar(pickVertex, target, constraints, realTime + pickTime, reverseSearch);
                                }
                                route.totalTime += pickTime;
                                if (returnPath)
                                    return route;
                                else
                                    return (route.totalTime, null);
                            }
                        }
                    }
                }
            }
            if (!returnPath)
            {
                return (route.totalTime, null);
            }
            else
            {
                return route;
            }
        }

        public (int, int[]) AStar(int x, int y, SortedList<int, List<int>> constraints, int beginTime, bool reverseSearch)
        {
            if (reverseSearch)
            {
                var tmp = x;
                x = y;
                y = tmp;
            }
            var queue = aStarQueue;
            EnqueueNode(x, 0, 0, null);

            while (queue.Count != 0)
            {
                var currNode = queue.Dequeue();
                queueCacheA[currNode.index] = null;
                var currRouteCost = currNode.routeCost;

                // If goal node.
                if (currNode.index == y)
                {
                    var result = new int[currNode.routeCost + 1];
                    result[currNode.routeCost] = currNode.index;
                    for (int i = currNode.routeCost; i > 0; i--)
                    {
                        currNode = currNode.predecessor;
                        result[i - 1] = currNode.index;
                    }
                    if (reverseSearch)
                        ReverseRoute(result);
                    CleanUpQueueCache();
                    return (currRouteCost, result);
                }

                int heuristicCost;
                int neighborRouteCost = currNode.routeCost + 1;
                int targetOffsetTime = 0;

                // Stay at node for another time step, if not constrained
                //TODO: Only before a blocked vertex?
                if (constraints != null)
                {
                    targetOffsetTime = reverseSearch == false ? beginTime + neighborRouteCost : beginTime - neighborRouteCost;
                    if (constraints.ContainsKey(targetOffsetTime))
                    {
                        foreach (var constraint in constraints[targetOffsetTime])
                        {
                            if (constraint == currNode.index)
                                goto Neighbors;
                        }
                    }
                    heuristicCost = neighborRouteCost + distancesCache[currNode.index][y];
                    EnqueueNode(currNode.index, neighborRouteCost, heuristicCost, currNode);
                }

                Neighbors:
                // Add all neighbors into queue, if edge is not constrained.
                foreach (var neighbor in vertices[currNode.index].edges)
                {
                    int neighborIdx = neighbor.x == currNode.index ? neighbor.y : neighbor.x;

                    // No return edges.
                    if (currNode.predecessor != null)
                    {
                        if (neighborIdx == currNode.predecessor.index)
                        {
                            continue;
                        }
                        else if (currNode.predecessor.index == currNode.index)
                        {
                            var tmp = currNode.predecessor;
                            while (tmp.predecessor != null && tmp.index == currNode.index)
                            {
                                tmp = tmp.predecessor;
                            }
                            if (tmp.index == neighborIdx)
                                continue;
                        }
                    }

                    heuristicCost = neighborRouteCost + distancesCache[neighborIdx][y];

                    // Check if constraints are not violated.
                    if (constraints != null && constraints.ContainsKey(targetOffsetTime))
                    {
                        foreach (var constrainedVertexIdx in constraints[targetOffsetTime])
                        {
                            if (constrainedVertexIdx == neighborIdx)
                                goto SkipEnqueue;
                        }
                    }
                    EnqueueNode(neighborIdx, neighborRouteCost, heuristicCost, currNode);
                    SkipEnqueue:
                    ;
                }
            }
            throw new ArgumentException("Shortest path not found, check the arguments.");

            void EnqueueNode(int nodeIndex, int routeCost, int heuristicCost, AStarNode prevNode)
            {
                AStarNode nextNode = queueCacheA[nodeIndex];

                // newCost invariants - route with longer optimal length will always have higher newCost
                //                    - of routes with the same optimal cost the one furthest from beginnig is preferred
                float newCost = (heuristicCost << 10) - routeCost;

                if (nextNode != null)
                {               // update value if better cost and node is not requeued already
                    if (nextNode.routeCost > routeCost && nextNode.index != nextNode.predecessor.index)
                    {
                        nextNode.routeCost = routeCost;
                        nextNode.predecessor = prevNode;
                        queue.UpdatePriority(nextNode, newCost);
                    }
                }
                else
                {                                // first visiting the vertex or requeue the current vertex
                    nextNode = nodeFactory.GetNode(nodeIndex, routeCost, prevNode);
                    queueCacheA[nodeIndex] = nextNode;
                    queue.Enqueue(nextNode, newCost);
                }
            }

            void CleanUpQueueCache()
            {
                queue.Clear();
                Array.Copy(emptyArr, queueCacheA, queueCacheA.Length);
                nodeFactory.ResetIndex();
            }

            static void ReverseRoute(int[] result)
            {
                int tmp;
                for (int i = 0; i < result.Length / 2; i++)
                {
                    tmp = result[i];
                    result[i] = result[result.Length - 1 - i];
                    result[result.Length - 1 - i] = tmp;
                }
            }
        }

        public class AStarNode : FastPriorityQueueNode
        {
            public int index;
            public int routeCost;
            public AStarNode predecessor;

            public void InitNode(int index, int cost, AStarNode predecessor)
            {
                this.routeCost = cost;
                this.predecessor = predecessor;
                this.index = index;
            }
        }

        public class AStarNodeFactory
        {
            private readonly AStarNode[] cache;
            private int index = 0;

            public AStarNodeFactory(int capacity)
            {
                cache = new AStarNode[capacity];
                for (int i = 0; i < capacity; i++)
                {
                    cache[i] = new AStarNode();
                }
            }

            public AStarNode GetNode(int vertexIndex, int cost, AStarNode predecessor)
            {
                AStarNode node;
                if (index == cache.Length)
                {
                    node = new AStarNode();
                    node.InitNode(vertexIndex, cost, predecessor);
                }
                else
                {
                    node = cache[index++];
                    node.InitNode(vertexIndex, cost, predecessor);
                }
                return node;
            }

            public void ResetIndex()
            {
                index = 0;
            }
        }

        private void BFS(Queue<QueueNode> queue, int source)
        {
            queue.Enqueue(new QueueNode(source, 0));

            while (queue.Count > 0)
            {
                var currNode = queue.Dequeue();
                int neighborDistance = currNode.distance + 1;

                // Add all neighbors into queue, if edge is not constrained.
                foreach (var neighbor in vertices[currNode.vertexId].edges)
                {
                    int neighborIdx = neighbor.x == currNode.vertexId ? neighbor.y : neighbor.x;
                    if (distancesCache[source][neighborIdx] == 0)
                    {
                        distancesCache[source][neighborIdx] = neighborDistance;
                        queue.Enqueue(new QueueNode(neighborIdx, neighborDistance));
                    }
                }
            }
            distancesCache[source][source] = 0;
        }

        private struct QueueNode
        {
            public int vertexId;
            public int distance;

            public QueueNode(int id, int distance)
            {
                this.vertexId = id;
                this.distance = distance;
            }
        }
    }
}