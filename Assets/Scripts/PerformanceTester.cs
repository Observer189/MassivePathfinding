using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PerformanceTester : MonoBehaviour
{
   public string[] MapFolders;
   public int TestCount;
   public int[] TestClusters;

   private TextMeshProUGUI testText;

   private void Awake()
   {
      testText = GameObject.FindGameObjectWithTag("Testing").GetComponent<TextMeshProUGUI>();
   }

   public IEnumerator TestClusterSizes()
   {
      var testMaps = CollectMapTestData();
      yield return null;
      var testData = new ClusterSizeTestData();
      testData.BuildClustersTime = new Dictionary<int, TimeSpan>();
      testData.CalculateIntegrationFieldTime = new Dictionary<int, TimeSpan>();
      testData.FillClearanceFieldTime = new Dictionary<int, TimeSpan>();
      testData.PathfindTime = new Dictionary<int, TimeSpan>();
      testData.totalClusterCount = new Dictionary<int, long>();

      for (int i = 0; i < testMaps.Count; i++)
      {
         var mapData = testMaps[i];
         for (int j = 0; j < TestClusters.Length; j++)
         {
            
            testText.text = $"Test proceeding... \n" +
                            $"Map {i}/{testMaps.Count} \n" +
                            $"Cluster size: {TestClusters[j]}";
            
            PathfindingMap.Instance.ClusterHeight = TestClusters[j];
            PathfindingMap.Instance.ClusterWidth = TestClusters[j];
            Debug.Log("Start load");
            var loadTime = PathfindingMap.Instance.LoadFromStringsTest(mapData.mapWidth,mapData.mapHeight,mapData.mapData);

            while (!loadTime.IsCompleted)
            {
               yield return null;
            }

            if (testData.BuildClustersTime.TryGetValue(TestClusters[j], out var timeSpan))
            {
               testData.BuildClustersTime[TestClusters[j]] += loadTime.Result.BuildClustersTime;
            }
            else
            {
               testData.BuildClustersTime[TestClusters[j]] = loadTime.Result.BuildClustersTime;
            }
            
            if (testData.CalculateIntegrationFieldTime.TryGetValue(TestClusters[j], out var timeS))
            {
               testData.CalculateIntegrationFieldTime[TestClusters[j]] += loadTime.Result.CalculateIntegrationFieldTime;
            }
            else
            {
               testData.CalculateIntegrationFieldTime[TestClusters[j]] = loadTime.Result.CalculateIntegrationFieldTime;
            }
            
            if (testData.FillClearanceFieldTime.TryGetValue(TestClusters[j], out var ts))
            {
               testData.FillClearanceFieldTime[TestClusters[j]] += loadTime.Result.FillClearanceFieldTime;
            }
            else
            {
               testData.FillClearanceFieldTime[TestClusters[j]] = loadTime.Result.FillClearanceFieldTime;
            }
            
            if (testData.totalClusterCount.TryGetValue(TestClusters[j], out var t))
            {
               testData.totalClusterCount[TestClusters[j]] += PathfindingMap.Instance.Clusters.Length;
            }
            else
            {
               testData.totalClusterCount[TestClusters[j]] = PathfindingMap.Instance.Clusters.Length;
            }
            

            yield return null;
         }
         
         yield return null;
      }

      var str = new StringBuilder("Cluster size, Build time \n");
      foreach (var kv in testData.BuildClustersTime)
      {
         str.AppendLine($"{kv.Key.ToString(CultureInfo.InvariantCulture)}, " +
                        $"{(kv.Value/testData.totalClusterCount[kv.Key]).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}");
      }
      File.WriteAllText(Application.dataPath + "/buildClustersTime.csv",str.ToString());
      str.Clear();
      
      str = new StringBuilder("Cluster size, Calculation time \n");
      foreach (var kv in testData.CalculateIntegrationFieldTime)
      {
         str.AppendLine($"{kv.Key.ToString(CultureInfo.InvariantCulture)}, {(kv.Value/testData.totalClusterCount[kv.Key]).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}");
         Debug.Log((kv.Value/testData.totalClusterCount[kv.Key]).TotalMilliseconds.ToString(CultureInfo.InvariantCulture));
         Debug.Log((kv.Value/testData.totalClusterCount[kv.Key]).Milliseconds.ToString(CultureInfo.InvariantCulture));
      }
      File.WriteAllText(Application.dataPath + "/calculationIntegration.csv",str.ToString());
      str.Clear();
      
      str = new StringBuilder("Cluster size, Fill time \n");
      foreach (var kv in testData.FillClearanceFieldTime)
      {
         str.AppendLine($"{kv.Key.ToString(CultureInfo.InvariantCulture)}, {(kv.Value/testData.totalClusterCount[kv.Key]).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}");
      }
      File.WriteAllText(Application.dataPath + "/fillClearance.csv",str.ToString());
      str.Clear();

   }

   public IEnumerator TestAstar()
   {
      foreach (var map in MapFolders)
      {
         var path = Application.dataPath + "/StreamingAssets/" + map;
         var fileNames = Directory.GetFiles(path,"*.map");
         
         var astarTimeText = new StringBuilder("optimal length, solution length, calculation time,\n");
         var hpaTimeText = new StringBuilder("optimal length, solution length, calculation time,\n");
         
         File.WriteAllText(Application.dataPath + "/astarTime.csv",string.Empty);
         File.WriteAllText(Application.dataPath + "/hpaTime.csv",string.Empty);
         for (int i = 0; i < fileNames.Length; i++)
         {
            //fileNames[i].Replace("\\", "/");
            //Debug.Log(fileNames[i]);

            var ttSw = Stopwatch.StartNew();
            
            var data = File.ReadAllLines(fileNames[i]);
            ttSw.Stop();
            Debug.Log($"File reading took {ttSw.ElapsedMilliseconds} ms");
            
            ttSw.Restart();
            var width = int.Parse(data[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
            var height = int.Parse(data[2].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
    
            data = data.Skip(4).ToArray();
            var tests = ParseTestScenario(fileNames[i]+".scen");
            ttSw.Stop();
            Debug.Log($"Parsing took {ttSw.ElapsedMilliseconds} ms");
            
            ttSw.Restart();
            PathfindingMap.Instance.LoadFromStrings(width,height,data);
            ttSw.Stop();
            Debug.Log($"Setting map up took {ttSw.ElapsedMilliseconds} ms");
            
            

            var sw = new Stopwatch();
            for (int k = 1; k < tests.Count; k++)
            {
               var test = tests[k];
               testText.text = $"Test proceeding... \n" +
                               $"Map {i}/{fileNames.Length} \n" +
                               $"Test {k}/{tests.Count}";
               
               float length = 0;
               sw.Restart();
               for (int j = 0; j < TestCount; j++)
               {
                  ///try
                  {
                     var p = PathfindingMap.Instance.PathfindAstar(test.startPos, test.goalPos);
                     if (p != null)
                     {
                        length = p.totalCost;
                     }
                  }
                  /*catch (Exception e)
                  {
                     Debug.Log($"start pos = {test.startPos}, goal = {test.goalPos}");
                     throw;
                  }*/
               }
               sw.Stop();
               astarTimeText.AppendLine(test.optimalPathLength.ToString(CultureInfo.InvariantCulture) 
                                        + ", "+ length.ToString(CultureInfo.InvariantCulture) + ", " + 
                                        ((double)sw.ElapsedMilliseconds/TestCount).ToString(CultureInfo.InvariantCulture));
               
               sw.Restart();
               for (int j = 0; j < TestCount; j++)
               {
                  try
                  {
                     var p = PathfindingMap.Instance.PathfindHPA(test.startPos, test.goalPos);
                     if (p != null)
                     {
                        length = p.totalCost;
                     }
                  }
                  catch (Exception e)
                  {
                     Debug.Log($"start pos = {test.startPos}, goal = {test.goalPos}");
                     throw;
                  }
               }
               sw.Stop();
               hpaTimeText.AppendLine(test.optimalPathLength.ToString(CultureInfo.InvariantCulture) 
                                      + ", "+ length.ToString(CultureInfo.InvariantCulture) + ", " + 
                                      ((double)sw.ElapsedMilliseconds/TestCount).ToString(CultureInfo.InvariantCulture));

               //yield return null;
            }

            File.AppendAllText(Application.dataPath + "/astarTime.csv",astarTimeText.ToString());
            astarTimeText.Clear();
            File.AppendAllText(Application.dataPath + "/hpaTime.csv",hpaTimeText.ToString());
            hpaTimeText.Clear();

            yield return null;
         }
         
         /*File.WriteAllText(Application.dataPath + "/astarTime.csv",astarTimeText.ToString());
         File.WriteAllText(Application.dataPath + "/hpaTime.csv",hpaTimeText.ToString());*/
         
      } 
   }

   public IEnumerator TestAstar2()
   {
      var testMaps = CollectMapTestData();
      yield return null;
      var testData = new SpeedTestData();
      testData.astarCalculationTime = new Dictionary<int, TimeSpan>();
      testData.hpaCalculationTime = new Dictionary<int, TimeSpan>();
      testData.sampleCounts = new Dictionary<int, long>();
      var sw = new Stopwatch();
      for (int i = 0; i < testMaps.Count; i++)
      {
         var mapData = testMaps[i];
         
         PathfindingMap.Instance.LoadFromStrings(mapData.mapWidth,mapData.mapHeight,mapData.mapData);

         for (int j = 0; j < testMaps[i].tests.Count; j++)
         {
            var test = testMaps[i].tests[j];
            testText.text = $"Test proceeding... \n" +
                            $"Map {i}/{testMaps.Count} \n" +
                            $"Test: {j}/{testMaps[i].tests.Count}";
            var testLength = Mathf.RoundToInt((float)test.optimalPathLength);
            
            sw.Restart();
            PathfindingMap.Instance.PathfindAstar(test.startPos,test.goalPos);
            sw.Stop();

            if (testData.sampleCounts.ContainsKey(testLength))
            {
               testData.sampleCounts[testLength] += 1;
            }
            else
            {
               testData.sampleCounts[testLength] = 1;
            }

            if (testData.astarCalculationTime.ContainsKey(testLength))
            {
               testData.astarCalculationTime[testLength] += sw.Elapsed;
            }
            else
            {
               testData.astarCalculationTime[testLength] = sw.Elapsed;
            }
            
            sw.Restart();
            PathfindingMap.Instance.PathfindHPA(test.startPos,test.goalPos);
            sw.Stop();
            
            if (testData.hpaCalculationTime.ContainsKey(testLength))
            {
               testData.hpaCalculationTime[testLength] += sw.Elapsed;
            }
            else
            {
               testData.hpaCalculationTime[testLength] = sw.Elapsed;
            }

            yield return null;
         }
         
         var str = new StringBuilder("Path length, A* time, HPA* time \n");
         foreach (var kv in testData.sampleCounts)
         {
            str.AppendLine($"{kv.Key.ToString(CultureInfo.InvariantCulture)}, " +
                           $"{(testData.astarCalculationTime[kv.Key]/kv.Value).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}, " +
                           $"{(testData.hpaCalculationTime[kv.Key]/kv.Value).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}");
         }
         File.WriteAllText(Application.dataPath + "/speedTest.csv",str.ToString());
         str.Clear();

         yield return null;
      }
      
   }

   private List<TestCase> ParseTestScenario(string path)
   {
      var strings = File.ReadAllLines(path);
      strings = strings.Skip(1).ToArray();
      var res = new List<TestCase>();
      foreach (var line in strings)
      {
         var values = line.Split(new[]{" ", "\t", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
         var testCase = new TestCase();
         testCase.startPos = new Vector2Int(int.Parse(values[5]), int.Parse(values[4]));
         testCase.goalPos = new Vector2Int(int.Parse(values[7]), int.Parse(values[6]));
         testCase.optimalPathLength = double.Parse(values[8], CultureInfo.InvariantCulture);
         
         res.Add(testCase);
      }

      return res;
   }

   private List<MapTestData> CollectMapTestData()
   {
      var testMaps = new List<MapTestData>();
      foreach (var map in MapFolders)
      {
         var path = Application.dataPath + "/StreamingAssets/" + map;
         var fileNames = Directory.GetFiles(path, "*.map");
         
         for (int i = 1; i < fileNames.Length; i++)
         {
            var data = File.ReadAllLines(fileNames[i]);

            var width = int.Parse(data[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
            var height = int.Parse(data[2].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);

            data = data.Skip(4).ToArray();
            var tests = ParseTestScenario(fileNames[i] + ".scen");
           
            
            testMaps.Add(new MapTestData{mapWidth = width,mapHeight = height,mapData = data,tests = tests});
         }
         
      }

      return testMaps;
   }


   public struct TestCase
   {
      public Vector2Int startPos;
      public Vector2Int goalPos;
      public double optimalPathLength;
   }
   
   public struct LoadTestData
   {
      public TimeSpan BuildClustersTime;
      public TimeSpan CalculateIntegrationFieldTime;
      public TimeSpan FillClearanceFieldTime;
   }
   
   public class MapTestData
   {
      public int mapWidth;
      public int mapHeight;
      public string[] mapData;

      public List<TestCase> tests;

   }

   public class ClusterSizeTestData
   {
      public Dictionary<int, TimeSpan> BuildClustersTime;
      public Dictionary<int, TimeSpan> CalculateIntegrationFieldTime;
      public Dictionary<int, TimeSpan> FillClearanceFieldTime;
      public Dictionary<int, TimeSpan> PathfindTime;
      public int testCount;
      public Dictionary<int, long> totalClusterCount;

   }
   
   public class SpeedTestData
   {
      public Dictionary<int, TimeSpan> astarCalculationTime;
      public Dictionary<int, TimeSpan> hpaCalculationTime;
      public Dictionary<int, long> sampleCounts;
   }
}
