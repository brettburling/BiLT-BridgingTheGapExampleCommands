using Autodesk.Revit.DB;
using System.Xml.Serialization;

namespace ExampleCommands.LandXML
{
    [XmlRoot("Line")]
    public class XmlLine
    {

        public string Start { get; set; }

        public string End { get; set; }

        public Line AsRvtLine()
        {
            var startPoint = Utils.ConvertStringToXYZ(Start);
            var endPoint = Utils.ConvertStringToXYZ(End);

            return Line.CreateBound(startPoint, endPoint);
        }
    }
}
