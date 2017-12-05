using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace OnlySQLEditor
{
    public class SyntaxModeFileProvider : ISyntaxModeFileProvider
    {
        public List<SyntaxMode> _syntaxModes = null;

        public SyntaxModeFileProvider()
        {
            using (var mem = new MemoryStream(Encoding.Default.GetBytes(Properties.Resources.SyntaxModes)))
            {
                _syntaxModes = SyntaxMode.GetSyntaxModes(mem);                
            }
        }

        public ICollection<SyntaxMode> SyntaxModes => _syntaxModes;

        public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
        {
            return new System.Xml.XmlTextReader(new MemoryStream(Properties.Resources.SQL_Mode));
        }

        public void UpdateSyntaxModeList()
        {
            
        }
    }

//    Public Class SyntaxModeFileProvider
//    Implements ISyntaxModeFileProvider

//    Public _syntaxModes As List(Of SyntaxMode) = Nothing

//    Public Sub New()
//        Using MemoryStreamMan As New MemoryStream(Encoding.Default.GetBytes(My.Resources.SyntaxModes))
//            _syntaxModes = SyntaxMode.GetSyntaxModes(MemoryStreamMan)
//        End Using
//    End Sub


//    Public ReadOnly Property SyntaxModes As System.Collections.Generic.ICollection(Of ICSharpCode.TextEditor.Document.SyntaxMode) Implements ISyntaxModeFileProvider.SyntaxModes
//        Get
//            Return _syntaxModes
//        End Get
//    End Property

//    Public Function GetSyntaxModeFile(ByVal syntaxMode As ICSharpCode.TextEditor.Document.SyntaxMode) As System.Xml.XmlTextReader Implements ISyntaxModeFileProvider.GetSyntaxModeFile
//        Return New System.Xml.XmlTextReader(New MemoryStream(My.Resources.SQL_Mode))
//    End Function

//    Public Sub UpdateSyntaxModeList() Implements ISyntaxModeFileProvider.UpdateSyntaxModeList

//    End Sub
//End Class

}
