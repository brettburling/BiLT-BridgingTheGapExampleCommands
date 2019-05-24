using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ExampleCommands.UI;
using System.Collections.Generic;
using System.Linq;

namespace ExampleCommands.Commands
{
    class CmdAdaptiveFamilyHost : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;



            //Collect all adaptive component family symbols with more than 1 placement point
            var collector = new FilteredElementCollector(doc);
            var filter = new ElementClassFilter(typeof(FamilySymbol));

            var adaptiveComponents = from fs in collector.WherePasses(filter).Cast<FamilySymbol>()
                                     where AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(fs.Family) &&
                                           AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(fs.Family) > 1
                                     select fs;


            //Display dialog and prompt for selection
            FormFamilySymbolSelector selector = new FormFamilySymbolSelector(adaptiveComponents);
            selector.ShowDialog();


            //select edge reference
            Reference hostEdge = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Edge, "Select edge:");
            var selectedId = hostEdge.ElementId;

            Element e = doc.GetElement(selectedId);



            //get the selected family
            var selectedFamilySymbol = selector.SelectedElement();


            //activate familySymbol
            selectedFamilySymbol.Activate();

            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {
                transGroup.Start("Place Dimensions");



                //hack join the element to a wall
                ElementId wallId = null;


                if (e is FamilyInstance & JoinGeometryUtils.GetJoinedElements(doc, e).Count < 1)
                {
                    using (Transaction transaction = new Transaction(doc))
                    {
                        transaction.Start("Create wall");

                        //////setup a failure handler to handle any warnings
                        ////FailureHandlingOptions failOpts = transaction.GetFailureHandlingOptions();
                        ////failOpts.SetFailuresPreprocessor(new WarningSwallower());
                        ////transaction.SetFailureHandlingOptions(failOpts);


                        wallId = Utils.CreateJoinedWall(doc, wallId, e as FamilyInstance);


                        doc.Regenerate();

                        transaction.Commit();
                    }
                }






using (Transaction transaction = new Transaction(doc))
{
    transaction.Start("Create Family");

    //get number of placement point
    var numberOfPoints =
        AdaptiveComponentFamilyUtils.GetNumberOfPlacementPoints(selectedFamilySymbol.Family);
    double numberOfSpaces = numberOfPoints - 1;

    //create family
    var familyInstance =
        AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, selectedFamilySymbol);


    //adjust placment point locations
    var placementPoints =
        AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(familyInstance);

    for (int i = 0; i < placementPoints.Count; i++)
    {

        double interval = i / numberOfSpaces;

        var location = new PointLocationOnCurve(PointOnCurveMeasurementType.NormalizedCurveParameter,
            interval, PointOnCurveMeasureFrom.Beginning);

        var pointOnEdge = doc.Application.Create.NewPointOnEdge(hostEdge, location);


        var p = doc.GetElement(placementPoints[i]) as ReferencePoint;

        p.SetPointElementReference(pointOnEdge);

    }



    transaction.Commit();
}

                transGroup.Assimilate();
            }

            return Result.Succeeded;
        }
    }



}
