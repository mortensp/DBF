using System.Windows.Controls;
using System.Windows.Data;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Cells;

namespace DBF.Helpers
{
    public class CustomCaptionSummaryCellRenderer : GridCaptionSummaryCellRenderer
    {

        public override void OnInitializeTemplateElement(DataColumnBase dataColumn, ContentControl uiElement, object dataContext)
        {
            base.OnInitializeTemplateElement(dataColumn, uiElement, dataContext);
        }

        public override void OnInitializeDisplayElement(DataColumnBase dataColumn, GridCaptionSummaryCell uiElement, object dataContext)
        {
            //if (dataContext is Syncfusion.Data.Group group)
            {
                // Find den kolonne, der er grupperet på
                //var groupedColumn = GetGroupedColumn(group);

                // Brug evt. group.Key eller group.Name afhængigt af din datamodel
                var groupName = "Name"; //"group.Name ?? group.Key?.ToString() ?? "";

                // Antal elementer i gruppen
                int itemsCount = 7; // group.ItemsCount;

                // Generér din custom tekst
                string captionText = GetCustomizedCaptionText( "Gruppe", groupName, itemsCount);

                // Sæt teksten på cellen
                uiElement.Content = captionText;
            }
            //else
            //{
            //    base.OnInitializeDisplayElement(dataColumn, uiElement, dataContext);
            //}
        }

        /// <summary>
        /// Method to get the Grouped Column.
        /// </summary>
        private GridColumn GetGroupedColumn(Group group)
        {
            var groupDesc = DataGrid.View.GroupDescriptions[group.Level - 1] as PropertyGroupDescription;
            foreach (var column in DataGrid.Columns)
            {
                if (column.MappingName == groupDesc.PropertyName)
                {
                    return column;
                }
            }
            return null;
        }

        /// <summary>
        /// Method to Customize the CaptionSummaryCell Text.
        /// </summary>
        private string GetCustomizedCaptionText(string columnName, object groupName, int itemsCount)
        {
            //entryText - instead of "Items", the entryText is assigned to Customize the CaptionSummaryCell Text.
            string entryText = string.Empty;

            if (itemsCount < 20)
                entryText = "entries in the Group";
            else if (itemsCount < 40)
                entryText = "elements in the Group";
            else if (itemsCount < 60)
                entryText = "list in the Group";
            else
                entryText = "items in the Group";

            if (groupName.ToString().Equals("1000"))
                groupName = "One Thousand";
            else if (groupName.ToString().Equals("1002"))
                groupName = "Thousand and Two";
            else if (groupName.ToString().Equals("1004"))
                groupName = "Thousand and Four";

            return string.Format("{0}: {1} - {2} {3}", columnName, groupName, itemsCount, entryText);
        }
    }

}