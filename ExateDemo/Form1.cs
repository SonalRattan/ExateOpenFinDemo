using Openfin.Desktop;
using Openfin.WinForm;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ExateDemo
{
    public partial class Form1 : Form
    {
        private AutoResetEvent _callbackEvent = new AutoResetEvent(false);
        private string _domContent;
        private int _reloadCount = -1;
        public Form1()
        {
            InitializeComponent();

            //Runtime options is how we set up the OpenFin Runtime environment,
            var runtimeOptions = new Openfin.Desktop.RuntimeOptions
            {

                Version = Openfin.Desktop.RuntimeOptions.LoadDefault().Version,
                Arguments = "--v=1 --disable-legacy-window --allow-file-access-from-files",
                RemoteDevToolsPort = 9090,
                EnableRemoteDevTools = true,
                ConfigBasePath = @"C:\tmp",
                RVMOptions = new Openfin.Desktop.RVMOptions
                {
                    NoUI = false,
                    DoNotLaunch = false,
                    DisableAutoUpdates = false
                },
                UUID = "exate-uuid"
            };

            var runtime = Openfin.Desktop.Runtime.GetRuntimeInstance(runtimeOptions);

            //Create the embedded view control.
            var chartEmbeddedView = new EmbeddedView();
            chartEmbeddedView.Dock = DockStyle.Fill;

            //Add the embedded view control to the main panel
            panel1.Controls.Add(chartEmbeddedView);

            //get path of the local html content
            //var fileUri = new Uri(Path.GetFullPath("./form.html"));
            string fileUri = @"http://www.google.co.uk";

            //initialize embeddedview 
            //last parameter to be replaced with the desired website e.g : @"http://www.google.co.uk"
            chartEmbeddedView.Initialize(runtimeOptions, new Openfin.Desktop.ApplicationOptions("exate-demo", "exate-demo-uuid", fileUri.ToString())
            {
                SupressWindowAlerts = true
            });

            //once we connect to the runtime
            chartEmbeddedView.Ready += (sender, e) =>
            {
                var window = (sender as EmbeddedView).OpenfinWindow;
                //hook to the DOMContentLoaded event
                window.DOMContentLoaded += Window_DOMContentLoaded;
                //calling this handler exmplicitly because Ready is called after inital content load
                Window_DOMContentLoaded(window, null);
            };
        }

        //this function will be called always when the content of the window is reloaded
        private void Window_DOMContentLoaded(object sender, EventArgs e)
        {
            var window = sender as Openfin.Desktop.Window;


            //obtain the entire content
            var content = getDomContent(window);

            //Here we can parse the content of DOM represented as string without the outer <html> tags

            //and finally execute JavaScript , script can be loaded from the text file instead of being hardcoded here

            if (_reloadCount++ >= 0)
            {
                var script = @"document.getElementById('counter').textContent = 'Page reloaded : " + _reloadCount + " time(s)'";

                //  script = @"document.getElementsByTagName('html')[0].innerHTML";

                window.executeJavascript(script, OkCallback, ErrorCallback);
            }


        }

        void OkCallback(Ack ack)
        {
            //on executeJavaScript success
        }


        void ErrorCallback(Ack ack)
        {
            //on executeJavaScript error
        }


        //this function will synchronously retrun the content of DOM excluding <html> tags
        private string getDomContent(Openfin.Desktop.Window window)
        {
            window.executeJavascript(@"document.getElementsByTagName('html')[0].innerHTML", AckCallback, NAckCallback);
            if (!_callbackEvent.WaitOne(new TimeSpan(0, 0, 10)))
            {
                //in case of timeout
                MessageBox.Show("Timeout reached while obtaining the DOM content ", "Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return _domContent;
        }

        private void AckCallback(Openfin.Desktop.Ack ack)
        {
            var json = ack.getJsonObject();
            _domContent = DesktopUtils.getJSONString(json, "data");
            _callbackEvent.Set();

        }

        private void NAckCallback(Openfin.Desktop.Ack ack)
        {
            _domContent = null;
            _callbackEvent.Set();
        }


    }
}

