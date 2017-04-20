using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace AbletonTimerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            //Installers.Add(serviceInstallerMain);
            //Installers.Add(serviceProcessInstallerMain);
        }

        private void serviceInstallerMain_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        private void serviceProcessInstallerMain_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        protected override void OnCommitted(System.Collections.IDictionary savedState)
        {
            new ServiceController(serviceInstallerMain.ServiceName).Start();
        }
    }
}
