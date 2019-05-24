using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using ExampleCommands.UI;
using System.Collections.Generic;

namespace ExampleCommands.Commands
{
    class CmdAdaptiveFamilyProjectTop : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;


            //select adaptive family instance
            var elementid = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new AdaptiveComponentSelectionFilter(), "Select adaptive component to project:").ElementId;
            var familyInstance = doc.GetElement(elementid) as FamilyInstance;


            //select target
            ElementId targetElementId = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, "Select targert element:").ElementId;


            //the reference intersector requires a 3d view. Find first 3d view. 
            //Note: If the 3d view found has elements hidden the projection many not work as expected.
            View3D view3d = Utils.GetFirstView3d(doc);


            //start a transaction and do projection
            using (Transaction transaction = new Transaction(doc))
            {
                transaction.Start("Project adaptive component");

                //get placement points
                var placementPoints = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(familyInstance);


                foreach (ElementId id in placementPoints)
                {
                    //get current location
                    var placementPoint = doc.GetElement(id) as ReferencePoint;
                    var currentLocation = placementPoint.Position;

                    //do projection
                    IEnumerable<XYZ> points = Utils.GetPointProjectedVertically(view3d, targetElementId, currentLocation);

                    //Find highest point
                    var highestPoint = Utils.GetHighestXYZ(points);

                    //move point
                    placementPoint.Position = highestPoint;
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }





}
