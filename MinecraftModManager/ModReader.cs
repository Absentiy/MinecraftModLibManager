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

            TomlTable mod_info = Toml.ToModel(tr);
            TomlTableArray modules = (TomlTableArray)mod_info["mods"];
            string? mod_desc = null;
            if (modules[0].ContainsKey("description"))
            {
                mod_desc = modules[0]["description"].ToString();
            }
            MinecraftMod mod = new(mod_file, mod_desc, modules[0]["modId"].ToString())
            {
                IsDisabled = mod_file.EndsWith(".disabled"),
                Type = ModType.Normal
            };

            for (int i = 1; i < modules.Count; i++)
            {
                TomlTable module_toml = modules[i];
                MinecraftMod module = new(mod_file, module_toml["description"].ToString(), module_toml["modId"].ToString())
                {
                    Type = ModType.Internal
                };
                mod.Modules.Add(module);
            }

            if (!mod_info.ContainsKey("dependencies"))
            {
                return mod;
            }

            TomlTable dep_table = (TomlTable)mod_info["dependencies"];
            string required_mod_id, module_dep_id;
            if (dep_table.ContainsKey(mod.ModID))
            {
                foreach (TomlTable item in (TomlTableArray)dep_table[mod.ModID])
                {
                    required_mod_id = item["modId"].ToString() ?? throw new Exception("ModId can't be null!");
                    if (required_mod_id is "forge" or "minecraft")
                    {
                        continue;
                    }
                    mod.ModDependencies.Add(new MinecraftMod.ModDependency(required_mod_id, (bool)item["mandatory"]));
                }
            }

            for (int i = 0; i < mod.Modules.Count; i++)
            {
                if (dep_table.ContainsKey(mod.Modules[i].ModID))
                {
                    foreach (TomlTable module_deps in (TomlTableArray)dep_table[mod.Modules[i].ModID])
                    {
                        module_dep_id = module_deps["modId"].ToString() ?? throw new Exception("ModId can't be null!");
                        if (module_dep_id is "forge" or "minecraft")
                        {
                            continue;
                        }
                        mod.Modules[i].ModDependencies.Add(new MinecraftMod.ModDependency(module_dep_id, (bool)module_deps["mandatory"]));
                    }
                }
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
