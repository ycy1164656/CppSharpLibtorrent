using System;
using System.IO;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace testCppShap
{
    public class SharpLibtorrent:ILibrary
    {

        private static readonly string PathProject = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../"));
        private static readonly string PathLibTorrentInclude = Path.Combine(PathProject, "libtorrent/include/libtorrent");


        public static void GenerateBindings() => ConsoleDriver.Run(new SharpLibtorrent());


        public void Postprocess(Driver driver, ASTContext ctx)
        {
             
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            
        }

        public void Setup(Driver driver)
        {
            
            var options = driver.Options;
            options.GeneratorKind = GeneratorKind.CSharp;
            options.OutputDir = Path.Combine(Environment.CurrentDirectory, "output");
            options.Verbose = true;
            options.UseHeaderDirectories = true;
            

            var module = options.AddModule("SharpLibtorrent");
            module.LibraryName = "SharpLibtorrent";
            module.OutputNamespace = "SharpLibtorrent";
            module.Headers.AddRange(Directory.EnumerateFiles(PathLibTorrentInclude, "*.hpp"));
            module.Headers.AddRange(Directory.EnumerateFiles(PathLibTorrentInclude, "*.h"));
            module.IncludeDirs.Add(PathLibTorrentInclude);


            foreach (string item in Directory.EnumerateDirectories(PathLibTorrentInclude))
            {
                module.Headers.AddRange(Directory.EnumerateFiles(item, "*.hpp"));
                module.Headers.AddRange(Directory.EnumerateFiles(item, "*.h"));
                module.IncludeDirs.Add(item);
            }


        }

        public void SetupPasses(Driver driver)
        {
             
        }
    }
}
