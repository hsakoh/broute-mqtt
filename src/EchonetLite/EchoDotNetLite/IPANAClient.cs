using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EchoDotNetLite
{

    public interface IPANAClient
    {
        Task RequestAsync(string address,byte[] request);

        event EventHandler<(string,byte[])> OnEventReceived;
    }
}
