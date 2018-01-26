using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownGoogleImg
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("键入列表文件地址：");
            var listStr = Console.ReadLine();
            var nameList = new List<string>();
            using (var streamReader = File.OpenText(listStr))
            {
                var str = streamReader.ReadLineAsync().GetAwaiter().GetResult();
                while (!string.IsNullOrEmpty(str))
                {
                    nameList.Add(str.ToLower());
                    str = streamReader.ReadLineAsync().GetAwaiter().GetResult();
                }
            }
            Console.WriteLine("键入保存文件夹地址：");
            var saveFolderStr = Console.ReadLine();
            var outputDirectoryInfo = new DirectoryInfo(saveFolderStr);
            if (!outputDirectoryInfo.Exists)
            {
                outputDirectoryInfo.Create();
            }
            Console.WriteLine("键入分类数量限制：");
            var limitStr = Console.ReadLine();
            int.TryParse(limitStr, out var limit);
            if (limit <= 0)
                limit = 10;
            Console.WriteLine("键入是否创建子文件夹(y/n)：");
            var creatSubStr = Console.ReadLine();
            var creatSub = true;
            if (!string.IsNullOrWhiteSpace(creatSubStr))
            {
                if (creatSubStr.Trim().ToLower() == "y")
                {
                    creatSub = true;
                }
                else
                {
                    creatSub = false;
                }
            }
            var taskList = new List<Task>();
            foreach (var item in nameList)
            {
                var para = new Tuple<string, int, DirectoryInfo,bool>(item, limit, outputDirectoryInfo,creatSub);
                taskList.Add(DownLoadFile(para));
                if (taskList.Count > 5)
                {
                    foreach (var task in taskList)
                    {
                        task.Start();
                    }
                    Task.WaitAll(taskList.ToArray());
                    Console.WriteLine($"执行中{nameList.IndexOf(item)}/{nameList.Count}");
                    taskList.Clear();
                }
            }
            foreach (var task in taskList)
            {
                task.Start();
            }
            Task.WaitAll(taskList.ToArray());
            Console.WriteLine("执行完毕。。。");
        }

        private static Task DownLoadFile(object obj)
        {
            return new Task(start=>
            {
                var para = start as Tuple<string, int, DirectoryInfo, bool>;
                var name = para.Item1;
                var limit = para.Item2;
                var outputDirectoryInfo = para.Item3;
                var creatSubStr = para.Item4;
                var output = outputDirectoryInfo;
                if (creatSubStr)
                {
                    output = outputDirectoryInfo.CreateSubdirectory(name);
                }

                var webClient = new WebClient();
                var index = 0;
                do
                {
                    index++;
                    try
                    {
                        var address = new Uri(@"https://storage.googleapis.com/artlab-public.appspot.com/stencils/selman/"+$"{name}-{index:D2}"+".svg");
                        var array = webClient.DownloadData(address);
                        var fileStream = new FileStream($"{output.FullName}\\{name}-{index:D2}.svg", FileMode.OpenOrCreate, FileAccess.Write);
                        fileStream.Write(array, 0, array.Length);
                        fileStream.Flush();
                        fileStream.Close();
                    }
                    catch (WebException)
                    {
                        webClient.Dispose();
                        webClient = new WebClient();
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(1000);
                        webClient.Dispose();
                        webClient = new WebClient();
                    }
                }
                while (index < limit);
            },obj);
        }
    }
}
