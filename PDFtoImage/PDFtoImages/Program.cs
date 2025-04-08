using GhostscriptSharp.Settings;
using GhostscriptSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
 
namespace PDFtoImages
{
    internal class Program
    {
        private static string OutputPath = @"D:\Channel\Code Sell\Working\Output";
        private static string InputPath = @"D:\Channel\Code Sell\Working\JBAH-GCL210561F.PDF";

        static void Main(string[] args)
        {
            ImageConverter(InputPath);
        }

        private static void ImageConverter(string inputFilePath)
        {
            try
            {
                int mPageCount = 0;
                if (!string.IsNullOrEmpty(inputFilePath))
                {
                    if (File.Exists(inputFilePath))
                    {
                        FileInfo fileInfo = new FileInfo(inputFilePath);
                        int PageCount = GetPDFPageCount(fileInfo.FullName);
                        Console.WriteLine("Input file name:" + fileInfo.FullName);
                        Console.WriteLine("Number of Pages :" + PageCount);
                        if (fileInfo.Extension.ToLower().Equals(".pdf"))
                        {
                            mPageCount = PageCount;
                            if (PageCount > 0)
                            {
                                Console.WriteLine("Started Converting images [" + DateTime.Now.ToString("hh:mm:ss tt") + " ]");
                                GhostscriptSettings ghostscriptSettings = new GhostscriptSettings();
                                for (int pCount = 1; PageCount >= pCount; pCount++)
                                {
                                    ghostscriptSettings.Device = GhostscriptSharp.Settings.GhostscriptDevices.jpeg;
                                    ghostscriptSettings.Page.Start = pCount;
                                    ghostscriptSettings.Page.End = PageCount;
                                    ghostscriptSettings.Resolution = new System.Drawing.Size(400, 400);
                                    GhostscriptPageSize ghostscriptPageSize = new GhostscriptPageSize();
                                    if (ghostscriptSettings.Size.Manual.Width == 0 && ghostscriptSettings.Size.Manual.Height == 0)
                                    {
                                        ghostscriptPageSize.Native = GhostscriptSharp.Settings.GhostscriptPageSizes.a7;
                                    }
                                    else
                                    {
                                        ghostscriptPageSize.Manual = new Size(ghostscriptSettings.Size.Manual.Width, ghostscriptSettings.Size.Manual.Height);
                                    }
                                    ghostscriptSettings.Size = ghostscriptPageSize;
                                    string inputPathVal = fileInfo.FullName;
                                    string outputPathVal = Path.Combine(OutputPath, "PageNo_" + pCount + "_" + fileInfo.Name.Trim().Replace(fileInfo.Extension, "") + ".jpeg");
                                    GhostscriptSharp.GhostscriptWrapper.GenerateOutput(inputPathVal, outputPathVal, ghostscriptSettings);
                                    Console.WriteLine(outputPathVal);
                                }
                                Console.WriteLine("End Converting images [" + DateTime.Now.ToString("hh:mm:ss tt") + " ]");
                            }

                        }
                    }
                }
                System.Environment.Exit(mPageCount);

            }
            catch (FileNotFoundException fEx)
            {
                Console.WriteLine("__________________________Start_____________________________________"
                    + "Datetime : " + DateTime.Now.ToString()
                    + "Message : " + fEx.Message
                    + Environment.NewLine + "StackTrace :" + fEx.StackTrace
                    + Environment.NewLine + "Source :" + fEx.Source
                    + "__________________________END_____________________________________");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("__________________________Start_____________________________________"
                    + "Datetime : " + DateTime.Now.ToString()
                    + "Message : " + ex.Message
                    + Environment.NewLine + "StackTrace :" + ex.StackTrace
                    + Environment.NewLine + "Source :" + ex.Source
                    + "__________________________END_____________________________________");
                Console.ReadKey();
            }

        }

        private static int GetPDFPageCount(string fullName)
        {
            return Regex.Matches(File.ReadAllText(fullName), @"/Type\s*/Page[^s]").Count;
        }
    }
}
