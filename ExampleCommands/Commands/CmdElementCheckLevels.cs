using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace ExampleCommands.Commands
{
    class CmdElementCheckLevels : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            //select element
            Reference selectedRef = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, "Select element:");
            var selectedId = selectedRef.ElementId;

            Element e = doc.GetElement(selectedId);






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

                        //setup a failure handler to handle any warnings
                        FailureHandlingOptions failOpts = transaction.GetFailureHandlingOptions();
                        failOpts.SetFailuresPreprocessor(new WarningSwallower());
                        transaction.SetFailureHandlingOptions(failOpts);


                        wallId = Utils.CreateJoinedWall(doc, wallId, e as FamilyInstance);


                        doc.Regenerate();

                        transaction.Commit();
                    }
                }






                //because document has been modified, re reference the selected element
                Element selectedElement = doc.GetElement(selectedId);
                var solids = new List<Solid>();


                //get solids
                if (selectedElement is FamilyInstance)
                {
                    //any neseted families?
                    var subxIds = (selectedElement as FamilyInstance).GetSubComponentIds();

                    if (subxIds.Count > 0)
                    {
                        foreach (var id in subxIds)
                        {
                            var subelement = doc.GetElement(id);
                            solids.AddRange(Utils.GetAllSolidsFromElement(subelement)); ;
                        }
                    }
                    else
                    {
                        solids.AddRange(Utils.GetAllSolidsFromElement(selectedElement));
                    }
                }
                else
                {
                    solids.AddRange(Utils.GetAllSolidsFromElement(selectedElement));
                }




                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Create spot elevations");


                    //get edges from solid

                    foreach (Solid solid in solids)
                    {
                        foreach (Edge edge in solid.Edges)
                        {
                            var reference = edge.GetEndPointReference(0);

                            var point = edge.AsCurve().GetEndPoint(0);
                            var bendPoint = point.Add(XYZ.Zero);
                            var endPoint = point.Add(XYZ.Zero);

                            doc.Create.NewSpotElevation(doc.ActiveView, reference, point, bendPoint, endPoint, point, true);

                        }
                    }

                    transaction.Commit();
                }



                //Clean up, if a wall was created delete it
                if (wallId != null)
                {
                    using (Transaction transaction = new Transaction(doc))
                    {
                        transaction.Start("Delete wall");

                        doc.Delete(wallId);

                        transaction.Commit();
                    }
                }



                transGroup.Assimilate();
            }
            return Result.Succeeded;



        }


    }


    public class WarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
        {
            IList<FailureMessageAccessor> failures = a.GetFailureMessages();

            foreach (FailureMessageAccessor f in failures)
            {
                if (f.GetSeverity().ToString() == "Warning")
                {
                    a.DeleteWarning(f);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }


}


