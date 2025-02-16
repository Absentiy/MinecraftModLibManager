using Microsoft.Win32;
using MinecraftModLibManager;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace MinecraftModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ModReader modReader;
        private List<MinecraftMod> minecraftMods = [];
        private Dictionary<string, ModType> modMap = [];

        public static string DataLocation
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mmm_v1");
            }
        }

        public MainWindow()
        {
            if (!Directory.Exists(DataLocation))
            {
                Directory.CreateDirectory(DataLocation);
            }
            modReader = new ModReader();
            LoadModMap();
            InitializeComponent();
            UpdateModsList();
        }

        #region ManagerCore
        private MinecraftMod? GetMod(string? id)
        {
            return minecraftMods.Find(x => x.ModID == id);
        }

        private void SetSelectedModType(ModType type)
        {
            if (ModTreeView.SelectedItem is not TreeViewItem node) return;
            MinecraftMod? minecraftMod = GetMod(node.Header.ToString());
            if(minecraftMod is null)
            {
                return;
            }
            minecraftMod.Type = modMap[minecraftMod.ModID] = type;
            UpdateView();
        }

        private void DeleteMod(string? id)
        {
            MinecraftMod? mod = GetMod(id);
            if (mod is null || id is null) return;
            if (mod.ModDependenciesCount > 0)
            {
                if (MessageBox.Show("This mod has some libs installed.\n Are you sure you want to remove this mod.", "Warning",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }
            mod.File.Delete();
            modMap.Remove(id);
            minecraftMods.Remove(mod);
        }

        private void SetModEnabled(string? id)
        {
            MinecraftMod? mod = GetMod(id);
            if(mod is null) return;
            mod.IsDisabled = !mod.IsDisabled;
            if (!mod.IsDisabled)
            {
                mod.File.MoveTo(mod.File.FullName.Replace(".disabled", string.Empty));
            }
            else
            {
                mod.File.MoveTo(mod.File.FullName + ".disabled");
            }
        }

        private void LoadModMap()
        {
            modMap = [];

            string file = Path.Combine(DataLocation, "modTypeMap.dat");
            if (!File.Exists(file))
            {
                return;
            }

            //Read file
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Trim('[', ']').Split(',');
                modMap[parts[0].Trim()] = (ModType)Enum.Parse(typeof(ModType), parts[1].Trim());
            }
        }

        private void ApplyModMap()
        {
            for (int i = 0; i < minecraftMods.Count; i++)
            {
                MinecraftMod item = minecraftMods[i];
                if (!modMap.TryGetValue(item.ModID, out ModType value))
                {
                    value = item.Type;
                    modMap[item.ModID] = value;
                }
                item.Type = value;
            }
        }

        private void SaveModMap()
        {
            string file = Path.Combine(DataLocation, "modTypeMap.dat");

            using StreamWriter sw = new(file);
            foreach (var item in modMap)
            {
                sw.WriteLine(item.ToString());
            }
        }

        private void UpdateModsList()
        {
            minecraftMods = modReader.ReadAllMods();
            ApplyModMap();
            UpdateView();
        }

        private void UpdateView()
        {
            ModTreeView.Items.Clear();
            foreach (MinecraftMod mod in minecraftMods)
            {
                TreeViewItem main_mod = AddMod(mod);
                foreach (MinecraftMod module in mod.Modules)
                {
                    AddMod(module, main_mod);
                }
            }
        }

        private TreeViewItem AddMod(MinecraftMod mod, TreeViewItem? parent = null)
        {
            TreeViewItem mod_node = new()
            {
                Tag = mod,
                Header = mod.ModID
            };
            switch (mod.Type)
            {
                case ModType.Library:
                    mod_node.Foreground = Brushes.Blue;
                    break;

                case ModType.Normal:
                    mod_node.Foreground = Brushes.Black;
                    break;

                case ModType.Addon:
                    mod_node.Foreground = Brushes.BlueViolet;
                    break;

                case ModType.Internal:
                    mod_node.Foreground = Brushes.Violet;
                    break;

                default:
                    break;
            }
            if (mod.IsDisabled)
            {
                mod_node.Foreground = Brushes.Gray;
            }
            mod_node.ToolTip = mod.Description?? "No description";

            bool ModExists(MinecraftMod.ModDependency dep)
            {
                foreach (string item in minecraftMods.Select(x => x.ModID))
                {
                    if(item == dep.ModId || !dep.Mandatory)
                    {
                        return true;
                    }
                }
                return false;
            }

            foreach(MinecraftMod.ModDependency mod_dep in mod.ModDependencies)
            {
                mod_node.Items.Add(new TreeViewItem()
                {
                    Header = mod_dep.ModId + (!mod_dep.Mandatory? " (Optional)" : string.Empty),
                    Tag = mod_dep,
                    Foreground = ModExists(mod_dep)? Brushes.Black : Brushes.Red
                });
            }

            if (parent != null)
            {
                parent.Items.Add(mod_node);
            }
            else
            {
                ModTreeView.Items.Add(mod_node);
            }
            return mod_node;
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateModsList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveModMap();
        }

        private void ModTreeView_SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            ModNameTB.Text = ModDescTB.Text = ModOtherTB.Text = "N/A";
            if (ModTreeView.SelectedItem is not TreeViewItem sel_item)
            {
                ModNameTB.Text = $"Name: {ModTreeView.SelectedItem}";
                return;
            }

            if(sel_item.Tag is MinecraftMod.ModDependency mod_dep)
            {
                ModNameTB.Text = $"Name: {mod_dep.ModId}";
                ModDescTB.Text = $"Is mandatory: {mod_dep.Mandatory}\n" +
                    $"Version range: {mod_dep.VersionRange}";
                return;
            }

            MinecraftMod? mod = GetMod(sel_item.Header.ToString());
            if(mod is null)
            {
                // maybe it is a module
                mod = GetMod(((TreeViewItem)sel_item.Parent).Header.ToString());// get parent mod
                if (mod is not null)
                {
                    mod = mod.Modules.FirstOrDefault(x => x.ModID == sel_item.Header.ToString());// get module
                }
                if (mod is null) return;// if not then skip
            }

            ModNameTB.Text = $"Name: {mod}";
            ModDescTB.Text = $"Type: {mod.Type}\nVersion: {mod.Version}\nAuthors: {mod.Authors ?? "None"}\nDescription: {mod.Description ?? "No description."}";
            ModOtherTB.Text = $"Modules: {string.Join(", ", mod.Modules)}\n" +
                $"Dependencies: {string.Join(", ", mod.ModDependencies)}";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            if (ofd.ShowDialog() == true)
            {
                if (string.IsNullOrEmpty(ofd.FileName))
                {
                    return;
                }
                string newfile = Path.Combine(modReader.ModDirectory, Path.GetFileName(ofd.FileName));
                if (File.Exists(newfile)) return;

                File.Copy(ofd.FileName, newfile, false);
                MinecraftMod? mod = ModReader.ReadMod(newfile) ?? throw new Exception("Invalid mod file!");
                minecraftMods.Add(mod);
                modMap[mod.ModID] = mod.Type;
                UpdateView();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            IEnumerable<MinecraftMod> libs = minecraftMods.Where(x => x.Type == ModType.Library);
            foreach (MinecraftMod mod in minecraftMods.Where(x => x.ModDependenciesCount > 0))
            {
                foreach (MinecraftMod lib in libs)
                {
                    if (mod.Requires(lib.ModID, false))
                    {
                        lib.APIUsedOnce = true;
                        continue;
                    }
                }
            }
            LibsFoundLBL.Text = string.Join(", ", libs.Where(x => !x.APIUsedOnce).Select(x => x.ModID));
        }

        private void ToogleMod_Click(object sender, RoutedEventArgs e)
        {
            if (ModTreeView.SelectedItem is not TreeViewItem sel_item)
            {
                return;
            }
            SetModEnabled(sel_item.Header.ToString());
            UpdateView();
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (ModTreeView.SelectedItem is not TreeViewItem sel_item)
            {
                return;
            }
            DeleteMod(sel_item.Header.ToString());
            UpdateView();
        }

        private void MarkAsNormalMod_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedModType(ModType.Normal);
        }

        private void MarkAsAddonMod_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedModType(ModType.Addon);
        }

        private void MarkAsLibraryMod_Click(object sender, RoutedEventArgs e)
        {
            SetSelectedModType(ModType.Library);
        }

    }
}