using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExampleCommands.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ExampleCommands.Commands
{
    class CmdAdaptiveFamilyCreate : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            //create point list
            List<XYZ> pointList = new List<XYZ>();
            pointList.Add(new XYZ(0, 0, 0));
            pointList.Add(new XYZ(0, 20, 0));


            //Collect all adaptive component family symbols with 2 placement points
            var collector = new FilteredElementCollector(doc);
            var filter = new ElementClassFilter(typeof(FamilySymbol));

            var adaptiveComponents = from fs in collector.WherePasses(filter).Cast<FamilySymbol>()
                                     where AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(fs.Family) &&
                                           AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(fs.Family) == 2
                                     select fs;


            //Display dialog and prompt for selection
            FormFamilySymbolSelector selector = new FormFamilySymbolSelector(adaptiveComponents);
            selector.ShowDialog();

            if (selector.DialogResult.Equals(DialogResult.OK))
            {

                //get the selected family
                var selectedFamilySymbol = selector.SelectedElement();

                //activate familySymbol
                selectedFamilySymbol.Activate();


                //create family
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Create Family");

                    var familyInstance =
                        AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, selectedFamilySymbol);


                    //adjust placment point locations
                    var placementPoints =
                        AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(familyInstance);


                    for (int i = 0; i < placementPoints.Count; i++)
                    {
                        var p = doc.GetElement(placementPoints[i]) as ReferencePoint;

                        p.Position = pointList[i];

                    }

                    transaction.Commit();
                }
            }

            return Result.Succeeded;
        }
    }



}
