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

        public void WriteLine(string x)
        {
            if (this.OutputTextEditor.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(WriteLine);
                this.Invoke(d, new object[] { x });
            }
            else
            {
                if (string.IsNullOrWhiteSpace(OutputTextEditor.Text))
                {
                    OutputTextEditor.Text = x;
                }
                else
                {
                    OutputTextEditor.Text += "\r\n" + x;
                }
                ScrollToEnd();
            }
        }

        public void Write(string x)
        {
            if (this.OutputTextEditor.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(Write);
                this.Invoke(d, new object[] { x });
            }
            else
            {
                OutputTextEditor.Text += x;
                ScrollToEnd();
            }
        }

        delegate void StringArgReturningVoidDelegate(string text);
        private void ConsoleWriter_WriteLineEvent(object sender, ConsoleWriterEventArgs e)
        {
            WriteLine(e.Value);            
            
        }

        private void ScrollToEnd()
        {
            OutputTextEditor.ActiveTextAreaControl.TextArea.Caret.Position = new TextLocation(0, OutputTextEditor.Document.TotalNumberOfLines);
            OutputTextEditor.ActiveTextAreaControl.TextArea.ScrollToCaret();

            Application.DoEvents();
        }

        private void ConsoleWriter_WriteEvent(object sender, ConsoleWriterEventArgs e)
        {
            Write(e.Value);            
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

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OutputTextEditor.Text = "";
            OutputTextEditor.Refresh();
        }

        private void newFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateEditor();
        }

        private void runTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex > -1)
            {
                var editor = (TextEditorControl)tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0];

                translate = new OnlySQL.Translate();                
                translate.Run(editor.Text, true);
            }
        }
        OnlySQL.Translate translate = null;
        private void runToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex > -1)
            {
                var editor = (TextEditorControl)tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0];

                translate = new OnlySQL.Translate();                
                translate.Run(editor.Text, false);
            }
        }

        private void runToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var running = translate != null && translate.IsRunning();

            runTestToolStripMenuItem.Enabled = !running;
            runToolStripMenuItem1.Enabled = !running;

            stopToolStripMenuItem.Enabled = running;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            translate.Stop();
        }
    }
}
