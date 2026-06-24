using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace PluginFachadaInteractiva.UI
{
    public class PanelTypeViewModel : INotifyPropertyChanged
    {
        public ElementId SymbolId { get; set; } = ElementId.InvalidElementId;
        public string FamilyName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public double WidthMm { get; set; }

        private double _weight;
        public double Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); }
        }

        private ElementId? _materialId;
        public ElementId? MaterialId
        {
            get => _materialId;
            set { _materialId = value; OnPropertyChanged(); }
        }

        public string DisplayName => $"{FamilyName}: {TypeName} ({WidthMm:F0} mm)";

        public Logic.PanelTypeInfo ToInfo() => new Logic.PanelTypeInfo
        {
            SymbolId = this.SymbolId,
            FamilyName = this.FamilyName,
            TypeName = this.TypeName,
            WidthMm = this.WidthMm
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MaterialItem
    {
        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }
}
