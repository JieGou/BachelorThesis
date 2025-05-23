﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace src_cs
{
    internal class InstanceGenerator
    {
        /// <summary>
        /// Generates an test instance. The instance is parametrized by warehouse size, number of agents
        /// existence of the special area such as freezer. TBD: More advanced parameters such as item distribution.
        /// </summary>
        /// <param name="aisles"></param>
        /// <param name="crossAisles"></param>
        /// <param name="aisleRows"></param>
        /// <param name="storageLevels"></param>
        /// <param name="items"></param>
        /// <param name="agents"></param>
        /// <param name="specialArea"></param>
        /// <returns></returns>
        public static WarehouseInstance GenerateInstance(InstanceDescription instanceDescriptoon, int seed)
        {
            var rand = new Random(seed);
            var layout = instanceDescriptoon.layout;
            var storageDesc = instanceDescriptoon.storageDescription;
            var orderDesc = instanceDescriptoon.ordersDescription;

            // Generate the grid
            // TODO: Special storage
            //仓库宽
            int width = 3 * layout.aisles - 2;
            //仓库宽
            int height = 2 * layout.crossAisles + (layout.crossAisles - 1) * layout.aisleRows + 6;
            //仓库网格划分
            Location[,] grid = new Location[width, height];

            List<StorageRack> storage = new List<StorageRack>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (isFloor(i, j))
                    {
                        grid[i, j] = new Floor();
                    }
                    else if (isStaging(i, j))
                    {
                        grid[i, j] = new StagingArea();
                    }
                    else
                    {
                        var rack = new StorageRack(storageDesc.storageLevels);
                        grid[i, j] = rack;
                        storage.Add(rack);
                    }
                }
            }

            // Fill the empty shelves randomly, more advanced methods will be implemented
            if (storageDesc.randomizedPlacement)
            {
                int count = storage.Count;
                var itemsList = GenerateRandomItems(storageDesc.uniqueItems, count * storageDesc.storageLevels, rand);
                itemsList = Enumerable.Range(1, count).ToList();//填充1到货架储物格数
                int itemsIdx = 0;
                foreach (var rack in storage)
                {
                    for (int j = 0; j < storageDesc.storageLevels; j++)
                    {
                        rack.items[j] = itemsList[itemsIdx++];
                    }
                }
            }
            else
            {
                // TODO: Zone item placement?
                throw new NotImplementedException("Other than random placement of items not implemented.");
            }

            // Generate orders
            var orders = GenerateRandomOrders(orderDesc.agents, orderDesc.itemsPerOrder, orderDesc.ordersPerAgent,
                                              storageDesc.uniqueItems, orderDesc.ordersVariance, orderDesc.itemsVariance, rand);

            return new WarehouseInstance(grid, orders);

            bool isFloor(int x, int y)
            {
                return x % 3 == 0 ||
                    y == 0 ||
                    y == height - 1 ||
                    (y - 3) % (layout.aisleRows + 2) == 0 ||
                    (y - 3) % (layout.aisleRows + 2) == 1;
            }

            bool isStaging(int x, int y)
            {
                return (y == 1 || y == 2 || y == height - 2 || y == height - 3)
                    && (x != 0 && x != width - 1);
            }
        }

        private static List<int> GenerateRandomItems(int uniqueItems, int itemsTotal, Random rand)
        {
            List<int> itemsList = new List<int>(itemsTotal);
            for (int i = 0; i < uniqueItems; i++)
            {
                itemsList.Add(i);
            }
            for (int i = itemsList.Count; i < itemsTotal; i++)
            {
                itemsList.Add(rand.Next(uniqueItems));
            }
            Shuffle(itemsList, rand);
            return itemsList;
        }

        /// <summary>
        /// 生成订单
        /// </summary>
        /// <param name="agents">机器人数量</param>
        /// <param name="averageItems"></param>
        /// <param name="averageOrders"></param>
        /// <param name="uniqueItems">订单不同的项数</param>
        /// <param name="orderVar"></param>
        /// <param name="itemVar"></param>
        /// <param name="rand"></param>
        /// <returns></returns>
        private static Order[][] GenerateRandomOrders(int agents, int averageItems, int averageOrders, int uniqueItems, int orderVar, int itemVar,
                                       Random rand)
        {
            Order[][] orders = new Order[agents][];
            for (int i = 0; i < agents; i++)
            {
                orders[i] = new Order[rand.Next(averageOrders - orderVar, averageOrders + orderVar + 1)];
            }

            for (int i = 0; i < agents; i++)
            {
                for (int j = 0; j < orders[i].Length; j++)
                {
                    int orderLength = rand.Next(averageItems - itemVar, averageItems + itemVar + 1);
                    int[] items = new int[orderLength];
                    orders[i][j] = new Order(new Point(2 * i, 2), new Point(2 * i, 2), items);
                    for (int k = 0; k < orderLength; k++)
                    {
                        while (true)
                        {
                            int item = rand.Next(uniqueItems);
                            bool itemUsed = false;
                            for (int l = 0; l < k; l++)
                            {
                                if (orders[i][j].items[l] == item)
                                    itemUsed = true;
                            }
                            if (!itemUsed)
                            {
                                orders[i][j].items[k] = item;
                                break;
                            }
                        }
                    }
                }
            }

            return orders;
        }

        private static void Shuffle(List<int> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    /// <summary>
    /// 区域抽象类
    /// </summary>
    public abstract class Location
    {
    }

    /// <summary>
    /// 地面作业区 即通行区
    /// </summary>
    public class Floor : Location
    {
    }

    /// <summary>
    /// 暂存区
    /// </summary>
    public class StagingArea : Location
    {
    }

    /// <summary>
    /// 储物架区
    /// </summary>
    public class StorageRack : Location
    {
        /// <summary>
        /// 货架层数
        /// </summary>
        private int levels;

        /// <summary>
        /// 摆放货物数组
        /// </summary>
        public int[] items;

        public StorageRack(int levels)
        {
            this.levels = levels;
            items = new int[levels];
        }
    }

    public class Order
    {
        public Point from;
        public Point to;
        public int[] items;

        public Order(Point from, Point to, int[] items)
        {
            this.from = from;
            this.to = to;
            this.items = items;
        }
    }
}