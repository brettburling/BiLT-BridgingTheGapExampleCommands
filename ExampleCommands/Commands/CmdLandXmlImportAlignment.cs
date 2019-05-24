using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ExampleCommands.LandXML;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace ExampleCommands.Commands
{
    class CmdLandXmlImportAlignment : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;

            //get the xml file path
            OpenFileDialog pathDialog = new OpenFileDialog();
            pathDialog.Filter = "Land XML Files(*.xml)|*.xml";
            pathDialog.ShowDialog();
            string filePath = pathDialog.FileName;



            //create an xml document
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(filePath);

            //create a collection to strore converted lines
            var curveCollection = new List<Curve>();

            //get first alignment
            var alignmentNodes = xmlDocument.GetElementsByTagName("Alignment").Item(0) as XmlElement;

            //get the horizontal alignment node
            var alignmentHorizontalData = alignmentNodes.GetElementsByTagName("CoordGeom").Item(0);

            //loop through each node convert it, add to collection
            foreach (XmlNode node in alignmentHorizontalData.ChildNodes)
            {
                if (node.Name.ToUpper() == "LINE")
                {
                    //Deserialize line node
                    XmlSerializer serializer = new XmlSerializer(typeof(XmlLine));
                    var line = (XmlLine)serializer.Deserialize(new XmlNodeReader(node));

                    //add to collection
                    curveCollection.Add(line.AsRvtLine());
                }
                else if (node.Name.ToUpper() == "CURVE")
                {
                    //Deserialize curve node
                    XmlSerializer serializer = new XmlSerializer(typeof(XmlCurve));
                    var curve = (XmlCurve)serializer.Deserialize(new XmlNodeReader(node));

                    //add to collection
                    //curveCollection.Add(curve.AsRvtArc());
                }
                else
                {
                    MessageBox.Show(string.Format("Node {0} conversion not implemented!", node.Name));
                }
            }


            //create model lines
            using (Transaction transaction = new Transaction(doc))
            {
                transaction.Start("Create alignment");

                //create a plane to draw the model lines on
                var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                var sketchPlane = SketchPlane.Create(doc, plane);

                //loop through collection create model curve
                foreach (Curve c in curveCollection)
                {
                    //convert curve to internal coordinates
                    var translatedCurve = Utils.TranslateToInternalCoordinates(doc, c);
                    doc.Create.NewModelCurve(translatedCurve, sketchPlane);
                }

                transaction.Commit();
            }

            return Result.Succeeded;
        }
    }












}
