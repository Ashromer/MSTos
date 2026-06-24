using Autodesk.Revit.DB;

namespace PluginFachadaInteractiva.Logic
{
    public class PanelTypeInfo
    {
        public ElementId SymbolId { get; set; } = ElementId.InvalidElementId;
        public string FamilyName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public double WidthMm { get; set; }
        public double Percentage { get; set; }
    }
}
