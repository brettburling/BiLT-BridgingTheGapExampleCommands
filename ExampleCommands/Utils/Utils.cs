using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExampleCommands
{
    public static class Utils
    {
        public static XYZ ConvertStringToXYZ(string stringToConvert)
        {
            var stringArray = stringToConvert.Split(' ');

            //points are written as northing,easting in landxml!!!
            var x = Convert.ToDouble(stringArray.GetValue(1));
            var y = Convert.ToDouble(stringArray.GetValue(0));


            //multiply by 3.28084 to convert to feet
            return new XYZ(x, y, 0).Multiply(3.28084);
        }

        public static XYZ TranslateToInternalCoordinates(Document doc, XYZ pointToTranslate)
        {
            var transform = doc.ActiveProjectLocation.GetTransform();
            return transform.OfPoint(pointToTranslate);
        }

        public static Curve TranslateToInternalCoordinates(Document doc, Curve curveToTranslate)
        {
            var transform = doc.ActiveProjectLocation.GetTransform();
            return curveToTranslate.CreateTransformed(transform);
        }

        public static XYZ GetHighestXYZ(IEnumerable<XYZ> points)
        {
            return (from p in points
                    orderby p.Z descending
                    select p).FirstOrDefault();
        }

        public static XYZ GetLowestXYZ(IEnumerable<XYZ> points)
        {
            return (from p in points
                    orderby p.Z
                    select p).FirstOrDefault();
        }

        public static IEnumerable<XYZ> GetPointProjectedVertically(View3D view, ElementId targetId, XYZ pointOfInterest)
        {
            //Move the point WAY down so that it is guaranteed (well almost) to be below target element
            //Everest is 29000ft so if we use 50000ft should be within typical working range :).
            var projectionBasePoint = pointOfInterest.Subtract(new XYZ(0, 0, 50000));


            //find all points of intersection between the target element and a vertical ray
            var intersector = new ReferenceIntersector(targetId, FindReferenceTarget.Element, view);
            var rwcList = intersector.Find(projectionBasePoint, XYZ.BasisZ);

            return from r in rwcList
                   select r.GetReference().GlobalPoint;
        }

        public static View3D GetFirstView3d(Document doc)
        {
            return new FilteredElementCollector(doc)
                            .OfClass(typeof(View3D))
                            .WhereElementIsNotElementType()
                            .Cast<View3D>()
                            .FirstOrDefault();
        }



        public static ElementId CreateJoinedWall(Document doc, ElementId wallId, FamilyInstance familyInstance)
        {
            //create a line to represent the wall center line
            var line = Line.CreateBound(XYZ.Zero, XYZ.BasisX);

            //find first level
            var collector = new FilteredElementCollector(doc);
            var filter = new ElementClassFilter(typeof(Level));
            var level = collector.WherePasses(filter).FirstElement() as Level;

            //create wall
            var wall = Wall.Create(doc, line, level.Id, false);

            //save the id so we can delete the wall later
            wallId = wall.Id;

            //if the selected element 
            var subIds = familyInstance.GetSubComponentIds();

            if (subIds.Count > 0)
            {
                foreach (var id in subIds)
                {
                    var subelement = doc.GetElement(id);
                    JoinGeometryUtils.JoinGeometry(doc, wall, subelement);
                }
            }
            else
            {
                JoinGeometryUtils.JoinGeometry(doc, wall, familyInstance);
            }
            return wallId;
        }


        public static List<Solid> GetAllSolidsFromElement(Element element)
        {
            //Set up geometry options
            var options = new Options();
            options.ComputeReferences = true;
            options.IncludeNonVisibleObjects = false;

            //get geometry
            var geometry = element.get_Geometry(options);

            return GetAllSolidsFromGeometry(geometry);
        }



        public static List<Solid> GetAllSolidsFromGeometry(GeometryElement geometry)
        {
            //create a list for all the solids found
            var solids = new List<Solid>();

            foreach (var geometryObject in geometry)
            {
                //solid?
                if (geometryObject is Solid)
                {
                    var solid = geometryObject as Solid;
                    solids.Add(solid);
                }

                //geometryInstance?
                if (geometryObject is GeometryInstance)
                {
                    var geometryInstance = geometryObject as GeometryInstance;
                    var nestedGeometryElement = geometryInstance.GetInstanceGeometry();
                    solids.AddRange(GetAllSolidsFromGeometry(nestedGeometryElement));
                }

                //geometryElement?
                if (geometryObject is GeometryElement)
                {
                    var geometryElement = geometryObject as GeometryElement;
                    solids.AddRange(GetAllSolidsFromGeometry(geometryElement));
                }
            }
            return solids;
        }

    }
}

