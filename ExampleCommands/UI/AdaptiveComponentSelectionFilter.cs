using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace ExampleCommands.UI
{
    public class AdaptiveComponentSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            //try cast element to familyInstance
            var familyInstance = e as FamilyInstance;

            //if cast succeeds, check if it is an adaptive component
            if (familyInstance != null)
                return AdaptiveComponentInstanceUtils.IsAdaptiveComponentInstance(familyInstance);

            //cast failed, therefore not an adaptive component
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
