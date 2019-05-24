using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Windows.Forms;
using Form = System.Windows.Forms.Form;

namespace ExampleCommands.UI
{
    public partial class FormFamilySymbolSelector : Form
    {

        public FormFamilySymbolSelector(IEnumerable<FamilySymbol> elements)
        {
            InitializeComponent();

            List<ElementToSelect> elementsToSelectFrom = new List<ElementToSelect>();

            foreach (var fs in elements)
            {
                elementsToSelectFrom.Add(new ElementToSelect(fs));
            }


            itemList.DataSource = elementsToSelectFrom;
            itemList.DisplayMember = "DisplayName";


        }

        public FamilySymbol SelectedElement()
        {
            var selectedItem = itemList.SelectedItem as ElementToSelect;
            return selectedItem.AsRvt();
        }


        private void okButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }


        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }





        private class ElementToSelect
        {
            private FamilySymbol _familySymbol;


            public ElementToSelect(FamilySymbol familySymbol)
            {
                _familySymbol = familySymbol;
            }

            public string DisplayName
            {
                get { return string.Format("{0} | {1}", _familySymbol.FamilyName, _familySymbol.Name); }

            }

            public FamilySymbol AsRvt()
            {
                return _familySymbol;
            }

        }
    }
}
