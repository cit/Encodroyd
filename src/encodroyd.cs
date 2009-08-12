// Encodryod - A Video Encoding software for your Android-Phone
// Copyright 2008 Andrea Cimitan <andrea.cimitan@gmail.com>
// Copyright 2009 Florian Adamsky <cit@ccc-r.de>

using System;
using System.Threading;
using System.Text.RegularExpressions;

using Gtk;
using GLib;
using Gdk;

class Encodroyd {

    private static Gtk.TargetEntry [ ] targetTable =
        new TargetEntry [ ] { new TargetEntry("text/uri-list",0,0),};
    private static Gdk.Pixbuf icon;
    private static Gtk.ProgressBar pBar;
    private static Gtk.ListStore listStore;

    private static Gtk.RadioButton highQualityRadio;
    private static Gtk.RadioButton lowQualityRadio;
    
    private static Gtk.Button skipCurrentButton;
    private static Gtk.Label statusLabel;

    private static object lockI = new object ();
    private static System.Diagnostics.Process proc;
    private static bool pulsing;
    
    public static void Main(string[] args) {
        
        Application.Init();

        Gtk.Window w = new Gtk.Window("Encodroyd");
        HBox mainHBox = new HBox();
        HBox dropHBox = new HBox();
        HBox spinVideoHBox = new HBox();
        HBox spinAudioHBox = new HBox();
        HBox statusButtonHBox = new HBox();
        VBox mainVBox = new VBox();
        VBox leftVBox = new VBox();
        VBox rightVBox = new VBox();
        VBox prefVBox = new VBox();
        VBox statusVBox = new VBox();
        MenuBar mb = new MenuBar ();
        //Gtk.Frame dndFrame = new Gtk.Frame();
        Gtk.Frame prefFrame = new Gtk.Frame("Preferences");
        Gtk.Frame statusFrame = new Gtk.Frame("Status");
        icon = new Pixbuf(null, "android.png");
        Gtk.EventBox image = new Gtk.EventBox();
        Gtk.Label dropLabel = new Gtk.Label("Drop videos here\nto convert");

        highQualityRadio = new RadioButton(null, "High Quality (H.264)");
        prefVBox.PackStart(highQualityRadio, true, true, 0);
        lowQualityRadio = new RadioButton(highQualityRadio, "Low Quality (MPEG4)");
        lowQualityRadio.Active = true;
        prefVBox.PackStart(lowQualityRadio, true, true, 0);

        
        statusLabel = new Gtk.Label("Idle");
        pBar = new ProgressBar();
        skipCurrentButton = new Gtk.Button();
        
        Gtk.ScrolledWindow scrollWin = new Gtk.ScrolledWindow();
        Gtk.TreeView tree = new TreeView();
        listStore = new ListStore(typeof (String));
        
        // set properties
        w.Icon = icon;

        // transparence
        //if (w.Screen.RgbaColormap != null)
        //  Gtk.Widget.DefaultColormap = w.Screen.RgbaColormap;
        
        mainHBox.BorderWidth = 6;
        mainHBox.Spacing = 6;
        dropHBox.BorderWidth = 6;
        dropHBox.Spacing = 6;
        spinVideoHBox.BorderWidth = 6;
        spinVideoHBox.Spacing = 6;
        spinAudioHBox.BorderWidth = 6;
        spinAudioHBox.Spacing = 6;
        statusButtonHBox.BorderWidth = 0;
        statusButtonHBox.Spacing = 6;
        leftVBox.BorderWidth = 6;
        leftVBox.Spacing = 6;
        rightVBox.BorderWidth = 6;
        rightVBox.Spacing = 6;
        prefVBox.BorderWidth = 6;
        prefVBox.Spacing = 6;
        statusVBox.BorderWidth = 6;
        statusVBox.Spacing = 6;
        statusLabel.Ellipsize = Pango.EllipsizeMode.Middle;
        scrollWin.ShadowType = ShadowType.In;
        statusLabel.SetSizeRequest(120,-1);
        skipCurrentButton.Sensitive = false;
        skipCurrentButton.Label = "Skip";
        
        // first hbox
        image.Add(new Gtk.Image(icon));
        dropHBox.Add(image);
        dropHBox.Add(dropLabel);
        
        // preferences frame
        prefFrame.Add(prefVBox);
        prefVBox.Add(spinVideoHBox);
        prefVBox.Add(spinAudioHBox);
        
        // status frame
        statusFrame.Add(statusVBox);
        statusVBox.Add(statusButtonHBox);
        statusVBox.Add(pBar);
        statusButtonHBox.Add(statusLabel);
        statusButtonHBox.Add(skipCurrentButton);
        
        // leftvbox
        leftVBox.Add(dropHBox);
        leftVBox.Add(prefFrame);
        leftVBox.Add(statusFrame);
        
        // right
        tree.Model = listStore;
        tree.HeadersVisible = true;
        tree.AppendColumn ("Queue", new CellRendererText (), "text", 0);
        
        // scrolledwindow
        scrollWin.Add(tree);
        
        // rightvbox
        rightVBox.Add(scrollWin);
        rightVBox.SetSizeRequest(200,-1);
        
        // menubar
        Menu fileMenu = new Menu ();
        MenuItem exitItem = new MenuItem("Exit");
        exitItem.Activated += new EventHandler (OnExitItemActivate);
        fileMenu.Append (exitItem);
        MenuItem fileItem = new MenuItem("File");
        fileItem.Submenu = fileMenu;
        mb.Append (fileItem);
        Menu helpMenu = new Menu ();
        MenuItem aboutItem = new MenuItem("About");
        aboutItem.Activated += new EventHandler (OnAboutItemActivate);
        helpMenu.Append (aboutItem);
        MenuItem helpItem = new MenuItem("Help");
        helpItem.Submenu = helpMenu;
        mb.Append (helpItem);
        
        // mainHBox
        mainVBox.Add(mb);
        mainVBox.Add(mainHBox);
        mainHBox.Add(leftVBox);
        mainHBox.Add(rightVBox);
        
        // window
        w.Add(mainVBox);
        w.ShowAll();
        
        // events
        Gtk.Drag.DestSet(dropHBox, DestDefaults.All, targetTable, Gdk.DragAction.Copy);
        dropHBox.DragDataReceived += DataReceived;
        skipCurrentButton.Clicked += new EventHandler(OnSkipOneButtonClicked);
        w.DeleteEvent += OnWindowDelete;
        
        Application.Run();
    }
    
