using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Image2BlackWhite
{
    //OPTIMALS FOR MYSQL WORKBENCH
    //<210;!=197;!=152
    //<210;!=197;!=191
    //<210;!=197;!=218

    public class Program
    {
        private static Regex NUMBER_PATTERN = new Regex("\\d+");

        public static void Main(string[] args)
        {
            Console.WriteLine("Enter input path: ");
            string image = Console.ReadLine();

            Console.WriteLine("Enter settings of conversion to black (semicolon-separated rules): ");

            Console.WriteLine("For R: ");
            string rRule = Console.ReadLine();
            ColorCondition[] rPredicates = ParseRule(rRule, color => color.R);

            Console.WriteLine("For G: ");
            string gRule = Console.ReadLine();
            ColorCondition[] gPredicates = ParseRule(gRule, color => color.G);

            Console.WriteLine("For B: ");
            string bRule = Console.ReadLine();
            ColorCondition[] bPredicates = ParseRule(bRule, color => color.B);

            if (rPredicates.Length != gPredicates.Length || rPredicates.Length != bPredicates.Length)
                throw new Exception();

            Predicate<Color>[] strictShouldBeBlack = Enumerable.Range(0, rPredicates.Length)
                .Where(i => rPredicates[i].IsStrict)
                .Select(i => new Predicate<Color>(color => rPredicates[i].Predicate(color) && gPredicates[i].Predicate(color) && bPredicates[i].Predicate(color)))
                .ToArray();

            Predicate<Color>[] shouldBeBlack = Enumerable.Range(0, rPredicates.Length)
                .Where(i => !rPredicates[i].IsStrict)
                .Select(i => new Predicate<Color>(color => rPredicates[i].Predicate(color) && gPredicates[i].Predicate(color) && bPredicates[i].Predicate(color)))
                .ToArray();

            using (Bitmap bmp = new Bitmap(image))
            {
                for(int i = 0; i < bmp.Height; ++i)
                {
                    for(int j = 0; j < bmp.Width; ++j)
                    {
                        Color pixel = bmp.GetPixel(j, i);

                        if(pixel.R == 53 && pixel.G == 61 && pixel.B == 213)
                        {
                            for (int k = 0; k <= 14 && i + k < bmp.Height; ++k)
                                for (int q = 0; q <= 14 && j + q < bmp.Width; ++q)
                                    bmp.SetPixel(j + q, i + k, Color.White);
                        }

                        if (pixel.R == 244 && pixel.G == 223 && pixel.B == 116)
                        {
                            for (int k = 0; k <= 10 && i + k < bmp.Height; ++k)
                                for (int q = -2; q <= 3 && j + q < bmp.Width; ++q)
                                {
                                    Color currentPixel = bmp.GetPixel(j + q, i + k);
                                    bmp.SetPixel(j + q, i + k, currentPixel.R > 200 && currentPixel.G > 200 && currentPixel.B > 200 ? Color.White : Color.Black);
                                }
                        }

                        if (pixel.R == 102 && pixel.G == 102 && pixel.B == 102)
                        {
                            Color[] testPixels = new Color[]
                            {
                                bmp.GetPixel(j + 1, i + 1),
                                bmp.GetPixel(j + 2, i + 2)
                            };

                            if (testPixels.All(testPixel => testPixel.R == 102 && testPixel.G == 102 && testPixel.B == 102))
                            {
                                for (int k = -2; k < 8 && i + k < bmp.Height; ++k)
                                    for (int q = -2; q < 8 && j + q < bmp.Width; ++q)
                                        bmp.SetPixel(j + q, i + k, Color.White);
                            }
                        }

                        bmp.SetPixel(j, i, strictShouldBeBlack.All(pred => pred(pixel)) && shouldBeBlack.Any(pred => pred(pixel)) ? Color.Black : Color.White);
                    }
                }

                int indexOfExtension = image.LastIndexOf('.');
                string name = image.Substring(0, indexOfExtension);
                string extension = image.Substring(indexOfExtension);
                
                if (File.Exists(name + "-modified" + extension))
                {
                    int i = 0;
                    for (; File.Exists(name + "-modified-" + i + extension); ++i) ;

                    bmp.Save(name + "-modified-" + i + extension);
                }
                else
                    bmp.Save(name + "-modified" + extension);
            }
        }

        public class ColorCondition
        {
            public Predicate<Color> Predicate { get; set; }
            public bool IsStrict { get; set; }
        }

        public static ColorCondition[] ParseRule(string ruleSet, Func<Color, byte> signalSelector)
        {
            string[] rules = ruleSet.Split(';');
            List<ColorCondition> conditions = new List<ColorCondition>();

            foreach(string rule in rules)
            {                
                string cleanRule = rule.Trim();
                int number = Convert.ToInt32(NUMBER_PATTERN.Match(cleanRule).Value);

                Predicate<Color> predicate = new Predicate<Color>(color =>
                {
                    byte b = signalSelector(color);

                    if (cleanRule.StartsWith("=x"))
                    {
                        if (cleanRule.Contains("!"))
                        {
                            int forbiddenNumber = Convert.ToInt32(NUMBER_PATTERN.Match(cleanRule).Value);

                            if (b == forbiddenNumber)
                                return false;
                        }

                        return color.R == color.G && color.G == color.B;
                    }

                    if (cleanRule.StartsWith(">="))
                    {
                        if (b < number)
                            return false;
                    }
                    else if (cleanRule.StartsWith(">"))
                    {
                        if (b <= number)
                            return false;
                    }

                    if (cleanRule.StartsWith("<="))
                    {
                        if (b > number)
                            return false;
                    }
                    else if (cleanRule.StartsWith("<"))
                    {
                        if (b >= number)
                            return false;
                    }

                    if (cleanRule.StartsWith("=") && b != number)
                        return false;

                    if (cleanRule.StartsWith("!=") && b == number)
                        return false;

                    if(cleanRule.StartsWith("["))
                    {
                        if (b < number)
                            return false;

                        int numberMax = Convert.ToInt32(NUMBER_PATTERN.Match(cleanRule, cleanRule.IndexOf(",")).Value);

                        if (b > numberMax)
                            return false;
                    }

                    return true;
                });

                conditions.Add(new ColorCondition()
                {
                    Predicate = predicate,
                    IsStrict = cleanRule.StartsWith("!=")
                });
            }

            return conditions.ToArray();
        }
    }
}
