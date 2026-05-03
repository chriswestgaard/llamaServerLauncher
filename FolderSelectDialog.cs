using System;
  using System.Windows.Forms;

  namespace LlamaServerLauncher
  {
      public class FolderSelectDialog : Form
      {
          public string SelectedFolder { get; private set; }
          private TextBox txtFolder;
          private Button btnBrowse;

          public FolderSelectDialog(string currentFolder)
          {
              Text = "Select Model Folder";
              Width = 420; Height = 120;
              FormBorderStyle = FormBorderStyle.FixedDialog;
              MaximizeBox = false; MinimizeBox = false;
              StartPosition = FormStartPosition.CenterParent;

              var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
              Controls.Add(p);

              var lbl = new Label { Text = "Folder:", AutoSize = true, Top = 10, Left = 10 };
              p.Controls.Add(lbl);

              txtFolder = new TextBox { Left = 70, Top = 7, Width = 250, Text = currentFolder };
              p.Controls.Add(txtFolder);

              btnBrowse = new Button { Text = "Browse…", Left = 330, Top = 5, Width = 60 };
              btnBrowse.Click += (s, e) => {
                  using var dlg = new FolderBrowserDialog();
                  if (dlg.ShowDialog() == DialogResult.OK)
                      txtFolder.Text = dlg.SelectedPath;
              };
              p.Controls.Add(btnBrowse);

              var ok = new Button { Text = "OK", Left = 260, Top = 50, Width = 80 };
              ok.Click += (s, e) => { SelectedFolder = txtFolder.Text; DialogResult = DialogResult.OK; Close(); };
              p.Controls.Add(ok);

              var cancel = new Button { Text = "Cancel", Left = 350, Top = 50, Width = 80 };
              cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
              p.Controls.Add(cancel);
          }
      }
  }