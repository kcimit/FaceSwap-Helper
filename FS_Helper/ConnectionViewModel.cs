using Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FS_Helper
{
    public class ConnectionViewModel : ViewModelBase
    {
        public ConnectionViewModel()
        {
            uiSynchronizationContext = SynchronizationContext.Current;
        }
    }
}
