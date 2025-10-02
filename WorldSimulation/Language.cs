using RandomExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldSimulation
{
    public class Language
    {
        static List<string> Vowels = new List<string>() { "a", "e", "i", "o", "u" };
        static List<string> StopsZ = new List<string>() { "b", "d", "g" };
        static List<string> StopsG = new List<string>() { "p", "t", "k" };
        static List<string> SibiliantsZ = new List<string>() { "v", "z", "j" };
        static List<string> SibiliantsG = new List<string>() { "f", "s", "sh", "h" };
        static List<string> Liquids = new List<string>() { "r", "l" };
        static List<string> Nasals = new List<string>() { "m", "n" };
        static List<string> Other = new List<string>() { "y", "w" };

        static List<string> Consonants = StopsZ.Union(StopsG).Union(SibiliantsG).Union(SibiliantsZ).Union(Liquids).Union(Nasals).Union(Other).ToList();

        RandomExt _random;

        public Language(int seed)
        {
            _random = new RandomExt(seed);
        }

        public string GenerateWord()
        {
            int syllables = 5;
            double rndS = _random.NextDouble();
            if ((rndS -= 0.2) < 0)
                syllables = 2;
            else if ((rndS -= 0.3) < 0)
                syllables = 3;
            else if ((rndS -= 0.3) < 0)
                syllables = 4;

            string word = "";
            bool noConsonants = false;

            for (int i = 0; i < syllables; i++)
            {
                int consonants = 0;
                double rnd = _random.NextDouble();
                if (rnd < 0.25)
                    consonants = 2;
                else if (rnd < 0.75)
                    consonants = 1;

                if (i == 0)
                    consonants = Math.Min(1, consonants);

                consonants = consonants == 0 && noConsonants ? 1 : consonants;
                noConsonants = consonants == 0 && !noConsonants ? true : false;

                for (int j = 0; j < consonants; j++)
                {
                    word += _random.NextItem(Consonants);
                }

                word += _random.NextItem(Vowels);
            }

            if (word.Length > 3 && syllables > 1 && _random.NextDouble() < 0.75)
            {
                word = word.Substring(0, word.Length - 1);
            }

            return word;
        }

        public string GenerateName()
        {
            string word = GenerateWord();
            return word.Substring(0, 1).ToUpper() + word.Substring(1, word.Length - 1);
        }

        public static string NameFromWord(string word) => word.Substring(0, 1).ToUpper() + word.Substring(1, word.Length - 1);
    }
}
