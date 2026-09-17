using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace DBF.Services;

public class PaginatorDocument : IDocumentPaginatorSource
{
    private readonly DocumentPaginator _paginator;

    public PaginatorDocument(DocumentPaginator paginator)
    {
        _paginator = paginator;
    }

    public DocumentPaginator DocumentPaginator => _paginator;

    public FixedDocumentSequence ToFixedDocumentSequence()
    {
        return ConvertToFixedDocument(this) as FixedDocumentSequence;
    }

    public static IDocumentPaginatorSource ConvertToFixedDocument(IDocumentPaginatorSource source)
    {
        var paginator = source.DocumentPaginator;

        var fixedDoc = new FixedDocument();

        for (int i = 0; i <  paginator.PageCount; i++)
        {
            DocumentPage dp = paginator.GetPage(i);

            var fixedPage = new FixedPage
                            {
                                Width  = paginator.PageSize.Width
                              , Height = paginator.PageSize.Height
                            };

            // DocumentPage.Visual is a Visual → we add it as background
            fixedPage.Background = new System.Windows.Media.VisualBrush(dp.Visual);

            var pc = new PageContent();
            ((IAddChild)pc).AddChild(fixedPage);

            fixedDoc.Pages.Add(pc);
        }

        // Wrap FixedDocument in a FixedDocumentSequence
        var fds    = new FixedDocumentSequence();
        var docRef = new DocumentReference();
        docRef.SetDocument(fixedDoc);
        fds.References.Add(docRef);

        return fds;
    }
}
