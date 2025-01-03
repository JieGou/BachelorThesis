﻿using System;
using System.Collections.Generic;
using System.Numerics;

namespace src_cs
{
    public class SetOperations
    {
        private static readonly int MAXSIZE = 1024;
        private static readonly int[] shifts = new int[] { 0, 1, 2, 4, 8, 16, 32, 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
        private static readonly ulong[][] masks;
        private readonly int classesCount;
        public readonly int longsUsed;

        public delegate bool IsCompleteDel(ulong[] sets);

        public readonly IsCompleteDel IsComplete;

        static SetOperations()
        {
            masks = new ulong[17][];
            for (int i = 0; i < 17; i++)
            {
                masks[i] = new ulong[MAXSIZE];
            }

            // Init masks periodical under 64 bits
            for (int i = 1; i < 7; i++)
            {
                ulong mask = 0;
                int step = shifts[i];
                for (int j = 0; j < 64; j += 2 * step)
                {
                    mask <<= (step + 1);
                    for (int k = 0; k < step; k++)
                    {
                        mask += 1;
                        if (k < step - 1)
                        {
                            mask <<= 1;
                        }
                    }
                }
                for (int j = 0; j < MAXSIZE; j++)
                {
                    masks[i][j] = mask;
                }
            }

            // Init masks periodical at or over 64 bits
            for (int i = 7; i < shifts.Length; i++)
            {
                ulong mask = ulong.MaxValue;
                int step = shifts[i];
                for (int j = 0; j < MAXSIZE; j += 2 * step)
                {
                    for (int k = 0; k < step; k++)
                    {
                        masks[i][j + k] = mask;
                    }
                }
            }
        }

        public SetOperations(int classesCount)
        {
            this.classesCount = classesCount;
            if (classesCount < 7)
            {
                this.longsUsed = 1;
                this.IsComplete = IsCompleteShort;
            }
            else
            {
                this.longsUsed = 1 << (classesCount - 6);
                this.IsComplete = IsCompleteLong;
            }
        }

        public virtual void Unify(ulong[] setSource, ulong[] setTarget)
        {
            if (longsUsed >= 4)
            {
                for (int i = 0; i < longsUsed; i += 4)
                {
                    Vector<ulong> src = new Vector<ulong>(setSource, i);
                    Vector<ulong> target = new Vector<ulong>(setTarget, i);
                    Vector.BitwiseOr(src, target).CopyTo(setTarget, i);
                }
            }
            else
            {
                for (int i = 0; i < longsUsed; i++)
                {
                    setTarget[i] |= setSource[i];
                }
            }
        }

        public void AddElement(ulong[] sets, int element)
        {
            if (element < 7)
            {
                for (int i = 0; i < longsUsed; i++)
                {
                    sets[i] <<= shifts[element];
                }
            }
            else
            {
                for (int i = longsUsed - 1 - shifts[element]; i >= 0; i--)
                {
                    sets[i + shifts[element]] = sets[i];
                }
                for (int i = 0; i < shifts[element]; i++)
                {
                    sets[i] = 0;
                }
            }
        }

        private bool IsCompleteLong(ulong[] sets)
        {
            ulong last = sets[longsUsed - 1];
            last >>= 63;
            if (last == 1)
                return true;
            return false;
        }

        private bool IsCompleteShort(ulong[] sets)
        {
            ulong value = sets[0];
            int bitIndex = (1 << classesCount) - 1;
            value <<= 63 - bitIndex;
            value >>= 63;
            if (value == 1)
                return true;
            return false;
        }

        public void FilterElement(ulong[] sets, int element)
        {
            for (int i = 0; i < longsUsed; i++)
            {
                sets[i] &= masks[element][i];
            }
        }

        public bool SearchSubset(ulong[] sets, int[] elements)
        {
            int bitIndex = 0;
            int longIndex = 0;
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != 0)
                {
                    if (elements[i] < 7)
                    {
                        bitIndex += 1 << (elements[i] - 1);
                    }
                    else
                    {
                        longIndex += 1 << (elements[i] - 7);
                    }
                }
            }
            ulong theOne = sets[longIndex];
            theOne <<= 63 - bitIndex;
            theOne >>= 63;
            if (theOne == 1) return true;
            return false;
        }