    static void OnSkipOneButtonClicked (object obj, EventArgs args) {

        proc.Kill();

    }
    
    static void DataReceived(object o, DragDataReceivedArgs args) {
        bool success = false;
        string data = System.Text.Encoding.UTF8.GetString(args.SelectionData.Data);
        
        switch(args.Info) {
        case 0:
            string [ ] uriList = Regex.Split(data, "\r\n");
            foreach(string uri in uriList) {

                if(uri.Length > 0) {
                    String filename = uri.Substring(7);
                    filename = System.Uri.UnescapeDataString(filename);
                    Gtk.Application.Invoke(delegate {
                            listStore.AppendValues(filename.Substring(filename.LastIndexOf("/")+1));
                        });
                    Conversion conversion = new Conversion(filename);
                    System.Threading.Thread tConversion = new System.Threading.Thread(conversion.DoConversion);
                    tConversion.Start();
                }
            }
            success = true;
            break;
        }
        
        Gtk.Drag.Finish(args.Context, success, false, args.Time);
    }
    
    static void UpdateLabelText(Gtk.Label label, String newstring) {

        Gtk.Application.Invoke(delegate {
                label.Text = newstring;
            });

    }
    
    static bool PulseProgressbar() {

        if (pulsing) {
            Gtk.Application.Invoke(delegate {
                    pBar.Text = "Converting...";
                    pBar.Pulse();
                });
        }
        else {
            Gtk.Application.Invoke(delegate {
                    pBar.Text = "";
                    pBar.Fraction = 0;
                });
        }
        return pulsing;
    }
    
    static void OnWindowDelete(object o, DeleteEventArgs args) {

        Application.Quit();

    }

    static void OnExitItemActivate(object o, EventArgs args) {

        Application.Quit ();

    }

    static void OnAboutItemActivate(object o, EventArgs args) {
        AboutDialog dialog = new AboutDialog ();
        
        dialog.Logo = icon;
        dialog.ProgramName = "Encodroyd";
        dialog.Version = "0.0.1.0";
        dialog.Comments = "A video encoding software for a Android phone";
        dialog.Copyright = "Copyright 2009 Florian Adamsky";
        dialog.Website = "http://www.cimitan.com/";
        dialog.WebsiteLabel = "Visit Homepage"; 
        
        dialog.Run ();
        dialog.Destroy();
    }

    public class Conversion {

        private String filename;
        
        public Conversion(String filename) {
            this.filename = filename;
        }
        
        public void DoConversion() {

            lock(lockI) {
                TreeIter iter;
                String outputfilename = filename.Substring(filename.LastIndexOf("/")+1);
                String outputpath = filename.Substring(0,filename.LastIndexOf("/"));
                
                pulsing = true;
                GLib.Timeout.Add (100, new GLib.TimeoutHandler (PulseProgressbar));
                UpdateLabelText(statusLabel, outputfilename);
                Gtk.Application.Invoke(delegate {
                        skipCurrentButton.Sensitive = true;
                    });
                
                proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = "ffmpeg";
                
                if (lowQualityRadio.Active)
                    proc.StartInfo.Arguments = " -i " + filename + " -s 480x320 -vcodec mpeg4 -acodec libfaac -ac 1 -ar 16000 -r 13 -ab 32000 -aspect 3:2 '" + outputpath + "/" + outputfilename.Substring(0, outputfilename.LastIndexOf(".")) + "[And-Low].mp4'";
                else if (highQualityRadio.Active)
                    proc.StartInfo.Arguments = " -i \"" + filename + "\" -s 480x320 -b 384k -vcodec libx264 -flags +loop+mv4 -cmp 256 -partitions +parti4x4+parti8x8+partp4x4+partp8x8+partb8x8 -subq 7 -trellis 1 -refs 5 -bf 0 -flags2 +mixed_refs -coder 0 -me_range 16 -g 250 -keyint_min 25 -sc_threshold 40 -i_qfactor 0.71 -qmin 10 -qmax 51 -qdiff 4 -acodec libfaac -r 13 \"" + outputpath + "/" + outputfilename.Substring(0, outputfilename.LastIndexOf(".")) + "[And-High].mp4\"";

                proc.Start();
                proc.WaitForExit();
                
                pulsing = false;
                Gtk.Application.Invoke(delegate {
                        skipCurrentButton.Sensitive = false;
                    });
                UpdateLabelText (statusLabel, "Idle");
                
                listStore.GetIterFirst (out iter);
                listStore.Remove(ref iter);
                // this will wait for the pulsing to finish
                System.Threading.Thread.Sleep (100);
            }
        }
    }
}
