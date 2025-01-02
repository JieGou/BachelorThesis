using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace src_cs
{
    /// <summary>
    /// 仓库实例类
    /// </summary>
    public class WarehouseInstance
    {
        public Graph graph;
        public OrderInstance[][] orders;

        public int AgentCount
        { get { return orders.Length; } }

        public WarehouseInstance(Graph graph, OrderInstance[][] orders)
        {
            this.graph = graph;
            this.orders = orders;
            // graph.Initialize(agents);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="grid">网络</param>
        /// <param name="orders">订单锯齿数组</param>
        public WarehouseInstance(Location[,] grid, Order[][] orders)
        {
            // Make graph from grid and storage
            int xAxis = grid.GetLength(0);
            int yAxis = grid.GetLength(1);
            Vertex[,] tmpGrid = new Vertex[xAxis, yAxis];
            //节点计算器
            int ctr = 0;
            for (int i = 0; i < xAxis; i++)
            {
                for (int j = 0; j < yAxis; j++)
                {
                    //(走道)左侧存储货物数组
                    int[] storageLeft = null;
                    //(走道)右侧存储货物数组
                    int[] storageRight = null;

                    if (grid[i, j] is StorageRack)//Graph图不考虑货架区
                    {
                        continue;
                    }
                    else if (grid[i, j] is Floor)
                    {
                        if (i > 0)
                        {
                            var tmp = grid[i - 1, j] as StorageRack;
                            if (tmp != null)
                            {
                                storageLeft = tmp.items;
                            }
                        }
                        if (i < xAxis - 1)
                        {
                            var tmp = grid[i + 1, j] as StorageRack;
                            if (tmp != null)
                            {
                                storageRight = tmp.items;
                            }
                        }
                        if (storageLeft != null || storageRight != null)
                        {
                            var left = ConvertStorage(storageLeft);
                            var right = ConvertStorage(storageRight);
                            tmpGrid[i, j] = new StorageVertex(ctr++, left, right, new System.Drawing.Point(i, j));
                        }
                        else
                        {
                            tmpGrid[i, j] = new Vertex(ctr++, new System.Drawing.Point(i, j));
                        }
                    }
                    else if (grid[i, j] is StagingArea)
                    {
                        tmpGrid[i, j] = new StagingVertex(ctr++, new System.Drawing.Point(i, j));
                    }
                }
            }
            this.graph = new Graph(tmpGrid);

            // Make OrderInstances 制作订单实例
            var itemMaster = new Dictionary<int, List<(int, int, int)>>();
            foreach (var vertex in graph.vertices)
            {
                var storageV = vertex as StorageVertex;
                if (storageV == null) continue;

                var storageLeft = storageV.itemsLeft;
                var storageRight = storageV.itemsRight;
                int height = storageLeft == null ? storageRight.GetLength(0) : storageLeft.GetLength(0);

                for (int i = 0; i < height; i++)
                {
                    if (storageLeft != null)
                    {
                        if (!itemMaster.ContainsKey(storageLeft[i, 0]))
                        {
                            itemMaster.Add(storageLeft[i, 0], new List<(int, int, int)> { (storageV.index, 0, i) });
                        }
                        else
                        {
                            itemMaster[storageLeft[i, 0]].Add((storageV.index, 0, i));
                        }
                    }
                    if (storageRight != null)
                    {
                        if (!itemMaster.ContainsKey(storageRight[i, 0]))
                        {
                            itemMaster.Add(storageRight[i, 0], new List<(int, int, int)> { (storageV.index, 1, i) });
                        }
                        else
                        {
                            itemMaster[storageRight[i, 0]].Add((storageV.index, 0, i));
                        }
                    }
                }
            }

            ctr = 0;
            var oInstances = new OrderInstance[orders.Length][];
            for (int i = 0; i < orders.Length; i++)
            {
                oInstances[i] = new OrderInstance[orders[i].Length];
                for (int j = 0; j < orders[i].Length; j++)
                {
                    var orderItems = new List<List<(int, int, int)>>();
                    var order = orders[i][j];
                    for (int k = 0; k < order.items.Length; k++)
                    {
                        orderItems.Add(itemMaster[order.items[k]]);
                    }
                    var from = graph.FindLocation(order.from);
                    var to = graph.FindLocation(order.to);
                    oInstances[i][j] = new OrderInstance(ctr++, orderItems, from.index, to.index, graph);
                }
            }
            this.orders = oInstances;

            // Init the graph data structures
            graph.Initialize(oInstances);

            // Make Orders from Graph and the array
            int[,] ConvertStorage(int[] storage)
            {
                if (storage == null)
                    return null;
                var newStorage = new int[storage.Length, 2];
                for (int i = 0; i < storage.Length; i++)
                {
                    newStorage[i, 0] = storage[i];
                }
                return newStorage;
            }
        }

        public void ModifyItems()
        {
            // Generate new distribution of items in a given warehouse
        }

        public void ModifyOrders()
        {
            // Generate new set of orders
        }
    }

    /// <summary>
    /// 测试实例描述类
    /// </summary>
    public struct InstanceDescription
    {
        /// <summary>
        /// 仓库布局
        /// </summary>
        public WarehouseLayout layout;

        /// <summary>
        /// 存储描述
        /// </summary>
        public StorageDescription storageDescription;

        /// <summary>
        /// 订单
        /// </summary>
        public OrdersDescription ordersDescription;

        /// <summary>
        /// 自测用样例
        /// </summary>
        /// <returns></returns>
        public static InstanceDescription TestXXX()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(5, 3, 3, false);
            id.storageDescription = new StorageDescription(1, true, 48);
            id.ordersDescription = new OrdersDescription(1, 1, 0, 3, 0);
            return id;
        }

        public static InstanceDescription Test1()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(5, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test2()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(8, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test3()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(11, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test4()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(14, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test5()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(17, 3, 0, 6, 0);
            return id;
        }

        public static InstanceDescription Test6()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(3, 3, 0, 7, 0);
            return id;
        }

        public static InstanceDescription Test7()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(3, 3, 0, 8, 0);
            return id;
        }

        public static InstanceDescription Test8()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(3, 3, 0, 9, 0);
            return id;
        }

        public static InstanceDescription Test9()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(3, 3, 0, 10, 0);
            return id;
        }

        public static InstanceDescription Test10()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 200);
            id.ordersDescription = new OrdersDescription(3, 3, 0, 11, 0);
            return id;
        }

        public static InstanceDescription GetTiny()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(5, 3, 5, false);
            id.storageDescription = new StorageDescription(3, true, 30);
            id.ordersDescription = new OrdersDescription(7, 3, 1, 7, 3);
            return id;
        }

        public static InstanceDescription GetSmall()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(10, 3, 10, false);
            id.storageDescription = new StorageDescription(5, true, 500);
            id.ordersDescription = new OrdersDescription(7, 3, 1, 7, 3);
            return id;
        }

        public static InstanceDescription GetMedium()
        {
            var id = new InstanceDescription();
            id.layout = new WarehouseLayout(30, 5, 10, false);
            id.storageDescription = new StorageDescription(5, true, 2000);
            id.ordersDescription = new OrdersDescription(4, 5, 2, 10, 2);
            return id;
        }
    }

    /// <summary>
    /// 仓库布局结构体
    /// </summary>
    public struct WarehouseLayout
    {
        /// <summary>
        /// 仓库中的过道数量。
        /// </summary>
        public int aisles;

        /// <summary>
        /// 仓库中的交叉过道数量。
        /// </summary>
        public int crossAisles;

        /// <summary>
        /// 仓库中每个过道的排数。
        /// </summary>
        public int aisleRows;

        // int aisleWidth = 1;
        // int crossAisleWidth = 2;
        /// <summary>
        /// 指示仓库是否有特殊区域（例如，冷藏区）
        /// </summary>
        public bool specialArea;  // 'fridge area'

        /// <summary>
        /// 使用指定的参数初始化 WarehouseLayout 结构体的新实例。
        /// </summary>
        /// <param name="aisles">仓库中的过道数量。</param>
        /// <param name="crossAisles">仓库中的交叉过道数量。</param>
        /// <param name="aisleRows">仓库中每个过道的排数。</param>
        /// <param name="specialArea">指示仓库是否有特殊区域。</param>
        public WarehouseLayout(int aisles, int crossAisles, int aisleRows, bool specialArea)
        {
            this.aisles = aisles;
            this.crossAisles = crossAisles;
            this.aisleRows = aisleRows;
            this.specialArea = specialArea;
        }
    }

    /// <summary>
    /// 货架存储描述类
    /// </summary>
    public struct StorageDescription
    {
        /// <summary>
        /// 货架层数
        /// </summary>
        public int storageLevels;

        /// <summary>
        /// 是否随机存放
        /// </summary>
        public bool randomizedPlacement;

        /// <summary>
        /// 货物种类数量
        /// </summary>
        public int uniqueItems;

        public StorageDescription(int storageLevels, bool randomizedPlacement, int uniqueItems)
        {
            this.storageLevels = storageLevels;
            this.randomizedPlacement = randomizedPlacement;
            this.uniqueItems = uniqueItems;
        }
    }

    /// <summary>
    /// 订单描述类
    /// </summary>
    public struct OrdersDescription
    {
        public int agents;
        public int ordersPerAgent;
        public int ordersVariance;
        public int itemsPerOrder;
        public int itemsVariance;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="agents">机器人数量</param>
        /// <param name="ordersPerAgent">每个机器人订单数量</param>
        /// <param name="ordersVariance">表示订单数量的方差，即订单数量的波动程度</param>
        /// <param name="itemsPerOrder">每个订单平均包含的物品数量</param>
        /// <param name="itemsVariance">物品数量的方差，即物品数量的波动程度</param>
        public OrdersDescription(int agents, int ordersPerAgent, int ordersVariance,
                                 int itemsPerOrder, int itemsVariance)
        {
            this.agents = agents;
            this.ordersPerAgent = ordersPerAgent;
            this.ordersVariance = ordersVariance;
            this.itemsPerOrder = itemsPerOrder;
            this.itemsVariance = itemsVariance;
        }
    }
}