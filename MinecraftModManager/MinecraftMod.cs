using System.IO;

namespace MinecraftModLibManager
{
    public enum ModType
    {
        Library, Normal, Addon, Internal
    }

    public class MinecraftMod
    {
        public class ModDependency
        {
            public string ModId { get; }
            public bool Mandatory { get; }
            public string VersionRange { get; }
            public ModDependency(string modId, bool required, string version)
            {
                ModId = modId;
                Mandatory = required;
                VersionRange = version;
            }

            public override string ToString()
            {
                return ModId;
            }
        }
        #region ModInternalFields
        public string? Description { get; set; }

        public string ModID { get; }

        public string DisplayName { get; }

        public string Version { get; }

        public string? Authors { get; set; }
        #endregion

        public List<ModDependency> ModDependencies { get; } = [];

        public List<MinecraftMod> Modules { get; } = [];

        public ModType Type { get; set; }

        public bool IsDisabled { get; set; }

        public bool APIUsedOnce { get; set; } = false;

        public FileInfo File { get; }

        public int ModDependenciesCount
        {
            get
            {
                int i = ModDependencies.Count;
                if(Modules.Count > 0)
                {
                    foreach(var m in Modules)
                    {
                        i += m.ModDependencies.Count;
                    }
                }
                return i;
            }
        }

        public MinecraftMod(string file, string? modID, string? displayName, string? version)
        {
            File = new FileInfo(file ?? throw new ArgumentNullException(nameof(file)));
            ModID = modID ?? throw new ArgumentNullException(nameof(modID));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public override string ToString()
        {
            return ModID;
        }

        internal bool Requires(string modID, bool strict)
        {
            foreach (ModDependency item in ModDependencies)
            {
                if (strict)
                {
                    if (item.ModId == modID && item.Mandatory)
                    {
                        return true;
                    }
                }
                else
                {
                    if (item.ModId == modID)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
