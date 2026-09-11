using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace DBF.Services;

public sealed class MemoryXpsDocument : IDisposable
{
    private readonly MemoryStream _stream;
    private readonly Package      _package;
    private readonly Uri          _packageUri;
    private readonly XpsDocument  _xpsDocument;

    public FixedDocumentSequence Fds { get; }

    public MemoryXpsDocument(byte[] xpsBytes)
    {
        _stream = new MemoryStream(xpsBytes, writable: false);

        _package = Package.Open(
                                 _stream
                               , FileMode.Open
                               , FileAccess.Read);

        string packageName = $"memorystream://{Guid.NewGuid():N}.xps";

        _packageUri = new Uri(packageName);

        PackageStore.AddPackage(
                                 _packageUri
                               , _package);

        _xpsDocument = new XpsDocument(
                                        _package
                                      ,          CompressionOption.Maximum
                                      ,          packageName);

        Fds = _xpsDocument.GetFixedDocumentSequence();
    }

    public void Dispose()
    {
        try
        {
            if (PackageStore.GetPackage(_packageUri) != null)
                PackageStore.RemovePackage(_packageUri);
        }

        catch
        {
            // Ignore cleanup errors
        }

        _xpsDocument.Close();
        _package.Close();
        _stream.Dispose();
    }
}
