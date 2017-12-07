using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnlySQLEditor
{
    public partial class frmEditor : Form
    {
        public TextEditorControl OutputTextEditor = null;
        public frmEditor()
        {
            InitializeComponent();
        }

        private void frmEditor_Load(object sender, EventArgs e)
        {
            OutputTextEditor = new TextEditorControl();
            OutputTextEditor.Font = new Font("Consolas", 10, FontStyle.Regular);
            OutputTextEditor.Dock = DockStyle.Fill;
            OutputTextEditor.IsReadOnly = true;
            OutputTextEditor.ShowLineNumbers = false;
            OutputTextEditor.ShowHRuler = false;


            OutputTextEditor.SetHighlighting("SQL");

            groupBox1.Controls.Add(OutputTextEditor);
            var consoleWriter = new ConsoleWriter();
            consoleWriter.WriteEvent += ConsoleWriter_WriteEvent;
            consoleWriter.WriteLineEvent += ConsoleWriter_WriteLineEvent;

            Console.SetOut(consoleWriter);            

            CreateEditor();
        }

        private void ConsoleWriter_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(OutputTextEditor.Text))
            {
                OutputTextEditor.Text = e.Value;
            }
            else
            {
                OutputTextEditor.Text += "\r\n" + e.Value;
            }
            ScrollToEnd();
        }

        private void ScrollToEnd()
        {
            OutputTextEditor.ActiveTextAreaControl.TextArea.Caret.Position = new TextLocation(0, OutputTextEditor.Document.TotalNumberOfLines);
            OutputTextEditor.ActiveTextAreaControl.TextArea.ScrollToCaret();
        }

        private void ConsoleWriter_WriteEvent(object sender, ConsoleWriterEventArgs e)
        {
            OutputTextEditor.Text += e.Value;
            ScrollToEnd();
        }

        public TextEditorControl CreateEditor()
        {
            var tab = new TabPage();
          
            var editor = new TextEditorControl
            {
                Dock = DockStyle.Fill
            };

            editor.Font = new Font("Consolas", 10, FontStyle.Regular);                       

            editor.SetHighlighting("SQL");

            tab.Controls.Add(editor);

            tabControl1.TabPages.Add(tab);

            tab.Text = "new" + tabControl1.TabPages.Count + ".osql";


            this.ActiveControl = editor;


            return editor;
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(tabControl1.SelectedIndex >-1)
            {
                var editor = (TextEditorControl)tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0];

                var translate = new OnlySQL.Translate();



                translate.Run(editor.Text, true);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OutputTextEditor.Text = "";
            OutputTextEditor.Refresh();
        }

        private void newFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateEditor();
        }
    }
}
