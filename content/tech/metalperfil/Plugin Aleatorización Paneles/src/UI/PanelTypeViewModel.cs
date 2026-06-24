using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace PluginAleatorizar.UI
{
    public class PanelTypeViewModel : INotifyPropertyChanged
    {
        public ElementId SymbolId { get; init; } = ElementId.InvalidElementId;
        public string FamilyName { get; init; } = string.Empty;
        public string TypeName { get; init; } = string.Empty;
        public string Name => $"{FamilyName}: {TypeName}";
        public double WidthMm { get; init; }

        private double _weight;
        public double Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); }
        }

        public Logic.PanelTypeInfo ToInfo() => new Logic.PanelTypeInfo
        {
            SymbolId = this.SymbolId,
            FamilyName = this.FamilyName,
            TypeName = this.TypeName,
            WidthMm = this.WidthMm,
            Percentage = this.Weight
        };

        public PanelTypeViewModel() { }

        public PanelTypeViewModel(Logic.PanelTypeInfo info)
        {
            SymbolId = info.SymbolId;
            FamilyName = info.FamilyName;
            TypeName = info.TypeName;
            WidthMm = info.WidthMm;
            Weight = info.Percentage;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
