﻿using System;
using System.IO;
using System.Reflection;
using ComputeSharp.BokehBlur.Processor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;

namespace ComputeSharp.BokehBlur
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine(">> Loading image");
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "city.jpg");
            using Image<Rgb24> image = Image.Load<Rgb24>(path);

            // Apply a series of processors and save the results
            foreach (var effect in new (string Name, IImageProcessor Processor)[]
            {
                ("bokeh", new HlslBokehBlurProcessor(80, 2, 3)),
                ("gaussian", new HlslGaussianBlurProcessor(80))
            })
            {
                Console.WriteLine($">> Applying {effect.Name}");
                using Image<Rgb24> copy = image.Clone();
                copy.Mutate(c => c.ApplyProcessor(effect.Processor));

                Console.WriteLine($">> Saving {effect.Name} to disk");
                string targetPath = Path.Combine(
                    Path.GetRelativePath(Path.GetDirectoryName(path), @"..\..\..\"),
                    $"{Path.GetFileNameWithoutExtension(path)}_{effect.Name}{Path.GetExtension(path)}");
                copy.Save(targetPath);
            }
        }
    }
}
