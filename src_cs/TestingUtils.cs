using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace src_cs
{
    //TODO 弄懂 仓库布局
    //TODO 求解的结果 绘制到布局图中
    internal class TestingUtils
    {
        public delegate Tour[][] SearchAlgorithm(WarehouseInstance instance);

        public static void RunTests(List<TestScenario> tests, int iterations)
        {
            StringBuilder sb = new StringBuilder();
            TextWriter writer = Console.Out;
            Stopwatch stopwatch = new Stopwatch();
            List<TestResult> results = new List<TestResult>();
            sb.Append("TestScenarioID,Iteration,ElapsedTime,SoC,Makespan\n");

            for (int testIdx = 0; testIdx < tests.Count; testIdx++)
            {
                var test = tests[testIdx];
                TestResult result = new TestResult();
                result.AddScenario(test);

                for (int i = 0; i < iterations; i++)
                {
                    //<image url="$(ProjectDir)\DocumentImages\WarehouseInstance_Sample01X.png"/>
                    //Note 重点关注创建仓库实例
                    var instance = InstanceGenerator.GenerateInstance(test.description, 0);
                    // TODO: Generate new items and orders without the need for whole new instance
                    Solver solver = SolverFactory.GetSolver(test.solver, instance);
                    stopwatch.Restart();

                    stopwatch.Start();
                    //<image url="$(ProjectDir)\DocumentImages\WarehouseInstance_Sample01RouteResultX.png"/>
                    var tours = solver.FindTours();
                    stopwatch.Stop();

                    int makespan = Tour.GetMakespan(tours);
                    int sumOfCosts = Tour.GetSumOfCosts(tours);
                    result.AddMeasurement((stopwatch.ElapsedMilliseconds, sumOfCosts, makespan, tours));
                    sb.Append($"{testIdx},{i},{stopwatch.ElapsedMilliseconds},{sumOfCosts},{makespan}\n");
                }

                result.Evaluate();
                results.Add(result);
            }
            Console.ReadKey();
            StreamWriter sw = new StreamWriter("./output.csv");
            sw.Write(sb.ToString());
            sw.Close();

            /*
            void LogResults() {
                writer.WriteLine($"Time elapsed in {i}-th iteration: {stopwatch.ElapsedMilliseconds}");

                 * Console.WriteLine("Agent {0} route has been found in {1}", i, sw.Elapsed);
                Console.WriteLine("   Items {0}\n    classes {1}", instance.agents[i].orders[0].vertices.Length, instance.agents[i].orders[0].classes[^1]);
                Console.WriteLine("   Constraint: {0}", constraints.Count);
            */
        }
    }

    /// <summary>
    /// 测试场景类
    /// </summary>
    public struct TestScenario
    {
        public SolverType solver;
        public InstanceDescription description;

        public TestScenario(SolverType solver, InstanceDescription description)
        {
            this.solver = solver;
            this.description = description;
        }
    }

    public class TestResult
    {
        public TestScenario scenario;
        public long avgTime;
        public int avgSOC;
        public int avgMakespan;
        public List<(long time, int SumOfCosts, int Makespan, Tour[][] sol)> results;

        public TestResult()
        {
            this.results = new List<(long, int, int, Tour[][])>();
        }

        public void AddMeasurement((long, int, int, Tour[][]) measurement)
        {
            results.Add(measurement);
        }

        public void AddScenario(TestScenario scenario)
        {
            this.scenario = scenario;
        }

        public void Evaluate()
        {
            long timeSum = 0;
            int SOCSum = 0;
            int makespanSum = 0;
            foreach (var record in results)
            {
                timeSum += record.time;
                SOCSum += record.SumOfCosts;
                makespanSum += record.Makespan;
            }
            avgTime = timeSum / results.Count;
            avgSOC = SOCSum / results.Count;
            avgMakespan = makespanSum / results.Count;
        }
    }

    public class SolverFactory
    {
        public static Solver GetSolver(SolverType type, WarehouseInstance instance)
        {
            return type switch
            {
                SolverType.CBS => new CBS(instance),
                SolverType.PrioritizedPlanner => new PrioritizedPlanner(instance),
                SolverType.PrioritizedPlannerClassesL => new PrioritizedPlanner(instance, PrioritizedPlanner.Heuristic.ClassesLow),
                SolverType.PrioritizedPlannerClassesH => new PrioritizedPlanner(instance, PrioritizedPlanner.Heuristic.ClassesHigh),
                SolverType.Heuristic => null,
                _ => throw new NotImplementedException("Solver not implemented."),
            };
        }
    }

    /// <summary>
    /// 定义用于解决多智能体路径规划（MAPF）问题的不同求解器类型的枚举。
    /// </summary>
    public enum SolverType
    {
        /// <summary>
        /// 基于冲突的搜索算法，用于解决MAPF问题。
        /// </summary>
        CBS,

        /// <summary>
        /// 优先级规划器，这是一种用于解决MAPF问题的启发式方法。
        /// </summary>
        PrioritizedPlanner,

        /// <summary>
        /// 优先级规划器的一个变体，可能使用了特定的启发式函数或参数设置，适用于处理具有较低优先级的智能体。
        /// </summary>
        PrioritizedPlannerClassesL,

        /// <summary>
        /// 优先级规划器的另一个变体，可能使用了特定的启发式函数或参数设置，适用于处理具有较高优先级的智能体。
        /// </summary>
        PrioritizedPlannerClassesH,

        /// <summary>
        /// 启发式算法，这是一种基于经验和直觉的算法，用于快速找到问题的近似解。
        /// </summary>
        Heuristic
    }
}