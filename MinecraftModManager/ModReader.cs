using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace MinecraftModLibManager
{
    internal class ModReader
    {
        private readonly List<MinecraftMod> mods;

        public string MinecraftPath { get; set; }

        public string ModDirectory
        {
            get
            {
                return Path.Combine(MinecraftPath, "mods");
            }
        }

        public ModReader()
        {
            MinecraftPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            mods = [];
        }

        public List<MinecraftMod> ReadAllMods()
        {
            mods.Clear();
            MinecraftMod? tmp;
            IEnumerable<string> jars = Directory.GetFiles(ModDirectory).Where(x => x.Contains(".jar"));
            foreach (string jar in jars)
            {
                tmp = ReadMod(jar);
                if (tmp is null) continue;
                mods.Add(tmp);
            }
            return mods;
        }

        public static MinecraftMod? ReadMod(string mod_file)
        {
            string? tr = GetFileFromJar(mod_file, "mods.toml");
            if (tr is null)
            {
                return null;
            }

            string? GetOptionalValue(TomlTable tb, string key)
            {
                if (!tb.ContainsKey(key))
                {
                    return null;
                }
                return tb[key].ToString();
            }

            TomlTable mod_info = Toml.ToModel(tr);
            TomlTableArray modules = (TomlTableArray)mod_info["mods"];
            MinecraftMod mod = new(mod_file, modules[0]["modId"].ToString(),
                modules[0]["displayName"].ToString(), modules[0]["version"].ToString())
            {
                IsDisabled = mod_file.EndsWith(".disabled"),
                Type = ModType.Normal,
                Authors = GetOptionalValue(modules[0], "authors"),
                Description = GetOptionalValue(modules[0], "description")
            };

            for (int i = 1; i < modules.Count; i++)
            {
                TomlTable module_toml = modules[i];
                MinecraftMod module = new(mod_file, module_toml["modId"].ToString(),
                    module_toml["displayName"].ToString(), module_toml["version"].ToString())
                {
                    IsDisabled = false,
                    Type = ModType.Internal,
                    Authors = GetOptionalValue(modules[0], "authors"),
                    Description = GetOptionalValue(module_toml, "description")
                };
                mod.Modules.Add(module);
            }

            if (!mod_info.ContainsKey("dependencies"))
            {
                return mod;
            }

            TomlTable dep_table = (TomlTable)mod_info["dependencies"];
            void ReadDependencies(MinecraftMod l_mod)
            {
                string required_mod_id;
                if (dep_table.ContainsKey(l_mod.ModID))
                {
                    foreach (TomlTable l_item in (TomlTableArray)dep_table[l_mod.ModID])
                    {
                        required_mod_id = l_item["modId"].ToString() ?? throw new Exception("ModId can't be null!");
                        if (required_mod_id is "forge" or "minecraft")
                        {
                            continue;
                        }
                        l_mod.ModDependencies.Add(new MinecraftMod.ModDependency(required_mod_id, (bool)l_item["mandatory"],
                            l_item["versionRange"].ToString() ?? throw new Exception("VersionRange can't be null!")));
                    }
                }
            }

            ReadDependencies(mod);
            for (int i = 0; i < mod.Modules.Count; i++)
            {
                ReadDependencies(mod.Modules[i]);
            }
            return mod;
        }

        private static string? GetFileFromJar(string jarfile, string filename)
        {
            try
            {
                ZipArchive jar = ZipFile.OpenRead(jarfile);
                ZipArchiveEntry? file = jar.Entries.FirstOrDefault(x => x.Name.Equals(filename, StringComparison.InvariantCulture));

                if (file == default)
                {
                    return null;
                }

                string data;
                using (StreamReader sr = new(file.Open(), Encoding.UTF8))
                {
                    data = sr.ReadToEnd();
                }
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
