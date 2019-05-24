using Autodesk.Revit.DB;
using System.Xml.Serialization;

namespace ExampleCommands.LandXML
{
    [XmlRoot("Curve")]
    public class XmlCurve
    {
        public string Start { get; set; }

        public string End { get; set; }

        public string Center { get; set; }

        [XmlAttribute("rot")]
        public string Rot { get; set; }

        [XmlAttribute("radius")]
        public double Radius { get; set; }

        public Arc AsRvtArc()
        {
            var startPoint = Utils.ConvertStringToXYZ(Start);
            var endPoint = Utils.ConvertStringToXYZ(End);
            var centerPoint = Utils.ConvertStringToXYZ(Center);

            var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);

            var directionS = (startPoint - centerPoint).Normalize();
            var startAngle = directionS.AngleOnPlaneTo(plane.XVec, plane.Normal);

            var directionE = (endPoint - centerPoint).Normalize();
            var endAngle = directionE.AngleOnPlaneTo(plane.XVec, plane.Normal);

            //multiply by 3.28084 to convert to feet
            var radiusInFeet = Radius * 3.28084;

            return Arc.Create(plane, radiusInFeet, startAngle, endAngle);
        }
    }
}