        public void CreateSet(ulong[] emptySet, int initElement)
        {
            if (initElement < 7)
            {
                int bitPosition = 1 << (initElement - 1);
                emptySet[0] = (ulong)1 << bitPosition;
            }
            else
            {
                int firstLongPosition = 1 << (initElement - 7);
                emptySet[firstLongPosition] = 1;
            }
        }
    }

    public sealed class SetSmall : SetOperations
    {
        public SetSmall(int classes) : base(classes)
        {
        }

        public override sealed void Unify(ulong[] setSource, ulong[] setTarget)
        {
            for (int i = 0; i < longsUsed; i++)
            {
                setTarget[i] |= setSource[i];
            }
        }
    }

    public sealed class SetLarge : SetOperations
    {
        public SetLarge(int classes) : base(classes)
        {
        }

        public override sealed void Unify(ulong[] setSource, ulong[] setTarget)
        {
            for (int i = 0; i < longsUsed; i += 4)
            {
                Vector<ulong> src = new Vector<ulong>(setSource, i);
                Vector<ulong> target = new Vector<ulong>(setTarget, i);
                Vector.BitwiseOr(src, target).CopyTo(setTarget, i);
            }
        }
    }

    internal class GTSPSolverFactory
    {
        private GTSPSolver[] solvers;
        private int tMax;

        public GTSPSolverFactory(int tMax)
        {
            this.solvers = new GTSPSolver[16];
            this.tMax = tMax;
        }

        public GTSPSolver GetSolver(int classes)
        {
            if (solvers[classes] == null)
            {
                return new GTSPSolver(classes, 50, tMax);
                // TODO : Solve variable item counts
            }
            else
            {
                solvers[classes].Init();
                return solvers[classes];
            }
        }
    }

    /// <summary>
    /// 解决广义旅行商问题（Generalized Traveling Salesman Problem, GTSP）类
    /// </summary>
    internal sealed class GTSPSolver
    {
        private readonly SetOperations so;
        private (int finishTime, int startTime, int pickVertex, int[] route) bestSol;
        private readonly bool[][] isVisited;
        private readonly ulong[][][] sets;
        private readonly int timeLimit;
        private long[] totalTime;
        private long[] totalLength;
        private int[] totalConstraints;
        private int[] totalLocations;
        private int[] toursFound;

        public GTSPSolver(int maxClasses, int maxItems, int maxTime)
        {
            so = new SetOperations(maxClasses);
            timeLimit = maxTime;

            // Allocate memory for subsets
            sets = new ulong[maxItems][][];
            for (int i = 0; i < maxItems; i++)
            {
                sets[i] = new ulong[timeLimit][];
            }
            for (int i = 0; i < maxItems; i++)
            {
                for (int j = 0; j < timeLimit; j++)
                {
                    sets[i][j] = new ulong[so.longsUsed];
                }
            }

            // Init shortest distances matrix
            this.isVisited = new bool[maxItems][];
            for (int i = 0; i < isVisited.Length; i++)
            {
                isVisited[i] = new bool[timeLimit];
            }

            // Init logging variables
            totalTime = new long[17];
            totalLength = new long[17];
            totalLocations = new int[17];
            totalConstraints = new int[17];
            toursFound = new int[17];
        }

        public void Init()
        {
            bestSol = (int.MaxValue, int.MaxValue, int.MaxValue, new int[0]);
            for (int i = 0; i < isVisited.Length; i++)
                Array.Clear(isVisited[i], 0, isVisited[i].Length);

            for (int i = 0; i < sets.Length; i++)
            {
                for (int j = 0; j < sets[i].Length; j++)
                {
                    Array.Clear(sets[i][j], 0, so.longsUsed);
                }
            }
        }

        public Tour SolveGTSP(Graph graph, List<Constraint> constraints, OrderInstance order, int timeOffset)
        {
            Init();
            var sortedList = new SortedList<int, List<int>>();
            for (int i = 0; i < constraints.Count; i++)
            {
                if (sortedList.ContainsKey(constraints[i].time))
                {
                    sortedList[constraints[i].time].Add(constraints[i].vertex);
                }
                else
                {
                    sortedList.Add(constraints[i].time, new List<int>() { constraints[i].vertex });
                }
            }
            var timer = new System.Diagnostics.Stopwatch();

            timer.Start();
            var result = FindShortestTour(graph, sortedList, order, timeOffset);
            timer.Stop();
            totalTime[order.classes[^1]] += timer.ElapsedMilliseconds;
            toursFound[order.classes[^1]]++;
            totalLength[order.classes[^1]] += result.Length;
            totalConstraints[order.classes[^1]] += constraints.Count;
            totalLocations[order.classes[^1]] += order.vertices.Length;

            return result;
        }

        public Tour FindShortestTour(Graph graph, SortedList<int, List<int>> constraints, OrderInstance order, int timeOffset)
        {
            var startLoc = order.startLoc;
            var targetLoc = order.targetLoc;
            var vertices = order.vertices;
            var classes = order.classes;
            var orderId = order.orderId;
            var pickTimes = order.pickTimes;
            var classesCount = classes[^1];
            SetOperations so;
            if (classesCount < 8)
            {
                so = new SetSmall(classesCount);
            }
            else
            {
                so = new SetLarge(classesCount);
            }

            /*
            ulong copy = 0;
            ulong uni = 0;
            */

            // Init paths from start location.
            for (int i = 0; i < vertices.Length; i++)
            {
                var (time, r) = graph.ShortestRoute(startLoc, vertices[i], 0, timeOffset, constraints, false, false);
                if (time == 0) continue;
                isVisited[i][time] = true;
                sets[i][time][0] = 1;
            }

            int tMax = timeLimit;
            // Find shortest tour
            for (int time = 0; time < timeLimit && time < tMax; time++)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (!isVisited[i][time]) continue;
                    int vertexClass_0 = classes[i];
                    so.FilterElement(sets[i][time], vertexClass_0);
                    so.AddElement(sets[i][time], vertexClass_0);

                    // Check, whether all classes has been visited
                    if (so.IsComplete(sets[i][time]))
                    {
                        var (t, r) = graph.ShortestRoute(vertices[i], targetLoc, pickTimes[i], time + timeOffset, constraints, false, true);
                        if (t == 0) continue;
                        int finishTime = time + t;
                        if (tMax > finishTime)
                        {   // Found the shortest tour so far
                            tMax = finishTime;
                            bestSol = (finishTime, time, i, r);
                        }
                        continue;
                    }

                    for (int j = 0; j < vertices.Length; j++)
                    {
                        int vertexClass_1 = classes[j];
                        if (vertexClass_0 == vertexClass_1) continue;
                        var (pickTime, r) = graph.ShortestRoute(vertices[i], vertices[j], pickTimes[i], time + timeOffset, constraints, false, false);
                        if (pickTime == 0) continue;
                        if (time + pickTime < timeLimit)
                        {
                            isVisited[j][time + pickTime] = true;
                            so.Unify(sets[i][time], sets[j][time + pickTime]); // Note : Happens (itemsInOrder/3) times per array entry on average
                        }
                    }
                }
            }

            // Reverse search the shortest tour.
            var solution = new LinkedList<(int from, int to, int pickTime, int[] route)>();
            int currTime = bestSol.startTime;
            var value = (vertices[bestSol.pickVertex], targetLoc, pickTimes[bestSol.pickVertex], bestSol.route);
            solution.AddFirst(value);

            int lastClass = classes[bestSol.pickVertex];
            int[] classesLeft = new int[classes[^1] + 1];
            for (int i = 1; i < classesLeft.Length; i++)
            {
                if (i != lastClass)
                    classesLeft[i] = i;
            }
            var shortestRoutesBck = new int[vertices.Length];
            for (int i = 0; i < classesLeft.Length - 1; i++)
            {
                var currNode = solution.First.Value;
                var previous = Backtrack(currTime, currNode.from);
                currTime -= previous.route.Length + previous.pickTime - 1;
                solution.AddFirst(previous);
            }

            return new Tour(timeOffset, solution);

            (int from, int to, int pickTime, int[] route) Backtrack(int visitTime, int lastVertex)
            {
                Array.Clear(shortestRoutesBck, 0, shortestRoutesBck.Length);
                for (int i = 0; i < vertices.Length; i++)
                {
                    int vClass = classes[i];
                    //int pickTime = order.pickTimes[i];
                    if (classesLeft[vClass] == 0) continue;
                    var (t, r) = graph.ShortestRoute(vertices[i], lastVertex, pickTimes[i], visitTime + timeOffset, constraints, true, true);
                    if (t == 0) continue;
                    int vTime = visitTime - t;
                    if (vTime < 0) continue;
                    ulong[] originSet = sets[i][vTime];
                    if (so.SearchSubset(originSet, classesLeft))
                    {
                        classesLeft[vClass] = 0;
                        return (vertices[i], lastVertex, pickTimes[i], r);
                    }
                    shortestRoutesBck[i] = t;
                }

                // Find route back to depot.
                bool allZero = true;
                for (int i = 0; i < classesLeft.Length; i++)
                {
                    if (classesLeft[i] > 0)
                    {
                        allZero = false;
                        break;
                    }
                }
                if (allZero)
                {
                    var (t1, r1) = graph.ShortestRoute(startLoc, lastVertex, 0, visitTime + timeOffset, constraints, true, true);
                    if (visitTime - t1 == 0 && t1 != 0)
                        return (startLoc, lastVertex, 0, r1);
                    (t1, r1) = graph.ShortestRoute(startLoc, lastVertex, 0, 0 + timeOffset, constraints, false, true);
#if DEBUG
                    if (visitTime - t1 != 0)
                        throw new Exception("Route beginning time is not correct.");
#endif
                    return (startLoc, lastVertex, 0, r1);
                }

                // If not found vertex at shortest routes, the route must be longer.
                int routeExtension = 1;
                while (routeExtension <= visitTime)
                {
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        // if (shortestRoutesBck[i] == 0) continue;
                        int vClass = classes[i];
                        if (classesLeft[vClass] == 0) continue;
                        int vTime = visitTime - shortestRoutesBck[i] - routeExtension;
                        if (vTime < 0) continue;
                        ulong[] originSet = sets[i][vTime];
                        if (so.SearchSubset(originSet, classesLeft))
                        {
                            var (t, r) = graph.ShortestRoute(vertices[i], lastVertex, pickTimes[i], vTime + timeOffset, constraints, false, true);
                            if (vTime + t == visitTime && t != 0)
                            {
                                classesLeft[vClass] = 0;
                                return (vertices[i], lastVertex, pickTimes[i], r);
                            }
                        }
                    }
                    routeExtension += 1;
                }
                throw new Exception();
            }
        }

        public static void FindMaxValues(OrderInstance[][] orders, out int maxClasses, out int maxItems, out int maxOrders,
                                         out int maxSolverTime)
        {
            maxOrders = 0;
            maxClasses = 0;
            maxItems = 0;
            int maxPickTime = 0;
            foreach (var agent in orders)
            {
                maxOrders = maxOrders < agent.Length ? agent.Length : maxOrders;
                foreach (var order in agent)
                {
                    maxClasses = maxClasses < order.classes[^1] ? order.classes[^1] : maxClasses;
                    maxItems = maxItems < order.vertices.Length ? order.vertices.Length : maxItems;
                    foreach (var item in order.pickTimes)
                    {
                        if (item < maxPickTime)
                            maxPickTime = item;
                    }
                }
            }
            // maxTime must be greater than the timespan of all orders of agent summed
            maxSolverTime = maxClasses * maxItems * (maxPickTime + 50);
        }

        public void PrintStatistic()
        {
            Console.WriteLine("The statistics of GTSP solver:");
            for (int i = 0; i < totalTime.Length; i++)
            {
                if (totalTime[i] != 0)
                {
                    Console.WriteLine($"  Unique item classes: {i}");
                    Console.WriteLine($"  Avg. pick locations: {totalLocations[i] / toursFound[i]}");
                    Console.WriteLine($"  Tours found:         {toursFound[i]}");
                    Console.WriteLine($"  Average tour length: {totalLength[i] / toursFound[i]}");
                    Console.WriteLine($"  Average constraints: {totalConstraints[i] / toursFound[i]}");
                    Console.WriteLine($"  Avg solve time (ms): {totalTime[i] / toursFound[i]}");
                    Console.WriteLine();
                }
            }
        }
    }
}