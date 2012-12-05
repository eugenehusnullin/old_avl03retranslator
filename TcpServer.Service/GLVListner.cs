using System.ServiceProcess;

namespace Retranslator
{
    public partial class GLVListner : ServiceBase
    {
        public GLVListner()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            
        }

        protected override void OnStop()
        {
        }
    }
}
